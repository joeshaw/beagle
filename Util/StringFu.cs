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
using System.IO;
using System.Text;
using System.Xml;

using Mono.Posix;

namespace Beagle.Util {

	public class StringFu {

		private StringFu () { } // class is static

		private const String timeFormat = "yyyyMMddHHmmss";

		static public string DateTimeToString (DateTime dt)
		{
			return dt.ToString (timeFormat);
		}

                static public DateTime StringToDateTime (string str)
                {
			if (str == null || str == "")
				return new DateTime ();

			return DateTime.ParseExact (str, timeFormat, CultureInfo.CurrentCulture);
                }

		static public string DateTimeToFuzzy (DateTime dt)
		{
			DateTime today = DateTime.Today;
			TimeSpan sinceToday = today - dt;
			
			string date = null, time = null;

			if (sinceToday.TotalDays <= 0)
				date = Catalog.GetString ("Today");
			else if (sinceToday.TotalDays < 1)
				date = Catalog.GetString ("Yesterday");
			else if (today.Year == dt.Year)
				date = dt.ToString ("MMM d");
			else
				date = dt.ToString ("MMM d, yyyy");
			
			time = dt.ToString ("h:mm tt");

			string fuzzy;

			if (date != null && time != null)
				fuzzy = date + ", " + time;
			else if (date != null)
				fuzzy = date;
			else 
				fuzzy = time;

			return fuzzy;
		}

		public static string DateTimeToPrettyString (DateTime date)
		{
			DateTime now = DateTime.Now;
			string short_time = date.ToShortTimeString ();

			if (date.Year == now.Year) {
				if (date.DayOfYear == now.DayOfYear) {
					/* To translators: {0} is the time of the day, eg. 13:45 */
					return String.Format (Catalog.GetString ("Today, {0}"), short_time);
				} else if (date.DayOfYear == now.DayOfYear - 1) {
					/* To translators: {0} is the time of the day, eg. 13:45 */
					return String.Format (Catalog.GetString ("Yesterday, {0}"), short_time);
				} else if (date.DayOfYear > now.DayOfYear - 6) {
					/* To translators: {0} is the number of days that have passed, {1} is the time of the day, eg. 13:45 */
					return String.Format (Catalog.GetString ("{0} days ago, {1}"),
							      now.DayOfYear - date.DayOfYear,
							      short_time);
				} else {
					return date.ToString ("MMMM d, h:mm tt");
				}
			}

			return date.ToString ("MMMM d yyyy, h:mm tt");
		}
		
