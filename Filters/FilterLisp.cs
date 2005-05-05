//
// FilterLisp.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Author: Wojciech Polak <wojciechpolak at gmail.com>

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

	public class FilterScheme : FilterSource {

		static string [] strKeyWords = {"and", "begin", "case", "cond", "define",
						"delay", "do", "else", "if", "lambda",
						"let", "let*", "letrec", "or", "quasiquote",
						"quote", "set!", "unquote", "unquote-splicing"};

		static string [] strCommonProcedures = {"abs", "append", "apply", "assoc", "assq",
							"assv", "caar", "cadr", "car", "cdr",
							"ceiling", "cons", "denominator", "display",
							"eval", "exp", "expt", "floor",  "gcd", "lcm",
							"length", "list-ref", "list-tail", "log",
							"map", "max", "member", "memq", "memv", "min",
							"modulo", "newline", "not", "numerator",
							"quotient", "rationalize", "read", "remainder",
							"reverse", "round", "sqrt", "string", "truncate",
							"vector", "write"};
		
		public FilterScheme ()
		{
			AddSupportedMimeType ("text/x-scheme");
		}

		override protected void DoOpen (FileInfo info)
		{
			foreach (string keyword in strKeyWords)
				KeyWordsHash [keyword] = true;

			foreach (string keyword in strCommonProcedures)
				KeyWordsHash [keyword] = true;

			SrcLangType = LangType.Lisp_Style;
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

	// TO DO: Add Emacs Lisp (FilterEmacsLisp), Common Lisp (FilterCommonLisp).
}
