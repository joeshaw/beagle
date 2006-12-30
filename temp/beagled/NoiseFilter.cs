//
// NoiseFilter.cs
//
// Copyright (C) 2006 Debajyoti Bera <dbera.web@gmail.com>
// Copyright (C) 2004-2005 Novell, Inc.
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
using System.IO;
using System.Collections;

using Lucene.Net.Analysis;
using LNSA = Lucene.Net.Analysis.Standard;

namespace Beagle.Daemon {

	// TokenFilter which does several fancy things
	// 1. Removes words which are potential noise like dhyhy8ju7q9
	// 2. Splits email addresses into meaningful tokens
	// 3. Splits hostnames into subparts
	class NoiseEmailHostFilter : TokenFilter {
			
		private bool tokenize_email_hostname;

		TokenStream token_stream;

		public NoiseEmailHostFilter (TokenStream input, bool tokenize_email_hostname)
			: base (input)
		{
			this.token_stream = input;
			this.tokenize_email_hostname = tokenize_email_hostname;
		}

		// FIXME: we should add some heuristics that are stricter
		// but explicitly try to avoid filtering out dates,
		// phone numbers, etc.
		private static bool IsNoise (string text)
		{
			// Anything really long is almost certainly noise.
			if (text.Length > 30) 
				return true;

			// Look at how often we switch between numbers and letters.
			// Scoring:
			// <letter> <digit>   1
			// <digit> <letter>   1
			// <x> <punct>+ <x>   1
			// <x> <punct>+ <y>   2
			const int transitions_cutoff = 4;
			int last_type = -1, last_non_punct_type = -1, first_type = -1;
			bool has_letter = false, has_digit = false, has_punctuation = false;
			int transitions = 0;
			for (int i = 0; i < text.Length && transitions < transitions_cutoff; ++i) {
				char c = text [i];
				int type = -1;
				if (Char.IsLetter (c)) {
					type = 1;
					has_letter = true;
				} else if (Char.IsDigit (c)) {
					type = 2;
					has_digit = true;
				} else if (Char.IsPunctuation (c)) {
					type = 3;
					has_punctuation = true;
				}
					
				if (type != -1) {
						
					if (type != last_type) {
						if (last_type == 3) {
							if (type != last_non_punct_type)
								++transitions;
						} else {
							++transitions;
						}
					}

					if (first_type == -1)
						first_type = type;

					last_type = type;
					if (type != 3)
						last_non_punct_type = type;
				}
			}

			// If we make too many transitions, it must be noise.
			if (transitions >= transitions_cutoff) 
				return true;

			// If we consist of nothing but digits and punctuation, treat it
			// as noise if it is too long.
			if (transitions == 1 && first_type != 1 && text.Length > 10)
				return true;

			// We are very suspicious of long things that make lots of
			// transitions
			if (transitions > 3 && text.Length > 10) 
				return true;

			// Beware of anything long that contains a little of everything.
			if (has_letter && has_digit && has_punctuation && text.Length > 10)
				return true;

			//Logger.Log.Debug ("BeagleNoiseFilter accepted '{0}'", text);
			return false;
				
		}

		// Dont scan these tokens for additional noise
		// Someone might like to search for emails, hostnames and
		// phone numbers (which fall under type NUM)
		private static readonly string tokentype_email
			= LNSA.StandardTokenizerConstants.tokenImage [LNSA.StandardTokenizerConstants.EMAIL];
		private static readonly string tokentype_host 
			= LNSA.StandardTokenizerConstants.tokenImage [LNSA.StandardTokenizerConstants.HOST];
		private static readonly string tokentype_number 
			= LNSA.StandardTokenizerConstants.tokenImage [LNSA.StandardTokenizerConstants.NUM];
		private static readonly string tokentype_alphanum
			= LNSA.StandardTokenizerConstants.tokenImage [LNSA.StandardTokenizerConstants.ALPHANUM];

