//
// FileNameFilter.cs
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
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;

namespace Beagle.Daemon.FileSystemQueryable {

	public class FileNameFilter {

		private class Pattern {
			private string exactMatch;
			private string prefix;
			private string suffix;
			private Regex  regex;

			public Pattern (string pattern)
			{
				if (pattern.StartsWith ("/") && pattern.EndsWith ("/")) {
					regex = new Regex (pattern.Substring (1, pattern.Length - 2));
					return;
				}

				int i = pattern.IndexOf ('*');
				if (i == -1) {
					exactMatch = pattern;
				} else {
					if (i > 0)
						prefix = pattern.Substring (0, i);
					if (i < pattern.Length-1)
						suffix = pattern.Substring (i+1);
				}
			}

			public bool IsMatch (string name)
			{
				if (exactMatch != null)
					return name == exactMatch;
				if (prefix != null && ! name.StartsWith (prefix))
					return false;
				if (suffix != null && ! name.EndsWith (suffix))
					return false;
				if (regex != null && ! regex.IsMatch (name))
					return false;
				return true;
			}

			public override string ToString ()
			{
				string str = "[pattern:";
				if (exactMatch != null)
					str += " exact=" + exactMatch;
				if (prefix != null)
					str += " prefix=" + prefix;
				if (suffix != null)
					str += " suffix=" + suffix;
				if (regex != null)
					str += " regex=" + regex.ToString ();
				str += "]";
				return str;
			}
		}

		static ArrayList LoadPatterns (string filename)
		{
			if (! File.Exists (filename))
				return null;
			
			ArrayList array = new ArrayList ();
			StreamReader sr = new StreamReader (filename);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				line = line.Trim ();
				if (line.Length > 0) {
					Pattern pattern = new Pattern (line);
					array.Add (pattern);
				}
			}
			
			return array;
		}

		//////////////////////////////////////////////////////////////////////
		
		private ArrayList defaultPatternsToIgnore;

		private void SetupDefaultPatternsToIgnore ()
		{
			defaultPatternsToIgnore = new ArrayList ();

			// Add our default skip patterns.
			// FIXME: This probably shouldn't be hard-wired.  Or should it?
			AddDefaultPatternToIgnore (".*",
						   "*~",
						   "#*#",
						   "*.cs", // FIXME: we skip other source code...
						   "*.o",
						   "*.a",
						   "*.S",
						   "*.la",
						   "*.lo",
						   "*.so",
						   "*.exe",
						   "*.dll",
						   "*.com",
						   "*.csproj",
						   "*.dsp",
						   "*.dsw",
						   "*.m4",
						   "*.pc",
						   "*.pc.in",
						   "*.in.in",
						   "*.omf",
						   "*.aux",
						   "po",
						   "aclocal",
						   "Makefile",
						   "Makefile.am",
						   "Makefile.in",
						   "CVS");
			
			// Read the ~/.neverindex file, which contains patterns
			// for files that should always be ignored.
			string home = Environment.GetEnvironmentVariable ("HOME");
			string neverindex = Path.Combine (home, ".neverindex");
			ArrayList patterns = LoadPatterns (neverindex);
			if (patterns != null) {
				foreach (Pattern pattern in patterns)
					defaultPatternsToIgnore.Add (pattern);
			}
		}

		public void AddDefaultPatternToIgnore (string pattern)
		{
			defaultPatternsToIgnore.Add (new Pattern (pattern));
		}
		
		public void AddDefaultPatternToIgnore (params string[] patterns)
		{
			foreach (string pattern in patterns)
				AddDefaultPatternToIgnore (pattern);
		}
		
		/////////////////////////////////////////////////////////////

		public FileNameFilter ()
		{
			SetupDefaultPatternsToIgnore ();
		}

		/////////////////////////////////////////////////////////////

		private class PerDirectoryInfo {
			string dir;
			DateTime noindexTimestamp;
			DateTime noindexLastCheck;
			ArrayList patternsToIgnore;

			public PerDirectoryInfo (string _dir)
			{
				dir = _dir;
				ConditionallyLoad ();
			}

			// If the directory's .noindex file has changed, load it.
			private void ConditionallyLoad ()
			{
				// Only re-check the .noindex file every 11 seconds.
				DateTime now = DateTime.Now;
				if ((now - noindexLastCheck).TotalSeconds < 11)
					return;
				noindexLastCheck = now;

				string noindex = Path.Combine (dir, ".noindex");

				if (File.Exists (noindex)) {
					DateTime timestamp = File.GetLastWriteTime (noindex);
					if (timestamp != noindexTimestamp) {
						patternsToIgnore = LoadPatterns (noindex);
						noindexTimestamp = timestamp;
					}
				} else {
					patternsToIgnore = null;
				}
			}

			public bool Ignore (string name)
			{
				ConditionallyLoad ();
				if (patternsToIgnore == null)
					return false;
				if (patternsToIgnore.Count == 0) // i.e. IgnoreAll
					return true;
				foreach (Pattern pattern in patternsToIgnore)
					if (pattern.IsMatch (name))
						return true;

				return false;
			}
		}

		
		private Hashtable perDirectoryCache = new Hashtable ();

		private PerDirectoryInfo GetPerDirectoryInfo (string dir)
		{
			if (dir == null)
				return null;

			PerDirectoryInfo info;
			lock (perDirectoryCache) {
				info = perDirectoryCache [dir] as PerDirectoryInfo;
				if (info == null) {
					info = new PerDirectoryInfo (dir);
					perDirectoryCache [dir] = info;
				}
			}
			return info;
		}

		// FIXME: We could make this more efficient by storing more information.
		// In particular, if ~/foo is an IgnoreAll directory, we could store
		// that info in the PerDirectoryInfo of subdir ~/foo/bar so that
		// we could avoid walking up the chain of directories.
		public bool Ignore (string path)
		{
			path = Path.GetFullPath (path);

			if (path == Environment.GetEnvironmentVariable ("HOME"))
				return false;

			string name = Path.GetFileName (path);

			foreach (Pattern pattern in defaultPatternsToIgnore)
				if (pattern.IsMatch (name))
					return true;

			string dir = Path.GetDirectoryName (path);
			PerDirectoryInfo perDir = GetPerDirectoryInfo (dir);

			if (perDir == null || perDir.Ignore (name))
				return true;

			// A file should be ignored if any of its parent directories
			// is ignored.
			return Ignore (dir);
		}

		public bool Ignore (FileSystemInfo info)
		{
			return Ignore (info.FullName);
		}
	}		


#if true
	class Test {
		static void Main (string [] args)
		{
			FileNameFilter filter = new FileNameFilter ();

			foreach (string arg in args) {
				FileSystemInfo info = null;
				if (Directory.Exists (arg))
					info = new DirectoryInfo (arg);
				else if (File.Exists (arg))
					info = new FileInfo (arg);

				if (info != null)
					Console.WriteLine ("{0} {1}",
							   filter.Ignore (info) ? "IGNORE" : "      ",
							   info.FullName);
			}
		}
	}
#endif
}
