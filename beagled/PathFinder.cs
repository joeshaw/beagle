//
// PathFinder.cs
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
using System.IO;

namespace Beagle.Daemon {

	public class PathFinder {

		private PathFinder () { }

		static private string Prefix {
			get { return ExternalStringsHack.Prefix; }
		}

		static private string PkgLibDir {
			get { return ExternalStringsHack.PkgLibDir; }
		}

		static public string FilterDir {
			get { return Path.Combine (PkgLibDir, "Filters"); }
		}

		static public string RootDir {
			get {
				string homedir = Environment.GetEnvironmentVariable ("HOME");
				string dir = Path.Combine (homedir, ".beagle");
				if (! Directory.Exists (dir)) {
					Directory.CreateDirectory (dir);
					// Make sure that ~/.beagle directory is only
					// readable by the owner.
					Mono.Posix.Syscall.chmod (dir,
								  (Mono.Posix.FileMode) 448);
				}
				return dir;

			}
		}

		static public string LogDir {
			get {
				string dir = Path.Combine (RootDir, "Log");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				return dir;
			}
		}

		static private string AppDir {
			get {
				string dir = Path.Combine (RootDir, "App");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				return dir;
			}
		}

		// We probably shouldn't expose this.  Use it only for good, not
		// for evil.
		static public string AppDataFileName (string appName, string dataName)
		{
			// FIXME: should make sure appName & dataName don't
			// contain evil characters.
			return Path.Combine (AppDir,
					     string.Format ("{0}_-_-{1}", appName, dataName));
		}

		
		static public bool HaveAppData (string appName, string dataName)
		{
			return File.Exists (AppDataFileName (appName, dataName));
		}

		static public Stream ReadAppData (string appName, string dataName)
		{
			return new FileStream (AppDataFileName (appName, dataName),
					       FileMode.Open,
					       FileAccess.Read);
		}

		static public string ReadAppDataLine (string appName, string dataName)
		{
			if (! HaveAppData (appName, dataName))
				return null;

			StreamReader sr = new StreamReader (ReadAppData (appName, dataName));
			string line = sr.ReadLine ();
			sr.Close ();

			return line;
		}

		static public Stream WriteAppData (string appName, string dataName)
		{
			return new FileStream (AppDataFileName (appName, dataName),
					       FileMode.Create,
					       FileAccess.Write);
		}

		static public void WriteAppDataLine (string appName, string dataName, string line)
		{
			if (line == null) {
				string fileName = AppDataFileName (appName, dataName); 
				if (File.Exists (fileName))
					File.Delete (fileName);
				return;
			}

			StreamWriter sw = new StreamWriter (WriteAppData (appName, dataName));
			sw.WriteLine (line);
			sw.Close ();
		}
	}
	

}
