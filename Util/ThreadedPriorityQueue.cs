//
// ThreadedPriorityQueue.cs
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
using System.Threading;

namespace Beagle.Util {

	public abstract class ThreadedPriorityQueue {

		private SortedList priorityQueue = new SortedList ();
		private int count;
		private Thread thread;
		private bool running = true;
		private bool paused = false;
		private Logger log = null;

		public delegate void WorkerStartHandler (object o);
		public event WorkerStartHandler WorkerStartEvent;
		public delegate void WorkerFinishedHandler (object o);
		public event WorkerFinishedHandler WorkerFinishedEvent;
		
		public ThreadedPriorityQueue ()
		{
		}

		public Logger Log {
			get { return log; }
			set { log = value; }
		}

		///////////////////////////////////////////////////////////////////

		public void Start ()
		{
			thread = new Thread (new ThreadStart (QueueWorker));
			thread.Start ();
		}

		///////////////////////////////////////////////////////////////////

		// FIXME: This implementation of a priority queue is very
		// inefficient if the number of distinct priority values used
		// is large.

		// If no priority is specified, revert to zero.
		public void Enqueue (object item)
		{
			Enqueue (item, 0);
		}

		public void Enqueue (object item, int priority)
		{
			//if (log != null)
			//  log.Info ("Enqueued {0} with priority {1}", item, priority);

			lock (priorityQueue) {
				Queue queue = priorityQueue [priority] as Queue;
				if (queue == null) {
					queue = new Queue ();
					priorityQueue [priority] = queue;
				}
				queue.Enqueue (item);
				++count;
				Monitor.Pulse (priorityQueue);
			}
		}

		private object Dequeue ()
		{
			object item;

			lock (priorityQueue) {
				int i = priorityQueue.Count - 1;
				if (i < 0)
					return null;

				Queue queue = priorityQueue.GetByIndex (i) as Queue;
				item = queue.Dequeue ();
				--count;
				if (queue.Count == 0)
					priorityQueue.RemoveAt (i);
			}

			//if (log != null)
			//  log.Info ("Dequeued {0}", item);
			return item;
		}

		public int Count {
			get { return count; }
		}

		public int TopPriority {
			get {
				lock (priorityQueue) {
					int i = priorityQueue.Count - 1;
					return (i >= 0) ? (int) priorityQueue.GetKey (i) : 0;
				}
			}
		}

		public void Shutdown ()
		{
			running = false;
			Enqueue (null); // Enqueue a bogus item to pulse the lock
		}

		public void Pause ()
		{
			paused = true;
		}

		public void Resume ()
		{
			if (paused) {
				paused = false;
				Enqueue (null); // Enqueue a bogus item to pulse the lock
			}
		}

		///////////////////////////////////////////////////////////////////

		protected abstract void ProcessQueueItem (object item);

		protected virtual int PostProcessSleepDuration ()
		{
			return 0;
		}

		protected virtual int EmptyQueueTimeoutDuration ()
		{
			return 0;
		}

		protected virtual void EmptyQueueTimeout ()
		{

		}

		///////////////////////////////////////////////////////////////////


		private void QueueWorker ()
		{
			if (WorkerStartEvent != null) 
				WorkerStartEvent (this);
			while (running) {

				if (paused) {
					lock (priorityQueue)
						Monitor.Wait (priorityQueue);
					continue;
				}

				// Pull the highest-priority item off of the queue.
				object item = Dequeue ();
				int sleep = 0;

				// If we found a non-null item, process it.
				if (item != null) {

					try {
						ProcessQueueItem (item);
					} catch (Exception e) {
						if (log != null)
							log.Warn (e);
					}


					// Afterwards, maybe sleep a little bit.
					// Our sleep will be interrupted if anything
					// is added to the queue.
					try {
						sleep = PostProcessSleepDuration ();
					} catch (Exception e) {
						if (log != null)
							log.Warn (e);
					}

					if (sleep > 0) {
						lock (priorityQueue) {
							// Only sleep if the queue is non-empty.
							if (Count > 0) 
								Monitor.Wait (priorityQueue, sleep);
						}
					}
				}

				// Wait for the next queue item.
				// We might want to perform some sort of
				// action only if our queue is empty and stays
				// empty for a given amount of time.
				while (Count == 0 && running) {
					
					bool timedOut = false;
					int timeout = 0;
						
					try {
						timeout = EmptyQueueTimeoutDuration ();
					} catch (Exception e) {
						if (log != null)
							log.Warn (e);
					}

					lock (priorityQueue) {
						if (Count == 0) {
							if (timeout > 0) {
								// Take the time we spent sleeping into account
								if (timeout > sleep) {
									Monitor.Wait (priorityQueue, timeout - sleep);
									sleep = 0;
								}
								if (Count == 0)
									timedOut = true;
							} else {
								Monitor.Wait (priorityQueue);
							}
						}
					}

					if (timedOut && running) {
						try {
							EmptyQueueTimeout ();
						} catch (Exception e) {
							if (log != null)
								log.Warn (e);
						}
					}
				}
			}
			if (WorkerFinishedEvent != null)
				WorkerFinishedEvent (this);
		}
	}
}
	
