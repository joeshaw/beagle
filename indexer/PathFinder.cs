//
// PathFinder.cs
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
using System.IO;

namespace Beagle {

	public class PathFinder {

		private PathFinder () { }

		static public String RootDir {
			get {
				String homedir = Environment.GetEnvironmentVariable ("HOME");
				String dir = Path.Combine (homedir, ".beagle");
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

		static private String AppDir {
			get {
				String dir = Path.Combine (RootDir, "App");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				return dir;
			}
		}

		// We probably shouldn't expose this.  Use it only for good, not
		// for evil.
		static public String AppDataFileName (String appName, String dataName)
		{
			// FIXME: should make sure appName & dataName don't
			// contain evil characters.
			return Path.Combine (AppDir,
					     String.Format ("{0}_-_-{1}", appName, dataName));
		}

		
		static public bool HaveAppData (String appName, String dataName)
		{
			return File.Exists (AppDataFileName (appName, dataName));
		}

		static public Stream ReadAppData (String appName, String dataName)
		{
			return new FileStream (AppDataFileName (appName, dataName),
					       FileMode.Open,
					       FileAccess.Read);
		}

		static public String ReadAppDataLine (String appName, String dataName)
		{
			if (! HaveAppData (appName, dataName))
				return null;

			StreamReader sr = new StreamReader (ReadAppData (appName, dataName));
			String line = sr.ReadLine ();
			sr.Close ();

			return line;
		}

		static public Stream WriteAppData (String appName, String dataName)
		{
			return new FileStream (AppDataFileName (appName, dataName),
					       FileMode.Create,
					       FileAccess.Write);
		}

		static public void WriteAppDataLine (String appName, String dataName, String line)
		{
			if (line == null) {
				String fileName = AppDataFileName (appName, dataName); 
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
