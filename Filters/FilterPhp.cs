//
// FilterPhp.cs
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

	public class FilterPhp : FilterSource {
		// http://docs.php.net/en/reserved.html
		static string [] strKeyWords = {"and", "or", "xor", "exception", "array", "as", "break", 
						"case", "class", "const", "continue", "declare", "default", 
						"die", "do", "echo", "else", "elseif", "empty", 
						"enddeclare", "endfor", "endforeach", "endif", 
						"extends", "for", "foreach", "function", "global", 
						"if", "include", "includeonce", "isset", "list", "new",
						"print", "require", "require_once", "return", "static", 
						"switch", "unset",  "use", "var", "while", "final", 
						"php_user_filter", "interface", "implements", "extends", 
						"public", "private", "protected", "abstract", "clone", 
						"try", "catch", "throw", "cfunction", 
						"old_function"};

		public FilterPhp ()
		{
			AddSupportedMimeType ("text/x-php");

		}

		override protected void DoOpen (FileInfo info)
		{
			foreach (string keyword in strKeyWords)
				KeyWordsHash [keyword] = true;

			// By default, "C" type comments are processed.
			// Php also supports "#" as comment, so,
			// adding Python_Style will process that as well.
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
