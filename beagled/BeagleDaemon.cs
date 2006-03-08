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
using System.Threading;

using Gtk;

using Beagle.Util;

#if ENABLE_WEBSERVICES
using Beagle.WebService;
#endif

namespace Beagle.Daemon {
	class BeagleDaemon {

		public static Thread MainLoopThread = null;

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
				int vm_size = SystemInformation.VmSize;
				int vm_rss = SystemInformation.VmRss;

				Logger.Log.Debug ("Memory usage: VmSize={0:.0} MB, VmRSS={1:.0} MB,  GC.GetTotalMemory={2}",
						  vm_size/1024.0, vm_rss/1024.0, GC.GetTotalMemory (false));

				if (vm_size > 300 * 1024) {
					Logger.Log.Debug ("VmSize too large --- shutting down");
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
				"  --deny-backend\tDeny a specific backend.\n" +
				"  --allow-backend\tAllow a specific backend.\n" +
				"  --list-backends\tList all the available backends.\n" +
				"  --add-static-backend\tAdd a static backend by path.\n" + 
				"  --disable-scheduler\tDisable the use of the scheduler.\n" +
				"  --help\t\tPrint this usage message.\n";

#if ENABLE_WEBSERVICES
			usage += "\n" +
				"  --web-global\t\tAllow global access to the Web & WebService interfaces.\n" +
				"  --web-port\t\tPort to use for the internal web server.\n" +
				"  --web-root\t\tRoot directory to use for the internal web server.\n" +
				"  --web-disable\t\tDisable Web & WebServices functionality.\n";			
#endif 

			Console.WriteLine (usage);
		}

		public static bool StartupProcess ()
		{
			// Profile our initialization
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			SetupSignalHandlers ();

			// Fire up our server
			if (! StartServer ()) {
				if (arg_replace)
				{
#if ENABLE_WEBSERVICES
					WebServiceBackEnd.Stop();
#endif			
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
			if (Environment.GetEnvironmentVariable ("BEAGLE_ENABLE_IN_PROCESS_INDEXING") == null)
				LuceneQueryable.IndexerHook = new LuceneQueryable.IndexerCreator (RemoteIndexer.NewRemoteIndexer);

			// Initialize syncronization to keep the indexes local if PathFinder.HomeDir
			// is on a non-block device, or if BEAGLE_SYNCHRONIZE_LOCALLY is set
			if ((! SystemInformation.IsPathOnBlockDevice (PathFinder.HomeDir) && Conf.Daemon.IndexSynchronization) ||
			    Environment.GetEnvironmentVariable ("BEAGLE_SYNCHRONIZE_LOCALLY") != null)
				IndexSynchronization.Initialize ();

			// Start the query driver.
			Logger.Log.Debug ("Starting QueryDriver");
			QueryDriver.Start ();

			// Start the Global Scheduler thread
			if (! arg_disable_scheduler) {
				Logger.Log.Debug ("Starting Scheduler thread");
				Scheduler.Global.Start ();
			}

			// Start our Inotify threads
			Inotify.Start ();
	
			// Test if the FileAdvise stuff is working: This will print a
			// warning if not.  The actual advice calls will fail silently.
			FileAdvise.TestAdvise ();

#if ENABLE_WEBSERVICES		
			//Beagle Web, WebService access initialization code:
			WebServiceBackEnd.Start();
#endif
			Shutdown.ShutdownEvent += OnShutdown;

			Conf.WatchForUpdates ();

			stopwatch.Stop ();

			Logger.Log.Debug ("Daemon initialization finished after {0}", stopwatch);

			if (arg_indexing_test_mode) {
				Thread.Sleep (1000); // Ugly paranoia: wait a second for the backends to settle.
				Logger.Log.Debug ("Running in indexing test mode");
				Scheduler.Global.EmptyQueueEvent += OnEmptySchedulerQueue;
				Scheduler.Global.Add (null); // pulse the scheduler
			}
			return false;
		}

		static void OnEmptySchedulerQueue ()
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
				Logger.Log.Error ("Unhandled exception thrown.  Exiting immediately.");
				Logger.Log.Error (ex);
				Environment.Exit (1);
			}
		}

		public static void DoMain (string[] args)
		{
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
					// Silently ignore the --heap-buddy argument: it gets handled
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

				case "--allow-backend":
					if (next_arg != null)
						QueryDriver.Allow (next_arg);
					++i; // we used next_arg
					break;
					
				case "--deny-backend":
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
#if ENABLE_WEBSERVICES
				case "--web-global":
					WebServiceBackEnd.web_global = true;
					WebServiceBackEnd.web_start = true;					
					break;

				case "--web-port":
					WebServiceBackEnd.web_port = next_arg;
					++i; 
					WebServiceBackEnd.web_start = true;
					break;

				case "--web-root":
					WebServiceBackEnd.web_rootDir = next_arg;
					++i; 
					WebServiceBackEnd.web_start = true;
					break;
					
				case "--web-disable":
					WebServiceBackEnd.web_start = false;
					break;				
#endif 
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
			if (Environment.UserName == "root" && ! Conf.Daemon.AllowRoot) {
				Console.WriteLine ("You can not run beagle as root.  Beagle is designed to run from your own");
				Console.WriteLine ("user account.  If you want to create multiuser or system-wide indexes, use");
				Console.WriteLine ("the beagle-build-index tool.");
				Console.WriteLine ();
				Console.WriteLine ("You can override this setting using the beagle-config or beagle-settings tools.");
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

			Application.InitCheck ("beagled", ref args);

			// Defer all actual startup until the main loop is
			// running.  That way shutdowns during the startup
			// process work correctly.
			GLib.Idle.Add (new GLib.IdleHandler (StartupProcess));

			// Start our event loop.
			Logger.Log.Debug ("Starting main loop");

			Application.Run ();

			// If we placed our sockets in a temp directory, try to clean it up
			// Note: this may fail because the helper is still running
			if (PathFinder.GetRemoteStorageDir (false) != PathFinder.StorageDir) {
				try {
					Directory.Delete (PathFinder.GetRemoteStorageDir (false));
				} catch (IOException) { }
			}

			Logger.Log.Debug ("Leaving BeagleDaemon.Main");

			if (arg_debug) {
				Thread.Sleep (500);
				ExceptionHandlingThread.SpewLiveThreads ();
			}
		}
		

		/////////////////////////////////////////////////////////////////////////////

		static void SetupSignalHandlers ()
		{
			// Force OurSignalHandler to be JITed
			OurSignalHandler (-1);

			// Set up our signal handler
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGINT, OurSignalHandler);
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGTERM, OurSignalHandler);
			if (Environment.GetEnvironmentVariable("BEAGLE_THERE_BE_NO_QUITTIN") == null)
				Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGQUIT, OurSignalHandler);
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

			if (signal == (int) Mono.Unix.Native.Signum.SIGQUIT) {
				ExceptionHandlingThread.AbortThreads ();
				return;
			}

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

		/////////////////////////////////////////////////////////////////////////////

		private static void OnShutdown ()
		{
#if ENABLE_WEBSERVICES
			WebServiceBackEnd.Stop();
#endif
			// Stop our Inotify threads
			Inotify.Stop ();

			// Shut down the global scheduler
			Scheduler.Global.Stop ();

			// Stop the messaging server
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
