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
		private ArrayList allPending = new ArrayList ();
		private bool allPendingSorted = false;

		public CrawlQueue (FileNameFilter _filter, LuceneDriver _driver, Logger _log)
		{
			filter = _filter;
			driver = _driver;
			log = _log;
		}

		/////////////////////////////////////////////////////////////////

		private class Pending : IComparable {
			public string   Path;
			public DateTime LastCrawl;
			public bool     Dirty;

			private int pathLen;

			public Pending (string path)
			{
				Path = path;

				pathLen = -1;
				string x = path;
				do {
					++pathLen;
					x = System.IO.Path.GetDirectoryName (x);
				} while (x != null);
			}

			public int CompareTo (object rhs)
			{
				Pending other = rhs as Pending;			
				if (other == null)
					return 1;

				if (Dirty != other.Dirty) {
					return Dirty ? -1 : 1;
				}

				int cmp = LastCrawl.CompareTo (other.LastCrawl);
				if (cmp != 0)
					return cmp;

				// If all other things are equal, do directories
				// higher up in the tree first.
				return pathLen.CompareTo (other.pathLen);
			}

			override public string ToString ()
			{
				return String.Format ("{0} ({1}) {2} {3}",
						      Path, pathLen,
						      Dirty ? "Dirty" : "clean",
						      LastCrawl);
			}
		}

		const string lastCrawlAttr = "LastCrawl";

		private void SetCrawlTime (string path)
		{
			DirectoryInfo dir = new DirectoryInfo (path);
			if (! dir.Exists)
				return;
			ExtendedAttribute.Set (dir, lastCrawlAttr, StringFu.DateTimeToString (DateTime.Now));
		}

		public void RegisterDirectory (string path)
		{
			DirectoryInfo dir = new DirectoryInfo (path);

			Pending pending = new Pending (path);

			string timeStr = ExtendedAttribute.Get (dir, lastCrawlAttr);
			pending.LastCrawl = StringFu.StringToDateTime (timeStr);

			pending.Dirty = ! driver.IsUpToDate (dir);

			allPending.Add (pending);
			allPendingSorted = false;
		}

		private Pending NextPending ()
		{
			if (! allPendingSorted) {
				allPending.Sort ();
				allPendingSorted = true;
			}

			Pending pending = null;
			bool contains;

			do {
				if (allPending.Count > 0) {
					pending = allPending [0] as Pending;
					allPending.RemoveAt (0);
				}

				lock (crawledPaths) {
					contains = crawledPaths.Contains (pending.Path);
				}
				
			} while (contains);

			return pending;
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

			// FIXME: This should probably happen after the crawl
			// has finished, not when it is scheduled.
			SetCrawlTime (path);
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

		override protected int EmptyQueueTimeoutDuration ()
		{
			// Process a pending directory after 1
			// minute of inactivity.
			return allPending.Count > 0 ? (1000 * 60) : 0;
		}

		override protected void EmptyQueueTimeout ()
		{
			Pending p = NextPending ();
			if (p != null) {
				ScheduleCrawl (p.Path);
			}
		}
	}
}
	
