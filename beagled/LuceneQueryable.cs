//
// LuceneQueryable.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;

using Beagle.Util;

namespace Beagle.Daemon {

	public class LuceneQueryable : IQueryable {
		
		private Scheduler scheduler = Scheduler.Global;

		private string index_name;
		private string index_dir;
		private LuceneDriver driver;
		private IIndexer indexer;
		private LuceneTaskCollector collector;
		private FileAttributesStore fa_store;

		//////////////////////////////////////////////////////////
		
		private Hashtable indexable_info_cache = UriFu.NewHashtable ();
		private class IndexableInfo {
			public Uri      Uri;
			public string   Path;
			public DateTime Mtime;
		}

		internal void CacheIndexableInfo (Indexable indexable)
		{
			if (indexable.IsNonTransient) {
				IndexableInfo info = new IndexableInfo ();
				info.Uri = indexable.Uri;
				info.Path = indexable.ContentUri.LocalPath;
				info.Mtime = FileSystem.GetLastWriteTime (info.Path);
				indexable_info_cache [info.Uri] = info;
			}
		}

		internal void UseCachedIndexableInfo (Uri uri)
		{
			IndexableInfo info = indexable_info_cache [uri] as IndexableInfo;
			if (info != null) {
				this.FileAttributesStore.AttachTimestamp (info.Path, info.Mtime);
				indexable_info_cache.Remove (uri);
			}
		}

		//////////////////////////////////////////////////////////

		public LuceneQueryable (string index_name)
		{
			this.index_name = index_name;
			index_dir = Path.Combine (PathFinder.RootDir, index_name);

			driver = new LuceneDriver (index_dir);

			indexer = driver;
			indexer.ChangedEvent += OnIndexerChanged;

			fa_store = new FileAttributesStore (BuildFileAttributesStore (driver.Fingerprint));

			collector = new LuceneTaskCollector (indexer);
		}

		virtual protected IFileAttributesStore BuildFileAttributesStore (string index_fingerprint)
		{
			return new FileAttributesStore_ExtendedAttribute (index_fingerprint);
		}

		protected string IndexDirectory {
			get { return index_dir; }
		}

		protected LuceneDriver Driver {
			get { return driver; }
		}

		public Scheduler ThisScheduler {
			get { return scheduler; }
		}

		public FileAttributesStore FileAttributesStore {
			get { return fa_store; }
		}

		/////////////////////////////////////////

		private class ChangeData : IQueryableChangeData {
			public ICollection AddedUris;
			public ICollection RemovedUris;
		}

		private void OnIndexerChanged (IIndexer     source,
					       ICollection  list_of_added_uris,
					       ICollection  list_of_removed_uris)
		{
			// Walk across the list of removed Uris and drop them
			// from the text cache.
			foreach (Uri uri in list_of_removed_uris) {
				TextCache.Delete (uri);
			}

			// Walk across the list of added Uris and mark the local
			// files with the cached timestamp.
			foreach (Uri uri in list_of_added_uris)
				UseCachedIndexableInfo (uri);

			// Propagate the event up through the Queryable.
			ChangeData change_data = new ChangeData ();
			change_data.AddedUris = list_of_added_uris;
			change_data.RemovedUris = list_of_removed_uris;
			QueryDriver.QueryableChanged (this, change_data);
		}

		/////////////////////////////////////////

		virtual public void Start ()
		{

		}

		/////////////////////////////////////////

		virtual public bool AcceptQuery (QueryBody body)
		{
			return true;
		}

		/////////////////////////////////////////

		virtual protected bool HitIsValid (Uri uri)
		{
			return true;
		}

		/////////////////////////////////////////

		virtual protected double RelevancyMultiplier (Hit hit)
		{
			return 1.0;
		}

		static protected double HalfLifeMultiplier (DateTime dt, int half_life_days)
		{
			double days = Math.Abs ((DateTime.Now - dt).TotalDays);
			if (days < 0)
				return 1.0f;
			return Math.Pow (0.5, days / (double) half_life_days);
		}

