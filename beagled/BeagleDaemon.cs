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

		static void ScanAssemblyForHandlers (Assembly assembly,
						     IndexerQueue indexerQueue)
		{
			foreach (Type t in assembly.GetTypes ()) {
				if (t.IsSubclassOf (typeof (PreIndexHandler))) {

					PreIndexHandler handler = (PreIndexHandler) Activator.CreateInstance (t);
					indexerQueue.PreIndexingEvent += handler.Run;
				}
				if (t.IsSubclassOf (typeof (PostIndexHandler))) {
					PostIndexHandler handler = (PostIndexHandler) Activator.CreateInstance (t);
					indexerQueue.PostIndexingEvent += handler.Run;
				}
			}
		}

		static void LoadHandlers (IndexerQueue indexerQueue) 
		{
			// FIXME: load handlers from plugins
			ScanAssemblyForHandlers (Assembly.GetExecutingAssembly (), 
						 indexerQueue);
		}

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
			if (Array.IndexOf (args, "--nofork") == -1) {
				if (!Daemonize ())
					return 1;
			}

			Application.Init ();

			DBusisms.Init ();


			IndexerQueue indexerQueue = new IndexerQueue ();
			LoadHandlers (indexerQueue);


			Indexer indexer = new Indexer (indexerQueue);
			DBusisms.Service.RegisterObject (indexer,
							 Beagle.DBusisms.IndexerPath);



			Application.Run ();
			return 0;
		}
	}
}
