//
// CrawlTask.cs
//
// Copyright (C) 2005 Novell, Inc.
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

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Daemon.FileSystemQueryable {

	public class CrawlTask : Scheduler.Task {

		LuceneQueryable queryable;
		FileSystemModel model;
		IIndexableGenerator current_generator;

		public CrawlTask (LuceneQueryable queryable,
				  FileSystemModel model)
		{
			this.queryable = queryable;
			this.model = model;
			this.Tag = "File System Crawler";
			this.Priority = Scheduler.Priority.Generator;
		}

		private class PostCrawlClosure {
			public FileSystemModel Model;
			public FileSystemModel.Directory Directory;
			public DateTime CrawlTime;

			public void Hook ()
			{
				Model.MarkAsCrawled (Directory, CrawlTime);
			}
		}
			

		protected override void DoTaskReal ()
		{
			// If our last generator is still doing stuff, just reschedule
			// and return.  This keeps us from generating more tasks until
			// the last one we started runs to completion.
			if (current_generator != null && current_generator.HasNextIndexable ()) {
				Reschedule = true;
				return;
			}

			current_generator = null;

			FileSystemModel.Directory next_dir = model.GetNextDirectoryToCrawl ();
			if (next_dir == null)
				return;

			int uncrawled, dirty;
			model.GetUncrawledCounts (out uncrawled, out dirty);
			this.Description = String.Format ("last={0}, uncrawled={1}, dirty={2})",
							  next_dir.FullName, uncrawled, dirty);

			Logger.Log.Debug ("Crawl Task Scheduling {0} (state={1})", next_dir.FullName, next_dir.State);

			// We want this task to get re-scheduled after it is run.
			Reschedule = true;

			// ...but if we are crawling a possibly-clean directory,
			// maybe wait a little bit extra.
			// FIXME: This will not respect BEAGLE_EXERCISE_THE_DOG
			TriggerTime = DateTime.Now.AddSeconds (5);

			// Set up a task group to mark the time on the directory
			// after we finish crawling it.
			PostCrawlClosure closure = new PostCrawlClosure ();
			closure.Model = model;
			closure.Directory = next_dir;
			closure.CrawlTime = DateTime.Now;
			Scheduler.TaskGroup group = Scheduler.NewTaskGroup ("Crawl " + next_dir.FullName, null,
									    new Scheduler.Hook (closure.Hook));

			// Construct an indexable generator and add it to the scheduler
			current_generator = new DirectoryIndexableGenerator (model, next_dir);
			Scheduler.Task task = queryable.NewAddTask (current_generator);
			task.AddTaskGroup (group);
			ThisScheduler.Add (task, Scheduler.AddType.OptionallyReplaceExisting);
		}
	}
}
