//
// FilterText.cs
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

	public class FilterSource : Beagle.Daemon.Filter {

		LineType SrcLineType;
		LangType SrcLangType;
		Hashtable KeyWordsHash;
		string StrConstIdentifier;

		public FilterSource ()
		{
			// Initialize the linetype member.
			SrcLineType = LineType.None;
			SrcLangType = LangType.None;
			StrConstIdentifier = " ";

			AddSupportedMimeType ("text/x-java");
			AddSupportedMimeType ("text/x-csrc");
			AddSupportedMimeType ("text/x-csharp");
			AddSupportedMimeType ("application/x-python");
			AddSupportedMimeType ("text/x-c++src");
		}

		StreamReader reader;

		override protected void DoOpen (FileInfo info)
		{
			int index;
			string [] KeyWords = {" "};

			string  [] cKeyWords  = {"auto", "break", "case", "char", "const", 
						 "continue", "default", "do", "double", "else",
						 "enum", "extern", "float", "for", "goto",
						 "if", "int", "long", "register", "return", "short",
						 "signed", "sizeof", "static", "strcut", "switch", "typedef",
						 "union", "unsigned", "void", "volatile", "while" };
			

			string [] cppKeyWords = {"asm", "auto", "bool", "break", "case", "catch", "char",
						 "class", "const", "const_cast", "continue", "default", "delete",
						 "do", "double", "dynamic_cast", "else", "enum", "explicit", 
						 "export", "extern", "false", "float", "for", "friend", "goto",
						 "if", "int", "long", "mutable", "namespace", "new", "operator",
						 "private", "public", "protected", "register", 
						 "reinterpret_cast", "return", "short", "signed", "sizeof", 
						 "static", "static_cast", "struct", "switch", "template", 
						 "this", "throw", "true" ,"try", "typedef", "typeid", 
						 "typename", "union", "unsigned", "using", "virtual",
						 "void", "volatile", "wchar_t"};
						 
		        string [] javaKeyWords = {"abstract", "boolean", "break", "byte", "case", "catch",
						  "char", "class", "const", "continue", "default", "do",
						  "double", "else", "extends", "final", "finally", "float", 
						  "for", "goto", "if", "implements", "import", "instanceof", 
						  "int", "interface", "long", "native", "new", "package", 
						  "private", "protected", "public", "return", "short", "static", 
						  "strictfp", "super", "switch", "synchronized", "this", "throw",
						  "throws", "transient", "try", "void", "volatile", "while" };

			string [] csharpKeyWords = {"abstract",  "as",  "base", "bool",  "break", 
	                                            "byte",  "case",  "catch",  "char", "checked", 
						    "class", "const", "continue", "decimal", "default", 
						    "delegate",  "do",  "double",  "else",  "enum", 
						    "event", "explicit",  "extern", "false", "finally", 
					            "fixed", "float", "for", "foreach", "goto", 
						    "if", "implicit", "in",  "int", "interface", 
						    "internal",  "is",  "lock",  "long",  "namespace", 
						    "new",  "null",  "object", "operator", "out", 
						    "override", "params", "private", "protected", "public", 
						    "readonly", "ref", "return", "sbyte", "sealed", 
						    "short", "sizeof", "stackalloc", "static", "string", 
						    "struct", "switch", "this", "throw", "true", 
						    "try", "typeof", "uint", "ulong", "unchecked", 
						    "unsafe", "ushort", "using", "virtual", "void", 
						    "volatile", "while"}; 

			string [] pythonKeyWords = {"and", "assert", "break", "class", "continue", "def", 
						    "del", "elif", "else", "except", "exec", "finally", 
						    "for", "from", "global", "if", "import", "in", "is",
						    "lambda", "not", "or", "pass", "print", "raise", "return", 
						    "try", "while", "yield"};

			switch (Flavor.MimeType) {
			case "text/x-csrc":
				KeyWords = cKeyWords;
				SrcLangType = LangType.C_Style;
				break;
			case "text/x-c++src":
				KeyWords = cppKeyWords;
				SrcLangType = LangType.C_Style;
				break;
			case "text/x-csharp":
				KeyWords = csharpKeyWords;
				SrcLangType = LangType.C_Style;
				break;
			case "text/x-java":
				KeyWords = javaKeyWords;
				SrcLangType = LangType.C_Style;
				break;
			case "application/x-python":
				KeyWords = pythonKeyWords;
				SrcLangType = LangType.Python;
				break;
			}

			KeyWordsHash = new Hashtable ();
			for (index = 0; index < KeyWords.Length; index ++)
				KeyWordsHash.Add (KeyWords[index], KeyWords[index]);

			Stream stream;
			stream = new FileStream (info.FullName,
						 FileMode.Open,
						 FileAccess.Read,
						 FileShare.Read);
			reader = new StreamReader (stream);

		}

		// Tokenize the passed string and add the relevant 
		// tokens for indexing.
		//
 		protected void ExtractTokens (string str)
		{
			//Console.WriteLine ("ExtractTokens : {0}", str);

			int index, kwindex;
			StringBuilder token = new StringBuilder();
			string splCharSeq = "";

			for (index = 0; index < str.Length; index++) {
				if ((str[index] == '/' || str[index] == '*') &&
				    (SrcLangType == LangType.C_Style)) {		
					splCharSeq += str[index];
					//Console.WriteLine ("splCharSeq : {0}", splCharSeq);
					
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
							//Console.WriteLine ("BlockCommentEnds: {0}", token);
							SrcLineType = LineType.None;
							token.Append (" ");
							AppendText (token.ToString());
							token.Remove (0, token.Length);
						} else if (SrcLineType != LineType.None)
							token.Append (splCharSeq);
						splCharSeq = "";
						break;
					}
				} else if (str[index] == '#' && SrcLangType == LangType.Python) {
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
				else if ((SrcLangType == LangType.Python) &&
					 ((index + 2) <= (str.Length-1)) && 
					 (str[index] == '\"' || str[index] == '\'') &&
					 (str[index] == str[index + 1] && str[index] == str[index + 2]) &&
					 StrConstIdentifier[0] == str[index]) {

					if (SrcLineType == LineType.StringConstant) {
						//Console.WriteLine ("Found String: {0}", token);
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
						//Console.WriteLine ("Found String: {0}", token);
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
							//Console.WriteLine ("Found Token: {0}", token);
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
				//Console.WriteLine ("Found splCharSeq: {0}", token);
				
				// if a single-line-comment ends with a "\", 
				// the lines that follows it are also considered as a comment,
				// till a line with out a "\" is found
				if (SrcLineType == LineType.SingleLineComment &&
				    str[str.Length - 1] != '\\')
					SrcLineType = LineType.None;
			} else if (SrcLangType == LangType.Python) {
				if (token.Length > 0 && !Char.IsDigit (token[0])) {
					//Console.WriteLine ("Found Token : {0}", token);
					token.Append (" ");
					AppendText (token.ToString());
				}
			}
		}

		protected void parseSourceFile (StreamReader reader)
		{
			string str = "";
			str = reader.ReadLine ();
			if (str == null)
				Finished ();
			else
				ExtractTokens (str);
		}
		
		override protected void DoPull ()
		{
			parseSourceFile (reader);
		}
		
		override protected void DoClose ()
		{
			KeyWordsHash.Clear ();
			reader.Close ();
		}
		private enum LineType {
			SingleLineComment,
			BlockComment,
			StringConstant, 
			None
		}
		
		private enum LangType {
			C_Style,
			Python,
			None
		}
	}
}