		private bool ProcessToken (ref Lucene.Net.Analysis.Token token)
		{
			string type = token.Type ();

			if (type == tokentype_email) {
				if (tokenize_email_hostname)
					ProcessEmailToken (token);
				return true;
			} else if (type == tokentype_host) {
				if (tokenize_email_hostname)
					ProcessURLToken (token);
				return true;
			} else if (type == tokentype_number) {
				// nobody will remember more than 20 digits
				return (token.TermText ().Length <= 20);
			} else if (type == tokentype_alphanum) {
				string text = token.TermText ();
				int begin = 0;
				bool found = false;
				// Check if number, in that case strip 0's from beginning
				foreach (char c in text) {
					if (! Char.IsDigit (c)) {
						begin = 0;
						break;
					} else if (! found) {
						if (c == '0')
							begin ++;
						else
							found = true;
					}
				}

				if (begin == 0)
					return ! IsNoise (text);
				token = new Lucene.Net.Analysis.Token (
					token.TermText ().Remove (0, begin),
					token.StartOffset (),
					token.EndOffset (),
					token.Type ());
				return true;
			} else
				// FIXME: Noise should be only tested on token type alphanum
				return ! IsNoise (token.TermText ());
		}

		private Queue parts = new Queue ();
		private Lucene.Net.Analysis.Token token;

		public override Lucene.Net.Analysis.Token Next ()
		{
			if (parts.Count != 0) {
				string part = (string) parts.Dequeue ();
				Lucene.Net.Analysis.Token part_token;
				// FIXME: Searching for google.com will not match www.google.com.
				// If we decide to allow google-style "abcd.1234" which means
				// "abcd 1234" as a consequtive phrase, then adjusting
				// the startOffset and endOffset would enable matching
				// google.com to www.google.com
				part_token = new Lucene.Net.Analysis.Token (part,
								       token.StartOffset (),
								       token.EndOffset (),
								       token.Type ());
				part_token.SetPositionIncrement (0);
				return part_token;
			}

			while ( (token = token_stream.Next ()) != null) {
				//Console.WriteLine ("Found token: [{0}]", token.TermText ());
				if (ProcessToken (ref token))
					return token;
			}
			return null;
		}

		char[] replace_array = { '@', '.', '-', '_', '+' };
		private void ProcessEmailToken (Lucene.Net.Analysis.Token token)
		{
			string email = token.TermText ();
			string[] tmp = email.Split (replace_array);
			int l = tmp.Length;

			// store username part as a large token
			int index_at = email.IndexOf ('@');
			tmp [l-1] = email.Substring (0, index_at);

			foreach (string s in tmp)
				parts.Enqueue (s);
			
		}

		private void ProcessURLToken (Lucene.Net.Analysis.Token token)
		{
			string hostname = token.TermText ();
			string[] host_parts = hostname.Split ('.');

			// remove initial www
			int begin_index = (host_parts [0] == "www" ? 1 : 0);
			// FIXME: Remove final tld
			// Any string of form "<alnum> '.')+<alnum>" has type HOST
			// Removing last token might remove important words from non-host
			// string of that form. To fix that, we need to match against the
			// huge list of TLDs.
			for (int i = begin_index; i < host_parts.Length; ++i)
				parts.Enqueue (host_parts [i]);

		}
	}

#if false
	public class AnalyzerTest {
		public static void Analyze (TextReader reader)
		{
			Lucene.Net.Analysis.Token lastToken = null;
			Analyzer indexing_analyzer = new LuceneCommon.BeagleAnalyzer (true);
			TokenStream stream = indexing_analyzer.TokenStream ("Text", reader);

			int position = 1;
			for (Lucene.Net.Analysis.Token t = stream.Next(); t != null; t = stream.Next())
			{
				position += (t.GetPositionIncrement() - 1);
				Console.WriteLine (t);
			}
		}
	}
#endif
}
