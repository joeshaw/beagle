//
// IndexerQueue.cs
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

using DBus;
using System;
using System.Collections;
using System.Threading;
using Beagle;

namespace Beagle.Daemon
{
	public class IndexerQueue {
		
		public delegate void PreIndexingHandler (PreIndexHandlerArgs a);
		public event PreIndexingHandler PreIndexingEvent;

		public delegate void PostIndexingHandler (PostIndexHandlerArgs a);
		public event PostIndexingHandler PostIndexingEvent;		

		IndexDriver driver = new MainIndexDriver ();

		// Contains Indexables
		ArrayList toBeIndexed = new ArrayList ();

		// Contains Obsoleted or Deleted Hits
		ArrayList toBeRemoved = new ArrayList ();

		private object queueLock = new object ();
		
		int sinceOptimize = 0;
		const int optimizeCount = 100;

		uint flushTimeout = 0;
		bool haveWorker = false;

		DateTime lastFlush = new DateTime (0);

		void CallPostIndexingEvent (ArrayList indexables)
		{
			if (PostIndexingEvent == null)
				return;

			PostIndexHandlerArgs args = new PostIndexHandlerArgs ();
			args.indexables = indexables;
			PostIndexingEvent (args);
		}

		ArrayList CallPreIndexingEvent (ArrayList indexables)
		{
			if (PreIndexingEvent == null) 
				return indexables;

			ArrayList ret = new ArrayList ();
			PreIndexHandlerArgs args = new PreIndexHandlerArgs ();
			foreach (Indexable i in indexables) {
				args.indexable = i;
				args.shouldIndex = true;
				PreIndexingEvent (args);
				if (args.shouldIndex)
					ret.Add (i);
			}
			return ret;
		}

		private void CleanupContent (ArrayList toIndex) {
			foreach (Indexable indexable in toIndex) {
				if (indexable.DeleteContent) {
					string path = indexable.ContentUri;
					if (path.StartsWith ("file://")) {
						path = path.Substring ("file://".Length);
					}
					Console.WriteLine ("deleting {0}", path);
					System.IO.File.Delete (path);
				}
			}
		}

		void Flush (ArrayList toIndex, ArrayList toRemove) {
			bool didSomething = false;
			
			System.Console.WriteLine ("flushing {0}", toIndex.Count);

			ArrayList toCleanup = toIndex;
			toIndex = CallPreIndexingEvent (toIndex); 
			if (toIndex.Count > 0) {
				driver.QuickAdd (toIndex);
				didSomething = true;
			}
			
			if (toRemove.Count > 0) {
				driver.Remove (toRemove);
				didSomething = true;
			}

			if (didSomething) {
				++sinceOptimize;

				if (sinceOptimize > optimizeCount) {
					driver.Optimize ();
					sinceOptimize = 0;
				}
			}

			CallPostIndexingEvent (toIndex);

			CleanupContent (toCleanup);

			System.Console.WriteLine ("done flushing", toIndex.Count);
		}

		void FlushCallback (object state) 
		{
			try {
				ArrayList toIndex; 
				ArrayList toRemove;
				
				lock (queueLock) {
					toIndex = toBeIndexed;
					toRemove = toBeRemoved;
					
					toBeIndexed = new ArrayList ();
					toBeRemoved = new ArrayList ();

					lastFlush = DateTime.Now;
					
					if (toIndex.Count == 0 && toRemove.Count == 0) {
						return;
					}
				}
			
				Flush (toIndex, toRemove);
			} catch (Exception e){
				System.Console.WriteLine ("Exception in the worker thread");
				System.Console.WriteLine (e);
			} finally {
				lock (queueLock) {
					System.Console.WriteLine ("Done flushing");
					haveWorker = false;
				}
			}
		}
			
		bool ScheduleFlush ()
		{
			lock (queueLock) {
				// If there is no work to do, we're done
				if (toBeIndexed.Count == 0 && toBeRemoved.Count == 0) {
					if (flushTimeout != 0) {
						Gtk.Timeout.Remove (flushTimeout);
						flushTimeout = 0;
					}
					return false;
				}

				if (lastFlush.AddSeconds(1) > DateTime.Now
				    || haveWorker) {
					if (flushTimeout == 0) {
						flushTimeout = Gtk.Timeout.Add (1000, new Gtk.Function (ScheduleFlush));
					}
					return true;
				}
				
				haveWorker = true;

				WaitCallback callback = new WaitCallback (FlushCallback);
				ThreadPool.QueueUserWorkItem (callback);
				
				if (flushTimeout != 0) {
					Gtk.Timeout.Remove (flushTimeout);
					flushTimeout = 0;
				}
				return false;
			}
		}

		public void ScheduleAdd (Indexable indexable)
		{
			lock (queueLock) {
				toBeIndexed.Add (indexable);
				System.Console.WriteLine ("scheduled an add: {0}", toBeIndexed.Count);

			}
			ScheduleFlush ();
		}

		public void ScheduleRemove (Hit hit)
		{
			if (hit == null)
				return;
			lock (queueLock) {
				toBeRemoved.Add (hit);
				System.Console.WriteLine ("scheduled a remove: {0}", toBeRemoved.Count);
			}
			ScheduleFlush ();
		}
	}
}
