//
// LuceneDriver.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.IO;

using Beagle.Util;

namespace Beagle.Daemon {
	
	public class SnippetFu {

		public delegate string StringSource ();

		// Returns true if any terms were actually highlighted, false otherwise.
		static private bool HighlightTerms (string [] stemmed_terms, ref string text)
		{
			bool actually_highlighted_something = false;
			int pos = 0;

			while (pos < text.Length) {
				
				// Find the beginning of the next token.
				if (! Char.IsLetterOrDigit (text [pos])) {
					++pos;
					continue;
				}

				// Find the end of the next token
				int next_pos = pos+1;
				while (next_pos < text.Length && Char.IsLetterOrDigit (text [next_pos]))
					++next_pos;

				string stemmed_token = null;

				foreach (string term in stemmed_terms) {

					// If this term is longer than the token in question, give up.
					if (next_pos - pos < term.Length)
						continue;

					// We cache the token, so as to avoid stemming it more than once
					// when considering multiple terms.
					if (stemmed_token == null) {
						string token = text.Substring (pos, next_pos - pos).ToLower ();
						stemmed_token = LuceneDriver.Stem (token);
					}

					if (term != stemmed_token)
						continue;

					// We have a match!
					// FIXME: We should tag the matches with something better than
					// <b>...</b>.  In particular, we need to be able to consistently
					// colorize different search terms.
					actually_highlighted_something = true;
					text = String.Concat (text.Substring (0, pos),
							      "<b>",
							      text.Substring (pos, next_pos - pos),
							      "</b>",
							      text.Substring (next_pos));
					next_pos += 7; // adjust for the length of the tags
					break;
				}

				pos = next_pos;
			}

			return actually_highlighted_something;
		}
		
		static public string GetSnippet (QueryBody    query_body,
						 StringSource string_source)
		{
			// FIXME: If the query doesn't have search text (or is null), we should
			// generate a 'summary snippet'.

			if (string_source == null)
				return null;

			IList query_terms = query_body.Text;
			int N = query_terms.Count;
			string[] stemmed_terms = new string [N];
			for (int i = 0; i < N; ++i) {
				string term = (string) query_terms [i];
				if (term [0] == '-')
					continue;
				stemmed_terms [i] = LuceneDriver.Stem ((string) query_terms [i]).ToLower ();
			}
			
			string snippet = null;
			int snippet_word_count = 0;

			string summary = null;
			int summary_word_count = 0;


			string str;
			int countdown = -1;
			while ( (str = string_source ()) != null) {

				int word_count = StringFu.CountWords (str, 10);
				if (word_count > summary_word_count && summary_word_count < 8) {
					summary = str;
					summary_word_count = word_count;
				}

				if (HighlightTerms (stemmed_terms, ref str)) {

					if (word_count > snippet_word_count) {
						snippet = str;
						countdown = 50;
						if (word_count < 3)
							countdown *= 2;
					}

				}

				if (countdown > 0) {
					--countdown;
					if (countdown == 0)
						break;
				}
			}

			if (snippet == null)
				snippet = summary;

			if (snippet != null) {

				const int max_snippet_length = 100;

				snippet = snippet.Trim ();

				// Prune the snippet to keep it from
				// being too long.  This is pretty hacky.
				if (snippet.Length > max_snippet_length) {
					int i, j;
					i = snippet.IndexOf ("<b>");
					if (i == -1) {
						i = 0;
						j = max_snippet_length;
					} else {
						j = snippet.IndexOf ("</b>", i);
						if (j == -1)
							j = i + max_snippet_length;
						else {
							i -= max_snippet_length / 2;
							j += max_snippet_length / 2;
							if (i < 0) {
								j -= i;
								i = 0;
							}
							if (j > snippet.Length) {
								i -= (j - snippet.Length);
								j = snippet.Length;
							}
							if (i < 0)
								i = 0;
							if (j > snippet.Length)
								j = snippet.Length;
						}

						snippet = snippet.Substring (i, j-i);
					}

					// FIXME: We should break the snippet on word
					// boundaries and only ellipsize when the snippet
					// doesn't end on a sentence boundary.
					if (i > 0)
						snippet = "..." + snippet;
					if (j < snippet.Length)
						snippet = snippet + "...";

				}
			}


			return snippet;
		}

		static public string GetSnippet (QueryBody  body,
						 TextReader reader)
		{
			return GetSnippet (body, new StringSource (reader.ReadLine));
		}

		static public string GetSnippetFromFile (QueryBody body,
							 string    filename)
		{
			return GetSnippet (body, new StreamReader (filename));
		}
	}

}
