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

#if ENABLE_WEBSVC
using Beagle.websvc;
using MA=Mono.ASPNET;
#endif

namespace Beagle.Daemon {
	class BeagleDaemon {

		public static Thread MainLoopThread = null;

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


		}

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


#if ENABLE_WEBSVC
		static Mono.ASPNET.ApplicationServer appServer = null;
		const string DEFAULT_XSP_ROOT="/usr/local/share/doc/xsp/test";
		const string DEFAULT_XSP_PORT = "8888";
		static string[] xsp_param = {"--port", "8888", "--root", DEFAULT_XSP_ROOT, 
			"--applications", "/:" + DEFAULT_XSP_ROOT + ",/beagle:" + DEFAULT_XSP_ROOT + "/beagle", "--nonstop"};
#endif 
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

#if ENABLE_WEBSVC
			bool web_global = false;
			bool web_start = false;
			string web_port = DEFAULT_XSP_PORT;
			string web_rootDir = DEFAULT_XSP_ROOT;
#endif

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

#if ENABLE_WEBSVC
				case "--web-global":
					web_global = true;
					break;

				case "--web-start":
					web_start = true;
					break;

				case "--web-port":
					web_port = next_arg;
					++i; 
					web_start = true;
					break;

				case "--web-root":
					web_rootDir = next_arg;
					++i; 
					web_start = true;
					break;
#endif 
				default:
					Console.WriteLine ("Ignoring unknown argument '{0}'", arg);
					break;

				}
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


			if (Environment.UserName == "root") {
				Logger.Log.Error ("You can not run beagle as root.");
				Logger.Log.Error ("Beagle is designed to be run from your own user account.");
				Environment.Exit (-1);
			}

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

#if ENABLE_WEBSVC		
			//Beagle Web, WebService access initialization code:
			string msg = "Started beagledWeb & beagledWebSvc Listener. Internal Web Server NOT started.";

			xsp_param[1] = web_port;
			xsp_param[3] = web_rootDir;
			if (web_start)	
			{
				//Start beagled internal web server (bgXsp)
				int ret = Mono.ASPNET.Server.initXSP(xsp_param, out appServer);
				msg = "Started beagledWeb & beagledWebSvc Listener \n";
				if (ret == 0)
					msg += "Internal Web Server started";
				else
					msg += "Error starting Internal Web Server";
			}	

			//start web-access server first
			beagledWeb.init (web_global);

			//Next start web-service server 
			beagledWebSvc.init (web_global);

			//Console.WriteLine (msg);
			Logger.Log.Debug (msg);

			msg = "Global WebAccess " + (web_global ? "Enabled":"Disabled");
			//Console.WriteLine (msg);
			Logger.Log.Debug (msg);
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

			Shutdown.ShutdownEvent += OnShutdown;

			stopwatch.Stop ();

			Logger.Log.Debug ("Ready to accept requests after {0}", 
					 stopwatch);
		
			// Start our event loop.
			Logger.Log.Debug ("Starting main loop");
			Application.Run ();

#if ENABLE_WEBSVC
			if (appServer != null) {
			    	appServer.Stop(); 
				appServer = null;
			}
#endif
			Logger.Log.Debug ("Leaving BeagleDaemon.Main");

			// Exiting will close the dbus connection, which
			// will release the com.novell.beagle service.
			return 0;
		}

		private static void OnShutdown ()
		{
#if ENABLE_WEBSVC
			if (appServer != null) {
			    	appServer.Stop(); 
				appServer = null;
			}
#endif
			// Stop our Inotify threads
			Inotify.Stop ();

			// Shut down the global scheduler
			Scheduler.Global.Stop ();

			// Stop our indexing helper process
			IndexHelperFu.Stop ();

#if false
			Logger.Log.Debug ("Unregistering Factory objects");
			factory.UnregisterAll ();
			Logger.Log.Debug ("Done unregistering Factory objects");
#endif

			Logger.Log.Debug ("Unregistering Daemon objects");
			DBusisms.UnregisterAll ();
			Logger.Log.Debug ("Done unregistering Daemon objects");

		}
	}
}
