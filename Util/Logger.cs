//
// Logger.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Beagle.Util {

	public class Logger {

		FileStream fs;
		StreamWriter sw;
		FileInfo lockFile;

		public Logger (string logPath)
		{
			lockFile = new FileInfo (logPath + ".LOCK");

#if false
			fs = new FileStream (logPath, FileMode.Append, FileAccess.Write);
			sw = new StreamWriter (fs);
#endif
		}

		private bool AcquireLock ()
		{
#if false
			try {
				lock (this) {
					FileStream L = new FileStream (lockFile.FullName,
								       FileMode.CreateNew,
								       FileAccess.Write);
					L.Close ();
				}
			} catch {
				return false;
			}
#endif
			return true;
		}

		private void WaitForLock ()
		{
#if false
			// After a certain amount of time, just stop waiting
			// for the lock and write to the log.
			int countdown = 10;
			while (countdown > 0) {
				if (AcquireLock ())
					return;
				Thread.Sleep (100);
				--countdown;
			}
			sw.WriteLine ("***** Log lock timed out!");
#endif
		}

		private void ReleaseLock ()
		{
#if false
			lock (this) {
				try {
					lockFile.Delete ();
				} catch { }
			}
#endif
		}
		
		private string GetStamp ()
		{
			return string.Format ("{0}[{1}] {2}",
					      Process.GetCurrentProcess().Id,
					      Environment.CommandLine,
					      DateTime.Now.ToString ("yy-MM-dd HH.mm.ss.ff"));
		}

		private void LogRaw (string format, params object[] args)
		{
#if false
			sw.Write (GetStamp ());
			sw.Write (": ");
			sw.WriteLine (String.Format (format, args));
			sw.Flush ();
			fs.Flush ();
#endif
		}

		public void Log (string format, params object[] args)
		{
			WaitForLock ();
			LogRaw (format, args);
			ReleaseLock ();
		}

		public void Log (Exception e)
		{
			WaitForLock ();
			LogRaw ("Exception Begin");
			LogRaw (e.Message);
			LogRaw (e.StackTrace);
			LogRaw ("Exception End");
			ReleaseLock ();
			
		}

	}
}

