//
// Scheduler.cs
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

	public class Scheduler {

		static private Scheduler global = new Scheduler ();

		static public Scheduler Global {
			get { return global; }
		}

		//////////////////////////////////////////////////////////////////////////////

		public enum Priority {
			Idle      = 0, // Do it when the system is idle
			Delayed   = 1, // Do it soon
			Immediate = 2, // Do it right now
		}

		public delegate void Hook ();

		//////////////////////////////////////////////////////////////////////////////

		public abstract class Task : IComparable {

			public string    Tag;
			
			public Priority  Priority = Priority.Idle;
			public int       SubPriority = 0;
			
			public DateTime  Timestamp;
			public DateTime  TriggerTime = DateTime.MinValue;

			public TaskGroup TaskGroup = null;

			public ITaskCollector Collector = null;
			public double         Weight = 1.0;

			///////////////////////////////

			private bool finished = false;

			public bool Finished {
				get { return finished; }
			}

			public void Cancel ()
			{
				if (! finished) {
					TaskGroupPrivate task_group = (TaskGroupPrivate) TaskGroup;
					if (task_group != null)
						task_group.Decrement ();
				}
				finished = true;
			}
			
			public void DoTask ()
			{
				if (! finished) {
					TaskGroupPrivate task_group = (TaskGroupPrivate) TaskGroup;
					if (task_group != null)
						task_group.Touch ();
					DoTaskReal ();
					Cancel ();
				}
			}

			protected abstract void DoTaskReal ();

			///////////////////////////////

			// Sort from lowest to highest priority
			public int CompareTo (object obj)
			{
				Task other = obj as Task;
				if (other == null)
					return 1;

				int cmp;
				cmp = this.Priority.CompareTo (other.Priority);
				if (cmp != 0)
					return cmp;
				
				cmp = this.SubPriority.CompareTo (other.SubPriority);
				if (cmp != 0)
					return cmp;

				cmp = other.Timestamp.CompareTo (this.Timestamp);
				if (cmp != 0)
					return cmp;
				
				// Try to break any ties
				return this.GetHashCode ().CompareTo (other.GetHashCode ());
			}
		}

		//////////////////////////////////////////////////////////////////////////////

		//
		// Task Groups
		//

		public TaskGroup NewTaskGroup (string name, Hook pre_hook, Hook post_hook)
		{
			return new TaskGroupPrivate (name, pre_hook, post_hook);
		}

		// We split the task group data structure into two parts:
		// TaskGroup and TaskGroupPrivate.  The TaskGroup we hand
		// back to the user exposes minimal functionality.
		public class TaskGroup {
			private string name;
			
			protected TaskGroup (string name) {
				this.name = name;
			}
			
			public string Name {
				get { return name; }
			}
		}

		private class TaskGroupPrivate : TaskGroup {
			private int task_count = 0;
			private bool touched = false;
			private bool finished = false;
			private Hook pre_hook;
			private Hook post_hook;

			public TaskGroupPrivate (string name,
						 Hook   pre_hook,
						 Hook   post_hook) : base (name)
			{
				this.pre_hook = pre_hook;
				this.post_hook = post_hook;
			}

			public void Increment ()
			{
				if (finished)
					throw new Exception ("Tried to increment a finished TaskGroup");
				++task_count;
			}

			public void Touch ()
			{
				if (finished)
					throw new Exception ("Tried to touch a finished TaskGroup");

				if (! touched) {
					if (pre_hook != null)
						pre_hook ();
					touched = true;
				}
			}

			public void Decrement ()
			{
				if (finished)
					throw new Exception ("Tried to decrement a finished TaskGroup");

				--task_count;
				// Only fire our post-hook if the pre-hook fired
				// (or would have fired, had it been non-null)
				if (task_count == 0 && touched) {
					if (post_hook != null)
						post_hook ();
					finished = true;
				}
			}
		}

		//////////////////////////////////////////////////////////////////////////////

		//
		// Task Collector
		//
		// This is a mechanism for executing tasks in sets, possibly outside of
		// priority order.
		//

		public interface ITaskCollector {

			double GetMinimumWeight ();
			double GetMaximumWeight ();

			void PreTaskHook ();
			void PostTaskHook ();
		}

		//////////////////////////////////////////////////////////////////////////////

		private ArrayList task_queue = new ArrayList ();
		private Hashtable task_by_tag = new Hashtable ();

		public void Add (Task task)
		{
			Task old_task = null;

			lock (task_queue) {
				old_task = task_by_tag [task.Tag] as Task;
				if (old_task == task)
					return;

				task.Timestamp = DateTime.Now;

				int i = task_queue.BinarySearch (task);
				if (i < 0)
					i = ~i;
				task_queue.Insert (i, task);
				task_by_tag [task.Tag] = task;

				if (task.TaskGroup != null)
					((TaskGroupPrivate) task.TaskGroup).Increment ();

				Monitor.Pulse (task_queue);
			}

			if (old_task != null)
				old_task.Cancel ();

		}

		//////////////////////////////////////////////////////////////////////////////

		Thread thread = null;
		public bool running = false;

		public void Start ()
		{
			lock (this) {
				if (thread != null)
					return;
				running = true;
				thread = new Thread (new ThreadStart (Worker));
				thread.Start ();
			}
		}

		public void Stop ()
		{
			lock (this) {
				if (running) {
					running = false;
					lock (task_queue)
						Monitor.Pulse (task_queue);
				}
			}
		}

		//
		// Delay Computations
		//
		// This code controls how we space out tasks
		//

		// FIXME: random magic constants
		const double idle_threshold      = 5.314159 * 60; // probably should be longer
		const double idle_ramp_up_time   = 5.271828 * 60; // probably should be longer
		const double default_delayed_rate_factor = 2.007; // work about 1/3rd of the time
		const double default_idle_rate_factor    = 19.03; // work about 1/20th of the time

		static DateTime first_time = DateTime.MinValue;
		private double GetIdleTime ()
		{
			return SystemInformation.InputIdleTime;
		}

		private double ComputeDelay (Priority priority_of_next_task,
					     double   duration_of_previous_task)
		{
			double delay;
			double rate_factor;

			rate_factor = 2.0;

			// Do everything faster the longer we are idle.
			double idle_time = GetIdleTime ();
			double idle_scale = 1.0;
			if (idle_time > idle_threshold) {
				double t = (idle_time - idle_threshold) / idle_ramp_up_time;				     
				idle_scale = (1 - Math.Min (t, 1.0));
			} 

			switch (priority_of_next_task) {
				
			case Priority.Immediate:
				rate_factor = 0;
				break;

			case Priority.Delayed:
				rate_factor = idle_scale * default_delayed_rate_factor;
				break;

			case Priority.Idle:
				rate_factor = idle_scale * default_idle_rate_factor;
				break;
			}

			// FIXME: should adjust rate factor based on load average

			Console.WriteLine ("rate_factor={0} idle_scale={1}", rate_factor, idle_scale);

			return rate_factor * duration_of_previous_task;
		}

		//
		// The main loop
		//

		// A convenience function.  There should be a 
		// constructor to TimeSpan that does this.
		private static TimeSpan TimeSpanFromSeconds (double t)
		{
			// Wait barfs if you hand it a negative TimeSpan,
			// so we are paranoid;
			if (t < 0.001)
				t = 0;

			// 1 tick = 100 nanoseconds
			long ticks = (long) (t * 1.0e+7);
			return new TimeSpan (ticks);
		}

		private void Worker ()
		{
			DateTime time_of_last_task = DateTime.MinValue;
			double   duration_of_last_task = 1;

			Hook pre_hook = null;
			Hook post_hook = null;
			ArrayList collection = new ArrayList ();

			while (running) {

				lock (task_queue) {

					Task task = null;
					int i;

					// First, remove finished tasks
					i = task_queue.Count - 1;
					while (i >= 0) {
						Task t = task_queue [i] as Task;
						if (! t.Finished)
							break;
						task_by_tag.Remove (t.Tag); // clean up our hashtable
						--i;
					}
					if (i < task_queue.Count - 1) {
						Console.WriteLine ("Removing {0} finished tasks", task_queue.Count - 1 - i);
						task_queue.RemoveRange  (i+1, task_queue.Count - 1 - i);
					}
					
					// If the task queue is now empty, wait on our lock
					// and then re-start our while loop
					if (task_queue.Count == 0) {
						Console.WriteLine ("Waiting on empty queue");
						Monitor.Wait (task_queue);
						continue;
					}

					// Find the next event that is past it's trigger time.
					i = task_queue.Count - 1;
					DateTime now = DateTime.Now;
					DateTime next_trigger_time = DateTime.MaxValue;
					task = null;
					while (i >= 0) {
						Task t = task_queue [i] as Task;
						if (! t.Finished) {
							if (t.TriggerTime < now) {
								task = t;
								break;
							} else {
								// Keep track of when the next possible trigger time is.
								if (t.TriggerTime < next_trigger_time)
									next_trigger_time = t.TriggerTime;
							}
						}
						--i;
					}

					// If we didn't find a task, wait for the next trigger-time
					// and then re-start our while loop.
					if (task == null) {
						Console.WriteLine ("Next trigger time in {0}s", (next_trigger_time - now).TotalSeconds);
						Monitor.Wait (task_queue, next_trigger_time - now);
						continue;
					}

					// If we did find a task, do we want to execute it right now?
					// Or should we wait a bit?

					// How should we space things out?
					double delay = 0;
					delay = ComputeDelay (task.Priority, duration_of_last_task);
					delay = Math.Min (delay, (next_trigger_time - DateTime.Now).TotalSeconds);

					// Adjust by the time that has actually elapsed since the
					// last task.
					delay -= (DateTime.Now - time_of_last_task).TotalSeconds;

					// If we still need to wait a bit longer, wait for the appropriate
					// amount of time and then re-start our while loop.
					if (delay > 0.001) {
						Monitor.Wait (task_queue, TimeSpanFromSeconds (delay));
						continue;
					}

					if (task.Collector == null) {

						pre_hook = null;
						post_hook = null;
						collection.Add (task);

					} else {

						// Collect stuff 
						
						pre_hook = new Hook (task.Collector.PreTaskHook);
						post_hook = new Hook (task.Collector.PostTaskHook);

						double weight = task.Weight;
						double min_weight = task.Collector.GetMinimumWeight ();
						double max_weight = task.Collector.GetMaximumWeight ();

						collection.Add (task);
						
						// We left i pointing at task
						--i;
						while (i >= 0 && weight < max_weight) {
							Task t = task_queue [i] as Task;
							if (! t.Finished 
							    && t.Collector == task.Collector
							    && t.TriggerTime < now) {

								// Only include differently-prioritized tasks
								// in the same collection if the total weight so far
								// is below the minimum.
								if (t.Priority != task.Priority && weight > min_weight)
									break;

								weight += t.Weight;
								if (weight > max_weight)
									break;

								collection.Add (t);
							}
							--i;
						}
					}
				}


				// If we actually found tasks we like, do them now.
				if (collection.Count > 0) {
					// FIXME: we should catch any exceptions thrown by pre_hook,
					// post_hook or task.DoTask.
					DateTime t1 = DateTime.Now;
					if (pre_hook != null)
						pre_hook ();
					foreach (Task task in collection)
						task.DoTask ();
					if (post_hook != null)
						post_hook ();
					DateTime t2 = DateTime.Now;

					duration_of_last_task = (t2 - t1).TotalSeconds;
					Console.WriteLine ("duration={0}", duration_of_last_task);
					time_of_last_task = t2;

					pre_hook = null;
					post_hook = null;
					collection.Clear ();
				}
			}
		}
	}

	class TestTask : Scheduler.Task {

		private class TestCollector : Scheduler.ITaskCollector {
			
			public double GetMinimumWeight ()
			{
				return 0;
			}

			public double GetMaximumWeight ()
			{
				return 5;
			}

			public void PreTaskHook ()
			{
				Console.WriteLine ("+++ Pre-Task Hook");
			}

			public void PostTaskHook ()
			{
				Console.WriteLine ("+++ Post-Task Hook");
			}
		}

		protected override void DoTaskReal ()
		{
			Console.WriteLine ("Doing task '{0}' at {1}", Tag, DateTime.Now);
			Thread.Sleep (200);
		}

		static void BeginTaskGroup ()
		{
			Console.WriteLine ("--- Begin Task Group!");
		}

		static void EndTaskGroup ()
		{
			Console.WriteLine ("--- End Task Group!");
		}

		static void Main ()
		{
			Scheduler sched = Scheduler.Global;

			Scheduler.TaskGroup tg = sched.NewTaskGroup ("foo",
								     new Scheduler.Hook (BeginTaskGroup),
								     new Scheduler.Hook (EndTaskGroup));

			sched.Start ();

			Scheduler.Task task;

			task = new TestTask ();
			task.Tag = "Foo";
			task.TaskGroup = tg;
			task.Priority = Scheduler.Priority.Delayed;
			task.TriggerTime = DateTime.Now.AddSeconds (7);
			sched.Add (task);

			task = new TestTask ();
			task.Tag = "Bar";
			task.TaskGroup = tg;
			task.Priority = Scheduler.Priority.Delayed;
			sched.Add (task);

			Scheduler.ITaskCollector collector = null;
			for (int i = 0; i < 1000; ++i) {
				if ((i % 10) == 0)
					collector = new TestCollector ();
				task = new TestTask ();
				task.Tag = String.Format ("Baboon {0}", i);
				task.Collector = collector;
				task.Priority = Scheduler.Priority.Delayed;
				sched.Add (task);
			}

			while (true) {
				Thread.Sleep (1000);
			}
		}



			

	}
}
