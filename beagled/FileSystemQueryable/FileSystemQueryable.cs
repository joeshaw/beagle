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
using System.Threading;


using Beagle.Daemon;
using Beagle.Util;


namespace Beagle.Daemon.FileSystemQueryable {

	[QueryableFlavor (Name="Files", Domain=QueryDomain.Local)]
	public class FileSystemQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("FileSystemQueryable");

		FileNameFilter filter;
		CrawlQueue crawlQ;
		Hashtable dirtyFiles = new Hashtable ();

		public FileSystemQueryable () : base (Path.Combine (PathFinder.RootDir, "FileSystemIndex"))
		{

			string home = Environment.GetEnvironmentVariable ("HOME");
			
			filter = new FileNameFilter ();
			crawlQ = new CrawlQueue (filter, Driver, log);

			TraverseDirectory (home);

			// Crawl a few important directories right away, just to be sure.
			// FIXME: This list shouldn't be hard-wired
			crawlQ.ScheduleCrawl (home);
			crawlQ.ScheduleCrawl (Path.Combine (home, "Desktop"));
			crawlQ.ScheduleCrawl (Path.Combine (home, "Documents"));
			Shutdown.AddQueue (crawlQ);
			crawlQ.Start ();

			Inotify.InotifyEvent += new InotifyHandler (OnInotifyEvent);
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
				       InotifyEventType.All & (~ InotifyEventType.Access));
		}

		// Do a breadth-first traversal of the home directory,
		// setting up watches and scheduling crawls.
		private void TraverseDirectory (string root)
		{
			Queue queue = new Queue ();
			int count = 0;

			log.Info ("Walking {0}", root);
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

					foreach (DirectoryInfo subdir in dir.GetDirectories ()) {
						if (! filter.Ignore (subdir.FullName))
							queue.Enqueue (subdir.FullName);
					}
				}

				blockedPaths.Remove (path);
			}

			stopwatch.Stop ();

			log.Info ("Processed {0} directories in {1}", count, stopwatch);
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
				TraverseDirectory (fullPath);
				return;
				
			case InotifyEventType.CreateFile:
			case InotifyEventType.Modify:
				dirtyFiles [fullPath] = true;
				return;

			case InotifyEventType.QueueOverflow:
				// If the queue overflows, we can't make any
				// assumptions about the state of the file system.
				crawlQ.ForgetAll ();
				return;

			}

			// Try to crawl any path we see activity in.
			// We only crawl each directory once, but that is enforced
			// inside of the CrawlQueue.
			crawlQ.ScheduleCrawl (path, 0);

			// Handle the events that trigger indexing operations.
			switch (type) {

			case InotifyEventType.DeleteSubdir:
			case InotifyEventType.DeleteFile:
				Driver.ScheduleDeleteFile (fullPath, 100);
				dirtyFiles.Remove (fullPath);
				break;

			case InotifyEventType.Close:
				if (dirtyFiles.Contains (fullPath)) {
					Driver.ScheduleAddFile (FileSystem.New (fullPath), 100);
					dirtyFiles.Remove (fullPath);
				} else {
					log.Debug ("{0} is not dirty", fullPath);
				}
				break;

			}
		}


	}
}
	
