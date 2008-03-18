
using System;
using System.Collections;

using Beagle.Util;

namespace Bludgeon {

	public class Action {

		// Note that the way we manipulate Action.Count and closure.Id
		// is violently non-threadsafe.  Fixing this should be pretty
		// easy, and is left as an exercise to the reader.

		public interface Handle { }
		
		// Should be private, but that is just too messy
		// for this code.
		static public int Count = 0;

		private class Closure : Handle {
			public uint Id;

			private GLib.TimeoutHandler timeout_handler;
			private GLib.IdleHandler idle_handler;

			public Closure (GLib.TimeoutHandler timeout_handler)
			{
				this.timeout_handler = timeout_handler;
				++Count;
			}

			public Closure (GLib.IdleHandler idle_handler)
			{
				this.idle_handler = idle_handler;
				++Count;
			}

			public bool Handler ()
			{
				if (Id == 0)
					return false;

				bool rv = false;
				if (timeout_handler != null)
					rv = timeout_handler ();
				else if (idle_handler != null)
					rv = idle_handler ();
				if (! rv) {
					Id = 0;
					--Count;
				}
				if (Count == 0)
					BludgeonMain.Shutdown ();

				return rv;
			}
		}

		// RENAMEME: It is linguistically awkward that we pass in
		// "handlers", and get back "handles".
		static public Handle Add (GLib.IdleHandler idle_handler)
		{
			Closure c;
			c = new Closure (idle_handler);
			c.Id = GLib.Idle.Add (new GLib.IdleHandler (c.Handler));
			return c;
		}

		static public Handle Add (uint t, GLib.TimeoutHandler timeout_handler)
		{
			Closure c;
			c = new Closure (timeout_handler);
			c.Id = GLib.Timeout.Add (t, new GLib.TimeoutHandler (c.Handler));
			return c;
		}

		static public void Cancel (Handle h)
		{
			Closure c = (Closure) h;

			if (c.Id != 0) {
				GLib.Source.Remove (c.Id);
				--Count;
			}
			if (Count == 0) {
				// We add an empty idle task, which will increment
				// the count up back above 0.  This keeps us from
				// shutting down the system until the next time control
				// is passed to the main loop.
				Add (null);
			}
		}
	}
}
