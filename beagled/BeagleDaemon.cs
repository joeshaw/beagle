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

using DBus;
using Gtk;
using System.Reflection;
using System;
using System.IO;
using Mono.Posix;
using Beagle.Util;
using System.Collections;

namespace Beagle.Daemon {
	class BeagleDaemon {

		private static ArrayList dbusObjects = new ArrayList ();
		private static FactoryImpl factory = null;

		private static bool Daemonize ()
		{
			Directory.SetCurrentDirectory ("/");

			int pid = Syscall.fork ();

			switch (pid) {
			case -1: // error
				Console.WriteLine ("Error trying to daemonize");
				return false;

			case 0: // child
				int fd = Syscall.open ("/dev/null", OpenFlags.O_RDWR);
				
				if (fd >= 0) {
					Syscall.dup2 (fd, 0);
					Syscall.dup2 (fd, 1);
					Syscall.dup2 (fd, 2);
				}

				Syscall.umask (022);
				Syscall.setsid ();
				break;

			default: // parent
				Syscall.exit (0);
				break;
			}

			return true;
		}

		private static void SetupLog (string logPath) {
			if (logPath != null) {
				FileStream fs = new FileStream (logPath,
								System.IO.FileMode.Append,
								FileAccess.Write);
				Logger.DefaultWriter = new StreamWriter (fs);
			}

			string debug = System.Environment.GetEnvironmentVariable ("BEAGLE_DEBUG");
			if (debug != null) {
				string[] debugArgs = debug.Split (',');
				foreach (string arg in debugArgs) {
					if (arg == "all") {
						Logger.DefaultLevel = LogLevel.Debug;
					}
				}
				
				foreach (string arg in debugArgs) {
					if (arg == "all")
						continue;

					if (arg[0] == '-') {
						string logName = arg.Substring (1);
						Logger log = Logger.Get (logName);
						log.Level = LogLevel.Info;
					} else {
						Logger log = Logger.Get (arg);
						log.Level = LogLevel.Debug;
					}
				}
			}
		}

		private static void OnServiceOwnerChanged (string serviceName,
							   string oldOwner,
							   string newOwner)
		{
			if (serviceName == "com.novell.Beagle") {

				if (newOwner == "") {
					DBusisms.BusDriver.ServiceOwnerChanged -= OnServiceOwnerChanged;
					Application.Quit ();
				}
			}
		}


		public static int ReplaceExisting () 
		{
			DBus.Service service = DBus.Service.Get (DBusisms.Connection, "com.novell.Beagle");
			DBusisms.BusDriver.ServiceOwnerChanged += OnServiceOwnerChanged;
			do {
				Ping proxy = (Ping)service.GetObject (typeof (Ping), "/com/novell/Beagle/Ping");
				proxy.Shutdown ();
				Application.Run ();
			} while (!DBusisms.InitService ());

			return 0;
		} 
		
		public static int Main (string[] args)
		{
			// Process the command-line arguments

			bool arg_replace = false;
			bool arg_out = false;
			bool arg_no_fork = false;

			int i = 0;
			while (i < args.Length) {
				
				string arg = args [i];
				++i;
				string next_arg = i < args.Length ? args [i] : null;

				switch (arg) {

				case "--replace":
					arg_replace = true;
					break;

				case "--out":
					arg_out = true;
					break;

				case "--nofork":
				case "--no-fork":
					arg_no_fork = true;
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

				default:
					Console.WriteLine ("Ignoring unknown argument '{0}'", arg);
					break;

				}

			}

			try {
				DBusisms.Init ();
				Application.Init ();
				
				if (!DBusisms.InitService ()) {
					if (arg_replace) {
						ReplaceExisting ();
					} else {
						System.Console.WriteLine ("Could not register com.novell.Beagle service.  There is probably another beagled instance running.  Use --replace to replace the running service");
						return 1;
					}
				}
			} catch (DBus.DBusException e) {
				System.Console.WriteLine ("Couldn't connect to the session bus.  See http://beaglewiki.org/index.php/Installing%20Beagle for information on setting up a session bus.");
				System.Console.WriteLine (e);
				return 1;
			} catch (Exception e) {
				System.Console.WriteLine ("Could not initialize Beagle's bus connection:\n{0}", e);
				return 1;
			}
			
			try {
				// FIXME: this could be better, but I don't want to
				// deal with serious cmdline parsing today
				if (arg_out) {
					SetupLog (null);
				} else {
					string logPath = Path.Combine (PathFinder.LogDir,
								       "Beagle");
					SetupLog (logPath);
				}
			} catch (Exception e) {
				System.Console.WriteLine ("Couldn't initialize logging.  This could mean that another Beagle instance is running: {0}", e);
				return 1;
			}

			QueryDriver queryDriver;
			try {

				// Construct and register our ping object.
				Ping ping = new Ping ();
				dbusObjects.Add (ping);
				DBusisms.Service.RegisterObject (ping, "/com/novell/Beagle/Ping");

				// Construct a query driver.  Among other things, this
				// loads and initializes all of the IQueryables.
				queryDriver = new QueryDriver ();
				
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
			
			Logger.Log.Info ("Beagle daemon started"); 

			if (! arg_no_fork) {
				if (! Daemonize ())
					return 1;
			}

			queryDriver.Start ();
			
			Shutdown.ShutdownEvent += OnShutdown;
			// Start our event loop.
			Application.Run ();

			Logger.Log.Info ("done");

			// Exiting will close the dbus connection, which
			// will release the com.novell.beagle service.
			return 0;
		}

		private static void OnShutdown ()
		{
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
