//
// FilterCpp.cs
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

using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterCpp : FilterSource {

		static string [] strKeyWords = {"asm", "auto", "bool", "break", "case", "catch", "char",
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
		public FilterCpp ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/x-c++src"));
		}

		override protected void DoOpen (FileInfo info)
		{
			foreach (string keyword in strKeyWords)
				KeyWordsHash [keyword] = true;
			SrcLangType = LangType.C_Style;
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
