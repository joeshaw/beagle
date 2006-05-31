//
// SnippetFu.cs
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

// FIXME: Hack. Use Lucence.Net highlighting.

namespace Beagle.Daemon {
	
	public class SnippetFu {

		public delegate string StringSource ();

		static private bool IsTokenSeparator (char c)
		{
			return Char.IsWhiteSpace (c)
				|| Char.IsSeparator (c)
				|| Char.IsPunctuation (c);
		}

		const int max_prior_words = 6;
		const int max_following_words = 6;

		static private int HighlightTerms (ArrayList stemmed_terms, string text, ref ArrayList matches)
		{
			int pos = 0, prev_stop_pos = 0, prev_end_pos = 0;
			string prev_match = "";
			int length = 0;

			while (pos < text.Length) {
				
				// Find the beginning of the next token.
				if (IsTokenSeparator (text [pos])) {
					++pos;
					continue;
				}
				
				// Find the end of the next token
				int next_pos = pos+1;
				while (next_pos < text.Length && !IsTokenSeparator (text [next_pos]))
					++next_pos;
				
				string stemmed_token = null;
				int hl_offset = 0;
				
				// Iterate through the stemmed terms and match the token
				for (int i = 0; i < stemmed_terms.Count; i++) {
					
					// If this term is longer than the token in question, give up.
					if (next_pos - pos < ((string)stemmed_terms [i]).Length)
						continue;
					
					// We cache the token, so as to avoid stemming it more than once
					// when considering multiple terms.
					if (stemmed_token == null) {
						string token = text.Substring (pos, next_pos - pos);
						stemmed_token = LuceneCommon.Stem (token);
					}

					if (String.Compare ((string) stemmed_terms [i], stemmed_token, true) != 0)
						continue;
					
					// We have a match!

				        int start_pos = pos;
					int stop_pos = next_pos;

					// FIXME: This is a hack, I should be shot.
					for (int count = 0; count <= max_prior_words && start_pos > 0; start_pos--) {
						if ((text[start_pos] == ' '))
							count++;
					}

					if (start_pos != 0)
						start_pos += 2;

					for (int count = 0; count <= max_following_words && stop_pos < text.Length; stop_pos++) {
						if (text[stop_pos] == ' ')
							count++;
					}

					if (stop_pos != text.Length)
						stop_pos--;

					bool append_to_prev_match = false;

					if (prev_stop_pos > start_pos) {
						start_pos = prev_end_pos;
						prev_match = prev_match.Substring (0, prev_match.Length - (prev_stop_pos - prev_end_pos));
						append_to_prev_match = true;
					}

					string new_match = String.Concat (text.Substring (start_pos, pos - start_pos),
									  "<font color=\"",
									  colors [(i - hl_offset) % colors.Length],
									  "\"><b>",
									  text.Substring (pos, next_pos-pos),
									  "</b></font>",
									  text.Substring (next_pos, stop_pos-next_pos));

					if (append_to_prev_match) {
						prev_match += new_match;
					} else {					
						if (prev_match != "") {
							matches.Add (prev_match);
							length += prev_match.Length;
						}
						prev_match = new_match;
					}

					prev_stop_pos = stop_pos;
					prev_end_pos = next_pos;

					break;
				}

				pos = next_pos;
			}
			
			// Add trailing match
			if (prev_match != "") {
				matches.Add (prev_match);
				length += prev_match.Length;
			}

			return length;
		}

		static string[] colors = new string [] {"red", "blue", "green", "orange", "purple", "brown"};

		const int soft_snippet_limit = 400;

		static public string GetSnippet (string[] query_terms, StringSource string_source)
		{
			// FIXME: If the query doesn't have search text (or is null), we should
			// generate a 'summary snippet'.

			if (string_source == null)
				return null;
			
			ArrayList matches = new ArrayList ();
			int found_snippet_length = 0;

			// remove stop words from query_terms
			ArrayList query_terms_list = new ArrayList (query_terms.Length);
			foreach (string term in query_terms) {
				if (LuceneCommon.IsStopWord (term))
					continue;
				query_terms_list.Add (term);
			}

			string str;
			while ( (str = string_source ()) != null) {
				found_snippet_length += HighlightTerms (query_terms_list, str, ref matches);
				if (found_snippet_length >= soft_snippet_limit)
					break;
			}

			string snippet = "";

			for (int i = 0; i < matches.Count && snippet.Length < soft_snippet_limit; i++)
				snippet += String.Concat((string)matches[i], " ... ");		
			return snippet;
		
		}
		
		static public string GetSnippet (string[] query_terms, TextReader reader)
		{
			return GetSnippet (query_terms, new StringSource (reader.ReadLine));
		}

		static public string GetSnippetFromFile (string[] query_terms, string filename)
		{
			FileStream stream = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			return GetSnippet (query_terms, new StreamReader (stream));
		}

		static public string GetSnippetFromTextCache (string[] query_terms, string filename)
		{
			TextReader reader = TextCache.UserCache.GetReader (filename);
			return GetSnippet (query_terms, reader);
		}
	}

}
