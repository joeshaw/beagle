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
using System.Text;
using System.Threading;

namespace Beagle.Util {

	public abstract class ThreadedPriorityQueue {

		const double expma_decay = 0.1;

		private SortedList priority_queue = new SortedList ();
		private int count;
		private Thread thread;
		private bool running = true;
		private bool paused = false;
		private Logger log = null;

		private int processed_count = 0;
		private DateTime last_processed_time;
		private double total_processing_time = 0;
		private double expma_processing_time = 0;
		private double total_gap_time = 0;
		private double expma_gap_time = 0;

		public class WorkerStartArgs 
		{
			public bool Success;
		}

		public delegate void WorkerStartHandler (object o,
							 WorkerStartArgs args);
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
			if (thread != null)
				return;

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

			lock (priority_queue) {
				Queue queue = priority_queue [priority] as Queue;
				if (queue == null) {
					queue = new Queue ();
					priority_queue [priority] = queue;
				}
				queue.Enqueue (item);
				++count;
				Monitor.Pulse (priority_queue);
			}
		}

		private Queue NextSubQueue ()
		{
			Queue queue = null;
				
			lock (priority_queue) {
				while (queue == null) {
					int i = priority_queue.Count - 1;
					if (i < 0)
						return null;
					queue = priority_queue.GetByIndex (i) as Queue;
					if (queue == null || queue.Count == 0) {
						priority_queue.RemoveAt (i);
						queue = null;
					}
				}
			}

			return queue;
		}

		private object PeekSubQueue (Queue queue)
		{
			lock (priority_queue) {
				return queue != null && queue.Count > 0 ? queue.Peek () : null;
			}
		}

		private object DequeueSubQueue (Queue queue)
		{
			object item = null;

			lock (priority_queue) {
				if (queue != null && queue.Count > 0) {
					item = queue.Dequeue ();
					--count;
				}
			}
			
			return item;
		}

		public int Count {
			get { return count; }
		}

		public int TopPriority {
			get {
				lock (priority_queue) {
					int i = priority_queue.Count - 1;
					return (i >= 0) ? (int) priority_queue.GetKey (i) : 0;
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

		// Return true to dequeue the item after processing.
		// If false, the item will remain in the queue and will show
		// up again in future ProcessQueueItem calls.
		protected abstract bool ProcessQueueItem (object item);

		protected virtual bool PeekAtQueueItem (object item)
		{
			return true;
		}

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
			WorkerStartArgs args = new WorkerStartArgs ();
			if (WorkerStartEvent != null) 
				WorkerStartEvent (this, args);
			if (!args.Success) 
			{
				return;
			}
			try {
			while (running) {

				if (paused) {
					lock (priority_queue)
						Monitor.Wait (priority_queue);
					continue;
				}

				// Pull the highest-priority subqueue off of the queue.
				Queue subqueue = NextSubQueue ();
				object item = PeekSubQueue (subqueue);

				int sleep = 0;

				// If we found a non-null item, process it.
				if (item != null) {

					bool needDequeue = true;

					double t = 0;

					DateTime start_time = DateTime.Now;

					try {
						needDequeue = ProcessQueueItem (item);
						DateTime end_time = DateTime.Now;

						t = (end_time - start_time).TotalSeconds;

					} catch (Exception e) {
						if (log != null)
							log.Warn (e);
					}

					if (t > 0) {
						
						if (processed_count > 0) {
							double t_gap;
							t_gap = (start_time - last_processed_time).TotalSeconds;
							total_gap_time += t_gap;
							expma_gap_time = expma_decay * t_gap + (1 - expma_decay) * expma_gap_time;
						}

						++processed_count;
						total_processing_time += t;
						expma_processing_time = expma_decay * t + (1 - expma_decay) * expma_processing_time;
					}
					
					last_processed_time = DateTime.Now;

					if (needDequeue)
						DequeueSubQueue (subqueue);


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
						lock (priority_queue) {
							// Only sleep if the queue is non-empty.
							if (Count > 0) 
								Monitor.Wait (priority_queue, sleep);
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

					lock (priority_queue) {
						if (Count == 0) {
							if (timeout > 0) {
								// Take the time we spent sleeping into account
								if (timeout > sleep) {
									Monitor.Wait (priority_queue, timeout - sleep);
									sleep = 0;
								}
								if (Count == 0)
									timedOut = true;
							} else {
								Monitor.Wait (priority_queue);
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
			} finally {
				if (WorkerFinishedEvent != null)
					WorkerFinishedEvent (this);
			}
		}

		///////////////////////////////////////////////////////////////////

		public void GetHumanReadableStatus (StringBuilder builder)
		{
			string str;

			lock (priority_queue) {

				str = String.Format ("{0} item{1} processed.\n",
						     processed_count,
						     processed_count == 1 ? "" : "s");
				builder.Append (str);
				
				if (processed_count > 0) {

					double t = (DateTime.Now - last_processed_time).TotalSeconds;
					str = String.Format ("Time since last item: {0:0.0}s\n", t);
					builder.Append (str);

					str = String.Format ("Average processing time: {0:0.000}s\n",
							     total_processing_time / processed_count);
					builder.Append (str);

					str = String.Format ("ExpMA processing time: {0:0.000}s\n",
							     expma_processing_time);
					builder.Append (str);
				}

				if (processed_count > 1) {

					str = String.Format ("Average gap time: {0:0.000}s\n",
							     total_gap_time / (processed_count - 1));
					builder.Append (str);

					str = String.Format ("ExpMA gap time: {0:0.000}s\n",
							     expma_gap_time);
					builder.Append (str);
				}

				if (count == 0) {
					builder.Append ("Queue is empty.");
					return;
				}

				str = String.Format ("{0} item{1} in queue.\n",
						     count,
						     count == 1 ? "" : "s");
				builder.Append (str);

				if (processed_count > 1) {
					double t = count * (total_processing_time / processed_count + total_gap_time / (processed_count - 1));
					str = String.Format ("Estimated time until queue is empty: {0:0.0}s\n", t);
					builder.Append (str);
				}

				int pos = 1;
				for (int i = priority_queue.Count - 1; i >= 0; --i) {
					int priority = (int) priority_queue.GetKey (i);
					Queue queue = (Queue) priority_queue.GetByIndex (i);
					foreach (object item in queue) {
						str = String.Format ("[{0}] {1}: {2}\n", pos, priority, item);
						builder.Append (str);
						++pos;
					}
				}
			}
		}
	}
}
	
