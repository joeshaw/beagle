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
//using Beagle;
using System.Reflection;
using System;
using System.IO;
using Mono.Posix;

namespace Beagle.Daemon {
	class BeagleDaemon {

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

		public static int Main (string[] args)
		{
			if (Array.IndexOf (args, "--nofork") == -1 && Array.IndexOf (args, "--no-fork") == -1) {
				if (! Daemonize ())
					return 1;
			}

			Application.Init ();

			// Connect to the session bus, acquire the com.novell.Beagle
			// service, and set up a BusDriver.
			DBusisms.Init ();

			// Construct and register our ping object.
			Ping ping = new Ping ();
			DBusisms.Service.RegisterObject (ping, "/com/novell/Beagle/Ping");

			// Construct a query driver.  Among other things, this
			// loads and initializes all of the IQueryables.
			QueryDriver queryDriver = new QueryDriver ();

			// Set up our D-BUS object factory.
			FactoryImpl factory = new FactoryImpl (queryDriver);
			DBusisms.Service.RegisterObject (factory, Beagle.DBusisms.FactoryPath);

			// Start our event loop.
			Application.Run ();

			return 0;
		}
	}
}