		// FIXME: A decaying half-life is a little sketchy, since data
		// will eventually decay beyond the epsilon and be dropped
		// from the results entirely, which is almost never what we
		// want, particularly in searches with a few number of
		// results.  But with a default half-life of 6 months, it'll
		// take over 13 years to fully decay outside the epsilon on
		// this multiplier alone.
		static protected double HalfLifeMultiplier (DateTime time)
		{
			// Default relevancy half-life is six months.
			return HalfLifeMultiplier (time, 182);
		}

		static protected double HalfLifeMultiplierFromProperty (Hit hit,
									double default_multiplier,
									params object [] properties)
		{
			double best_m = -1.0;

			foreach (object obj in properties) {
				string key = obj as string;
				string val = hit [key];
				if (val != null) {
					DateTime dt = StringFu.StringToDateTime (val);
					double this_m;
					this_m = HalfLifeMultiplier (dt, 182);  /* 182 days == six months */
					if (this_m > best_m)
						best_m = this_m;
				}
			}

			if (best_m < 0)
				best_m = default_multiplier;
			return best_m;
		}



		/////////////////////////////////////////

		public virtual void DoQuery (QueryBody            body,
					     IQueryResult         query_result,
					     IQueryableChangeData i_change_data)
		{
			ChangeData change_data = (ChangeData) i_change_data;

			ICollection added_uris = null;

			if (change_data != null) {

				if (change_data.RemovedUris != null)
					foreach (Uri uri in change_data.RemovedUris)
						query_result.Subtract (uri);

				// If nothing was added, we can safely return now: this change
				// cannot have any further effect on an outstanding live query.
				if (change_data.AddedUris == null
				    || change_data.AddedUris.Count == 0)
					return;
				
				added_uris = change_data.AddedUris;
			}

			Driver.DoQuery (body, 
					query_result,
					added_uris,
					new LuceneDriver.UriFilter (HitIsValid),
					new LuceneDriver.RelevancyMultiplier (RelevancyMultiplier));
		}

		/////////////////////////////////////////

		public virtual string GetSnippet (QueryBody body, Hit hit)
		{
			// Look up the hit in our text cache.  If it is there,
			// use the cached version to generate a snippet.

			TextReader reader = TextCache.GetReader (hit.Uri);
			if (reader == null)
				return null;

			return SnippetFu.GetSnippet (body, reader);
		}

		/////////////////////////////////////////

		public virtual int GetItemCount ()
		{
			return Driver.GetItemCount ();
		}

		//////////////////////////////////////////////////////////////////////////////////

		//
		// The types involved here are defined below
		//

		public Scheduler.Task NewAddTask (Indexable indexable)
		{
			LuceneTask task;
			task = new LuceneTask (this, this.indexer, indexable);
			task.Collector = collector;
			return task;
		}

		public Scheduler.Task NewAddTask (IIndexableGenerator generator, Scheduler.Hook generator_hook)
		{
			LuceneTask task;
			task = new LuceneTask (this, this.indexer, generator);
			task.Priority = Scheduler.Priority.Generator;
			task.GeneratorHook = generator_hook;
			return task;
		}

		public Scheduler.Task NewAddTask (IIndexableGenerator generator)
		{
			return this.NewAddTask (generator, null);
		}

		public Scheduler.Task NewRemoveTask (Uri uri)
		{
			LuceneTask task;
			task = new LuceneTask (this, this.indexer, uri);
			task.Collector = collector;
			return task;
		}

		public Scheduler.Task NewTaskFromHook (Scheduler.TaskHook hook)
		{
			Scheduler.Task task = Scheduler.TaskFromHook (hook);
			task.Collector = collector;
			return task;
		}

		//////////////////////////////////////////////////////////////////////////////////

		private class LuceneTaskCollector : Scheduler.ITaskCollector {

			IIndexer indexer;

