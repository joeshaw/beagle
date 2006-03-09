//
// NoiseFilter.cs
//
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

using Lucene.Net.Analysis;
using LNSA = Lucene.Net.Analysis.Standard;

namespace Beagle.Daemon {

	class NoiseFilter : TokenFilter {
			
		static int total_count = 0;
		static int noise_count = 0;

		TokenStream token_stream;

		public NoiseFilter (TokenStream input) : base (input)
		{
			token_stream = input;
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

		private bool IgnoreNoise (Lucene.Net.Analysis.Token token)
		{
			string type = token.Type ();

			if (type == tokentype_email ||
			    type == tokentype_host)
				return true;

			if (type == tokentype_number)
				// nobody will remember more than 10 digits
				return (token.TermText ().Length <= 10);

			return false;
		}

		public override Lucene.Net.Analysis.Token Next ()
		{
			Lucene.Net.Analysis.Token token;
			while ( (token = token_stream.Next ()) != null) {
#if false
				if (total_count > 0 && total_count % 5000 == 0)
					Logger.Log.Debug ("BeagleNoiseFilter filtered {0} of {1} ({2:0.0}%)",
							  noise_count, total_count, 100.0 * noise_count / total_count);
#endif
				++total_count;
				if (IgnoreNoise (token))
					return token;
				if (IsNoise (token.TermText ())) {
					++noise_count;
					continue;
				}
				return token;
			}
			return null;
		}
	}




}
