//
// Crawler.cs
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

using Mono.Posix;

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public abstract class Crawler {
		
		private string fingerprint;

		private Thread queueThread = null;
		private object queueLock = new object ();
		private ArrayList queue = new ArrayList ();
		private bool queueStop = false;
		private bool queueStopWhenEmpty = false;
		private string nowCrawling = null;

		const string fingerprintAttr = "Fingerprint";
		const string mtimeAttr = "MTime";


		/////////////////////////////////////////////////////////////

		public Crawler (string _fingerprint)
		{
			fingerprint = _fingerprint;

			StartQueue ();
		}


		/////////////////////////////////////////////////////////////
		
		//
		// Public API
		//

		public void ScheduleCrawl (FileSystemInfo info)
		{
			lock (queueLock) {

				if (queueStop) {
					throw new Exception (String.Format ("Attempt to schedule crawl of {0} in a stopped crawler", 
									    info.FullName));
				}

				// Filter out things we don't want to crawl.
				if (SkipByName (info))
					return;

				// Filter out duplicate crawl requests.
				if (nowCrawling == info.FullName)
					return;

				foreach (FileSystemInfo other in queue) {
					if (other.FullName == info.FullName)
						return;
				}

				// Add the item to the queue and pulse the lock.
				queue.Add (info);
				Monitor.Pulse (queueLock);
			}
		}

		public void ScheduleCrawl (string path)
		{
			if (Directory.Exists (path))
				ScheduleCrawl (new DirectoryInfo (path));
			else if (File.Exists (path))
				ScheduleCrawl (new FileInfo (path));
		}

		public void Stop ()
		{
			lock (queueLock) {
				queueStop = true;
				Monitor.Pulse (queueLock);
			}
		}

		public void StopWhenEmpty ()
		{
			lock (queueLock) {
				queueStopWhenEmpty = true;
				Monitor.Pulse (queueLock);
			}
		}


		/////////////////////////////////////////////////////////////

		//
		// Fill these functions in
		//

		protected virtual bool SkipFileByName (DirectoryInfo dir, string name)
		{
			return false;
		}

		protected virtual bool SkipDirectoryByName (DirectoryInfo parent, string name)
		{
			return false;
		}

		protected abstract void CrawlFile (FileSystemInfo info);
		

		/////////////////////////////////////////////////////////////

		//
		// Implementation Details
		//

		protected bool SkipByName (FileSystemInfo info)
		{
			if (info is FileInfo) {
				FileInfo file = (FileInfo) info;
				return SkipFileByName (file.Directory, file.Name);
			} else if (info is DirectoryInfo) {
				DirectoryInfo dir = (DirectoryInfo) info;
				return SkipDirectoryByName (dir.Parent, dir.Name);
			} else
				return true;
		}

		// Check if a file is a symlink.
		private static bool IsSymLink (FileSystemInfo info)
		{
			Stat stat = new Stat ();
			Syscall.lstat (info.FullName, out stat);
			int mode = (int) stat.Mode & (int)StatModeMasks.TypeMask;
			return mode == (int) StatMode.SymLink;
		}


		// Check if a file needs to be crawled.
		private bool NeedsCrawl (FileSystemInfo info)
		{
			if (! info.Exists)
				return false;

			if (IsSymLink (info))
				return false;

			if (BU.ExtendedAttribute.Check (info, fingerprint))
				return false;

			return true;
		}
	       
		// Launch a thread to manage the queue.
		private void StartQueue ()
		{
			lock (this) {
				if (queueThread != null)
					return;
				queueThread = new Thread (new ThreadStart (WorkQueue));
				queueThread.Start ();
			}
		}

		private void WorkQueue ()
		{
			while (true) {
				
				// Get the next item to crawl.  If necessary,
				// wait until an item becomes available.
				FileSystemInfo info = null;
				lock (queueLock) {
					if (queue.Count == 0) {
						if (queueStopWhenEmpty) {
							queueStop = true;
							return;
						}
						Monitor.Wait (queueLock);
					}
					if (queue.Count > 0) {
						info = (FileSystemInfo) queue [0];
						queue.RemoveAt (0);
						nowCrawling = info.FullName;
					}
				}

				if (queueStop)
					return;

				if (info == null)
					continue;

				// If this item is just a file, just crawl it
				// individually.
				if (info is FileInfo) {

					if (NeedsCrawl (info))
						CrawlFile (info);

				} else if (info is DirectoryInfo) {

					DirectoryInfo dir = (DirectoryInfo) info;
					
					if (! SkipDirectoryByName (dir.Parent, dir.FullName)) {

						if (NeedsCrawl (dir))
							CrawlFile (dir);
						
						foreach (FileInfo file in dir.GetFiles ()) {
							if (NeedsCrawl (file) && ! SkipFileByName (dir, info.FullName))
								CrawlFile (file);
						}

						foreach (DirectoryInfo subdir in dir.GetDirectories ())
							ScheduleCrawl (subdir);
					}
				}
				
				lock (queueLock) {
					nowCrawling = null;
				}
			}
		}
	}
}
