//
// CrawlQueue.cs
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
using System.Threading;

using Beagle;
using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	class CrawlQueue : ThreadedPriorityQueue {
		
		private FileNameFilter filter;
		private LuceneDriver driver;
		private Logger log;

		private Hashtable crawledPaths = new Hashtable ();

		public CrawlQueue (FileNameFilter _filter, LuceneDriver _driver, Logger _log)
		{
			filter = _filter;
			driver = _driver;
			log = _log;
		}

		/////////////////////////////////////////////////////////////////

		public void ScheduleCrawl (string path, int priority)
		{
			path = Path.GetFullPath (path);
			lock (crawledPaths) {
				if (crawledPaths.Contains (path))
					return;
			}

			if (filter.Ignore (path))
				return;

			Enqueue (path, priority);
		}

		public void ScheduleCrawl (string path)
		{
			ScheduleCrawl (path, 0);
		}
		
		public void ForgetPath (string path)
		{
			path = Path.GetFullPath (path);
			lock (crawledPaths) {
				crawledPaths.Remove (path);
			}
		}

		public void ForgetAll ()
		{
			lock (crawledPaths) {
				crawledPaths.Clear ();
			}
		}

		/////////////////////////////////////////////////////////////////

		override protected void ProcessQueueItem (object item)
		{
			string path = (string) item;

			// We only want to crawl any given path once.
			lock (crawledPaths) {
				if (crawledPaths.Contains (path))
					return;
				crawledPaths [path] = true;
			}

			DirectoryInfo dir = new DirectoryInfo (path);
			if (! dir.Exists)
				return;

			if (log != null)
				log.Info ("Crawling {0}", path);

			// The Lucene Driver checks the EAs on the file
			// and drops the add if it appears to be up-to-date.
			driver.ScheduleAddFile (dir, 0);
			foreach (FileSystemInfo fsinfo in dir.GetFileSystemInfos ()) {
				if (! filter.Ignore (fsinfo.FullName))
					driver.ScheduleAddFile (fsinfo, 0);
			}
		}

		override protected int PostProcessSleepDuration ()
		{
			int n = TopPriority;
			return n < 0 ? -n : 0;
		}
	}
}
	
