//
// BeagleDaemon.cs
//
// Copyright (C) 2004-2006 Novell, Inc.
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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Thread = System.Threading.Thread;
using GLib;

using Beagle.Util;
using Log = Beagle.Util.Log;

namespace Beagle.Daemon {
	class BeagleDaemon {

		public static Thread MainLoopThread = null;
		private static MainLoop main_loop = null;

		private static Server server = null;

		private static bool arg_replace = false;
		private static bool arg_disable_scheduler = false;
		private static bool arg_indexing_test_mode = false;

		public static bool StartServer ()
		{
			Logger.Log.Debug ("Starting messaging server");

			try {
				server = new Server ("socket");
				server.Start ();
			} catch (InvalidOperationException) {
				return false;
			}

			return true;
		}

		public static void ReplaceExisting () 
		{
			Logger.Log.Info ("Attempting to replace another beagled.");

			do {
				ShutdownRequest request = new ShutdownRequest ();
				Logger.Log.Info ("Sending Shutdown");
				request.Send ();
				// Give it a second to shut down the messaging server
				Thread.Sleep (1000);
			} while (! StartServer ());			
		}

		private static void LogMemoryUsage ()
		{
			while (! Shutdown.ShutdownRequested) {
				int vm_rss = SystemInformation.VmRss;

				SystemInformation.LogMemoryUsage ();

				if (vm_rss > 300 * 1024) {
					Logger.Log.Debug ("VmRss too large --- shutting down");
					Shutdown.BeginShutdown ();
				}

				Thread.Sleep (5000);
			}
		}

		private static void PrintUsage ()
		{
			string usage =
				"beagled: The daemon to the Beagle search system.\n" +
				"Web page: http://beagle-project.org\n" +
				"Copyright (C) 2004-2006 Novell, Inc.\n\n";

			usage +=
				"Usage: beagled [OPTIONS]\n\n" +
				"Options:\n" +
				"  --foreground, --fg\tRun the daemon in the foreground.\n" +
				"  --background, --bg\tRun the daemon in the background.\n" +
				"  --replace\t\tReplace a running daemon with a new instance.\n" +
				"  --debug\t\tWrite out debugging information.\n" +
				"  --debug-memory\tWrite out debugging information about memory use.\n" +
				"  --indexing-test-mode\tRun in foreground, and exit when fully indexed.\n" +
				"  --indexing-delay\tTime to wait before indexing.  (Default 60 seconds)\n" +
				"  --backend\t\tConfigure which backends to use.  Specifically:\n" +
				"    --backend <name>\tOnly start backend 'name'\n" +
				"    --backend +<name>\tAdditionally start backend 'name'\n" +
				"    --backend -<name>\tDisable backend 'name'\n" +
				"  --allow-backend\t(DEPRECATED) Start only the specific backend.\n" +
				"  --deny-backend\t(DEPRECATED) Deny a specific backend.\n" +
				"  --list-backends\tList all the available backends.\n" +
				"  --add-static-backend\tAdd a static backend by path.\n" + 
				"  --disable-scheduler\tDisable the use of the scheduler.\n" +
				"  --help\t\tPrint this usage message.\n";

			Console.WriteLine (usage);
		}

		public static bool StartupProcess ()
		{
			Log.Debug ("Beginning main loop");

			// Profile our initialization
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			// Fire up our server
			if (! StartServer ()) {
				if (arg_replace)
				{
					ReplaceExisting ();
				}		
				else {
					Logger.Log.Error ("Could not set up the listener for beagle requests.  "
							  + "There is probably another beagled instance running.  "
							  + "Use --replace to replace the running service");
					Environment.Exit (1);
				}
			}
			
			// Set up out-of-process indexing
			LuceneQueryable.IndexerHook = new LuceneQueryable.IndexerCreator (RemoteIndexer.NewRemoteIndexer);

			// Initialize synchronization to keep the indexes local if PathFinder.StorageDir
			// is on a non-block device, or if BEAGLE_SYNCHRONIZE_LOCALLY is set
			if ((! SystemInformation.IsPathOnBlockDevice (PathFinder.StorageDir) && Conf.Daemon.IndexSynchronization) ||
			    Environment.GetEnvironmentVariable ("BEAGLE_SYNCHRONIZE_LOCALLY") != null)
				IndexSynchronization.Initialize ();

			// Start the query driver.
			Logger.Log.Debug ("Starting QueryDriver");
			QueryDriver.Start ();

			bool initially_on_battery = SystemInformation.UsingBattery && ! Conf.Indexing.IndexOnBattery;

			// Start the Global Scheduler thread
			if (! arg_disable_scheduler) {
				if (! initially_on_battery) {
					Logger.Log.Debug ("Starting Scheduler thread");
					Scheduler.Global.Start ();
				} else
					Log.Debug ("Beagle started on battery, not starting scheduler thread");
			}

			// Poll the battery status so we can shut down the
			// scheduler if needed.  Ideally at some point this
			// will become some sort of D-BUS signal, probably from
			// something like gnome-power-manager.
			prev_on_battery = initially_on_battery;
			GLib.Timeout.Add (5000, CheckBatteryStatus);

			// Start our Inotify threads
			Inotify.Start ();
	
			// Test if the FileAdvise stuff is working: This will print a
			// warning if not.  The actual advice calls will fail silently.
			FileAdvise.TestAdvise ();

			Conf.WatchForUpdates ();

			stopwatch.Stop ();

			Logger.Log.Debug ("Daemon initialization finished after {0}", stopwatch);

			SystemInformation.LogMemoryUsage (); 

			if (arg_indexing_test_mode) {
				Thread.Sleep (1000); // Ugly paranoia: wait a second for the backends to settle.
				Logger.Log.Debug ("Running in indexing test mode");
				Scheduler.Global.EmptyQueueEvent += OnEmptySchedulerQueue;
				Scheduler.Global.Add (null); // pulse the scheduler
			}
			return false;
		}

