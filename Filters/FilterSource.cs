//
// FilterSource.cs
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
using System.IO;
using System.Text;
namespace Beagle.Filters {

	public abstract class FilterSource : Beagle.Daemon.Filter {

		protected enum LangType {
			None,
			C_Style,
			Python_Style
		};
		
		protected LangType SrcLangType;
		protected Hashtable KeyWordsHash;

		private enum LineType {
			None,
			SingleLineComment,
			BlockComment,
			StringConstant
		};
		
		LineType SrcLineType;
		string StrConstIdentifier;

		public FilterSource ()
		{
			// Initialize the linetype member.
			SrcLineType = LineType.None;
			SrcLangType = LangType.None;
			StrConstIdentifier = " ";

			KeyWordsHash = new Hashtable ();
		}

		// Tokenize the passed string and add the relevant 
		// tokens for indexing.
		//
 		protected void ExtractTokens (string str)
		{
			int index, kwindex;
			StringBuilder token = new StringBuilder();
			string splCharSeq = "";

			for (index = 0; index < str.Length; index++) {
				if ((str[index] == '/' || str[index] == '*') &&
				    (SrcLangType == LangType.C_Style)) {		
					splCharSeq += str[index];
					
					switch (splCharSeq) {
						
					case "//":
						if (SrcLineType == LineType.None) {
							SrcLineType = LineType.SingleLineComment;
							token.Remove (0, token.Length);
						} else
							token.Append (splCharSeq);
						splCharSeq = "";
						break;
						
					case "/*":
						if (SrcLineType == LineType.None) {
							SrcLineType = LineType.BlockComment;
							token.Remove (0, token.Length);
						} else 
							token.Append (splCharSeq);
						splCharSeq = "";
						break;
							
					case "*/":
						if (SrcLineType == LineType.BlockComment) {
							SrcLineType = LineType.None;
							token.Append (" ");
							AppendText (token.ToString());
							token.Remove (0, token.Length);
						} else if (SrcLineType != LineType.None)
							token.Append (splCharSeq);
						splCharSeq = "";
						break;
					}
				} else if (str[index] == '#' && SrcLangType == LangType.Python_Style) {
					if (SrcLineType == LineType.None) {
						SrcLineType = LineType.SingleLineComment;
						token.Remove (0, token.Length);
					} else
						token.Append (str[index]);
				}
				// FIXME: we evaluate *ALL* escape 
				// sequences on strings.  Do we really need to 
				// do this for comments??? And also "\n", "\t" etc????
				else if (SrcLineType == LineType.StringConstant && 
					 str[index] == '\\') {
					if ((index + 1) <= (str.Length-1))
						token.Append (str[index + 1]);
					index ++; 
				}
				// Well the typical python ''' or """ stuff 
				else if ((SrcLangType == LangType.Python_Style) &&
					 ((index + 2) <= (str.Length-1)) && 
					 (str[index] == '\"' || str[index] == '\'') &&
					 (str[index] == str[index + 1] && str[index] == str[index + 2]) &&
					 StrConstIdentifier[0] == str[index]) {

					if (SrcLineType == LineType.StringConstant) {
						SrcLineType = LineType.None;
						token.Append (" ");
						AppendText (token.ToString());
						token.Remove (0, token.Length);
					} else {
						StrConstIdentifier = "" + str[index] +
							str[index+1] + str[index+2];
						SrcLineType = LineType.StringConstant;
						token.Remove (0, token.Length);
						index += 2;
					}
					       
					splCharSeq = "";
				}
				else if (str[index] == '\"' || str[index] == '\'') {

					if (SrcLineType == LineType.StringConstant &&
					    StrConstIdentifier.Length == 1 &&
					    StrConstIdentifier[0] == str[index]) {
						SrcLineType = LineType.None;
						token.Append (" ");
						AppendText (token.ToString());
						token.Remove (0, token.Length);

					} else if (SrcLineType == LineType.None) {
						StrConstIdentifier = "" + str[index];
						SrcLineType = LineType.StringConstant;
						token.Remove (0, token.Length);
					} else
						token.Append (str[index]);
					splCharSeq = "";

				} else if (SrcLineType != LineType.None) {
					token.Append (splCharSeq + str[index]);
					splCharSeq = "";

				} else if (SrcLineType == LineType.None) {
					if (Char.IsLetter (str[index]) ||
					    Char.IsDigit (str[index]) ||
					    str[index] == '_')
						token.Append (str[index]);
					else {
						token = token.Replace(" ", "");
						if (token.Length > 0 && 
						    !KeyWordsHash.Contains (token.ToString())) {
							token.Append (" ");
							if (!Char.IsDigit (token[0]))
								AppendText (token.ToString());
						}
						// reset the token
						token.Remove (0, token.Length);
					}
					splCharSeq = "";
				}
		        }
			if (SrcLineType != LineType.None) {
				token.Append (splCharSeq);

				if (token.Length > 0 && token [token.Length - 1] == '\\')
					token = token.Remove (token.Length-1, 1);
			       
				token.Append (" ");
				AppendText (token.ToString());
				
				// if a single-line-comment ends with a "\", 
				// the lines that follows it are also considered as a comment,
				// till a line with out a "\" is found
				if (SrcLineType == LineType.SingleLineComment &&
				    str[str.Length - 1] != '\\')
					SrcLineType = LineType.None;
			} else if (SrcLangType == LangType.Python_Style) {
				if (token.Length > 0 && !Char.IsDigit (token[0])) {
					token.Append (" ");
					AppendText (token.ToString());
				}
			}
		}
	}
}
