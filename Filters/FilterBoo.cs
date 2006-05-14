//
// FilterBoo.cs
//
// Paul Betts (Paul.Betts@Gmail.com)
// Copyright (C) 2006 Novell Inc.
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

using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterBoo : FilterSource {

		static string[] strKeyWords =  {"abstract", "and", "as", "ast", "break", "continue", 
						    "callable", "cast", "char", "class", "constructor", 
						    "def", "destructor", "do", "elif", "else", "ensure", 
						    "enum", "event", "except", "failure", "final", "from", 
						    "for", "false", "get", "given", "goto", "import", "interface", 
						    "internal", "is", "isa", "if", "in", "not", "null", 
						    "of", "or", "otherwise", "override", "pass", "namespace", 
						    "partial", "public", "protected", "private", "raise", 
						    "ref", "return", "retry", "set", "self", "static", 
						    "struct", "try", "transient", "true", "typeof", 
						    "unless", "virtual", "when", "while", "yield"};

		public FilterBoo ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/x-boo"));
		}

		override protected void DoOpen (FileInfo info)
		{
			foreach (string keyword in strKeyWords)
				KeyWordsHash [keyword] = true;
			SrcLangType = LangType.Python_Style;
		}

		override protected void DoPull ()
		{
			string str = TextReader.ReadLine ();
			if (str == null)
				Finished ();
			else
				ExtractTokens (str);
		}
	}
}
