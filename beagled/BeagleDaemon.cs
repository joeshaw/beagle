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


namespace Beagle.Daemon {
	class BeagleDaemon {

		private static ArrayList dbusObjects = new ArrayList ();
		private static FactoryImpl factory = null;

#if ENABLE_NETWORK
		private static NetworkService network = null;
#endif

		private static void SetupLog (bool echo)
		{

			// FIXME: We should probably clean up old logs

			string log_name = String.Format ("{0:yyyy-MM-dd-HH-mm-ss}-Beagle", DateTime.Now);
			string log_path = Path.Combine (PathFinder.LogDir, log_name);
			string log_link = Path.Combine (PathFinder.LogDir, "Beagle");

			// Open the log file and set it as the default
			// destination for log messages.
			// Also redirect stdout and stderr to the same file.
			FileStream log_stream = new FileStream (log_path,
								FileMode.Append,
								FileAccess.Write,
								FileShare.Write);
			TextWriter log_writer = new StreamWriter (log_stream);

			File.Delete (log_link);
			Mono.Posix.Syscall.symlink (log_path, log_link);

			Logger.DefaultWriter = log_writer;
			Logger.DefaultEcho = echo;

			if (! echo) {
				Console.SetOut (log_writer);
				Console.SetError (log_writer);

				// Redirect stdin to /dev/null
				FileStream dev_null_stream = new FileStream ("/dev/null",
									     FileMode.Open,
									     FileAccess.Read,
									     FileShare.ReadWrite);
				TextReader dev_null_reader = new StreamReader (dev_null_stream);
				Console.SetIn (dev_null_reader);
			}

			// Parse the contents of the BEAGLE_DEBUG environment variable
			// and adjust the default log levels accordingly.
			string debug = System.Environment.GetEnvironmentVariable ("BEAGLE_DEBUG");
			if (debug != null) {
				string[] debugArgs = debug.Split (',');
				foreach (string arg in debugArgs) {
					if (arg.Trim () == "all") {
						Logger.DefaultLevel = LogLevel.Debug;
					}
				}
				
				foreach (string arg_raw in debugArgs) {
					string arg = arg_raw.Trim ();

					if (arg.Length == 0 || arg == "all")
						continue;

					if (arg[0] == '-') {
						string name = arg.Substring (1);
						Logger log = Logger.Get (name);
						log.Level = LogLevel.Info;
					} else {
						Logger log = Logger.Get (arg);
						log.Level = LogLevel.Debug;
					}
				}
			}

			Logger.Log.Info ("Starting Beagle Daemon (version {0})", ExternalStringsHack.Version);
			Logger.Log.Debug ("Command Line: {0}",
					  Environment.CommandLine != null ? Environment.CommandLine : "(null)");


		}

		private static void OnServiceOwnerChanged (string serviceName,
							   string oldOwner,
							   string newOwner)
		{
			if (serviceName == "com.novell.Beagle" && newOwner == "") {
				DBusisms.BusDriver.ServiceOwnerChanged -= OnServiceOwnerChanged;
				Application.Quit ();
			}
		}


		public static int ReplaceExisting () 
		{
			Logger.Log.Info ("Attempting to replace another beagled.");
			DBus.Service service = DBus.Service.Get (DBusisms.Connection, "com.novell.Beagle");
			DBusisms.BusDriver.ServiceOwnerChanged += OnServiceOwnerChanged;
			do {
				Logger.Log.Info ("Building Remote Control Proxy");
				RemoteControlProxy proxy = RemoteControl.GetProxy (service);
				Logger.Log.Info ("Sending Shutdown");
				proxy.Shutdown ();
				Application.Run ();
			} while (! DBusisms.InitService ());

			return 0;
		} 

		private static void LogMemoryUsage ()
		{
			while (! Shutdown.ShutdownRequested) {
				Logger.Log.Debug ("Memory usage: VmSize={0}  GC.GetTotalMemory={1}",
						  SystemInformation.VmSize (),
						  GC.GetTotalMemory (false));
				Thread.Sleep (1000);
			}
		}
		
