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

		public static int Main (string[] args)
		{
			try {
				// FIXME: this could be better, but I don't want to
				// deal with serious cmdline parsing today
				if (Array.IndexOf (args, "--out") != -1) {
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

			try {
				Application.Init ();
				
				DBusisms.Init ();

				if (!DBusisms.InitService ()) {
					Logger.Log.Fatal ("Could not register com.novell.Beagle service.  There is probably another beagled instance running.");
					return 1;
				}
			} catch (DBus.DBusException e) {
				Logger.Log.Fatal ("Couldn't connect to the session bus.  See http://beaglewiki.org/index.php/Installing%20Beagle for information on setting up a session bus.");
				Logger.Log.Debug (e);
				return 1;
			} catch (Exception e) {
				Logger.Log.Fatal ("Could not initialize Beagle's bus connection:\n{0}", e);
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
				FactoryImpl factory = new FactoryImpl (queryDriver);
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

			if (Array.IndexOf (args, "--nofork") == -1 && Array.IndexOf (args, "--no-fork") == -1) {
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
			foreach (object o in dbusObjects)
				DBusisms.Service.UnregisterObject (o);
			dbusObjects = null;
		}

	}
}
