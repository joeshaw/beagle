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
		
		public event IQueryableChangedHandler ChangedEvent;

		private Scheduler scheduler = Scheduler.Global;

		private LuceneDriver driver;
		private LuceneTaskCollector collector;

		public LuceneQueryable (string index_name)
		{
			string index_dir = Path.Combine (PathFinder.RootDir, index_name);

			driver = new LuceneDriver (index_dir);
			driver.ChangedEvent += OnDriverChanged; 

			collector = new LuceneTaskCollector (driver);
		}

		protected LuceneDriver Driver {
			get { return driver; }
		}

		public Scheduler ThisScheduler {
			get { return scheduler; }
		}

		/////////////////////////////////////////

		private class ChangeData : IQueryableChangeData {
			public ICollection AddedUris;
			public ICollection RemovedUris;
		}

		private void OnDriverChanged (LuceneDriver source,
					      ICollection  list_of_added_uris,
					      ICollection  list_of_removed_uris)
		{
			if (ChangedEvent != null) {
				ChangeData change_data = new ChangeData ();
				change_data.AddedUris = list_of_added_uris;
				change_data.RemovedUris = list_of_removed_uris;

				ChangedEvent (this, change_data);
			}
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

		public virtual void DoQuery (QueryBody            body,
					     IQueryResult         query_result,
					     IQueryableChangeData i_change_data)
		{
			ChangeData change_data = (ChangeData) i_change_data;

			ICollection added_uris = null;

			if (change_data != null) {

				if (change_data.RemovedUris != null
				    && change_data.RemovedUris.Count > 0) {
					query_result.Subtract (change_data.RemovedUris);
				}

				// If nothing was added, bale out at this point: this change
				// cannot have any further effect on an outstanding live query.
				if (change_data.AddedUris == null
				    || change_data.AddedUris.Count == 0)
					return;
				
				added_uris = change_data.AddedUris;
			}

			Driver.DoQuery (body, query_result, added_uris, new LuceneDriver.UriFilter (HitIsValid));
		}

		/////////////////////////////////////////

		public virtual string GetHumanReadableStatus ()
		{
			return "implement me!";
		}

		//////////////////////////////////////////////////////////////////////////////////

		//
		// The types involved here are defined below
		//

		public Scheduler.Task NewAddTask (Indexable indexable)
		{
			LuceneTask task;
			task = new LuceneTask (Driver, indexable);
			task.Collector = collector;
			return task;
		}

		public Scheduler.Task NewAddTask (IIndexableGenerator generator)
		{
			LuceneTask task;
			task = new LuceneTask (Driver, generator);
			task.Priority = Scheduler.Priority.Generator;
			return task;
		}

		public Scheduler.Task NewRemoveTask (Uri uri)
		{
			LuceneTask task;
			task = new LuceneTask (Driver, uri);
			task.Collector = collector;
			return task;
		}

		//////////////////////////////////////////////////////////////////////////////////

		private class LuceneTaskCollector : Scheduler.ITaskCollector {

			LuceneDriver driver;

			public LuceneTaskCollector (LuceneDriver driver)
			{
				this.driver = driver;
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
				driver.Flush ();
				if (driver.NeedsOptimize)
					driver.Optimize ();
			}
			
		}

		//////////////////////////////////////////////////////////////////////////////////

		private class LuceneTask : Scheduler.Task {

			LuceneDriver driver;

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


			public LuceneTask (LuceneDriver driver, Indexable indexable) // Add
			{
				this.driver = driver;
				this.indexable = indexable;
				
				this.Tag = indexable.Uri.ToString ();
				this.Weight = 1;
			}

			public LuceneTask (LuceneDriver driver, Uri uri) // Remove
			{
				this.driver = driver;
				this.uri = uri;

				this.Tag = uri.ToString ();
				this.Weight = 0.499999;
			}

			public LuceneTask (LuceneDriver driver, IIndexableGenerator generator) // Add Many
			{
				this.driver = driver;
				this.generator = generator;

				this.Tag = generator.StatusName;
				this.Weight = hard_wired_generation_count;
			}

			protected override void DoTaskReal ()
			{

				if (indexable != null) {
					driver.Add (indexable);
				} else if (uri != null) {
					driver.Remove (uri);
				} else if (generator != null) {

					// Since this is a generator, we want the task to
					// get re-scheduled after it is run.
					Reschedule = true;

					for (int count = 0; count < hard_wired_generation_count; ++count) {
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
						if (generated != null)
							driver.Add (generated);
					}
				}
			}
		}

		//////////////////////////////////////////////////////////////////////////////////

		private class MarkingClosure {
			LuceneDriver driver;
			string path;
			DateTime mtime;
			
			public MarkingClosure (LuceneDriver driver,
					       string       path,
					       DateTime     mtime)
			{
				this.driver = driver;
				this.path = path;
				this.mtime = mtime;
			}
				
			public void Mark ()
			{
				driver.AttachTimestamp (path, mtime);
			}
		}
				
		
		protected Scheduler.TaskGroup NewMarkingTaskGroup (string path, DateTime mtime)
		{
			MarkingClosure mc = new MarkingClosure (Driver, path, mtime);
			Scheduler.Hook post_hook = new Scheduler.Hook (mc.Mark);
			return Scheduler.NewTaskGroup ("mark " + path, null, post_hook);
		}
	}
}
