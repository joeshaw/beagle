//
// Crawler.cs
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
		
		static ArrayList defaultPatterns = new ArrayList ();
		ArrayList patterns = new ArrayList ();
		
		static Regex PatternToRegex (String pattern)
		{
			pattern = "^" + Regex.Escape (pattern) + "$";
			pattern = pattern.Replace ("\\?", ".");
			pattern = pattern.Replace ("\\*", ".*");
			return new Regex (pattern);
		}
		
		public void Add (String pattern)
		{
			patterns.Add (PatternToRegex (pattern));
		}
		
		public void Add (params String[] patterns)
		{
			foreach (String pattern in patterns)
				Add (pattern);
		}
		
		static public void AddDefault (String pattern)
		{
			defaultPatterns.Add (PatternToRegex (pattern));
		}
		
		static public void AddDefault (params String[] patterns)
		{
			foreach (String pattern in patterns)
				AddDefault (pattern);
		}
		
		public void Load (String path)
		{
			StreamReader sr = new StreamReader (path);
			String line;
			while ((line = sr.ReadLine ()) != null) {
				line = line.Trim ();
				if (line.Length > 0)
					Add (line);
			}
		}
		
		public bool IsEmpty {
			get { return patterns.Count == 0; }
		}
		
		public bool IsMatch (String path)
		{
			String fileName = Path.GetFileName (path);
			foreach (Regex regex in defaultPatterns)
				if (regex.IsMatch (fileName))
					return true;
			foreach (Regex regex in patterns)
				if (regex.IsMatch (fileName))
					return true;
			return false;
		}
	}
}