		private static void OnEmptySchedulerQueue ()
		{
			Logger.Log.Debug ("Scheduler queue is empty: terminating immediately");
			Shutdown.BeginShutdown ();
			Environment.Exit (0); // Ugly work-around: We need to call Exit here to avoid deadlocking.
		}

		public static void Main (string[] args)
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

		public static void DoMain (string[] args)
		{
			SystemInformation.InternalCallInitializer.Init ();
			SystemInformation.SetProcessName ("beagled");

			// Process the command-line arguments
			bool arg_debug = false;
			bool arg_debug_memory = false;
			bool arg_fg = false;

			int i = 0;
			while (i < args.Length) {
				
				string arg = args [i];
				++i;
				string next_arg = i < args.Length ? args [i] : null;

				switch (arg) {
				case "-h":
				case "--help":
					PrintUsage ();
					Environment.Exit (0);
					break;

				case "--heap-buddy":
				case "--heap-shot":
				case "--mdb":
				case "--mono-debug":
					// Silently ignore these arguments: they get handled
					// in the wrapper script.
					break;

				case "--list-backends":
					Console.WriteLine ("Current available backends:");
					Console.Write (QueryDriver.ListBackends ());
					Environment.Exit (0);
					break;

				case "--fg":
				case "--foreground":
					arg_fg = true;
					break;

				case "--bg":
				case "--background":
					arg_fg = false;
					break;

				case "--replace":
					arg_replace = true;
					break;

				case "--debug":
					arg_debug = true;
					break;

				case "--debug-memory":
					arg_debug = true;
					arg_debug_memory = true;
					break;
					
				case "--indexing-test-mode":
					arg_indexing_test_mode = true;
					arg_fg = true;
					break;

				case "--backend":
					if (next_arg == null) {
						Console.WriteLine ("--backend requires a backend name");
						Environment.Exit (1);
						break;
					}

					if (next_arg.StartsWith ("--")) {
						Console.WriteLine ("--backend requires a backend name. Invalid name '{0}'", next_arg);
						Environment.Exit (1);
						break;
					}

					if (next_arg [0] != '+' && next_arg [0] != '-')
						QueryDriver.OnlyAllow (next_arg);
					else {
						if (next_arg [0] == '+')
							QueryDriver.Allow (next_arg.Substring (1));
						else
							QueryDriver.Deny (next_arg.Substring (1));
					}

					++i; // we used next_arg
					break;
				
				case "--allow-backend":
					// --allow-backend is deprecated, use --backends 'name' instead
					// it will disable reading the list of enabled/disabled backends
					// from conf and start the backend given
					if (next_arg != null)
						QueryDriver.OnlyAllow (next_arg);
					++i; // we used next_arg
					break;
					
				case "--deny-backend":
					// deprecated: use --backends -'name' instead
					if (next_arg != null)
						QueryDriver.Deny (next_arg);
					++i; // we used next_arg
					break;

			       case "--add-static-backend": 
					if (next_arg != null)
						QueryDriver.AddStaticQueryable (next_arg);
					++i;
					break;

				case "--disable-scheduler":
					arg_disable_scheduler = true;
					break;

				case "--indexing-delay":
					if (next_arg != null) {
						try {
							QueryDriver.IndexingDelay = Int32.Parse (next_arg);
						} catch {
							Console.WriteLine ("'{0}' is not a valid number of seconds", next_arg);
							Environment.Exit (1);
						}
					}

					++i;
					break;

				case "--autostarted":
					if (! Conf.Searching.Autostart) {
						Console.WriteLine ("Autostarting is disabled, not starting");
						Environment.Exit (0);
					}
					break;

				default:
					Console.WriteLine ("Unknown argument '{0}'", arg);
					Environment.Exit (1);
					break;

				}
			}

			if (arg_indexing_test_mode) {
				LuceneQueryable.OptimizeRightAway = true;
			}
				

			// Bail out if we are trying to run as root
			if (Environment.UserName == "root" && Environment.GetEnvironmentVariable ("SUDO_USER") != null) {
				Console.WriteLine ("You appear to be running beagle using sudo.  This can cause problems with");
				Console.WriteLine ("permissions in your .beagle and .wapi directories if you later try to run");
				Console.WriteLine ("as an unprivileged user.  If you need to run beagle as root, please use");
				Console.WriteLine ("'su -c' instead.");
				Environment.Exit (-1);
			}

			if (Environment.UserName == "root" && ! Conf.Daemon.AllowRoot) {
				Console.WriteLine ("You can not run beagle as root.  Beagle is designed to run from your own");
				Console.WriteLine ("user account.  If you want to create multiuser or system-wide indexes, use");
				Console.WriteLine ("the beagle-build-index tool.");
				Console.WriteLine ();
				Console.WriteLine ("You can override this setting using the beagle-config or beagle-settings tools.");
				Environment.Exit (-1);
			}

			try {
				string tmp = PathFinder.HomeDir;
			} catch (Exception e) {
				Console.WriteLine ("Unable to start the daemon: {0}", e.Message);
				Environment.Exit (-1);
			}

			MainLoopThread = Thread.CurrentThread;

			Log.Initialize (PathFinder.LogDir,
					"Beagle", 
					// FIXME: We always turn on full debugging output!  We are still
					// debugging this code, after all...
					//arg_debug ? LogLevel.Debug : LogLevel.Warn,
					LogLevel.Debug,
					arg_fg);

			Logger.Log.Info ("Starting Beagle Daemon (version {0})", ExternalStringsHack.Version);

			Logger.Log.Info ("Running on {0}", SystemInformation.MonoRuntimeVersion);
			
			Logger.Log.Debug ("Command Line: {0}",
					  Environment.CommandLine != null ? Environment.CommandLine : "(null)");

			if (! ExtendedAttribute.Supported) {
				Logger.Log.Warn ("Extended attributes are not supported on this filesystem.  " +
						 "Performance will suffer as a result.");
			}

			// Start our memory-logging thread
			if (arg_debug_memory)
				ExceptionHandlingThread.Start (new ThreadStart (LogMemoryUsage));

			// Do BEAGLE_EXERCISE_THE_DOG_HARDER-related processing.
			ExerciseTheDogHarder ();

			// Initialize GObject type system
			g_type_init ();
			
			if (SystemInformation.XssInit ())
				Logger.Log.Debug ("Established a connection to the X server");
			else
				Logger.Log.Debug ("Unable to establish a connection to the X server");
			XSetIOErrorHandler (BeagleXIOErrorHandler);

			QueryDriver.Init ();
			Server.Init ();

			SetupSignalHandlers ();
			Shutdown.ShutdownEvent += OnShutdown;

			main_loop = new MainLoop ();
			Shutdown.RegisterMainLoop (main_loop);

			// Defer all actual startup until the main loop is
			// running.  That way shutdowns during the startup
			// process work correctly.
			GLib.Idle.Add (new GLib.IdleHandler (StartupProcess));

			// Start our event loop.
			Logger.Log.Debug ("Starting main loop");
			main_loop.Run ();

			// We're out of the main loop now, join all the
			// running threads so we can exit cleanly.
			ExceptionHandlingThread.JoinAllThreads ();

			// If we placed our sockets in a temp directory, try to clean it up
			// Note: this may fail because the helper is still running
			if (PathFinder.GetRemoteStorageDir (false) != PathFinder.StorageDir) {
				try {
					Directory.Delete (PathFinder.GetRemoteStorageDir (false));
				} catch (IOException) { }
			}

			Log.Info ("Beagle daemon process shut down cleanly.");
		}

