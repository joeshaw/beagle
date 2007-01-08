//
// Shutdown.cs
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
using System.Threading;
using System.Collections;
using Beagle.Util;

namespace Beagle.Daemon {

	public class Shutdown {

		static public bool Debug = false;

		static object shutdownLock = new object ();
		static Hashtable workers = new Hashtable ();
		static Hashtable workers_names = new Hashtable ();
		static bool shutdownRequested = false;

		public delegate void ShutdownHandler ();
		public static event ShutdownHandler ShutdownEvent;

		public static bool WorkerStart (object o, string name)
		{
			lock (shutdownLock) {
				if (shutdownRequested) {
					return false;
				}
				int refcount = 0;
				if (workers.Contains (o))
					refcount = (int)workers[o];
				++refcount;
				workers[o] = refcount;
				workers_names[o] = name;

				if (Debug)
					Logger.Log.Debug ("worker added: name={0} refcount={1}", name, refcount);
			}
			return true;
		}

		public static bool WorkerStart (object o)
		{
			return WorkerStart (o, o.ToString ());
		}

		public static void WorkerFinished (object o)
		{
			lock (shutdownLock) {
				if (!workers.Contains (o)) {
					Logger.Log.Warn ("extra WorkerFinished called for {0}", o);
					return;
				}

				int refcount = (int)workers[o];
				--refcount;
				if (refcount == 0) {
					if (Debug)
						Logger.Log.Debug ("worker removed: name={0}", workers_names[o]);
					workers.Remove (o);
					workers_names.Remove (o);
				} else {
					if (Debug)
						Logger.Log.Debug ("worker finished: name={0} refcount={1}", workers_names[o], refcount);
					workers[o] = refcount;
				}

				Monitor.Pulse (shutdownLock);
			}
		}

		static public bool ShutdownRequested {
			get { return shutdownRequested; }
			set {
				lock (shutdownLock)
					shutdownRequested = value;
			}
		}

		private static GLib.MainLoop main_loop = null;

		public static void RegisterMainLoop (GLib.MainLoop loop)
		{
			main_loop = loop;
		}

		// Our handler triggers an orderly shutdown when it receives a signal.
		// However, this can be annoying if the process gets wedged during
		// shutdown.  To deal with that case, we make a note of the time when
		// the first signal comes in, and we allow signals to unconditionally
		// kill the process after 5 seconds have passed.

		static DateTime signal_time = DateTime.MinValue;

		public static void BeginShutdown ()
		{
			lock (shutdownLock) {
				shutdownRequested = true;
			}

			// FIXME: This whole "unconditional killing after 5 seconds because
			// beagled can hang while shutting down" thing should not occur. Any such
			// incident should be immediately investigated and fixed. Hint: Sending
			// kill -quit `pidof beagled` will probably reveal that beagled got locked
			// when signal handler was called and some thread was executing some native
			// method.
			bool first_signal = false;
			if (signal_time == DateTime.MinValue) {
				Log.Always ("Shutdown requested");
				signal_time = DateTime.Now;
				first_signal = true;
			}

			if (! first_signal) {
				double t = (DateTime.Now - signal_time).TotalSeconds;
				const double min_t = 5;

				if (t < min_t) {
					Logger.Log.Debug ("Signals can force an immediate shutdown in {0:0.00}s", min_t-t);
					return;
				} else {
					Logger.Log.Debug ("Forcing immediate shutdown.");
					Environment.Exit (0);
				}
			}

			if (ShutdownEvent != null) {
				try {
					ShutdownEvent ();
				} catch (Exception ex) {
					Logger.Log.Warn (ex, "Caught unhandled exception during shutdown event");
				}
			}

			int count = 0;
			
			lock (shutdownLock) { 
				while (workers.Count > 0) {
					++count;
					Logger.Log.Debug ("({0}) Waiting for {1} worker{2}...",
							  count,
							  workers.Count,
							  workers.Count > 1 ? "s" : "");					
					foreach (object o in workers.Keys) 
						Logger.Log.Debug ("waiting for {0}", workers_names[o]);
					Monitor.Wait (shutdownLock);
				}
			}

			Logger.Log.Info ("All workers have finished.  Exiting main loop.");
			main_loop.Quit ();
		}
	}
}
