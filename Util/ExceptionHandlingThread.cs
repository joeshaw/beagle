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
using System.Threading;

namespace Beagle.Util {

	public class ExceptionHandlingThread {

		private static Hashtable live_threads = new Hashtable ();
		private Thread thread;
		private ThreadStart method;

		private ExceptionHandlingThread (ThreadStart method)
		{
			this.method = method;
		}

		private void ThreadStarted ()
		{
			try {
				this.method ();
			} catch (Exception e) {
				Logger.Log.Warn ("Exception caught while executing {0}:{1}",
						 this.method.Target, this.method.Method);
				Logger.Log.Warn (e);
			}

			lock (live_threads)
				live_threads.Remove (this.thread);
		}

		public static Thread Start (ThreadStart method)
		{
			ExceptionHandlingThread eht = new ExceptionHandlingThread (method);

			eht.thread = new Thread (new ThreadStart (eht.ThreadStarted));

			eht.thread.Name = String.Format ("ExceptionHandlingThread: {0}:{1}",
							 method.Target, method.Method);

			lock (live_threads)
				live_threads [eht.thread] = eht.thread.Name;

			eht.thread.Start ();

			return eht.thread;
		}

		public static void SpewLiveThreads ()
		{
			bool have_live_thread = false;

			lock (live_threads) {
				foreach (string str in live_threads.Values) {
					Logger.Log.Debug ("Live ExceptionHandlingThread: {0}", str);
					have_live_thread = true;
				}
			}

			if (! have_live_thread)
				Logger.Log.Debug ("No live ExceptionHandlingThreads!");
		}
	}
}
