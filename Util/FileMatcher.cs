//
// FileMatcher.cs
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

namespace Beagle.Util {
	public class FileMatcher {

		private class FilePattern {
			private string exactMatch;
			private string prefix;
			private string suffix;
			private Regex  regex;

			public FilePattern (string pattern)
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
		}
		
		static ArrayList defaultPatterns = new ArrayList ();

		static FileMatcher ()
		{
			// Add our default patterns.
			AddDefaultPattern (".*",
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
			string neverIndex = Path.Combine (home, ".neverindex");
			if (File.Exists (neverIndex)) {
				StreamReader sr = new StreamReader (neverIndex);
				string line;
				while ((line = sr.ReadLine ()) != null) {
					line = line.Trim ();
					if (line.Length > 0)
						AddDefaultPattern (line);
				}
			}
		}

		static public void AddDefaultPattern (string pattern)
		{
			defaultPatterns.Add (new FilePattern (pattern));
		}

		static public void AddDefaultPattern (params string[] patterns)
		{
			foreach (string pattern in patterns)
				AddDefaultPattern (pattern);
		}
		
		/////////////////////////////////////////////////////////////

		private bool matchAnything = false;
		private ArrayList patterns = new ArrayList ();

		public FileMatcher ()
		{ }

		public FileMatcher (string path)
		{
			Load (path);
		}
		
		public void AddPattern (string pattern)
		{
			patterns.Add (new FilePattern (pattern));
		}
		
		public void AddPattern (params string[] patterns)
		{
			foreach (string pattern in patterns)
				AddPattern (pattern);
		}
		
		public void Load (string path)
		{
			StreamReader sr = new StreamReader (path);
			string line;
			bool addedSomething = false;
			while ((line = sr.ReadLine ()) != null) {
				line = line.Trim ();
				if (line.Length > 0) {
					AddPattern (line);
					addedSomething = true;
				}
			}

			// 
			if (! addedSomething)
				matchAnything = true;
		}
		
		private bool IsMatchInternal (string name)
		{
			if (matchAnything)
				return true;
			
			foreach (FilePattern pattern in defaultPatterns) {
				if (pattern.IsMatch (name))
					return true;
			}

			foreach (FilePattern pattern in patterns) {
				if (pattern.IsMatch (name))
					return true;
			}

			return false;
		}

		public bool IsMatch (string path)
		{
			return IsMatchInternal (Path.GetFileName (path));
		}

		public bool IsMatch (FileSystemInfo info)
		{
			return IsMatchInternal (info.Name);
		}
	}

#if false
	class Test {
		static void Main (string [] args)
		{
			FileMatcher matcher = new FileMatcher ();

			foreach (string arg in args) {
				if (arg [0] == '+')
					matcher.AddPattern (arg.Substring (1));
			}

			foreach (string arg in args) {
				if (arg [0] != '+') {
					Console.WriteLine ("{0} {1}" ,
							   matcher.IsMatch (arg) ? "YES" : "no ",
							   arg);
				}
			}
		}
	}
#endif
}
