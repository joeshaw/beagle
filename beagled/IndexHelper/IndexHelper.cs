
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
using System.Threading;

using Mono.Posix;

using Gtk;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.IndexHelper {
	
	class IndexHelperTool {

		static DateTime last_activity;
		static Server server;

		static void Main (string [] args)
		{
			bool run_by_hand = (Environment.GetEnvironmentVariable ("BEAGLE_RUN_HELPER_BY_HAND") != null);
			bool log_in_fg = (Environment.GetEnvironmentVariable ("BEAGLE_LOG_IN_THE_FOREGROUND_PLEASE") != null);

			last_activity = DateTime.Now;

			Logger.DefaultLevel = LogLevel.Debug;

			Logger.LogToFile (PathFinder.LogDir, "IndexHelper", run_by_hand || log_in_fg);

			Application.InitCheck ("IndexHelper", ref args);

			SetupSignalHandlers ();

			Shutdown.ShutdownEvent += OnShutdown;

			// Start the server
			Logger.Log.Debug ("Starting messaging server");
			bool server_has_been_started = false;
			try {
				server = new Server ("socket-helper");
				server.Start ();
				server_has_been_started = true;
			} catch (InvalidOperationException ex) {
				Logger.Log.Error ("Couldn't start server:");
				Logger.Log.Error (ex);
			}

			if (server_has_been_started) {
				// Set the IO priority to idle so we don't slow down the system
				if (Environment.GetEnvironmentVariable ("BEAGLE_EXERCISE_THE_DOG") == null)
					IoPriority.SetIdle ();
				
				// Start the monitor thread, which keeps an eye on memory usage and idle time.
				ExceptionHandlingThread.Start (new ThreadStart (MemoryAndIdleMonitorWorker));

				// Start a thread that watches the daemon and begins a shutdown
				// if it terminates.
				ExceptionHandlingThread.Start (new ThreadStart (DaemonMonitorWorker));

				Application.Run ();

				// If we palced our sockets in a temp directory, try to clean it up
				// Note: this may fail because the daemon is still running
				if (PathFinder.GetRemoteStorageDir (false) != PathFinder.StorageDir) {
					try {
						Directory.Delete (PathFinder.GetRemoteStorageDir (false));
					} catch (IOException) { }
				}
			}

			Environment.Exit (0);
		}

		static public void ReportActivity ()
		{
			last_activity = DateTime.Now;
		}

		static void MemoryAndIdleMonitorWorker ()
		{
			int vmrss_original = SystemInformation.VmRss;

			const double max_idle_time = 5; // minutes

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
		
		static void DaemonMonitorWorker ()
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
				socket.Connect (new UnixEndPoint (socket_name));
				
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

		// The integer values of the Mono.Posix.Signal enumeration don't actually
		// match the Linux signal numbers of Linux.  Oops!
		// This is fixed in Mono.Unix, but for the moment we want to maintain
		// compatibility with mono 1.0.x.
		const int ACTUAL_LINUX_SIGINT  = 2;
		const int ACTUAL_LINUX_SIGQUIT = 3;
		const int ACTUAL_LINUX_SIGTERM = 15;

		static void SetupSignalHandlers ()
		{
			// Force OurSignalHandler to be JITed
			OurSignalHandler (-1);

			// Set up our signal handler
			Mono.Posix.Syscall.sighandler_t sig_handler;
			sig_handler = new Mono.Posix.Syscall.sighandler_t (OurSignalHandler);
                        Mono.Posix.Syscall.signal (ACTUAL_LINUX_SIGINT, sig_handler);
                        Mono.Posix.Syscall.signal (ACTUAL_LINUX_SIGQUIT, sig_handler);
                        Mono.Posix.Syscall.signal (ACTUAL_LINUX_SIGTERM, sig_handler);
		}

		// Our handler triggers an orderly shutdown when it receives a signal.
		// However, this can be annoying if the process gets wedged during
		// shutdown.  To deal with that case, we make a note of the time when
		// the first signal comes in, and we allow signals to unconditionally
		// kill the process after 5 seconds have passed.
		static DateTime signal_time = DateTime.MinValue;
		static void OurSignalHandler (int signal)
		{
			// This allows us to call OurSignalHandler w/o doing anything.
			// We want to call it once to ensure that it is pre-JITed.
			if (signal < 0)
				return;
			Logger.Log.Debug ("Handling signal {0}", signal);

			bool first_signal = false;
			if (signal_time == DateTime.MinValue) {
				signal_time = DateTime.Now;
				first_signal = true;
			}

			if (Shutdown.ShutdownRequested) {
				
				if (first_signal) {
					Logger.Log.Debug ("Shutdown already in progress.");
				} else {
					double t = (DateTime.Now - signal_time).TotalSeconds;
					const double min_t = 5;

					if (t < min_t) {
						Logger.Log.Debug ("Signals can force an immediate shutdown in {0:0.00}s", min_t-t);
					} else {
						Logger.Log.Debug ("Forcing immediate shutdown.");
						Environment.Exit (0);
					}
				}

			} else {
				Logger.Log.Debug ("Initiating shutdown in response to signal.");
				Shutdown.BeginShutdown ();
			}
		}

		static void OnShutdown ()
		{
			server.Stop ();
		}
	}

}
