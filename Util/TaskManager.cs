//
// TaskManager.cs
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

	public interface ITask {
		DateTime When ();
		ITask DoTask ();
	}

	// FIXME: We need a shutdown method

	public class TaskManager {

		private ArrayList array = new ArrayList ();
		private Thread thread = null;
		private bool running = false;

		private class Scheduled : IComparable {
			public DateTime When;
			public ITask Task;
			public bool Cancelled = false;

			// Note that Scheduled objects sort reversed
			// on their When field, so later Scheduleds
			// preceed earlier ones.
			public int CompareTo (object rhs)
			{
				Scheduled other = rhs as Scheduled;
				if (other == null)
					return 1;
				return other.When.CompareTo (When);
			}

			public void DoTask ()
			{
				if (Cancelled || Task == null)
					return;
				Task = Task.DoTask ();
				if (Task != null)
					When = Task.When ();
			}

			public bool Finished {
				get { return Task == null; }
			}
		}

		public class Handle {

			private Scheduled scheduled;

			internal Handle (object obj)
			{
				scheduled = (Scheduled) obj;
			}
			
			public DateTime When {
				get { return scheduled.When; }
			}

			public bool Cancelled {
				get { return scheduled.Cancelled; }
			}

			public void Cancel ()
			{
				scheduled.Cancelled = true;
			}

			public bool Finished {
				get { return scheduled.Finished; }
			}
		}			

		////////////////////////////////////////////////////////

		public TaskManager ()
		{

		}

		public void Start ()
		{
			lock (this) {
				if (thread != null)
					return;

				running = true;
				
				thread = new Thread (new ThreadStart (QueueWorker));
				thread.Start ();
			}
		}

		private void AddInternal (Scheduled sch)
		{
			lock (array) {
				int i = array.BinarySearch (sch);
				if (i < 0)
					i = ~i;
				array.Insert (i, sch);

				Monitor.Pulse (array);
			}
		}

		public Handle Add (ITask task)
		{
			Scheduled sch = new Scheduled ();
			sch.When = task.When ();
			sch.Task = task;

			AddInternal (sch);
			
			return new Handle (sch);
		}

		////////////////////////////////////////////////////

		private void QueueWorker ()
		{
			lock (array) {

				while (running) {
					
					// Wait for the array to not be empty.
					while (array.Count == 0) {
						Console.WriteLine ("+++ Waiting for items");
						Monitor.Wait (array);
						if (! running)
							return;
					}

					// Find the next scheduled task.
					Scheduled sch = array [array.Count - 1] as Scheduled;
					
					// If it has been cancelled, throw it away.
					if (sch.Cancelled) {
						array.RemoveAt (array.Count - 1);
						continue;
					}

					// Compute the time until the next task.
					double t = (sch.When - DateTime.Now).TotalSeconds;

					if (t < 0.05) {
						// If we are close to that time, do the task
						// and reschedule the recurrance if necessary.
						sch.DoTask ();
						array.RemoveAt (array.Count - 1);
						if (! sch.Finished)
							AddInternal (sch);
					} else {
						// Otherwise wait the requested amount of time.
						// 1 tick = 100 nanoseconds
						long ticks = (long) (t * 1.0e+7);
						TimeSpan waitSpan = new TimeSpan (ticks);
						Console.WriteLine ("+++ waiting {0}s", t);
						Monitor.Wait (array, waitSpan);
					}
					
				}
			}
		}
	}
}
