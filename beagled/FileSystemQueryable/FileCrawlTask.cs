//
// FileCrawlTask.cs
//
// Copyright (C) 2005 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.IO;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public class FileCrawlTask : Scheduler.Task {

		object big_lock = new object ();
		FileSystemQueryable queryable;
		DirectoryModel current_dir = null;
		IIndexableGenerator current_generator = null;
		bool is_active = false;

		Scheduler.Hook our_post_hook;

		public FileCrawlTask (FileSystemQueryable queryable)
		{
			this.queryable = queryable;
			this.Tag = "File Crawler";
			this.Priority = Scheduler.Priority.Delayed;

			this.our_post_hook = new Scheduler.Hook (PostCrawlHook);
		}

		public bool IsActive {
			get { lock (big_lock) return is_active; }
		}

		private void PostCrawlHook ()
		{
			Logger.Log.Debug ("Done crawling '{0}'", current_dir.FullName);

			queryable.DoneCrawlingOneDirectory (current_dir);

			current_generator = null;
			current_dir = null;
		}

		override protected void DoTaskReal ()
		{
			// If our last generator is still doing stuff, just reschedule
			// and return.  This keeps us from generating more tasks until
			// the last one we started runs to completion.
			if ((current_generator != null && current_generator.HasNextIndexable ())
				|| current_dir != null) {
				Reschedule = true;
				return;
			}

			lock (big_lock) {
				is_active = true;
				current_dir = queryable.GetNextDirectoryToCrawl ();
				if (current_dir == null) {
					Logger.Log.Debug ("Done crawling!!!!");
					is_active = false;
					return;
				}
			}
			
			if (!current_dir.IsAttached) {
				Reschedule = true;
				return;
			}

			if (FileSystemQueryable.Debug)
				Logger.Log.Debug ("Starting crawl of '{0}'", current_dir.FullName);

			// Schedule a DirectoryIndexableGenerator
			// for that directory, and then reschedule ourselves.
			try {
				current_generator = new DirectoryIndexableGenerator (queryable, current_dir);
			} catch (DirectoryNotFoundException ex) {
				Logger.Log.Debug ("Couldn't crawl '{0}'", current_dir.FullName);

				// FIXME: If our attempt to crawl the directory fails, just
				// mark it as uncrawlable and move on.  This isn't optimal behavior,
				// but works around bugs involving weird permissions for now.
				current_dir.MarkAsUncrawlable ();
				current_dir = null;
			}
			
			if (current_generator != null) {
				Scheduler.TaskGroup group;
				group = Scheduler.NewTaskGroup ("Crawl task group", null, our_post_hook);

				Scheduler.Task task;
				task = queryable.NewAddTask (current_generator);
				task.AddTaskGroup (group);
				SpawnChild (task);
			}

			Reschedule = true;
		}


	}
}
