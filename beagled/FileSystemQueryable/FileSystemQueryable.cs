//
// FileSystemQueryable.cs
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
using System.Reflection;
using System.Text;
using System.Threading;


using Beagle.Daemon;
using Beagle.Util;


namespace Beagle.Daemon.FileSystemQueryable {

	[QueryableFlavor (Name="Files", Domain=QueryDomain.Local)]
	public class FileSystemQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("FileSystemQueryable");

		private FileNameFilter filter;
		private CrawlQueue crawlQ;
		private EventStatistics eventStats;

		public FileSystemQueryable () : base (Path.Combine (PathFinder.RootDir, "FileSystemIndex"))
		{
			filter = new FileNameFilter ();			
			crawlQ = new CrawlQueue (filter, Driver, log);
			eventStats = new EventStatistics ();

			Inotify.InotifyEvent += new InotifyHandler (OnInotifyEvent);
		}

		//////////////////////////////////////////////////////////////////////////

		public void StartWorker ()
		{
			string home = Environment.GetEnvironmentVariable ("HOME");

			TraverseDirectory (home, true);

			// Crawl a few important directories right away, just to be sure.
			// FIXME: This list shouldn't be hard-wired
			crawlQ.ScheduleCrawl (home);
			crawlQ.ScheduleCrawl (Path.Combine (home, "Desktop"));
			crawlQ.ScheduleCrawl (Path.Combine (home, "Documents"));
			Shutdown.AddQueue (crawlQ);
			crawlQ.Start ();

			log.Info ("FileSystemQueryable start-up thread finished");
			
			// FIXME: Do we need to re-run queries when we are fully started?
		}

		public override void Start ()
		{
			base.Start ();

			Thread th = new Thread (new ThreadStart (StartWorker));
			th.Start ();
		}

		//////////////////////////////////////////////////////////////////////////

		// Filter out hits where the files seem to no longer exist.
		override protected bool HitIsValid (Hit hit)
		{
			string path = hit.Uri.LocalPath;
			return File.Exists (path) || Directory.Exists (path);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Inotify-related code
		//

		private Hashtable watchedPaths = new Hashtable ();
		private Hashtable blockedPaths = new Hashtable ();

		private void Watch (string path)
		{
			Inotify.Watch (path, 
				       InotifyEventType.Open
				       | InotifyEventType.CreateSubdir
				       | InotifyEventType.DeleteSubdir
				       | InotifyEventType.DeleteFile
				       | InotifyEventType.CloseWrite
				       | InotifyEventType.QueueOverflow);
		}

		// Do a breadth-first traversal of the home directory,
		// setting up watches and scheduling crawls.
		private void TraverseDirectory (string root, bool isInitial)
		{
			Queue queue = new Queue ();
			int count = 0;

			log.Info ("Watching {0}", root);
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();
			
			queue.Enqueue (root);

			while (queue.Count > 0) {
				++count;
				string path = (string) queue.Dequeue ();
			
				DirectoryInfo dir = new DirectoryInfo (path);
				if (dir.Exists) {
					blockedPaths [path] = true;
					watchedPaths [path] = true;
					Watch (path);

					if (isInitial)
						crawlQ.RegisterDirectory (path);
					else
						crawlQ.ScheduleCrawl (path, 0);

					foreach (DirectoryInfo subdir in dir.GetDirectories ()) {
						if (! filter.Ignore (subdir.FullName)
						    && ! FileSystem.IsSymLink (subdir.FullName))
							queue.Enqueue (subdir.FullName);
					}
				}

				blockedPaths.Remove (path);
			}

			stopwatch.Stop ();

			log.Info ("Watched {0} director{1} in {2}", 
				  count, count == 1 ? "y" : "ies", stopwatch);
		}

		private void OnInotifyEvent (int              wd,
					     string           path,
					     string           subitem,
					     InotifyEventType type,
					     int              cookie)
		{
			// Having to do a hash table lookup per event sucks.
			// It would be nicer if there was a faster way to do
			// this lookup.
			if (! watchedPaths.Contains (path))
				return;

			string fullPath = Path.Combine (path, subitem);
			if (blockedPaths.Contains (fullPath) || filter.Ignore (fullPath))
				return;

			// Handle a few simple event types
			switch (type) {

			case InotifyEventType.CreateSubdir:
				TraverseDirectory (fullPath, false);
				return;
				
			case InotifyEventType.QueueOverflow:
				// If the queue overflows, we can't make any
				// assumptions about the state of the file system.
				crawlQ.ForgetAll ();
				return;

			}

			eventStats.AddEvent (path);

			// Try to crawl any path we see activity in.
			// We only crawl each directory once, but that is enforced
			// inside of the CrawlQueue.
			if (subitem != "") {
				crawlQ.ScheduleCrawl (path, 0);
			}

			// Handle the events that trigger indexing operations.
			switch (type) {

			case InotifyEventType.DeleteSubdir:
			case InotifyEventType.DeleteFile:
				crawlQ.ForgetPath (fullPath);
				eventStats.ForgetPath (fullPath);
				Driver.ScheduleDeleteFile (fullPath, 100);
				break;

			case InotifyEventType.CloseWrite:
				Driver.ScheduleAddFile (FileSystem.New (fullPath), 100);
				break;

			}
		}

		//////////////////////////////////////////////////////////////////////////

		public override string GetHumanReadableStatus ()
		{
			StringBuilder builder = new StringBuilder ();
			string str;

			builder.Append ("Crawl Queue:\n");
			str = String.Format ("{0} pending director{1} to crawl.\n",
					     crawlQ.PendingCount,
					     crawlQ.PendingCount == 1 ? "y" : "ies");
			builder.Append (str);
			crawlQ.GetHumanReadableStatus (builder);

			builder.Append ("\n\n");
			builder.Append ("Indexing Queue:\n");
			builder.Append (base.GetHumanReadableStatus ());

			return builder.ToString ();
		}


	}
}
	
