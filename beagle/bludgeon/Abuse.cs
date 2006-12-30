
using System;
using System.Collections;

using Beagle.Util;

namespace Bludgeon {

	public class Abuse {

		DirectoryObject root;
		EventTracker tracker;

		IHammer[] hammers;
		Random random = new Random ();
		
		public int    TotalCount = -1; // in iterations
		public double TotalTime  = -1; // in minutes

		public int Cycles    = -1;
		public int MinCycles =  1;
		public int MaxCycles = -1;

		// These are delays that are introduced between calls
		// to HammerOnce.  They are measured in seconds.
		public double Pause    = -1; 
		public double MinPause =  0;
		public double MaxPause = -1;

		const int    default_count  =  10;
		const int    default_cycles = 100;
		const double default_pause  =   0;

		// This is where we track the state of our current cycle
		// of abuse.
		int      count;
		int      cycles_remaining;
		DateTime start_time;

		GLib.IdleHandler idle_handler;
		GLib.TimeoutHandler timeout_handler;
		Daemon.VerifiedHandler verified_handler;

		public Abuse (DirectoryObject root,
			      EventTracker    tracker,
			      ICollection     hammers)
		{
			this.root = root;
			this.tracker = tracker;

			this.hammers = new IHammer [hammers.Count];
			int i = 0;
			foreach (IHammer hammer in hammers)
				this.hammers [i++] = hammer;

			idle_handler = new GLib.IdleHandler (AbuseWorker);
			timeout_handler = new GLib.TimeoutHandler (RescheduleAbuse);
			verified_handler = new Daemon.VerifiedHandler (VerifiedWorker);
		}

		public void Run ()
		{
			count = 0;
			cycles_remaining = GetCycles ();
			start_time = DateTime.Now;
			
			// We start by verifying the index, to make sure we
			// are in a reasonable state.
			Daemon.WaitUntilVerified (root, verified_handler);
		}

		///////////////////////////////////////////////////////////////////////

		private int GetCycles ()
		{
			if (Cycles > 0)
				return Cycles;
			else if (MaxCycles > MinCycles)
				return MinCycles + random.Next (MaxCycles - MinCycles);
			return default_cycles;
		}

		private int GetPauseInMs ()
		{
			double t = default_pause;
			if (Pause >= 0)
				t = Pause;
			else if (MaxPause > MinPause)
				t = MinPause + random.NextDouble () * (MaxPause - MinPause);
			return (int) (1000 * t);
		}

		private bool AbuseWorker ()
		{
			// Pick a hammer, and use it.
			int i;
			i = random.Next (hammers.Length);
			if (! hammers [i].HammerOnce (root, tracker))
				return false;

			--cycles_remaining;
			if (cycles_remaining == 0) {
				cycles_remaining = GetCycles ();
				++count;

				// Verify the index
				Daemon.WaitUntilVerified (root, verified_handler);
				return false;
			}

			return true;
		}

		private bool RescheduleAbuse ()
		{
			Action.Add (idle_handler);

			return false;
		}

		private void VerifiedWorker (bool index_is_sane)
		{
			// If the index is bad, just return.  The index-checking
			// code will generate spew to tell us what went wrong, so
			// we don't need to output anything.
			if (! index_is_sane)
				return;
			
			// Are we finished yet?
			bool finished = false;
			if (hammers.Length == 0) {
				finished = true;
			} else if (TotalTime > 0) {
				double t;
				t = (DateTime.Now - start_time).TotalSeconds;
				finished = (t > 60*TotalTime);
			} else {
				int target_count;
				target_count = TotalCount;
				if (target_count < 0)
					target_count = default_count;
				finished = (count >= target_count);
			}
			
			// If we aren't finished, schedule some more abuse.
			if (! finished) {
				if (count == 0)
					Action.Add (idle_handler);
				else
					Action.Add ((uint) GetPauseInMs (), timeout_handler);
			}
		}
	}
}
