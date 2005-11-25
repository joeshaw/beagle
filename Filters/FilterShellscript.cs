//
// FilterShellscript.cs
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

	public class FilterShellscript : FilterSource {

		static string  [] strKeyWords  = { "bash", "mv", "cp", "ls", "ps", "exit", 
						  "export", "echo", "if", "else", "elif", 
						  "then", "fi", "while", "do", "done", "until", 
						  "case", "in", "esac", "select", "for",
						  "function", "time", "break", "cd", "continue",
						  "declare", "fg", "kill", "pwd", "read", "return",
						  "set", "test", "unset", "wait", "touch" };
			
		public FilterShellscript ()
		{
			// FIXME: Add other shell mime-types, if they are different
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-shellscript"));
		}

		override protected void DoOpen (FileInfo info)
		{
			foreach (string keyword in strKeyWords)
				KeyWordsHash [keyword] = true;
			SrcLangType = LangType.Shell_Style;
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