			public LuceneTaskCollector (IIndexer indexer)
			{
				this.indexer = indexer;
			}

			public double GetMinimumWeight ()
			{
				return 0;
			}

			public double GetMaximumWeight ()
			{
				// FIXME: this is totally arbitrary
				return 37;
			}

			public void PreTaskHook ()
			{
				// Do nothing
			}

			public void PostTaskHook ()
			{
				indexer.Flush ();
			}
			
		}

		//////////////////////////////////////////////////////////////////////////////////

		private class LuceneTask : Scheduler.Task {

			LuceneQueryable queryable;

			IIndexer indexer;

			// If non-null, add this Indexable
			Indexable indexable = null;

			// If non-null, remove this Uri
			Uri uri = null;

			// If non-null, add this IIndexableGenerator
			IIndexableGenerator generator = null;

			// FIXME: number of items generated
			// from the Indexable shouldn't be
			// hard-wired
			const int hard_wired_generation_count = 30;

			// Hook to be invoked after the IIndexableGenerator
			// has finished processing a batch of Indexables,
			// just prior to flushing the driver.
			public Scheduler.Hook GeneratorHook;

			public LuceneTask (LuceneQueryable queryable, IIndexer indexer, Indexable indexable) // Add
			{
				this.queryable = queryable;
				this.indexer = indexer;
				this.indexable = indexable;
				
				this.Tag = indexable.Uri.ToString ();
				this.Weight = 1;
			}

			public LuceneTask (LuceneQueryable queryable, IIndexer indexer, Uri uri) // Remove
			{
				this.queryable = queryable;
				this.indexer = indexer;
				this.uri = uri;

				this.Tag = uri.ToString ();
				this.Weight = 0.499999;
			}

			public LuceneTask (LuceneQueryable queryable, IIndexer indexer, IIndexableGenerator generator) // Add Many
			{
				this.queryable = queryable;
				this.indexer = indexer;
				this.generator = generator;

				this.Tag = generator.StatusName;
				this.Weight = hard_wired_generation_count;
			}

			protected override void DoTaskReal ()
			{

				if (indexable != null) {
					queryable.CacheIndexableInfo (indexable);
					indexer.Add (indexable);
				} else if (uri != null) {
					indexer.Remove (uri);
				} else if (generator != null) {

					// Since this is a generator, we want the task to
					// get re-scheduled after it is run.
					Reschedule = true;

					int count;
					for (count = 0; count < hard_wired_generation_count; ++count) {
						if (!generator.HasNextIndexable ()) {
							// ...except if there is no more work to do, of course.
							Reschedule = false;
							break;
						}

						Indexable generated = generator.GetNextIndexable ();

						// Note that the indexable generator can return null.
						// This means that the generator didn't have an indexable
						// to return this time through, but it does not mean that
						// its processing queue is empty.
						if (generated != null) {
							queryable.CacheIndexableInfo (generated);
							indexer.Add (generated);
						}
					}

					if (count > 0 && this.GeneratorHook != null)
						this.GeneratorHook ();

					indexer.Flush ();
				}
			}
		}

		//////////////////////////////////////////////////////////////////////////////////

		private class MarkingClosure {
			FileAttributesStore fa_store;
			string path;
			DateTime mtime;
			
			public MarkingClosure (FileAttributesStore fa_store,
					       string              path,
					       DateTime            mtime)
			{
				this.fa_store = fa_store;
				this.path = path;
				this.mtime = mtime;
			}
				
			public void Mark ()
			{
				fa_store.AttachTimestamp (path, mtime);
			}
		}
				
		
		protected Scheduler.TaskGroup NewMarkingTaskGroup (string path, DateTime mtime)
		{
			MarkingClosure mc = new MarkingClosure (FileAttributesStore, path, mtime);
			Scheduler.Hook post_hook = new Scheduler.Hook (mc.Mark);
			return Scheduler.NewTaskGroup ("mark " + path, null, post_hook);
		}
	}
}
