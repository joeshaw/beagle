//
// PathFinder.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;

namespace Dewey {

	public class PathFinder {

		private PathFinder () { }

		static public String RootDir {
			get {
				String homedir = Environment.GetEnvironmentVariable ("HOME");
				String dir = Path.Combine (homedir, ".dewey");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				// FIXME: We should set some reasonable permissions on the
				// .dewey directory.
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

		static private String AppDataFileName (String appName, String dataName)
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
