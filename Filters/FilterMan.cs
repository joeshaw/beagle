//
// Beagle
//
// FilterMan.cs : Trivial implementation of a man-page filter.
//
// Author :
//      Michael Levy <mlevy@wardium.homeip.net>
//
// Copyright (C) 2004 Michael levy
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

using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterMan : Beagle.Daemon.Filter {
		StreamReader reader;
		
		public FilterMan ()
		{
			// Make this a general troff filter.
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-troff-man"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/x-troff-man"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-troff"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/x-troff"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/troff"));

			SnippetMode = true;
		}
 		/*
 			FIXME: 
 			Right now we don't handle pages with just one line like:
 				.so man3/strcpy.3
			Which is in strncpy.3.gz and points to strcpy.3.gz
		*/
		protected void ParseManFile (StreamReader reader)
		{
			string str;
			/*
			   The regular expression for a complete man header line is built to allow a suite of 
			   non-spaces, or words separated by spaces which are encompassed in quotes
			   The regexp should be :
			   
			Regex headerRE = new Regex (@"^\.TH\s+" +
						    @"(?<title>(\S+|(""(\S+\s*)+"")))\s+" +
						    @"(?<section>\d+)\s+" + 
						    @"(?<date>(\S+|(""(\S+\s*)+"")))\s+" +
						    @"(?<source>(\S+|(""(\S+\s*)+"")))\s+" +
						    @"(?<manual>(\S+|(""(\S+\s*)+"")))\s*" +
						    "$");
						    
			 But there seem to be a number of broken man pages, and the current filter can be used 
			 for general troff pages.
			*/
			Regex headerRE = new Regex (@"^\.TH\s+" +
						    @"(?<title>(\S+|(""(\S+\s*)+"")))\s*");
						    
			while ((str = reader.ReadLine ()) != null) {
				if (str.StartsWith (".\"")) { 
					/* Comment in man page */
					continue;
				} else if (str.StartsWith (".TH ")) {
					MatchCollection matches = headerRE.Matches (str);
					if (matches.Count != 1) {
						Console.Error.WriteLine ("In title Expected 1 match but found {0} matches in '{1}'",
									  matches.Count, str);
						continue;
					}
					foreach (Match theMatch in matches) {
						AddProperty (Beagle.Property.New ("dc:title",
										  theMatch.Groups ["title"].ToString ()));
					}
                      		} else {
                      			// A "regular" string

                      			// FIXME: We need to strip out other macros
					// (.SH for example)
                      			AppendText (str);
				
                      		}
                      		
			}  
			Finished ();
		}

		override protected void DoOpen (FileInfo info)
		{
			Stream stream;
			stream = new FileStream (info.FullName,
						 FileMode.Open,
						 FileAccess.Read,
						 FileShare.Read);
			reader = new StreamReader (stream);
		}
		override protected void DoPull ()
		{
			ParseManFile (reader);
		}
	}
}
