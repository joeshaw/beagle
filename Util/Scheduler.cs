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
using System.Text;
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
		public delegate void TaskHook (Task task);

		//////////////////////////////////////////////////////////////////////////////

		public abstract class Task : IComparable {

			// A unique identifier
			public string    Tag;

			// Some metadata
			public string    Creator;
			public string    Description;
			
			public Priority  Priority = Priority.Idle;
			public int       SubPriority = 0;
			
			public DateTime  Timestamp;
			public DateTime  TriggerTime = DateTime.MinValue;

			public ITaskCollector Collector = null;
			public double         Weight = 1.0;

			public bool Reschedule = false;

			///////////////////////////////

			private ArrayList task_groups = null;

			public void AddTaskGroup (TaskGroup group)
			{
				if (task_groups == null)
					task_groups = new ArrayList ();
				task_groups.Add (group);
			}

			private void IncrementAllTaskGroups ()
			{
				if (task_groups != null) {
					foreach (TaskGroupPrivate group in task_groups) {
						if (! group.Finished)
							group.Increment ();
					}
				}
			}

			private void DecrementAllTaskGroups ()
			{
				if (task_groups != null) {
					foreach (TaskGroupPrivate group in task_groups) {
						if (! group.Finished)
							group.Decrement ();
					}
				}
			}

			private void TouchAllTaskGroups ()
			{
				if (task_groups != null) {
					foreach (TaskGroupPrivate group in task_groups) {
						if (! group.Finished)
							group.Touch ();
					}
				}
			}

			///////////////////////////////

			private Scheduler scheduler = null;

			public Scheduler ThisScheduler {
				get { return scheduler; }
			}

			public void Schedule (Scheduler scheduler)
			{
				// Increment the task groups the first
				// time a task is scheduled.
				if (this.scheduler == null)
					IncrementAllTaskGroups ();
				this.scheduler = scheduler;
			}

			///////////////////////////////

			private bool cancelled = false;

			public bool Cancelled {
				get { return cancelled; }
			}

			public void Cancel ()
			{
				if (! cancelled)
					DecrementAllTaskGroups ();
				cancelled = true;
			}

			///////////////////////////////

			// The Task's count keeps track of how many
			// times it has been executed.

			private int count = 0;
			
			public int Count {
				get { return count; }
			}

			///////////////////////////////
			
			public void DoTask ()
			{
				if (! cancelled) {
					TouchAllTaskGroups ();
					try {
						DoTaskReal ();
					} catch (Exception ex) {
						Logger.Log.Warn ("Caught exception in DoTaskReal");
						Logger.Log.Warn ("        Tag: {0}", Tag);
						Logger.Log.Warn ("    Creator: {0}", Creator);
						Logger.Log.Warn ("Description: {0}", Description);
						Logger.Log.Warn ("   Priority: {0} ({1})", Priority, SubPriority);
						Logger.Log.Warn (ex);
					}
					if (Reschedule) {
						Console.WriteLine ("Rescheduling {0}!", Tag);
						Reschedule = false;
						++count;
						ThisScheduler.Add (this);
					} else {
						DecrementAllTaskGroups ();
					}
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

			public override string ToString ()
			{
				StringBuilder sb = new StringBuilder ();

				sb.AppendFormat ("{0} {1}\n", Priority, SubPriority);
					
				sb.Append (Tag + "\n");

				double t = (TriggerTime - DateTime.Now).TotalSeconds;
				if (t > 0) {
					if (t < 120)
						sb.AppendFormat ("Trigger in {0:0.00} seconds\n", t);
					else
						sb.AppendFormat ("Trigger at {0}\n", TriggerTime);
				}

				if (Creator != null)
					sb.AppendFormat ("Creator: {0}\n", Creator);

				if (Description != null)
					sb.Append (Description + "\n");

				return sb.ToString ();
			}
		}

		private class TaskHookWrapper : Task {

			TaskHook hook;
			
			public TaskHookWrapper (TaskHook hook) 
			{
				this.hook = hook;
			}

			protected override void DoTaskReal ()
			{
				if (hook != null)
					hook (this);
			}
		}

		public static Task TaskFromHook (TaskHook hook)
		{
			return new TaskHookWrapper (hook);
		}

		//////////////////////////////////////////////////////////////////////////////

		//
		// Task Groups
		//

		public static TaskGroup NewTaskGroup (string name, Hook pre_hook, Hook post_hook)
		{
			return new TaskGroupPrivate (name, pre_hook, post_hook);
		}

		// We split the task group data structure into two parts:
		// TaskGroup and TaskGroupPrivate.  The TaskGroup we hand
		// back to the user exposes minimal functionality.
		public abstract class TaskGroup {
			private string name;
			
			protected TaskGroup (string name) {
				this.name = name;
			}
			
			public string Name {
				get { return name; }
			}

			public abstract bool Finished { get; }
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

			public override bool Finished {
				get { return finished; }
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
				if (task != null) {
					old_task = task_by_tag [task.Tag] as Task;
					if (old_task == task)
						return;

#if false
					Logger.Log.Debug ("Adding task");
					Logger.Log.Debug ("Tag: {0}", task.Tag);
					if (task.Description != null)
						Logger.Log.Debug ("Desc: {0}", task.Description);
#endif

					task.Timestamp = DateTime.Now;
					task.Schedule (this);

					int i = task_queue.BinarySearch (task);
					if (i < 0)
						i = ~i;
					task_queue.Insert (i, task);
					task_by_tag [task.Tag] = task;
				}
					
				Monitor.Pulse (task_queue);
			}

			if (old_task != null)
				old_task.Cancel ();

		}

		public Task GetByTag (string tag)
		{
			lock (task_queue) {
				return task_by_tag [tag] as Task;
			}
		}

		public bool ContainsByTag (string tag)
		{
			Task task = GetByTag (tag);
			return task != null && !task.Cancelled;
		}



		//////////////////////////////////////////////////////////////////////////////

		private string status_str = null;

		public string GetHumanReadableStatus ()
		{
			StringBuilder sb = new StringBuilder ();

			sb.Append ("Scheduler:\n");

			if (status_str != null)
				sb.Append ("Status: " + status_str + "\n");

			lock (task_queue) {
				int pos = 1;
				for (int i = task_queue.Count - 1; i >= 0; --i) {
					Task task = task_queue [i] as Task;
					if (task == null || task.Cancelled)
						continue;

					sb.AppendFormat ("{0} ", pos);
					sb.Append (task.ToString ());
					sb.Append ("\n");

					++pos;
				}

				if (pos == 1)
					sb.Append ("Scheduler queue is empty.\n");
			}

			sb.Append ("\n");

			return sb.ToString ();
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
		const double default_delayed_rate_factor =  9.03; // work about 1/10th of the time
		const double default_idle_rate_factor    = 2.097; // work about 1/3rd of the time

		static DateTime first_time = DateTime.MinValue;
		private double GetIdleTime ()
		{
			return SystemInformation.InputIdleTime;
		}

		private double ComputeDelay (Priority priority_of_next_task,
					     double   duration_of_previous_task)
		{
			double rate_factor;

			rate_factor = 2.0;

			// Do everything faster the longer we are idle.
			double idle_time = GetIdleTime ();
			double idle_scale = 1.0;
			bool is_idle = false;
			if (idle_time > idle_threshold) {
				is_idle = true;
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

			// FIXME: we should do something more sophisticated than this
			// with the load average.
			// Random numbers galore!
			double load_average = SystemInformation.LoadAverageOneMinute;
			if (load_average > 3.001)
				rate_factor *= 5.002;
			else if (load_average > 1.5003)
				rate_factor *= 2.004;

			double delay = rate_factor * duration_of_previous_task;

			// space out delayed tasks a bit when we aren't idle
			if (! is_idle
			    && priority_of_next_task == Priority.Delayed
			    && delay < 0.5)
				delay = 0.5;

			return delay;
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

		private void DescribeTaskQueue (string note, int i0, int i1)
		{
			Console.WriteLine ("----------------------");
			Console.WriteLine (note);
			for (int i=i0; i<i1; ++i) {
				Task t = task_queue [i] as Task;
				string xxx;
				if (t == null)
					xxx = "(null)";
				else if (t.Cancelled)
					xxx = t.Tag + " CANCELLED";
				else
					xxx = t.Tag;
				Console.WriteLine ("{0}: {1}", i, xxx);
			}
			Console.WriteLine ("----------------------");
		}

		// Remove nulls and cancelled tasks from the queue.
		// Note: this does no locking!
		private void CleanQueue ()
		{
			int i = task_queue.Count - 1;
			while (i >= 0) {
				Task t = task_queue [i] as Task;
				if (t != null) {
					if (! t.Cancelled)
						break;
					// Remove cancelled items from the tag hash
					task_by_tag.Remove (t.Tag);
				}
				--i;
			}
			if (i < task_queue.Count - 1)
				task_queue.RemoveRange  (i+1, task_queue.Count - 1 - i);
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
					int task_i = -1;
					int i;

					// First, remove any null or cancelled tasks
					// we find in the task_queue.
					CleanQueue ();
					
					// If the task queue is now empty, wait on our lock
					// and then re-start our while loop
					if (task_queue.Count == 0) {
						status_str = "Waiting on empty queue";
						Monitor.Wait (task_queue);
						status_str = "Working";
						continue;
					}

					// Find the next event that is past it's trigger time.
					i = task_queue.Count - 1;
					DateTime now = DateTime.Now;
					DateTime next_trigger_time = DateTime.MaxValue;
					task = null;
					while (i >= 0) {
						Task t = task_queue [i] as Task;
						if (t != null && ! t.Cancelled) {
							if (t.TriggerTime < now) {
								task = t;
								task_i = i; // Remember the task's position in the queue.
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
						status_str = String.Format ("Next trigger time is in {0:0.00}s ({1})",
									    (next_trigger_time - now).TotalSeconds,
									    next_trigger_time);
						Monitor.Wait (task_queue, next_trigger_time - now);
						status_str = "Working";
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
						status_str = String.Format ("Will execute next task in {0:0.00}s", delay);
						Monitor.Wait (task_queue, TimeSpanFromSeconds (delay));
						status_str = "Working";
						continue;
					}

					// Remove this task from the queue
					task_queue [task_i] = null;
					task_by_tag.Remove (task.Tag);

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
							if (t != null
							    && ! t.Cancelled
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

								// Remove the task from the queue and clean
								// up the by-tag hash table.
								task_queue [i] = null;
								task_by_tag.Remove (t.Tag);
							}
							--i;
						}
					}

					// Clean the queue again
					// (We need to do this to keep rescheduled tasks from blocking
					// stuff from getting cleaned off the end of the queue)
					CleanQueue ();
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
					time_of_last_task = t2;

					pre_hook = null;
					post_hook = null;
					collection.Clear ();
				}
			}
		}
	}

#if false
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
			if (Tag == "Bar")
				Reschedule = true;
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

			Scheduler.TaskGroup tg = Scheduler.NewTaskGroup ("foo",
									 new Scheduler.Hook (BeginTaskGroup),
									 new Scheduler.Hook (EndTaskGroup));

			sched.Start ();

			Scheduler.Task task;

			task = new TestTask ();
			task.Tag = "Foo";
			task.AddTaskGroup (tg);
			task.Priority = Scheduler.Priority.Delayed;
			task.TriggerTime = DateTime.Now.AddSeconds (7);
			sched.Add (task);

			task = new TestTask ();
			task.Tag = "Bar";			
			task.AddTaskGroup (tg);
			task.Priority = Scheduler.Priority.Delayed;
			sched.Add (task);

			Scheduler.ITaskCollector collector = null;
			for (int i = 0; i < 20; ++i) {
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
#endif
}