		public static string DurationToPrettyString (DateTime end_time, DateTime start_time)
		{
			TimeSpan span = end_time - start_time;

			string span_str = ""; 

			if (span.Hours > 0) {
				span_str = String.Format (Catalog.GetPluralString ("{0} hour", "{0} hours", span.Hours), span.Hours);
					
				if (span.Minutes > 0)
					span_str += ", ";
			}

			if (span.Minutes > 0) {
				span_str += String.Format (Catalog.GetPluralString ("{0} minute", "{0} minutes", span.Minutes), span.Minutes);
			}
					
			
			return span_str;
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

		// FIXME: how do we do this operation in a culture-neutral way?
		static public string[] SplitQuoted (string str)
		{
			char[] specialChars = new char [2] { ' ', '"' };
			
			ArrayList array = new ArrayList ();
			
			int i;
			while ((i = str.IndexOfAny (specialChars)) != -1) {
				if (str [i] == ' ') {
					if (i > 0)
						array.Add (str.Substring (0, i));
					str = str.Substring (i+1);
				} else if (str [i] == '"') {
					int j = str.IndexOf ('"', i+1);
					if (i > 0)
						array.Add (str.Substring (0, i));
					if (j == -1) {
						if (i+1 < str.Length)
							array.Add (str.Substring (i+1));
						str = "";
					} else {
						if (j-i-1 > 0)
						array.Add (str.Substring (i+1, j-i-1));
						str = str.Substring (j+1);
					}
				}
			}
			if (str != "")
				array.Add (str);
			
			string [] retval = new string [array.Count];
			for (i = 0; i < array.Count; ++i)
				retval [i] = (string) array [i];
			return retval;
		}

		static public bool ContainsWhiteSpace (string str)
		{
			foreach (char c in str)
				if (char.IsWhiteSpace (c))
					return true;
			return false;
		}

		static char[] CharsToQuote = { ';', '?', ':', '@', '&', '=', '$', ',', '#', '%', '"', ' ' };

		static public string HexEscape (string str)
		{
			StringBuilder builder = new StringBuilder ();
			int i;

			while ((i = str.IndexOfAny (CharsToQuote)) != -1) {
				if (i > 0)
					builder.Append (str.Substring (0, i));
				builder.Append (Uri.HexEscape (str [i]));
				str = str.Substring (i+1);
			}
			builder.Append (str);

			return builder.ToString ();
		}

		static public string PathToQuotedFileUri (string path)
		{
			path = Path.GetFullPath (path);
			return Uri.UriSchemeFile + Uri.SchemeDelimiter + HexEscape (path);
		}

		// These strings should never be exposed to the user.
		static int uid = 0;
		static object uidLock = new object ();
		static public string GetUniqueId ()
		{
			lock (uidLock) {
				if (uid == 0) {
					Random r = new Random ();
					uid = r.Next ();
				}
				++uid;
				
				return string.Format ("{0}-{1}-{2}-{3}",
						      Environment.GetEnvironmentVariable ("USER"),
						      Environment.GetEnvironmentVariable ("HOST"),
						      DateTime.Now.Ticks,
						      uid);
			}
		}

                static string [] replacements = new string [] {
                        "&amp;", "&lt;", "&gt;", "&quot;", "&apos;",
                        "&#xD;", "&#xA;"};

                static private StringBuilder cachedStringBuilder;
                static private char QuoteChar = '\"';

                private static bool IsInvalid (int ch)
                {
                        switch (ch) {
                        case 9:
                        case 10:
                        case 13:
                                return false;
                        }
                        if (ch < 32)
                                return true;
                        if (ch < 0xD800)
                                return false;
                        if (ch < 0xE000)
                                return true;
                        if (ch < 0xFFFE)
                                return false;
                        if (ch < 0x10000)
                                return true;
                        if (ch < 0x110000)
                                return false;
                        else
                                return true;
                }
		
		static public string EscapeStringForHtml (string source, bool skipQuotations)
		{
                        int start = 0;
                        int pos = 0;
                        int count = source.Length;
                        char invalid = ' ';
                        for (int i = 0; i < count; i++) {
                                switch (source [i]) {
                                case '&':  pos = 0; break;
                                case '<':  pos = 1; break;
                                case '>':  pos = 2; break;
                                case '\"':
                                        if (skipQuotations) continue;
                                        if (QuoteChar == '\'') continue;
                                        pos = 3; break;
                                case '\'':
                                        if (skipQuotations) continue;
                                        if (QuoteChar == '\"') continue;
                                        pos = 4; break;
                                case '\r':
                                        if (skipQuotations) continue;
                                        pos = 5; break;
                                case '\n':
                                        if (skipQuotations) continue;
                                        pos = 6; break;
                                default:
                                        if (IsInvalid (source [i])) {
                                                invalid = source [i];
                                                pos = -1;
                                                break;
                                        }
                                        else
                                                continue;
                                }
                                if (cachedStringBuilder == null)
                                        cachedStringBuilder = new StringBuilder
						();
                                cachedStringBuilder.Append (source.Substring (start, i - start));
                                if (pos < 0) {
                                        cachedStringBuilder.Append ("&#x");
                                        if (invalid < (char) 255)
                                                cachedStringBuilder.Append (((int) invalid).ToString ("X02", CultureInfo.InvariantCulture));
                                        else
                                                cachedStringBuilder.Append (((int) invalid).ToString ("X04", CultureInfo.InvariantCulture));
                                        cachedStringBuilder.Append (";");
                                }
                                else
                                        cachedStringBuilder.Append (replacements [pos]);
                                start = i + 1;
                        }
                        if (start == 0)
                                return source;
                        else if (start < count)
                                cachedStringBuilder.Append (source.Substring (start, count - start));
                        string s = cachedStringBuilder.ToString ();
                        cachedStringBuilder.Length = 0;
                        return s;
		}

		static public string CleanupInvalidXmlCharacters (string str)
		{
			int len = str.Length;
			char[] char_array = new char[len];

			for (int i = 0; i < len; i++) {
				if (IsInvalid (str[i]))
					char_array[i] = ' ';
				else
					char_array[i] = str[i];
			}

			return new string (char_array);
		}
		
		static public int CountWords (string str, int max_words)
		{
			if (str == null)
				return 0;

			bool last_was_white = true;
			int words = 0;
			for (int i = 0; i < str.Length; ++i) {
				if (Char.IsWhiteSpace (str [i])) {
					last_was_white = true;
				} else {
					if (last_was_white) {
						++words;
						if (max_words > 0 && words >= max_words)
							break;
					}
					last_was_white = false;
				}
			}

			return words;
		}

		static public int CountWords (string str)
		{
			return CountWords (str, -1);
		}
	}
}
