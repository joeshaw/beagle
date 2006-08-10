//
// ExceptionHandlingThread.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Runtime.InteropServices;
using System.Threading;

namespace Beagle.Util {

	public class ExceptionHandlingThread {

		private static ArrayList live_threads = new ArrayList ();
		private Thread thread;
		private ThreadStart method;

		private ExceptionHandlingThread (ThreadStart method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			this.method = method;
		}

		private void ThreadStarted ()
		{
			this.thread.Name = String.Format ("EHT {0:00000} {1}:{2}", wrap_gettid (), method.Target == null ? "(static)" : method.Target.ToString (), method.Method);

			try {
				this.method ();
			} catch (ThreadAbortException e) {
				Logger.Log.Debug ("{0}:\n{1}\n", this.thread.Name, e.StackTrace);
			} catch (Exception e) {
				Logger.Log.Warn (e, "Exception caught while executing {0}:{1}",
						 this.method.Target, this.method.Method);
			}

			lock (live_threads)
				live_threads.Remove (this.thread);
		}

		public static Thread Start (ThreadStart method)
		{
			ExceptionHandlingThread eht = new ExceptionHandlingThread (method);

			eht.thread = new Thread (new ThreadStart (eht.ThreadStarted));

			lock (live_threads)
				live_threads.Add (eht.thread);

			eht.thread.Start ();

			return eht.thread;
		}

		public static void SpewLiveThreads ()
		{
			bool have_live_thread = false;

			lock (live_threads) {
				foreach (Thread t in live_threads) {
					Logger.Log.Debug ("Live ExceptionHandlingThread: {0}", t.Name);
					have_live_thread = true;
				}
			}

			if (! have_live_thread)
				Logger.Log.Debug ("No live ExceptionHandlingThreads!");
		}

		public static void AbortThreads ()
		{
			ArrayList cancel_threads = null;

			// Copy the list to avoid recursively locking
			lock (live_threads)
				cancel_threads = (ArrayList) live_threads.Clone ();

			foreach (Thread t in cancel_threads) {
				Logger.Log.Debug ("Aborting thread: {0}", t.Name);
				t.Abort ();
			}
		}

		[DllImport ("libbeagleglue")]
		static extern uint wrap_gettid ();
	}
}