		public static int Main (string[] args)
		{
			// Process the command-line arguments

			bool arg_replace = false;
			bool arg_debug = false;
			bool arg_debug_inotify = false;
			bool arg_debug_memory = false;
			bool arg_network = false;
			bool arg_fg = false;
			int arg_port = 0;

			int i = 0;
			while (i < args.Length) {
				
				string arg = args [i];
				++i;
				string next_arg = i < args.Length ? args [i] : null;

				switch (arg) {

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

				case "--debug-inotify":
					arg_debug = true;
					arg_debug_inotify = true;
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

				default:
					Console.WriteLine ("Ignoring unknown argument '{0}'", arg);
					break;

				}
			}

			// Initialize logging.
			// If we saw the --debug arg, set the default logging level
			// accordingly.

			if (arg_debug)
				Logger.DefaultLevel = LogLevel.Debug;

			if (arg_debug_inotify)
				Inotify.Log.Level = LogLevel.Debug;
			else {
				// FIXME: A hard-wired constant for Inotify.Log doesn't really
				// belong here.
				// This overrides the default, so this logger is "decoupled".
				Inotify.Log.Level = LogLevel.Warn;
			}

			bool echo = false;
			if (arg_fg) {
				// If we are running in the foreground, we want to echo the log to stdout.
				echo = true;
			}
			SetupLog (echo);

			Stopwatch stopwatch = new Stopwatch ();

			try {
				Logger.Log.Debug ("Initializing D-BUS");
				DBusisms.Init ();
				Application.Init ();

				Logger.Log.Debug ("Acquiring com.novell.Beagle D-BUS service");
				if (!DBusisms.InitService ()) {
					if (arg_replace) {
						ReplaceExisting ();
					} else {
						Logger.Log.Fatal ("Could not register com.novell.Beagle service.  "
								  + "There is probably another beagled instance running.  "
								  + "Use --replace to replace the running service");
						return 1;
					}
				}
				// We want to spend as little time as possible
				// between InitService and actually being able 
				// to serve requests
				stopwatch.Start ();
				
			} catch (DBus.DBusException e) {
				Logger.Log.Fatal ("Couldn't connect to the session bus.  "
						  + "See http://beaglewiki.org/index.php/Installing%20Beagle "
						  + "for information on setting up a session bus.");
				Logger.Log.Fatal (e);
				return 1;
			} catch (Exception e) {
				Logger.Log.Fatal ("Could not initialize Beagle's bus connection.");
				Logger.Log.Fatal (e);
				return 1;
			}

			// Construct a query driver.
			Logger.Log.Debug ("Constructing QueryDriver");
			QueryDriver queryDriver;
			queryDriver = new QueryDriver ();

			try {
				// Construct and register our remote control object.
				Logger.Log.Debug ("Initializing RemoteControl");
				RemoteControlImpl rci = new RemoteControlImpl ();
				dbusObjects.Add (rci);
				DBusisms.Service.RegisterObject (rci, Beagle.DBusisms.RemoteControlPath);
				
				// Set up our D-BUS object factory.
				factory = new FactoryImpl (queryDriver);
				dbusObjects.Add (factory);
				DBusisms.Service.RegisterObject (factory, Beagle.DBusisms.FactoryPath);
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
					network = new NetworkService(queryDriver, arg_port);
					network.Start ();
				} catch {
					Logger.Log.Error ("Could not initialize network service"); 
				}
			}

#endif
			// Start the Global Scheduler thread
			Logger.Log.Debug ("Starting Scheduler thread");
			Scheduler.Global.Start ();

			// Start our memory-logging thread
			if (arg_debug_memory) {
				Thread th = new Thread (new ThreadStart (LogMemoryUsage));
				th.Start ();
			}

			// Start our Inotify threads
			Logger.Log.Debug ("Starting Inotify threads");
			Inotify.Start ();

			// Actually start up our QueryDriver.
			Logger.Log.Debug ("Starting QueryDriver");
			queryDriver.Start ();

			// Test if the FileAdvise stuff is working: This will print a
			// warning if not.  The actual advice calls will fail silently.
			FileAdvise.TestAdvise ();

			Shutdown.ShutdownEvent += OnShutdown;

			stopwatch.Stop ();

			Logger.Log.Debug ("Ready to accept requests after {0}", 
					 stopwatch);
			
			// Start our event loop.
			Logger.Log.Debug ("Starting main loop");
			Application.Run ();

			Logger.Log.Debug ("Leaving BeagleDaemon.Main");

			// Exiting will close the dbus connection, which
			// will release the com.novell.beagle service.
			return 0;
		}

		private static void OnShutdown ()
		{
			// Stop our Inotify threads
			Inotify.Stop ();

			// Shut down the global scheduler
			Scheduler.Global.Stop ();

			Logger.Log.Debug ("Unregistering Factory objects");
			factory.UnregisterAll ();
			Logger.Log.Debug ("Done unregistering Factory objects");
			Logger.Log.Debug ("Unregistering Daemon objects");
			foreach (object o in dbusObjects)
				DBusisms.Service.UnregisterObject (o);
			dbusObjects = null;
			Logger.Log.Debug ("Done unregistering Daemon objects");

		}
	}
}
