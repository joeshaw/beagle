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
				date = dt.ToString (Catalog.GetString ("MMM d"));
			else
				date = dt.ToString (Catalog.GetString ("MMM d, yyyy"));
			
			time = dt.ToString (Catalog.GetString ("h:mm tt"));

			string fuzzy;

			if (date != null && time != null)
				/* Translators: {0} is a date (e.g. 'Today' or 'Apr 23'), {1} is the time */
				fuzzy = String.Format (Catalog.GetString ("{0}, {1}"), date, time);
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
				} else if (date.DayOfYear > now.DayOfYear - 6 && date.DayOfYear < now.DayOfYear) {
					/* To translators: {0} is the number of days that have passed, {1} is the time of the day, eg. 13:45 */
					return String.Format (Catalog.GetString ("{0} days ago, {1}"),
							      now.DayOfYear - date.DayOfYear,
							      short_time);
				} else {
					return date.ToString (Catalog.GetString ("MMMM d, h:mm tt"));
				}
			}

			return date.ToString (Catalog.GetString ("MMMM d yyyy, h:mm tt"));
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
				/* Translators: {0} is a file size in bytes */
				return String.Format (Catalog.GetString ("{0} bytes"), len);

			if (len < oneMb)
				/* Translators: {0} is a file size in kilobytes */
				return String.Format (Catalog.GetString ("{0:0.0} KB"), len/(double)1024);

			/* Translators: {0} is a file size in megabytes */
			return String.Format (Catalog.GetString ("{0:0.0} MB"), len/(double)oneMb);
		}

		// Here we:
		// (1) Replace non-alphanumeric characters with spaces
		// (2) Inject whitespace between lowercase-to-uppercase
		//     transitions (so "FooBar" becomes "Foo Bar")
		//     and transitions between letters and numbers
		//     (so "cvs2svn" becomes "cvs 2 svn")
		static public string FuzzyDivide (string line)
		{
			// Allocate a space slightly bigger than the
			// original string.
			StringBuilder builder;
			builder = new StringBuilder (line.Length + 4);

			int prev_case = 0;
			bool last_was_space = true; // don't start w/ a space
			for (int i = 0; i < line.Length; ++i) {
				char c = line [i];
				int this_case = 0;
				if (Char.IsLetterOrDigit (c)) {
					if (Char.IsUpper (c))
						this_case = +1;
					else if (Char.IsLower (c))
						this_case = -1;
					if (this_case != prev_case
					    && !(this_case == -1 && prev_case == +1)) {
						if (! last_was_space) {
							builder.Append (' ');
							last_was_space = true;
						}
					}
					
					if (c != ' ' || !last_was_space) {
						builder.Append (c);
						last_was_space = (c == ' ');
					}

					prev_case = this_case;
				} else {
					if (! last_was_space) {
						builder.Append (' ');
						last_was_space = true;
					}
					prev_case = 0;
				}
			}

			return builder.ToString ();
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

		// Translate all %xx codes into real characters
		static public string HexUnescape (string str)
		{
			int i = 0, pos = 0;
			while ((i = str.IndexOf ('%', pos)) != -1) {
				pos = i;
				char unescaped = UriFu.HexUnescape (str, ref pos);
				str = str.Remove (i, 3);
				str = str.Insert (i, new String(unescaped, 1));
				pos -= 2;
			}
			return str;
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
			if (str == null)
				return null;

			int len = str.Length;
			
			// Find the first invalid character in the string
			int i = 0;
			while (i < len && ! IsInvalid (str [i]))
				++i;

			// If the string doesn't contain invalid characters,
			// just return it.
			if (i >= len)
				return str;

			// Otherwise copy the first chunk, then go through
			// character by character looking for more invalid stuff.

			char [] char_array = new char[len];
			
			for (int j = 0; j < i; ++j)
				char_array [j] = str [j];
			char_array [i] = ' ';

			for (int j = i+1; j < len; ++j) {
				char c = str [j];
				if (IsInvalid (c))
					char_array [j] = ' ';
				else
					char_array [j] = c;
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

		// Strip trailing slashes and make sure we only have 1 leading slash
		static public string SanitizePath (string path)
		{
			if (path.StartsWith ("//")) {
				int pos;
				for (pos = 2; pos < path.Length; pos++)
					if (path [pos] != '/')
						break;

				path = path.Substring (pos - 1);
			}
			if (!(path.Length == 1 && path [0] == '/'))
				path = path.TrimEnd ('/');

			return path;
		}
	}
}
