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

		public delegate IIndexer IndexerCreator (string name);

		static private IndexerCreator indexer_hook = null;

		static public IndexerCreator IndexerHook {
			set { indexer_hook = value; }
		}

		//////////////////////////////////////////////////////////
		
		private Scheduler scheduler = Scheduler.Global;

		private string index_name;
		private string index_dir;
		private LuceneDriver driver;
		private IIndexer indexer;
		private LuceneTaskCollector collector;
		private FileAttributesStore fa_store;

		private LuceneDriver.UriRemapper to_internal_uris = null;
		private LuceneDriver.UriRemapper from_internal_uris = null;

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
			driver = new LuceneDriver (this.index_name);

			if (indexer_hook != null)
				indexer = indexer_hook (this.index_name);
			if (indexer == null)
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
			get { return driver.IndexDirectory; }
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

		public void SetUriRemappers (LuceneDriver.UriRemapper to_internal_uris,
					     LuceneDriver.UriRemapper from_internal_uris)
		{
			this.to_internal_uris = to_internal_uris;
			this.from_internal_uris = from_internal_uris;
		}

		/////////////////////////////////////////

		protected virtual void AbusiveAddHook (Uri uri)
		{

		}

		protected virtual void AbusiveRemoveHook (Uri uri)
		{

		}

		protected virtual void AbusiveRenameHook (Uri old_uri, Uri new_uri)
		{

		}

		/////////////////////////////////////////

		// *** FIXME *** FIXME *** FIXME *** FIXME ***
		// When we rename a directory, we need to somehow
		// propagate change information to files under that
		// directory.  Example: say that file foo is in
		// directory bar, and there is an open query that
		// matches foo.  The tile probably says something
		// like "foo, in folder bar".
		// Then assume I rename bar to baz.  That notification
		// will go out, so a query matching bar will get
		// updated... but the query matching foo will not.
		// What should really happen is that the tile
		// should change to say "foo, in folder baz".
		// But making that work will require some hacking
		// on the QueryResults.
		// *** FIXME *** FIXME *** FIXME *** FIXME ***

		private class ChangeData : IQueryableChangeData {
			public ICollection AddedUris;
			public ICollection RemovedUris;
		}

		private void OnIndexerChanged (IIndexer    source,
					       ICollection list_of_added_uris,
					       ICollection list_of_removed_uris,
					       ICollection list_of_renamed_uris)
		{
			// If we have renamed uris, synthesize some approproate
			// ChangeData.
			// Right now we assume that there will never be adds/removes
			// and renames in the same event.  That is true now, but could
			// change in the future.
			if (list_of_renamed_uris != null && list_of_renamed_uris.Count > 0) {

				IEnumerator x = list_of_renamed_uris.GetEnumerator ();

				while (x.MoveNext ()) {
					Uri old_uri = x.Current as Uri;
					if (from_internal_uris != null)
						old_uri = from_internal_uris (old_uri);
					if (x.MoveNext ()) {
						Uri new_uri = x.Current as Uri;

						AbusiveRenameHook (old_uri, new_uri);

						Logger.Log.Debug ("*** Faking change data {0} => {1}", old_uri, new_uri);

						ChangeData fake_change_data = new ChangeData ();
						fake_change_data.AddedUris = new Uri [1] { new_uri };
						fake_change_data.RemovedUris = new Uri [1] { old_uri };
						QueryDriver.QueryableChanged (this, fake_change_data);
					}
				}

				return;
			}

			// Walk across the list of removed Uris and drop them
			// from the text cache.
			foreach (Uri uri in list_of_removed_uris) {
				TextCache.Delete (uri);
			}

			// Walk across the list of added Uris and mark the local
			// files with the cached timestamp.
			foreach (Uri uri in list_of_added_uris) {
				UseCachedIndexableInfo (uri);
				AbusiveAddHook (uri);
			}

			// Propagate the event up through the Queryable.
			ChangeData change_data = new ChangeData ();

			// Keep a copy of the original list of Uris to remove
			ICollection original_list_of_removed_uris = list_of_removed_uris;

			// If necessary, remap Uris
			if (from_internal_uris != null) {
				Uri [] remapped_adds = new Uri [list_of_added_uris.Count];
				Uri [] remapped_removes = new Uri [list_of_removed_uris.Count];

				int i = 0;
				foreach (Uri uri in list_of_added_uris)
					remapped_adds [i++] = from_internal_uris (uri);
				i = 0;
				foreach (Uri uri in list_of_removed_uris)
					remapped_removes [i++] = from_internal_uris (uri);

				list_of_added_uris = remapped_adds;
				list_of_removed_uris = remapped_removes;
			}
			
			change_data.AddedUris = list_of_added_uris;
			change_data.RemovedUris = list_of_removed_uris;

			// We want to make sure all of our remappings are done
			// before calling this hook, since it can (and should)
			// break the link between uids and paths.
			foreach (Uri uri in original_list_of_removed_uris) {
				AbusiveRemoveHook (uri);
			}

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

		// Schedule all non-valid Uris for removal.
		private bool HitIsValidOrElse (Uri uri)
		{
			bool is_valid = HitIsValid (uri);

			if (! is_valid) {

				// FIXME: There is probably a race here --- what if the hit
				// becomes valid sometime between calling HitIsValid
				// and the removal task being executed?
				if (to_internal_uris != null)
					uri = to_internal_uris (uri);

				Scheduler.Task task = NewRemoveTask (uri);
				ThisScheduler.Add (task, Scheduler.AddType.DeferToExisting);
			}

			return is_valid;
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

		protected virtual ICollection DoBonusQuery (QueryBody body)
		{
			return null;
		}

		public void DoQuery (QueryBody            body,
				     IQueryResult         query_result,
				     IQueryableChangeData i_change_data)
		{
			ChangeData change_data = (ChangeData) i_change_data;
			
			ICollection added_uris = null;
			ICollection extra_uris = null;

			if (change_data != null) {
				
				if (change_data.RemovedUris != null) {
					foreach (Uri uri in change_data.RemovedUris) {
						Logger.Log.Debug ("**** Removing {0}", uri);
						query_result.Subtract (uri);
					}
				}

				// If nothing was added, we can safely return now: this change
				// cannot have any further effect on an outstanding live query.
				if (change_data.AddedUris == null
				    || change_data.AddedUris.Count == 0)
					return;

				if (to_internal_uris != null) {
					Uri [] remapped_uris = new Uri [change_data.AddedUris.Count];
					int i = 0;
					foreach (Uri uri in change_data.AddedUris)
						remapped_uris [i++] = to_internal_uris (uri);
					added_uris = remapped_uris;
				} else {
					added_uris = change_data.AddedUris;
				}
			} else {
				
				// We only generate extra Uris via the bonus query when we
				// don't have change data.
				extra_uris = DoBonusQuery (body);
			}

			Driver.DoQuery (body, 
					query_result,
					added_uris,
					extra_uris,
					new LuceneDriver.UriFilter (HitIsValidOrElse),
					from_internal_uris,
					new LuceneDriver.RelevancyMultiplier (RelevancyMultiplier));
		}

		/////////////////////////////////////////

		public virtual string GetSnippet (QueryBody body, Hit hit)
		{
			// Look up the hit in our text cache.  If it is there,
			// use the cached version to generate a snippet.

			Uri uri = hit.Uri;
			if (to_internal_uris != null)
				uri = to_internal_uris (uri);

			TextReader reader = TextCache.GetReader (uri, from_internal_uris);
			if (reader == null)
				return null;

			string snippet = SnippetFu.GetSnippet (body, reader);
			reader.Close ();

			return snippet;
		}

		/////////////////////////////////////////

		public virtual int GetItemCount ()
		{
			return indexer.GetItemCount ();
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

		public Scheduler.Task NewRemoveTask (Uri uri) // This should be an external Uri
		{
			LuceneTask task;
			if (to_internal_uris != null)
				uri = to_internal_uris (uri);
			task = new LuceneTask (this, this.indexer, uri);
			task.Collector = collector;
			return task;
		}

		// old_uri should be an internal Uri
		// new_uri should be an external Uri
		public Scheduler.Task NewRenameTask (Uri old_uri, Uri new_uri)
		{
			LuceneTask task;
			if (to_internal_uris != null)
				old_uri = to_internal_uris (old_uri);
			task = new LuceneTask (this, this.indexer, old_uri, new_uri);

			// To avoid grouping with anything else, we create our own collector
			task.Collector = new LuceneTaskCollector (indexer);
			
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

			// If uri != null && other_uri == null, remove uri
			// If both are non-null, rename uri => other_uri
			Uri uri = null;
			Uri other_uri = null;

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
				
				this.Tag = indexable.DisplayUri.ToString ();
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

			public LuceneTask (LuceneQueryable queryable, IIndexer indexer, Uri old_uri, Uri new_uri) // Rename
			{
				this.queryable = queryable;
				this.indexer = indexer;
				this.uri = old_uri;
				this.other_uri = new_uri;

				this.Tag = String.Format ("{0} => {1}", old_uri, new_uri);
				this.Weight = 0.1; // In theory renames are light-weight
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
					if (! (indexable.Uri.IsFile 
					       && queryable.FileAttributesStore.IsUpToDate (indexable.Uri.LocalPath))) {
						queryable.CacheIndexableInfo (indexable);
						indexer.Add (indexable);
					}
				} else if (uri != null && other_uri != null) {
					indexer.Rename (uri, other_uri);
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
						// FIXME: Shouldn't we just break if generated is null?
						// Right now we just call GetNextIndexable a bunch of times
						// when we don't have more work to do.
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
