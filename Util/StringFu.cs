//
// StringFu.cs
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
using System.Globalization;
using System.Text;

namespace Beagle.Util {

	public class StringFu {

		private StringFu () { } // class is static

		private const String timeFormat = "yyyyMMddHHmmss";

		static public string DateTimeToString (DateTime dt)
		{
			return dt.ToString (timeFormat);
		}

                static public DateTime StringToDateTime (String str)
                {
			return DateTime.ParseExact (str, timeFormat, CultureInfo.CurrentCulture);
                }

		static public string DateTimeToFuzzy (DateTime dt)
		{
			// FIXME: not particularly fuzzy
			return dt.ToString ();
		}

		static public string FileLengthToString (long len)
		{
			const long oneMb = 1024*1024;

			if (len < 0)
				return "*BadLength*";

			if (len < 1024)
				return String.Format ("{0} bytes", len);

			if (len < oneMb)
				return String.Format ("{0:0.0} kb", len/(double)1024);

			return String.Format ("{0:0.0} Mb", len/(double)oneMb);
		}

		// FIXME: This is pretty inefficient
		static public string[] FuzzySplit (string line)
		{
			int i;

			// Replace non-alphanumeric characters with spaces
			StringBuilder builder = new StringBuilder (line.Length);
			for (i = 0; i < line.Length; ++i) {
				if (Char.IsLetterOrDigit (line [i]))
					builder.Append (line [i]);
				else
					builder.Append (" ");
			}
			line = builder.ToString ();

			// Inject whitespace on all case changes except
			// from upper to lower.
			i = 0;
			int prevCase = 0;
			while (i < line.Length) {
				int thisCase;
				if (Char.IsUpper (line [i]))
					thisCase = +1;
				else if (Char.IsLower (line [i]))
					thisCase = -1;
				else
					thisCase = 0;

				if (prevCase != thisCase
				    && !(prevCase == +1 && thisCase == -1)) {
					line = line.Substring (0, i) + " " + line.Substring (i);
					++i;
				}

				prevCase = thisCase;
				
				++i;
			}
			
			// Filter out empty parts
			ArrayList partsArray = new ArrayList ();
			foreach (string str in line.Split (' ')) {
				if (str != "")
					partsArray.Add (str);
			}

			// Assemble the array to return
			string[] parts = new string [partsArray.Count];
			for (i = 0; i < partsArray.Count; ++i)
				parts [i] = (string) partsArray [i];
			return parts;
		}
		
		// Match strings against patterns that are allowed to contain
		// glob-style * wildcards.
		// This recursive implementation is not particularly efficient,
		// and probably will fail for weird corner cases.
		static public bool GlobMatch (string pattern, string str)
		{
			if (pattern == "*")
				return true;
			else if (pattern.StartsWith ("**"))
				return GlobMatch (pattern.Substring (1), str);
			else if (str == "" && pattern != "")
				return false;

			int i = pattern.IndexOf ('*');
			if (i == -1)
				return pattern == str;
			else if (i > 0 && i < str.Length)
				return pattern.Substring (0, i) == str.Substring (0, i)
					&& GlobMatch (pattern.Substring (i), str.Substring (i));
			else if (i == 0)
				return GlobMatch (pattern.Substring (1), str.Substring (1))
					|| GlobMatch (pattern.Substring (1), str)
					|| GlobMatch (pattern, str.Substring (1));
			
			return false;
		} 
	}
}
