//
// LuceneQueryable.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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

	public abstract class LuceneQueryable : IQueryable {

		public delegate IIndexer IndexerCreator (string name, int minor_version);

		static private IndexerCreator indexer_hook = null;

		static public IndexerCreator IndexerHook {
			set { indexer_hook = value; }
		}
		
		virtual protected IIndexer LocalIndexerHook ()
		{
			return null;
		}

		//////////////////////////////////////////////////////////

		public delegate void OptimizeAllHandler ();

		static private OptimizeAllHandler OptimizeAllEvent;

		static public void OptimizeAll ()
		{
			if (OptimizeAllEvent != null)
				OptimizeAllEvent ();
		}

		//////////////////////////////////////////////////////////
		
		private Scheduler scheduler = Scheduler.Global;
		private FileAttributesStore fa_store = null;

		private string index_name;
		private int minor_version;
		private bool read_only_mode;

		private LuceneQueryingDriver driver;
		private IIndexer indexer = null;
		private LuceneTaskCollector collector;

		private LuceneQueryingDriver.UriFilter our_uri_filter;
		private LuceneCommon.HitFilter our_hit_filter;

		//////////////////////////////////////////////////////////

		public LuceneQueryable (string index_name) : this (index_name, -1, false) { }

		public LuceneQueryable (string index_name, bool read_only_mode) : this (index_name, -1, read_only_mode) { }

		public LuceneQueryable (string index_name, int minor_version) : this (index_name, minor_version, false) { }

		public LuceneQueryable (string index_name, int minor_version, bool read_only_mode)
		{
			this.index_name = index_name;
			this.minor_version = minor_version;
			this.read_only_mode = read_only_mode;

			driver = BuildLuceneQueryingDriver (this.index_name, this.minor_version, this.read_only_mode);
			our_uri_filter = new LuceneQueryingDriver.UriFilter (this.HitIsValid);
			our_hit_filter = new LuceneCommon.HitFilter (this.HitFilter);

			// If the queryable is in read-only more, don't 
			// instantiate an indexer for it.
			if (read_only_mode)
				return;

			indexer = LocalIndexerHook ();
			if (indexer == null && indexer_hook != null)
				indexer = indexer_hook (this.index_name, this.minor_version);
			indexer.FlushEvent += OnFlushEvent;

			collector = new LuceneTaskCollector (indexer);

			OptimizeAllEvent += OnOptimizeAllEvent;

			// Schedule an optimize, just in case
			ScheduleOptimize ();
		}

		protected string IndexName {
			get { return index_name; }
		}

		protected string IndexDirectory {
			get { return driver.TopDirectory; }
		}

		protected string IndexFingerprint {
			get { return driver.Fingerprint; }
		}

		protected LuceneQueryingDriver Driver {
			get { return driver; }
		}

		public Scheduler ThisScheduler {
			get { return scheduler; }
		}

		/////////////////////////////////////////

		virtual public void Start ()
		{

		}

		/////////////////////////////////////////

		virtual public bool AcceptQuery (Query query)
		{
			return true;
		}

		/////////////////////////////////////////

		virtual protected bool HitIsValid (Uri uri)
		{
			return true;
		}

		virtual protected bool HitFilter (Hit hit)
		{
			return true;
		}

		/////////////////////////////////////////

		virtual protected Hit PostProcessHit (Hit hit)
		{
			return hit;
		}

		/////////////////////////////////////////

		// DEPRECATED: This does nothing, since everything is now
		// time-based.
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

			// These get fed back to LuceneQueryingDriver.DoQuery
			// as a search subset, and hence need to be internal
			// Uris when we are remapping.
			public ICollection AddedUris;

			// These get reported directly to clients in
			// Subtract events, and thus need to be external Uris
			// when we are remapping.
			public ICollection RemovedUris;
		}

		public void DoQuery (Query                query,
				     IQueryResult         query_result,
				     IQueryableChangeData i_change_data)
		{
			ChangeData change_data = (ChangeData) i_change_data;
			
			ICollection added_uris = null;

			if (change_data != null) {
				
				if (change_data.RemovedUris != null) 
					query_result.Subtract (change_data.RemovedUris);

				// If nothing was added, we can safely return now: this change
				// cannot have any further effect on an outstanding live query.
				if (change_data.AddedUris == null
				    || change_data.AddedUris.Count == 0)
					return;

				added_uris = change_data.AddedUris;
			}
			
			Driver.DoQuery (query, 
					query_result,
					added_uris,
					our_uri_filter,
					our_hit_filter);
		}

		/////////////////////////////////////////

		protected string GetSnippetFromTextCache (string [] query_terms, Uri uri)
		{
			// Look up the hit in our text cache.  If it is there,
			// use the cached version to generate a snippet.

			TextReader reader;
			reader = TextCache.UserCache.GetReader (uri);
			if (reader == null)
				return null;

			string snippet = SnippetFu.GetSnippet (query_terms, reader);
			reader.Close ();

			return snippet;
		}

		// When remapping, override this with
		// return GetSnippetFromTextCache (query_terms, remapping_fn (hit.Uri))
		virtual public string GetSnippet (string [] query_terms, Hit hit)
		{
			return GetSnippetFromTextCache (query_terms, hit.Uri);
		}

		/////////////////////////////////////////

		public virtual int GetItemCount ()
		{
			// If we're in read-only mode, query the driver and 
			// not the indexer for the item count.
			if (indexer == null)
				return driver.GetItemCount ();
			else
				return indexer.GetItemCount ();
		}

		/////////////////////////////////////////

		public FileStream ReadDataStream (string name)
		{
			string path = Path.Combine (Path.Combine (PathFinder.IndexDir, this.IndexName), name);

			if (!File.Exists (path))
				return null;

			return new FileStream (path, System.IO.FileMode.Open, FileAccess.Read);
		}

		public string ReadDataLine (string name)
		{
			FileStream stream = ReadDataStream (name);

			if (stream == null)
				return null;

			StreamReader reader = new StreamReader (stream);
			string line = reader.ReadLine ();
			reader.Close ();

			return line;
		}

		public FileStream WriteDataStream (string name)
		{
			string path = Path.Combine (Path.Combine (PathFinder.IndexDir, this.IndexName), name);
			
			return new FileStream (path, System.IO.FileMode.Create, FileAccess.Write);
		}

		public void WriteDataLine (string name, string line)
		{
			if (line == null) {
				string path = Path.Combine (Path.Combine (PathFinder.IndexDir, this.IndexName), name);

				if (File.Exists (path))
					File.Delete (path);

				return;
			}

			FileStream stream = WriteDataStream (name);
			StreamWriter writer = new StreamWriter (stream);
			writer.WriteLine (line);
			writer.Close ();
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

		// Adding a single indexable

		private delegate bool PreAddHookDelegate (Indexable indexable);
		
		private class AddTask : Scheduler.Task {
			IIndexer indexer;
			Indexable indexable;
			PreAddHookDelegate pre_add_hook;

			public AddTask (IIndexer           indexer,
					Indexable          indexable,
					PreAddHookDelegate pre_add_hook)
			{
				this.indexer = indexer;
				this.indexable = indexable;
				this.pre_add_hook = pre_add_hook;
				this.Tag = indexable.DisplayUri.ToString ();
				this.Weight = 1;
			}

			override protected void DoTaskReal ()
			{
				if (pre_add_hook == null || pre_add_hook (indexable))
					indexer.Add (indexable);
			}
		}

		virtual protected bool PreAddHook (Indexable indexable)
		{
			return true;
		}

		// If we are remapping Uris, indexables should be added to the
		// index with the internal Uri attached.  This the receipt
		// will come back w/ an internal Uri.  In order for change
		// notification to work correctly, we have to map it to
		// an external Uri.
		virtual protected void PostAddHook (IndexerAddedReceipt receipt)
		{
			// Does nothing by default
		}

		public Scheduler.Task NewAddTask (Indexable indexable)
		{
			AddTask task;
			task = new AddTask (this.indexer, indexable,
					    new PreAddHookDelegate (this.PreAddHook));
			task.Collector = collector;
			return task;
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Adding an indexable generator

		private class AddGeneratorTask : Scheduler.Task {
			IIndexer indexer;
			IIndexableGenerator generator;
			PreAddHookDelegate pre_add_hook;

			// Hook to be invoked after the IIndexableGenerator
			// has finished processing a batch of Indexables,
			// just prior to flushing the driver.
			Scheduler.Hook pre_flush_hook;

			// FIXME: number of items generated
			// from the Indexable shouldn't be
			// hard-wired
			const int hard_wired_generation_count = 30;

			public AddGeneratorTask (IIndexer            indexer,
						 IIndexableGenerator generator,
						 PreAddHookDelegate  pre_add_hook,
						 Scheduler.Hook      pre_flush_hook)
			{
				this.indexer = indexer;
				this.generator = generator;
				this.pre_add_hook = pre_add_hook;
				this.pre_flush_hook = pre_flush_hook;
				this.Tag = generator.StatusName;
				this.Weight = hard_wired_generation_count;
			}

			override protected void DoTaskReal ()
			{
				// Since this is a generator, we want the task to
				// get re-scheduled after it is run.
				Reschedule = true;

				bool did_something = false;
				for (int count = 0; count < hard_wired_generation_count; ++count) {
					if (! generator.HasNextIndexable ()) {
						// ...except if there is no more work to do, of course.
						Reschedule = false;
						break;
					}

					Indexable generated;
					generated = generator.GetNextIndexable ();

					// Note that the indexable generator can return null.
					// This means that the generator didn't have an indexable
					// to return this time through, but it does not mean that
					// its processing queue is empty.
					if (generated == null)
						break;

					if (pre_add_hook == null || pre_add_hook (generated)) {
						indexer.Add (generated);
						did_something = true;
					}
				}
				
				if (did_something) {
					if (pre_flush_hook != null)
						pre_flush_hook ();
					indexer.Flush ();
				}
			}
		}

		public Scheduler.Task NewAddTask (IIndexableGenerator generator, Scheduler.Hook pre_flush_hook)
		{
			AddGeneratorTask task;
			task = new AddGeneratorTask (this.indexer,
						     generator,
						     new PreAddHookDelegate (this.PreAddHook),
						     pre_flush_hook);
			return task;
		}

		public Scheduler.Task NewAddTask (IIndexableGenerator generator)
		{
			return NewAddTask (generator, null);
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Removing a single item from the index

		private delegate bool PreRemoveHookDelegate (Uri uri);

		private class RemoveTask : Scheduler.Task {
			IIndexer indexer;
			Uri uri;
			PreRemoveHookDelegate pre_remove_hook;

			public RemoveTask (IIndexer              indexer,
					   Uri                   uri,
					   PreRemoveHookDelegate pre_remove_hook)
			{
				this.indexer = indexer;
				this.uri = uri;
				this.pre_remove_hook = pre_remove_hook;

				this.Tag = uri.ToString ();
				this.Weight = 0.24999; // this is arbitrary
			}

			override protected void DoTaskReal ()
			{
				if (pre_remove_hook == null || pre_remove_hook (uri)) {
					if (uri != null)
						indexer.Remove (uri);
				}
			}
		}

		virtual protected bool PreRemoveHook (Uri uri)
		{
			return true;
		}

		// If we are remapping Uris, receipt.Uri will be passed in as an
		// internal Uri.  It needs to be mapped to an external uri for
		// change notification to work properly.
		virtual protected void PostRemoveHook (IndexerRemovedReceipt receipt)
		{
			// Does nothing by default
		}

		public Scheduler.Task NewRemoveTask (Uri uri)
		{
			RemoveTask task;
			task = new RemoveTask (this.indexer, uri,
					       new PreRemoveHookDelegate (this.PreRemoveHook));
			task.Collector = collector;
			return task;
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Optimize the index
		
		private class OptimizeTask : Scheduler.Task {
			IIndexer indexer;

			public OptimizeTask (IIndexer indexer)
			{
				this.indexer = indexer;
			}

			override protected void DoTaskReal ()
			{
				indexer.Optimize ();
			}
		}

		public Scheduler.Task NewOptimizeTask ()
		{
			Scheduler.Task task;
			task = new OptimizeTask (this.indexer);
			task.Tag = "Optimize " + IndexName;
			task.Priority = Scheduler.Priority.Delayed;
			task.Collector = collector;

			return task;
		}

		private void OnOptimizeAllEvent ()
		{
			ThisScheduler.Add (NewOptimizeTask ());
		}

		private void ScheduleOptimize ()
		{
			if (Environment.GetEnvironmentVariable ("BEAGLE_DISABLE_SCHEDULED_OPTIMIZATIONS") != null)
				return;

			int segment_count;
			segment_count = driver.SegmentCount;
			if (segment_count <= 1)
				return;

			double optimize_delay;
			optimize_delay = 600.0 / segment_count;
			Logger.Log.Debug ("Will optimize {0} in {1:0.0}s", IndexName, optimize_delay);

			Scheduler.Task task;
			task = NewOptimizeTask ();
			task.TriggerTime = DateTime.Now.AddSeconds (optimize_delay);
			ThisScheduler.Add (task);
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Other hooks

		// If this returns true, a task will automatically be created to
		// add the child.  Note that the PreAddHook will also be called,
		// as usual.
		virtual protected bool PreChildAddHook (Indexable child)
		{
			return true;
		}

		//////////////////////////////////////////////////////////////////////////////////

		private void OnFlushEvent (IIndexer source, IndexerReceipt [] receipts)
		{
			// This means that our flush is complete, so we
			// schedule an optimize and then return.
			if (receipts == null) {
				ScheduleOptimize ();
				return;
			}

			if (receipts.Length == 0)
				return;
			
			if (fa_store != null)
				fa_store.BeginTransaction ();

			ArrayList added_uris = new ArrayList ();
			ArrayList removed_uris  = new ArrayList ();

			for (int i = 0; i < receipts.Length; ++i) {
				
				if (receipts [i] is IndexerAddedReceipt) {
					
					IndexerAddedReceipt r;
					r = (IndexerAddedReceipt) receipts [i];

					// Add the Uri to the list for our change data
					// before doing any post-processing.
					// This ensures that we have internal uris when
					// we are remapping.
					added_uris.Add (r.Uri);
					
					// Call the appropriate hook
					try {
						// Map from internal->external Uris in the PostAddHook
						PostAddHook (r);
					} catch (Exception ex) {
						Logger.Log.Warn ("Caught exception in PostAddHook '{0}' '{1}' '{2}'",
								 r.Uri, r.FilterName, r.FilterVersion);
						Logger.Log.Warn (ex);
					}

					// Every added Uri also needs to be listed as removed,
					// to avoid duplicate hits in the query.  Since the
					// removed Uris need to be external Uris, we add them
					// to the list *after* post-processing.
					removed_uris.Add (r.Uri);


				} else if (receipts [i] is IndexerRemovedReceipt) {

					IndexerRemovedReceipt r;
					r = (IndexerRemovedReceipt) receipts [i];
					
					// Drop the removed item from the text cache
					TextCache.UserCache.Delete (r.Uri);

					
					// Call the appropriate hook
					try {
						PostRemoveHook (r);
					} catch (Exception ex) {
						Logger.Log.Warn ("Caught exception in PostRemoveHook '{0}'",
								 r.Uri);
						Logger.Log.Warn (ex);
					}

					// Add the removed Uri to the list for our
					// change data.  This will be an external Uri
					// when we are remapping.
					removed_uris.Add (r.Uri);
					
				} else if (receipts [i] is IndexerChildIndexablesReceipt) {
					
					IndexerChildIndexablesReceipt r;
					r = (IndexerChildIndexablesReceipt) receipts [i];

					foreach (Indexable child in r.Children) {
						bool please_add_a_new_task = false;

						try {
							please_add_a_new_task = PreChildAddHook (child);
						} catch (InvalidOperationException ex) {
							// Queryable does not support adding children
						} catch (Exception ex) {
							Logger.Log.Warn ("Caught exception in PreChildAddHook '{0}'", child.DisplayUri);
							Logger.Log.Warn (ex);
						}

						if (please_add_a_new_task) {
							Scheduler.Task task = NewAddTask (child);
							ThisScheduler.Add (task);
						}
					}
				}
			}

			if (fa_store != null)
				fa_store.CommitTransaction ();

			// Propagate the change notification to any open queries.
			if (added_uris.Count > 0 || removed_uris.Count > 0) {
				ChangeData change_data;
				change_data = new ChangeData ();
				change_data.AddedUris = added_uris;
				change_data.RemovedUris = removed_uris;

				QueryDriver.QueryableChanged (this, change_data);
			}
		}

		//////////////////////////////////////////////////////////////////////////////////

		//
		// It is often convenient to have easy access to a FileAttributeStore
		//

		virtual protected IFileAttributesStore BuildFileAttributesStore ()
		{
                        if (ExtendedAttribute.Supported)
                                return new FileAttributesStore_ExtendedAttribute (IndexFingerprint);
                        else
                                return new FileAttributesStore_Sqlite (IndexDirectory, IndexFingerprint);

		}

		public FileAttributesStore FileAttributesStore {
			get { 
				if (fa_store == null)
					fa_store = new FileAttributesStore (BuildFileAttributesStore ());
				return fa_store;
			}
		}

		//////////////////////////////////////////////////////////////////////////////////

		virtual protected LuceneQueryingDriver BuildLuceneQueryingDriver (string index_name,
										  int    minor_version,
										  bool   read_only_mode)
		{
			return new LuceneQueryingDriver (index_name, minor_version, read_only_mode);
		}
	}
}