		/////////////////////////////////////////////////////////////////////////////
		
		private static bool prev_on_battery = false;

		private static bool CheckBatteryStatus ()
		{
			if (prev_on_battery && (! SystemInformation.UsingBattery || Conf.Indexing.IndexOnBattery)) {
				if (! SystemInformation.UsingBattery)
					Log.Info ("Deletected a switch from battery to AC power.  Restarting scheduler.");
				Scheduler.Global.Start ();
				prev_on_battery = false;
			} else if (! prev_on_battery && SystemInformation.UsingBattery && ! Conf.Indexing.IndexOnBattery) {
				Log.Info ("Detected a switch from AC power to battery.  Stopping scheduler.");
				Scheduler.Global.Stop ();
				prev_on_battery = true;
			}

			return true;
		}


		/////////////////////////////////////////////////////////////////////////////

		private delegate int XIOErrorHandler (IntPtr display);

		[DllImport ("libX11.so.6")]
		extern static private int XSetIOErrorHandler (XIOErrorHandler handler);

		private static int BeagleXIOErrorHandler (IntPtr display)
		{
			Logger.Log.Debug ("Lost our connection to the X server!  Trying to shut down gracefully");

			if (! Shutdown.ShutdownRequested)
				Shutdown.BeginShutdown ();

			Logger.Log.Debug ("Xlib is forcing us to exit!");

			ExceptionHandlingThread.SpewLiveThreads ();
	
			// Returning will cause xlib to exit immediately.
			return 0;
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

			// Ignore SIGPIPE
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGPIPE, Mono.Unix.Native.Stdlib.SIG_IGN);
		}

