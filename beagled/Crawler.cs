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

		private static BU.Logger log = BU.Logger.Get ("crawler");

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

		private class PendingCrawl {
			public FileSystemInfo FileSystemInfo;
			public int MaxDepth;
		}

		// Note: This should only be called by code that is holding the queueLock.
		// Otherwise there is a race condition, since duplicate queue items could be
		// added to the queue between the call to BuildPendingCrawl and the
		// actual insertion into the queue.
		private PendingCrawl BuildPendingCrawl (FileSystemInfo info, int maxDepth)
		{
			// Filter out things we don't want to crawl.
			if (SkipByName (info))
				return null;

			// Filter out duplicate crawl requests.
			if (nowCrawling == info.FullName)
				return null;

			foreach (PendingCrawl other in queue) {
				if (other.FileSystemInfo.FullName == info.FullName)
					return null;
			}

			PendingCrawl pending = new PendingCrawl ();
			pending.FileSystemInfo = info;
			pending.MaxDepth = maxDepth;
			
			return pending;
		}

		/////////////////////////////////////////////////////////////

		//
		// Public API
		//

		// Use maxDepth == -1 for no limit.
		public void ScheduleCrawl (FileSystemInfo info, int maxDepth)
		{
			lock (queueLock) {

				if (queueStop) {
					throw new Exception (String.Format ("Attempt to schedule crawl of {0} in a stopped crawler", 
									    info.FullName));
				}

				PendingCrawl pending = BuildPendingCrawl (info, maxDepth);
				if (pending == null)
					return;

				// Add the item to the queue and pulse the lock.
				queue.Add (pending);
				Monitor.Pulse (queueLock);
			}
		}
		
		// Use maxDepth == -1 for no limit.
		public void SchedulePriorityCrawl (FileSystemInfo info, int maxDepth)
		{
			lock (queueLock) {
				if (queueStop) {
					throw new Exception (String.Format ("Attempt to schedule crawl of {0} in a stopped crawler", 
									    info.FullName));
				}

				PendingCrawl pending = BuildPendingCrawl (info, maxDepth);
				if (pending == null)
					return;

				// Add the item to the queue and pulse the lock.
				queue.Insert (0, pending);
				Monitor.Pulse (queueLock);
			}
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

		protected virtual bool SkipByName (FileSystemInfo info)
		{
			return false;
		}

		protected abstract void CrawlFile (FileSystemInfo info);


		/////////////////////////////////////////////////////////////

		//
		// Implementation Details
		//

		// Check if a file needs to be crawled.
		private bool NeedsCrawl (FileSystemInfo info)
		{
			if (! info.Exists)
				return false;

			if (BU.FileSystem.IsSymLink (info.FullName))
				return false;

			if (BU.ExtendedAttribute.Check (info.FullName, fingerprint))
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
			if (!Shutdown.WorkerStart (this))
				return;
			try {

			while (true) {
				
				// Get the next item to crawl.  If necessary,
				// wait until an item becomes available.
				PendingCrawl pending = null;
				lock (queueLock) {
					if (queue.Count == 0) {
						if (queueStopWhenEmpty) {
							queueStop = true;
							return;
						}
						Monitor.Wait (queueLock);
					}
					if (queue.Count > 0) {
						pending = queue [0] as PendingCrawl;
						queue.RemoveAt (0);
						nowCrawling = pending.FileSystemInfo.FullName;
						log.Debug ("Crawling {0}", nowCrawling);
					}
				}

				if (queueStop) {
					return;
				}

				if (pending == null)
					continue;

				FileSystemInfo info = pending.FileSystemInfo;

				// If this item is just a file, just crawl it
				// individually.
				if (info is FileInfo) {

					if (NeedsCrawl (info))
						CrawlFile (info);

				} else if (info is DirectoryInfo) {

					DirectoryInfo dir = (DirectoryInfo) info;
					
					if (! SkipByName (dir) && dir.Exists) {

						if (NeedsCrawl (dir))
							CrawlFile (dir);
						
						foreach (FileInfo file in dir.GetFiles ()) {
							if (NeedsCrawl (file) && ! SkipByName (file))
								CrawlFile (file);
						}

						foreach (DirectoryInfo subdir in dir.GetDirectories ()) {
							if (pending.MaxDepth != 0) 
								ScheduleCrawl (subdir, pending.MaxDepth - 1);
							else {
								// Just crawl the directories as files, don't descend into them.
								if (NeedsCrawl (subdir) && ! SkipByName (subdir))
									CrawlFile (subdir);
							}
						}


					}
				}
				
				lock (queueLock) {
					nowCrawling = null;
				}
			}
			} finally {
				Shutdown.WorkerFinished (this);
			}
		}
	}
}
