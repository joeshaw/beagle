
// IndexHelper.cs
//
// Copyright (C) 2005 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.Collections;
using System.IO;
using SNS = System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

using GLib;

using Beagle.Daemon;
using Beagle.Util;
using Log = Beagle.Util.Log;
using Thread = System.Threading.Thread;

namespace Beagle.IndexHelper {
	
	class IndexHelperTool {
		private static MainLoop main_loop;

		private static DateTime last_activity;
		private static Server server;

		// Current state with filtering.
		public static Uri CurrentUri;
		public static Filter CurrentFilter;

		[DllImport ("libc")]
		extern static private int unsetenv (string name);

		public static void Main (string [] args)
		{
			try {
				DoMain (args);
			} catch (Exception ex) {
				Logger.Log.Error (ex, "Unhandled exception thrown.  Exiting immediately.");
				Environment.Exit (1);
			}
		}

		[DllImport("libgobject-2.0.so.0")]
		static extern void g_type_init ();

		private static void DoMain (string [] args)
		{
			SystemInformation.SetProcessName ("beagled-helper");

			bool run_by_hand = (Environment.GetEnvironmentVariable ("BEAGLE_RUN_HELPER_BY_HAND") != null);
			bool log_in_fg = (Environment.GetEnvironmentVariable ("BEAGLE_LOG_IN_THE_FOREGROUND_PLEASE") != null);
			
			// FIXME: We always turn on full debugging output!  We are still
			// debugging this code, after all...
			//bool debug = (Environment.GetEnvironmentVariable ("BEAGLE_DEBUG_FLAG_IS_SET") != null);

			last_activity = DateTime.Now;

			Log.Initialize (PathFinder.LogDir,
					"IndexHelper",
					//debug ? LogLevel.Debug : LogLevel.Warn,
					LogLevel.Debug,
					run_by_hand || log_in_fg);

			// Initialize GObject type system
			g_type_init ();

			// Set the IO priority to idle, nice ourselves, and set
			// a batch scheduling policy so we that we play nice
			// on the system
			if (Environment.GetEnvironmentVariable ("BEAGLE_EXERCISE_THE_DOG") == null) {
				SystemPriorities.ReduceIoPriority ();

				int nice_to_set;
				
				// We set different nice values because the
				// internal implementation of SCHED_BATCH
				// unconditionally imposes a +5 penalty on
				// processes, and we want to be at nice +17,
				// because it has a nice timeslice.
				if (SystemPriorities.SetSchedulerPolicyBatch ())
					nice_to_set = 12;
				else
					nice_to_set = 17;

				SystemPriorities.Renice (nice_to_set);
			} else
				Log.Always ("BEAGLE_EXERCISE_THE_DOG is set");

			Server.Init ();

			SetupSignalHandlers ();

			Shutdown.ShutdownEvent += OnShutdown;

			main_loop = new MainLoop ();
			Shutdown.RegisterMainLoop (main_loop);

			// Start the server
			Log.Always ("Starting messaging server");
			bool server_has_been_started = false;
			try {
				server = new Server ("socket-helper");
				server.Start ();
				server_has_been_started = true;
			} catch (InvalidOperationException ex) {
				Logger.Log.Error (ex, "Couldn't start server.  Exiting immediately.");
			}

			if (server_has_been_started) {
				// Start the monitor thread, which keeps an eye on memory usage and idle time.
				ExceptionHandlingThread.Start (new ThreadStart (MemoryAndIdleMonitorWorker));

				// Start a thread that watches the daemon and begins a shutdown
				// if it terminates.
				ExceptionHandlingThread.Start (new ThreadStart (DaemonMonitorWorker));

				// Start the main loop
				main_loop.Run ();

				ExceptionHandlingThread.JoinAllThreads ();

				// If we placed our sockets in a temp directory, try to clean it up
				// Note: this may fail because the daemon is still running
				if (PathFinder.GetRemoteStorageDir (false) != PathFinder.StorageDir) {
					try {
						Directory.Delete (PathFinder.GetRemoteStorageDir (false));
					} catch (IOException) { }
				}

				Log.Always ("Index helper process shut down cleanly.");
			}
		}

		public static void ReportActivity ()
		{
			last_activity = DateTime.Now;
		}

		private static void MemoryAndIdleMonitorWorker ()
		{
			int vmrss_original = SystemInformation.VmRss;

			const double max_idle_time = 30; // minutes

			const double threshold = 5.0;
			const int max_request_count = 0;
			int last_vmrss = 0;

			while (! Shutdown.ShutdownRequested) {

				double idle_time;
				idle_time = (DateTime.Now - last_activity).TotalMinutes;
				if (idle_time > max_idle_time && RemoteIndexerExecutor.Count > 0) {
					Logger.Log.Debug ("No activity for {0:0.0} minutes, shutting down", idle_time);
					Shutdown.BeginShutdown ();
					return;
				}

				// Check resident memory usage
				int vmrss = SystemInformation.VmRss;
				double size = vmrss / (double) vmrss_original;
				if (vmrss != last_vmrss)
					Logger.Log.Debug ("Helper Size: VmRSS={0:0.0} MB, size={1:0.00}, {2:0.0}%",
							  vmrss/1024.0, size, 100.0 * (size - 1) / (threshold - 1));
				last_vmrss = vmrss;
				if (size > threshold
				    || (max_request_count > 0 && RemoteIndexerExecutor.Count > max_request_count)) {
					if (RemoteIndexerExecutor.Count > 0) {
						Logger.Log.Debug ("Process too big, shutting down!");
						Shutdown.BeginShutdown ();
						return;
					} else {
						// Paranoia: don't shut down if we haven't done anything yet
						Logger.Log.Debug ("Deferring shutdown until we've actually done something.");
						Thread.Sleep (1000);
					}
				} else {
					Thread.Sleep (3000);
				}
			}
		}
		