		// Mono signal handler allows setting of global variables;
		// anything else e.g. function calls, even reentrant native methods are risky
		private static void OurSignalHandler (int signal)
		{
			// This allows us to call OurSignalHandler w/o doing anything.
			// We want to call it once to ensure that it is pre-JITed.
			if (signal < 0)
				return;

			// Set shutdown flag to true so that other threads can stop initializing
			if ((Mono.Unix.Native.Signum) signal != Mono.Unix.Native.Signum.SIGUSR1)
				Shutdown.ShutdownRequested = true;

			// Do all signal handling work in the main loop and not in the signal handler.
			GLib.Idle.Add (new GLib.IdleHandler (delegate () { HandleSignal (signal); return false; }));
		}

		private static void HandleSignal (int signal)
		{
			Logger.Log.Debug ("Handling signal {0} ({1})", signal, (Mono.Unix.Native.Signum) signal);

			// If we get SIGUSR1, turn the debugging level up.
			if ((Mono.Unix.Native.Signum) signal == Mono.Unix.Native.Signum.SIGUSR1) {
				LogLevel old_level = Log.Level;
				Log.Level = LogLevel.Debug;
				Log.Debug ("Moving from log level {0} to Debug", old_level);
				GLib.Idle.Add (new GLib.IdleHandler (delegate () { RemoteIndexer.SignalRemoteIndexer (); return false; }));
				return;
			}

			Logger.Log.Debug ("Initiating shutdown in response to signal.");
			Shutdown.BeginShutdown ();
		}

		/////////////////////////////////////////////////////////////////////////////

		private static void OnShutdown ()
		{
			// Stop our Inotify threads
			Inotify.Stop ();

			// Stop the global scheduler and ask it to shutdown
			Scheduler.Global.Stop (true);

			// Stop the messaging server
			if (server != null)
				server.Stop ();
		}

		/////////////////////////////////////////////////////////////////////////////


		private static ArrayList exercise_files = new ArrayList ();

		private static void ExerciseTheDogHarder ()
		{
			string path;				
			path = Environment.GetEnvironmentVariable ("BEAGLE_EXERCISE_THE_DOG_HARDER");
			if (path == null)
				return;

			DirectoryInfo dir = new DirectoryInfo (path);
			foreach (FileInfo file in dir.GetFiles ())
				exercise_files.Add (file);
			if (exercise_files.Count == 0)
				return;

			int N = 5;
			if (N > exercise_files.Count)
				N = exercise_files.Count;
			
			for (int i = 0; i < N; ++i)
				ExceptionHandlingThread.Start (new ThreadStart (ExerciseTheDogHarderWorker));
		}

		private static void ExerciseTheDogHarderWorker ()
		{
			Random rng = new Random ();

			while (! Shutdown.ShutdownRequested) {

				FileInfo file = null;			
				int i;

			
				lock (exercise_files) {
					do {
						i = rng.Next (exercise_files.Count);
						file = exercise_files [i] as FileInfo;
					} while (file == null);
					exercise_files [i] = null;
				}

				string target;
				target = Path.Combine (PathFinder.HomeDir, "_HARDER_" + file.Name);

				Logger.Log.Debug ("ETDH: Copying {0}", file.Name);
				file.CopyTo (target, true);
				
				lock (exercise_files)
					exercise_files [i] = file;

				Thread.Sleep (500 + rng.Next (500));
			}
		}
	}

}
