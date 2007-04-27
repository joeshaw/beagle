//
// LuceneQueryable.cs
//
// Copyright (C) 2004-2007 Novell, Inc.
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
using System.Collections.Generic;
using System.IO;

using Beagle.Util;

namespace Beagle.Daemon {

	public abstract class LuceneQueryable : IQueryable {
		
		static public bool Debug = false;
		static public bool OptimizeRightAway = false;

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

		private LuceneQueryingDriver.UriFilter our_uri_filter;
		private LuceneCommon.HitFilter our_hit_filter;
		private Scheduler.Task our_final_flush_task = null;
		private Scheduler.Task our_optimize_task = null;

		private object request_lock = new object ();
		private IndexerRequest pending_request = new IndexerRequest ();

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

			OptimizeAllEvent += OnOptimizeAllEvent;

			// Schedule an optimize, just in case
			ScheduleOptimize ();

			Shutdown.ShutdownEvent += new Shutdown.ShutdownHandler (OnShutdownEvent);
		}

		public string IndexName {
			get { return index_name; }
		}

		public string IndexDirectory {
			get { return driver.TopDirectory; }
		}

		public string IndexFingerprint {
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

		virtual protected void ShutdownHook ()
		{

		}

		private void OnShutdownEvent ()
		{
			lock (request_lock) 
				pending_request.Cleanup ();

			// Clear the deferred indexables
			foreach (Indexable indexable in deferred_indexables.Values)
				indexable.Cleanup ();
				
			try {
				ShutdownHook ();
			} catch (Exception ex) {
				Logger.Log.Warn (ex, "Caught exception in shutdown hook");
			}
		}

		/////////////////////////////////////////

		virtual public bool AcceptQuery (Query query)
		{
			// Accept all queries by default.
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

			// Index listeners never return any initial matches.
			if (change_data == null && query.IsIndexListener)
				return;

			if (change_data != null) {
				
				if (change_data.RemovedUris != null)
					query_result.Subtract (change_data.RemovedUris);

				// If nothing was added, we can safely return now: this change
				// cannot have any further effect on an outstanding live query.
				if (change_data.AddedUris == null
				    || change_data.AddedUris.Count == 0)
					return;

				added_uris = change_data.AddedUris;

				// If this is an index listener, we don't need to do a query:
				// we just build up synthethic hits and add them unconditionally.
				if (query.IsIndexListener) {
					ArrayList synthetic_hits = new ArrayList ();
					foreach (Uri uri in added_uris) {
						if (our_uri_filter != null) {
							bool accept = false;

							try {
								accept = our_uri_filter (uri);
							} catch (Exception e) {
								Log.Warn (e, "Caught an exception in HitIsValid for {0}", uri);
							}

							if (! accept)
								continue;
						}

						Hit hit = new Hit ();
						hit.Uri = uri;

						if (our_hit_filter != null) {
							bool accept = false;

							try {
								accept = our_hit_filter (hit);
							} catch (Exception e) {
								Log.Warn (e, "Caught an exception in HitFilter for {0}", hit.Uri);
							}

							if (! accept)
								continue;
						}

						synthetic_hits.Add (hit);
					}
					if (synthetic_hits.Count > 0)
						query_result.Add (synthetic_hits);
					return;
				}
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

		private DateTime last_state_change = DateTime.MinValue;

		public QueryableStatus GetQueryableStatus ()
		{
			QueryableStatus status = new QueryableStatus ();

			status.ProgressPercent = this.ProgressPercent;
			status.ItemCount = driver.GetItemCount ();
			status.IsIndexing = this.IsIndexing;

			return status;
		}

		/////////////////////////////////////////

		private bool is_indexing = false;

		// Reports whether the backend is performing the initial crawling and indexing
		protected bool IsIndexing {
			get { return is_indexing; }
			set {
				bool changed = (is_indexing != value);

				is_indexing = value;

				if (changed)
					QueryDriver.QueryableChanged (this, null);
			}
		}

		protected virtual int ProgressPercent {
			get { return -1; }
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
			
			return new FileStream (path, System.IO.FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
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

		// More hooks.  These are mostly here for the file system backend.

		virtual protected bool PreAddIndexableHook (Indexable indexable)
		{
			// By default, we like everything.
			return true;
		}

		// If we are remapping Uris, indexables should be added to the
		// index with the internal Uri attached.  This the receipt
		// will come back w/ an internal Uri.  In order for change
		// notification to work correctly, we have to map it to
		// an external Uri.
		// Return the remapped uri.
		virtual protected Uri PostAddHook (Indexable indexable, IndexerAddedReceipt receipt)
		{
			// By default, remapped uri is the indexable uri
			return indexable.Uri;
		}

		virtual protected Uri PostRemoveHook (Indexable indexable)
		{
			// By default, remapped uri is the indexable uri
			return indexable.Uri;
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Adding a single indexable
		
		private class AddTask : Scheduler.Task {
			LuceneQueryable    queryable;
			Indexable          indexable;

			public AddTask (LuceneQueryable    queryable,
					Indexable          indexable)
			{
				this.queryable = queryable;
				this.indexable = indexable;
				this.Tag = indexable.DisplayUri.ToString ();
				this.Weight = 1;
			}

			override protected void DoTaskReal ()
			{
				if (queryable.PreAddIndexableHook (indexable)) {
					queryable.AddIndexable (indexable);
					queryable.ConditionalFlush ();
				} else if (indexable.LocalState ["IndexerIndexableGenerated"] != null) {
					// For indexer generated indexables, either the indexable or an
					// IndexableType.Ignore request _has_ to go back to the indexhelper.
					indexable.Type = IndexableType.Ignore;
					queryable.AddIndexable (indexable);
					queryable.ConditionalFlush ();
				}
			}

			override protected void DoCleanup ()
			{
				indexable.Cleanup ();
			}
		}

		public Scheduler.Task NewAddTask (Indexable indexable)
		{
			AddTask task;
			task = new AddTask (this, indexable);
			task.Source = this;
			return task;
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Adding an indexable generator

		private class AddGeneratorTask : Scheduler.Task {
			LuceneQueryable queryable;
			IIndexableGenerator generator;

			public AddGeneratorTask (LuceneQueryable     queryable,
						 IIndexableGenerator generator)
			{
				this.queryable = queryable;
				this.generator = generator;
				this.Tag = generator.StatusName;
			}

			override protected void DoTaskReal ()
			{
				// Since this is a generator, we want the task to
				// get re-scheduled after it is run.
				Reschedule = true;

				// Number of times a null indexable was returned.  We don't want
				// to spin tightly in a loop here if we're not actually indexing
				// things.
				int misfires = 0;
				bool flushed = false;

				do {
					if (! generator.HasNextIndexable ()) {
						// Of course, don't reschedule if there is no more work to do.
						Reschedule = false;
						break;
					}

					Indexable generated;
					generated = generator.GetNextIndexable ();

					// Note that the indexable generator can return null.
					// This means that the generator didn't have an indexable
					// to return this time through, but it does not mean that
					// its processing queue is empty.
					if (generated == null) {
						misfires++;

						if (misfires > 179) // Another totally arbitrary number
							break;
						else
							continue;
					}

					if (queryable.PreAddIndexableHook (generated)) {
						queryable.AddIndexable (generated);
					} else if (generated.LocalState ["IndexerIndexableGenerated"] != null) {
						// For indexer generated indexables, either the indexable or an
						// IndexableType.Ignore request _has_ to go back to the indexhelper.
						generated.Type = IndexableType.Ignore;
						queryable.AddIndexable (generated);
					} else {
						generated.Cleanup ();
					}
					
					// We keep adding indexables until a flush goes through.
				} while (! (flushed = queryable.ConditionalFlush ()));

				if (! flushed)
					queryable.Flush ();

				generator.PostFlushHook ();
			}

			override protected void DoCleanup ()
			{
			}
		}

		public Scheduler.Task NewAddTask (IIndexableGenerator generator)
		{
			AddGeneratorTask task;
			task = new AddGeneratorTask (this, generator);
			task.Source = this;
			return task;
		}

		//////////////////////////////////////////////////////////////////////////////////

		// There used to be a separate type of task for doing removes.
		// This is all that remains of that old code.
		public Scheduler.Task NewRemoveTask (Uri uri)
		{
			Indexable indexable;
			indexable = new Indexable (IndexableType.Remove, uri);
			
			return NewAddTask (indexable);
		}

		//////////////////////////////////////////////////////////////////////////////////

		public Scheduler.Task NewRemoveByPropertyTask (Property prop)
		{
			PropertyRemovalGenerator prg = new PropertyRemovalGenerator (driver, prop);

			return NewAddTask (prg);
		}

		///////////////////////////////////////////////////////////////////////////////////

		//
		// An IIndexableGenerator that returns remove Indexables for
		// all items which match a certain property
		//

		private class PropertyRemovalGenerator : IIndexableGenerator {

			private LuceneQueryingDriver driver;
			private Property prop_to_match;
			private Uri[] uris_to_remove;
			private int idx;

			public PropertyRemovalGenerator (LuceneQueryingDriver driver, Property prop)
			{
				this.driver = driver;
				this.prop_to_match = prop;
			}

			public Indexable GetNextIndexable ()
			{
				Indexable indexable;

				indexable = new Indexable (IndexableType.Remove, uris_to_remove [idx]);
				idx++;

				return indexable;
			}

			public bool HasNextIndexable ()
			{
				if (uris_to_remove == null)
					uris_to_remove = this.driver.PropertyQuery (this.prop_to_match);

				if (idx < uris_to_remove.Length)
					return true;
				else 
					return false;
			}

			public string StatusName {
				get {
					return String.Format ("Removing {0}={1}", prop_to_match.Key, prop_to_match.Value);
				}
			}

			public void PostFlushHook () { }
		}


		//////////////////////////////////////////////////////////////////////////////////

		// When all other tasks are complete, we need to do a final flush.
		// We schedule that as a maintenance task.

		private class FinalFlushTask : Scheduler.Task {
			LuceneQueryable queryable;

			public FinalFlushTask (LuceneQueryable queryable)
			{
				this.queryable = queryable;

			}

			override protected void DoTaskReal ()
			{
				queryable.Flush ();
			}
		}

		private void ScheduleFinalFlush ()
		{
			if (our_final_flush_task == null) {
				our_final_flush_task = new FinalFlushTask (this);

				our_final_flush_task.Tag = "Final Flush for " + IndexName;
				our_final_flush_task.Priority = Scheduler.Priority.Maintenance;
				our_final_flush_task.SubPriority = 100; // do this first when starting maintenance
				our_final_flush_task.Source = this;
			}
			
			ThisScheduler.Add (our_final_flush_task);
		}


		//////////////////////////////////////////////////////////////////////////////////

		// Optimize the index

		private DateTime last_optimize_time = DateTime.MinValue;

		public DateTime LastOptimizeTime {
			get { return last_optimize_time; }
			set { last_optimize_time = value; }
		}
		
		private class OptimizeTask : Scheduler.Task {
			LuceneQueryable queryable;

			public OptimizeTask (LuceneQueryable queryable)
			{
				this.queryable = queryable;
			}

			override protected void DoTaskReal ()
			{
				queryable.Optimize ();
				queryable.LastOptimizeTime = DateTime.Now;
			}
		}

		public Scheduler.Task NewOptimizeTask ()
		{
			Scheduler.Task task;
			task = new OptimizeTask (this);
			task.Tag = "Optimize " + IndexName;
			task.Priority = Scheduler.Priority.Maintenance;
			task.Source = this;

			return task;
		}

		private void OnOptimizeAllEvent ()
		{
			Scheduler.Task task;
			task = NewOptimizeTask (); // construct an optimizer task
			task.Priority = Scheduler.Priority.Delayed; // but boost the priority
			ThisScheduler.Add (task);
		}

		private void ScheduleOptimize ()
		{
			double optimize_delay;

			// Really we only want to optimize at most once a day, even if we have
			// indexed a ton of dat
			TimeSpan span = DateTime.Now - last_optimize_time;
			if (span.TotalDays > 1.0)
				optimize_delay = 10.0; // minutes;
			else
				optimize_delay = (new TimeSpan (TimeSpan.TicksPerDay) - span).TotalMinutes;

			if (our_optimize_task == null)
				our_optimize_task = NewOptimizeTask ();

			if (OptimizeRightAway || Environment.GetEnvironmentVariable ("BEAGLE_UNDER_BLUDGEON") != null)
				optimize_delay = 1/120.0; // half a second

			// Changing the trigger time of an already-scheduled process
			// does what you would expect.
			our_optimize_task.TriggerTime = DateTime.Now.AddMinutes (optimize_delay);

			// Adding the same task more than once is a harmless no-op.
			ThisScheduler.Add (our_optimize_task);
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Other hooks

		// If this returns true, a task will automatically be created to
		// add the indexable
		virtual protected bool PreFilterGeneratedAddHook (Indexable indexable)
		{
			return true;
		}

		virtual protected void PreFlushHook (IndexerRequest flushed_request)
		{ }

		virtual protected void PostFlushHook (IndexerRequest    flushed_request,
						      IndexerReceipt [] receipts)
		{ }

		//////////////////////////////////////////////////////////////////////////////////

		protected void AddIndexable (Indexable indexable)
		{
			indexable.Source = QueryDriver.GetQueryable (this).Name;

			lock (request_lock)
				pending_request.Add (indexable);

			// Schedule a final flush every time we add anything.
			// Better safe than sorry.
			ScheduleFinalFlush ();
		}

		protected void Optimize ()
		{
			lock (request_lock) {
				pending_request.OptimizeIndex = true;
				Flush ();
			}
		}

		// Returns true if we actually did flush, false otherwise.
		protected bool ConditionalFlush ()
		{
			lock (request_lock) {
				if (pending_request.Count > 37) { // a total arbitrary magic number
					Flush ();
					return true;
				}
			}
			return false;
		}

		private Dictionary<int, Indexable> deferred_indexables = new Dictionary<int, Indexable> ();

		protected void Flush ()
		{
			IndexerRequest flushed_request;

			lock (request_lock) {
				if (pending_request.IsEmpty)
					return;

				flushed_request = pending_request;
				pending_request = new IndexerRequest ();

				// We hold the request_lock when calling PreFlushHook, so
				// that no other requests can come in until it exits.
				PreFlushHook (flushed_request);
			}

			IndexerReceipt [] receipts;
			receipts = indexer.Flush (flushed_request);

			PostFlushHook (flushed_request, receipts);

			// Silently return if we get a null back.  This is probably
			// a bad thing to do.
			if (receipts == null)
				return;

			// Nothing happened (except maybe an optimize, which does not
			// generate a receipt).  Also do nothing.
			if (receipts.Length == 0)
				return;

			// Update the cached count of items in the driver
			driver.SetItemCount (indexer.GetItemCount ());

			// Something happened, so schedule an optimize just in case.
			ScheduleOptimize ();
			
			if (fa_store != null)
				fa_store.BeginTransaction ();

			ArrayList added_uris = new ArrayList ();
			ArrayList removed_uris  = new ArrayList ();

			for (int i = 0; i < receipts.Length; ++i) {

				if (receipts [i] is IndexerAddedReceipt) {
					
					IndexerAddedReceipt r;
					r = (IndexerAddedReceipt) receipts [i];
					Indexable indexable = flushed_request.GetRequestIndexable (r);

					if (indexable == null) {
						// Must be a previously deferred indexable.
						indexable = deferred_indexables [r.Id];

						if (indexable == null) {
							Log.Warn ("Unable to match up indexable id# {0} to any indexable object!",
								  r.Id);
							continue;
						}

						deferred_indexables.Remove (r.Id);
					}

					// Add the Uri to the list for our change data
					// *before* doing any post-processing.
					// This ensures that we have internal uris when
					// we are remapping.
					added_uris.Add (indexable.Uri);
					
					// Call the appropriate hook
					Uri notification_uri = indexable.Uri;
					try {
						// Map from internal->external Uris in the PostAddHook
						notification_uri = PostAddHook (indexable, r);
					} catch (Exception ex) {
						Logger.Log.Warn (ex, "Caught exception in PostAddHook '{0}' '{1}' '{2}'",
								 indexable.Uri, r.FilterName, r.FilterVersion);
					}

					// Every added Uri also needs to be listed as removed,
					// to avoid duplicate hits in the query.  Since the
					// removed Uris need to be external Uris, we add them
					// to the list *after* post-processing.
					removed_uris.Add (notification_uri);

				} else if (receipts [i] is IndexerRemovedReceipt) {

					IndexerRemovedReceipt r;
					r = (IndexerRemovedReceipt) receipts [i];

					Indexable indexable = flushed_request.GetRequestIndexable (r);
					if (indexable == null) { // Should never happen
						Log.Warn ("Unable to match indexable-remove #{0} to any request!", r.Id);
						continue;
					}

					// Call the appropriate hook
					Uri notification_uri = indexable.Uri;
					try {
						notification_uri = PostRemoveHook (indexable);
					} catch (Exception ex) {
						Logger.Log.Warn (ex, "Caught exception in PostRemoveHook '{0}'",
								 indexable.Uri);
					}

					// Add the removed Uri to the list for our
					// change data.  This will be an external Uri
					// when we are remapping.
					removed_uris.Add (notification_uri);
					
				} else if (receipts [i] is IndexerIndexablesReceipt) {
					
					IndexerIndexablesReceipt r;
					r = (IndexerIndexablesReceipt) receipts [i];

					foreach (Indexable indexable in r.Indexables) {
						bool please_add_a_new_task = false;
						indexable.LocalState ["IndexerIndexableGenerated"] = true;

						try {
							please_add_a_new_task = PreFilterGeneratedAddHook (indexable);
						} catch (InvalidOperationException ex) {
							// Queryable does not support adding children
						} catch (Exception ex) {
							Logger.Log.Warn (ex, "Caught exception in PreFilterGeneratedAddHook '{0}'", indexable.DisplayUri);
						}

						if (please_add_a_new_task) {
							if (Debug) {
								if (indexable.IsChild)
									Log.Debug ("Adding filter-generated child indexable {0} to parent {1}", indexable.Uri, indexable.ParentUri);
								else
									Log.Debug ("Adding filter-generated indexable {0}", indexable.Uri);
							}

						} else {
							if (Debug)
								Log.Debug ("Ignoring filter generated indexable {0}", indexable.Uri);
							indexable.Type = IndexableType.Ignore;
						}

						// FIXME: Maybe we should use an indexable generator instead?
						Scheduler.Task task = NewAddTask (indexable);
						task.SubPriority = 1; // So we jump ahead of other individual tasks
						ThisScheduler.Add (task);
					}

				} else if (receipts [i] is IndexerDeferredReceipt) {
					
					IndexerDeferredReceipt r;
					r = (IndexerDeferredReceipt) receipts [i];
					Indexable indexable = flushed_request.GetRequestIndexable (r);

					if (indexable == null) { // Should never happen
						Log.Warn ("Unable to match indexable(now deferred) #{0} to any request!", r.Id);
						continue;
					}

					deferred_indexables [r.Id] = indexable;
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