		private static void DaemonMonitorWorker ()
		{
			string storage_dir = PathFinder.GetRemoteStorageDir (false);

			if (storage_dir == null) {
				Logger.Log.Debug ("The daemon doesn't appear to have started");
				Logger.Log.Debug ("Shutting down helper.");
				Shutdown.BeginShutdown ();
				return;
			}

			// FIXME: We shouldn't need to know the  name of the daemon's socket.
			string socket_name;
			socket_name = Path.Combine (storage_dir, "socket");

			try {
				SNS.Socket socket;
				socket = new SNS.Socket (SNS.AddressFamily.Unix, SNS.SocketType.Stream, 0);
				socket.Connect (new Mono.Unix.UnixEndPoint (socket_name));
				
				ArrayList socket_list = new ArrayList ();
				
				while (! Shutdown.ShutdownRequested) {
					socket_list.Add (socket);
					SNS.Socket.Select (socket_list, null, null, 1000000); // 1000000 microseconds = 1 second
					if (socket_list.Count != 0) {
						Logger.Log.Debug ("The daemon appears to have gone away.");
						Logger.Log.Debug ("Shutting down helper.");
						Shutdown.BeginShutdown ();
					}
				}
			} catch (SNS.SocketException) {
				Logger.Log.Debug ("Caught a SocketException while trying to monitor the daemon");
				Logger.Log.Debug ("Shutting down");
				Shutdown.BeginShutdown ();
			}
		}

		/////////////////////////////////////////////////////////////////////////////

		private static void SetupSignalHandlers ()
		{
			// Force OurSignalHandler to be JITed
			OurSignalHandler (-1);

			// Set up our signal handler
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGINT, OurSignalHandler);
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGTERM, OurSignalHandler);
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGUSR1, OurSignalHandler);
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGUSR2, OurSignalHandler);

			// Ignore SIGPIPE
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGPIPE, Mono.Unix.Native.Stdlib.SIG_IGN);
		}

		// Our handler triggers an orderly shutdown when it receives a signal.
		// However, this can be annoying if the process gets wedged during
		// shutdown.  To deal with that case, we make a note of the time when
		// the first signal comes in, and we allow signals to unconditionally
		// kill the process after 5 seconds have passed.
		private static DateTime signal_time = DateTime.MinValue;
		private static void OurSignalHandler (int signal)
		{
			// This allows us to call OurSignalHandler w/o doing anything.
			// We want to call it once to ensure that it is pre-JITed.
			if (signal < 0)
				return;

			// SIGUSR1 and SIGUSR2 are informational signals.  For other
			// signals that we handle, set the shutdown flag to true so
			// that other threads can stop initializing
			if ((Mono.Unix.Native.Signum) signal != Mono.Unix.Native.Signum.SIGUSR1 &&
			    (Mono.Unix.Native.Signum) signal != Mono.Unix.Native.Signum.SIGUSR2)
				Shutdown.ShutdownRequested = true;

			// Do all signal handling work in the main loop and not in the signal handler.
			GLib.Idle.Add (new GLib.IdleHandler (delegate () { HandleSignal (signal); return false; }));
		}

		private static void HandleSignal (int signal)
		{
			Log.Warn ("Handling signal {0} ({1})", signal, (Mono.Unix.Native.Signum) signal);

			// If we get SIGUSR1, turn the debugging level up.
			if ((Mono.Unix.Native.Signum) signal == Mono.Unix.Native.Signum.SIGUSR1) {
				LogLevel old_level = Log.Level;
				Log.Level = LogLevel.Debug;
				Log.Debug ("Moving from log level {0} to Debug", old_level);
			}

			string span = StringFu.TimeSpanToString (DateTime.Now - last_activity);

			if (CurrentUri == null)
				Log.Warn ("Filtering status ({0} ago): no document is currently being filtered.", span);
			else if (CurrentFilter == null)
				Log.Warn ("Filtering status ({0} ago): determining filter for {1}", span, CurrentUri);
			else
				Log.Warn ("Filtering status ({0} ago): filtering {1} with {2}", span, CurrentUri, CurrentFilter);

			// Don't shut down on information signals (SIGUSR1 and SIGUSR2)
			if ((Mono.Unix.Native.Signum) signal == Mono.Unix.Native.Signum.SIGUSR1 ||
			    (Mono.Unix.Native.Signum) signal == Mono.Unix.Native.Signum.SIGUSR2)
				return;

			Logger.Log.Debug ("Initiating shutdown in response to signal.");
			Shutdown.BeginShutdown ();
		}

		private static void OnShutdown ()
		{
			if (server != null)
				server.Stop ();
		}
	}

}
