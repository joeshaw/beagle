//
// Beagle
//
// FilterRTF.cs : Trivial implementation of a RTF-document filter.
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Beagle.Filters {

	public class FilterRTF : Beagle.Daemon.Filter {
		StreamReader reader;
		
		public FilterRTF ()
		{
			// Make this a general rtf filter.
			AddSupportedMimeType ("application/rtf");
		}
 		/*
 			FIXME: 
 			Right now we don't handle certain control words,
			that can look like text and but not.
			Ex: font names
		*/
		protected void ParseRTFFile (StreamReader reader)
		{
			string str;
			string[] tokens;
			while ((str = reader.ReadLine ()) != null) {
				tokens = str.Split (' ');
				for (int i = 0; i < tokens.Length; i++) {
					if (tokens[i].IndexOf (";}") > -1 ||
					    tokens[i].IndexOf ("{\\") > -1 ||
					    tokens[i].StartsWith ("\\")) {
						/* Control word in the RTF */
						continue;
					}
					AppendText (tokens[i]);
					AppendWhiteSpace ();
				}
			}  
			Finished ();
		}

		override protected void DoOpen (FileInfo info)
		{
			reader = info.OpenText ();
		}
		override protected void DoPull ()
		{
			ParseRTFFile (reader);
		}
	}
}
