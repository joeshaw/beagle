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

using Beagle.Util;

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

		static public string BackendDir {
			get { return Path.Combine (PkgLibDir, "Backends"); }
		}

		// The user's personal files are under this and their dotfiles are in it.
		// It is usually found via HOME, but that can be overridden by setting
		// BEAGLE_HOME
		static string home_dir;
		static object home_dir_lock = new object ();
		static public string HomeDir {
			get {
				lock (home_dir_lock) {
					if (home_dir == null) {
						home_dir = Environment.GetEnvironmentVariable ("BEAGLE_HOME");
						if (home_dir == null)
							home_dir = Environment.GetEnvironmentVariable ("HOME");
						if (home_dir == null)
							throw new Exception ("Couldn't get HOME or BEAGLE_HOME");
						if (home_dir.EndsWith ("/"))
							home_dir = home_dir.Remove (home_dir.Length - 1, 1);
						if (! Directory.Exists (home_dir))
							throw new Exception ("Home directory '"+home_dir+"' doesn't exist");
					}
				}
				
				return home_dir;
			}
		}

		// The storage directory is the place where beagle stores its private data.
		// Fun fact #1: By default this is ~/.beagle
		// Fun fact #2: It can be overridden by setting BEAGLE_STORAGE
		static string storage_dir;
		static object storage_dir_lock = new object ();
		static public string StorageDir {
			get {
				lock (storage_dir_lock) {
					if (storage_dir == null) {
						storage_dir = Environment.GetEnvironmentVariable ("BEAGLE_STORAGE");

						if (storage_dir == null)
							storage_dir = Path.Combine (HomeDir, ".beagle");
						else if (storage_dir.EndsWith ("/"))
							storage_dir = storage_dir.Remove (storage_dir.Length - 1, 1);

						if (! Directory.Exists (storage_dir)) {
							Directory.CreateDirectory (storage_dir);
							// Make sure that the directory is only
							// readable by the owner.
							Mono.Posix.Syscall.chmod (storage_dir, (Mono.Posix.FileMode) 448);
						}
					}
				}

				return storage_dir;
			}
		}

		static public string LogDir {
			get {
				string dir = Path.Combine (StorageDir, "Log");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				return dir;
			}
		}

		static private string AppDir {
			get {
				string dir = Path.Combine (StorageDir, "App");
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
