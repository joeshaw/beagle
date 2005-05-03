//
// BeagleDaemon.cs
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
using System.IO;
using System.Reflection;
using System.Threading;

using DBus;
using Gtk;

using Beagle.Util;

#if ENABLE_WEBSERVICES
using Beagle.WebService;
#endif

namespace Beagle.Daemon {
	class BeagleDaemon {

		public static Thread MainLoopThread = null;

		private static FactoryImpl factory = null;

#if ENABLE_NETWORK
		private static NetworkService network = null;
#endif

		private static void OnNameOwnerChanged (string name,
							string oldOwner,
							string newOwner)
		{
			if (name == "com.novell.Beagle" && newOwner == "") {
#if HAVE_OLD_DBUS
				DBusisms.BusDriver.ServiceOwnerChanged -= OnNameOwnerChanged;
#else
				DBusisms.BusDriver.NameOwnerChanged -= OnNameOwnerChanged;
#endif
				Application.Quit ();
			}
		}


		public static int ReplaceExisting () 
		{
			Logger.Log.Info ("Attempting to replace another beagled.");
			DBus.Service service = DBus.Service.Get (DBusisms.Connection, "com.novell.Beagle");
#if HAVE_OLD_DBUS
			DBusisms.BusDriver.ServiceOwnerChanged += OnNameOwnerChanged;
#else
			DBusisms.BusDriver.NameOwnerChanged += OnNameOwnerChanged;
#endif
			do {
				Logger.Log.Info ("Building Remote Control Proxy");
				RemoteControlProxy proxy = RemoteControl.GetProxy (service);
				Logger.Log.Info ("Sending Shutdown");
				proxy.Shutdown ();
				Application.Run ();
			} while (! DBusisms.InitService (Beagle.DBusisms.Name));

			return 0;
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


		public static int Main (string[] args)
		{
			// Process the command-line arguments
			bool arg_replace = false;
			bool arg_debug = false;
			bool arg_debug_memory = false;
			bool arg_fg = false;
			bool arg_disable_scheduler = false;
#if ENABLE_NETWORK
			bool arg_network = false;
			int arg_port = 0;
#endif

#if ENABLE_WEBSERVICES
			WebServicesArgs wsargs = new WebServicesArgs();
#endif
			int i = 0;
			while (i < args.Length) {
				
				string arg = args [i];
				++i;
				string next_arg = i < args.Length ? args [i] : null;

				switch (arg) {
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

				case "--disable-scheduler":
					arg_disable_scheduler = true;
					break;

#if ENABLE_NETWORK
				case "--enable-network":
					arg_network = true;
					break;

				case "--port":
					try {
						arg_port = Convert.ToInt32(next_arg);
					} catch (Exception e) {
						Console.WriteLine("Ignoring malformed port argument '{0}'",next_arg);
					}
					++i; // we used next_arg
					break;
#endif

#if ENABLE_WEBSERVICES
				case "--web-global":
					wsargs.web_global = true;
					break;

				case "--web-start":
					wsargs.web_start = true;
					break;

				case "--web-port":
					wsargs.web_port = next_arg;
					++i; 
					wsargs.web_start = true;
					break;

				case "--web-root":
					wsargs.web_rootDir = next_arg;
					++i; 
					wsargs.web_start = true;
					break;
#endif 
				default:
					Console.WriteLine ("Ignoring unknown argument '{0}'", arg);
					break;

				}
			}

			// Bail out if we are trying to run as root
			if (Environment.UserName == "root") {
				Console.WriteLine ("You can not run beagle as root.");
				Console.WriteLine ("Beagle is designed to be run from your own user account.");
				Environment.Exit (-1);
			}

			MainLoopThread = Thread.CurrentThread;

			// Initialize logging.
			// If we saw the --debug arg, set the default logging level
			// accordingly.
			if (arg_debug)
				Logger.DefaultLevel = LogLevel.Debug;

			Logger.LogToFile (PathFinder.LogDir, "Beagle", arg_fg);

			Logger.Log.Info ("Starting Beagle Daemon (version {0})", ExternalStringsHack.Version);
			
			// FIXME: This try/catch is to work around a bug in mono 1.1.3
			try {
				Logger.Log.Debug ("Command Line: {0}",
						  Environment.CommandLine != null ? Environment.CommandLine : "(null)");
			} catch (Exception ex) { }

			// Make sure that extended attributes can be set.  If not, bail out with a message.
			// FIXME FIXME FIXME: This assumes that EAs work the same on your storage dir
			// as they do on your home dir, which is obviously not a good assumption.
			// We just need to pick a file under the home dir with the right permissions
			// and test it.
			if (! ExtendedAttribute.Test (PathFinder.StorageDir)) {
				Logger.Log.Fatal ("Could not set extended attributes on a file in your home "
						  + "directory.  See "
						  + "http://www.beaglewiki.org/index.php/Enable%20Extended%20Attributes "
						  + "for more information.");
				return 1;
			}

			// Start our memory-logging thread
			if (arg_debug_memory) {
				Thread th = new Thread (new ThreadStart (LogMemoryUsage));
				th.Start ();
			}

			try {
				Logger.Log.Debug ("Initializing D-BUS");
				DBusisms.Init ();
				Application.InitCheck ("beagled", ref args);

				Logger.Log.Debug ("Acquiring {0} D-BUS service", Beagle.DBusisms.Name);
				if (!DBusisms.InitService (Beagle.DBusisms.Name)) {
					if (arg_replace) {
						ReplaceExisting ();
					} else {
						Logger.Log.Fatal ("Could not register com.novell.Beagle service.  "
								  + "There is probably another beagled instance running.  "
								  + "Use --replace to replace the running service");
						return 1;
					}
				}
				
			} catch (DBus.DBusException e) {
				Logger.Log.Fatal ("Couldn't connect to the session bus.  "
						  + "See http://beaglewiki.org/index.php/Installing%20Beagle "
						  + "for information on setting up a session bus.");
				Logger.Log.Fatal (e.Message);
				return 1;
			} catch (Exception e) {
				Logger.Log.Fatal ("Could not initialize Beagle's bus connection.");
				Logger.Log.Fatal (e);
				return 1;
			}

			SetupSignalHandlers ();

			// Set up our helper process and the associated monitoring
			IndexHelperFu.Start ();
			
			// We want to spend as little time as possible
			// between InitService and actually being able 
			// to serve requests
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			try {
				// Construct and register our remote control object.
				Logger.Log.Debug ("Initializing RemoteControl");
				RemoteControlImpl rci = new RemoteControlImpl ();
				DBusisms.RegisterObject (rci, Beagle.DBusisms.RemoteControlPath);
				
				// Set up our D-BUS object factory.
				factory = new FactoryImpl ();
				DBusisms.RegisterObject (factory, Beagle.DBusisms.FactoryPath);
			} catch (DBus.DBusException e) {
				Logger.Log.Fatal ("Couldn't register DBus objects."); 
				Logger.Log.Debug (e);
				return 1;
			} catch (Exception e) {
				Logger.Log.Fatal ("Could not initialize Beagle:\n{0}", e);
				return 1;
			}

#if ENABLE_NETWORK
			if (arg_network) {
				try {
					// Set up network service
					network = new NetworkService(arg_port);
					network.Start ();
				} catch {
					Logger.Log.Error ("Could not initialize network service"); 
				}
			}
#endif

			// Set up out-of-process indexing
			if (Environment.GetEnvironmentVariable ("BEAGLE_ENABLE_IN_PROCESS_INDEXING") == null)
				LuceneQueryable.IndexerHook = new LuceneQueryable.IndexerCreator (RemoteIndexer.NewRemoteIndexer);

			// Start the query driver.
			Logger.Log.Debug ("Starting QueryDriver");
			QueryDriver.Start ();

			// Start the Global Scheduler thread
			if (! arg_disable_scheduler) {
				Logger.Log.Debug ("Starting Scheduler thread");
				Scheduler.Global.Start ();
			}

			// Start our Inotify threads
			Logger.Log.Debug ("Starting Inotify threads");
			Inotify.Start ();
	
			// Test if the FileAdvise stuff is working: This will print a
			// warning if not.  The actual advice calls will fail silently.
			FileAdvise.TestAdvise ();

#if ENABLE_WEBSERVICES		
			//Beagle Web, WebService access initialization code:
			WebServiceBackEnd.Start(wsargs);
#endif
			Shutdown.ShutdownEvent += OnShutdown;

			stopwatch.Stop ();

			Logger.Log.Debug ("Ready to accept requests after {0}", 
					 stopwatch);
		
			// Start our event loop.
			Logger.Log.Debug ("Starting main loop");

			Application.Run ();

#if ENABLE_WEBSERVICES
			WebServiceBackEnd.Stop();
#endif
			Logger.Log.Debug ("Leaving BeagleDaemon.Main");

			// Exiting will close the dbus connection, which
			// will release the com.novell.beagle service.
			return 0;
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

			// Stop our indexing helper process
			IndexHelperFu.Stop ();

			Logger.Log.Debug ("Unregistering Daemon objects");
			DBusisms.UnregisterAll ();
			Logger.Log.Debug ("Done unregistering Daemon objects");

		}
	}
}
