//
// Shutdown.cs
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
using System.Threading;

namespace Beagle.Daemon {

	public class Shutdown {

		static object shutdownLock = new object ();
		static int numberOfWorkers = 0;
		static bool shutdownRequested = false;

		static void WorkerBegin ()
		{
			lock (shutdownLock) {
				++numberOfWorkers;
			}
		}

		static void WorkerEnd ()
		{
			lock (shutdownLock) {
				--numberOfWorkers;
				Monitor.Pulse (shutdownLock);
			}
		}

		static public bool ShutdownRequested {
			get { return shutdownRequested; }
		}

		static public void BeginShutdown ()
		{
			lock (shutdownLock) {
				if (shutdownRequested)
					return;

				shutdownRequested = true;

				Console.WriteLine ("Beginning shutdown");
				int count = 0;

				while (numberOfWorkers > 0) {
					++count;
					Console.WriteLine ("({0}) Waiting for {1} worker{2}...",
							   count,
							   numberOfWorkers,
							   numberOfWorkers > 1 ? "s" : "");
					Monitor.Wait (500);
				}

				Gtk.Application.Quit ();
			}
		}
 
	}
}
