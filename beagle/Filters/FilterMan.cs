//
// FilterMan.cs
//
// Copyright (C) 2004 Michael Levy <mlevy@wardium.homeip.net>
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

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters {

	// FIXME: Right now we don't handle pages with just one line like:
	//   .so man3/strcpy.3
	// Which is in strncpy.3.gz and points to strcpy.3.gz

	public class FilterMan : Beagle.Daemon.Filter {

		private StreamReader reader;

		public FilterMan ()
		{
			// 1: Fixes and updates
			SetVersion (1);

			SnippetMode = true;
			SetFileType ("documentation");
		}

		protected override void RegisterSupportedTypes ()
		{
			// Make this a general troff filter.
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-troff-man"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/x-troff-man"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-troff"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/x-troff"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/troff"));
		}

		protected void ParseManFile (StreamReader reader)
		{
			string line = null;
			
			// The regular expression for a complete man header line is built to allow a suite of 
			// non-spaces, or words separated by spaces which are encompassed in quotes
			// The regexp should be :
			//
			// Regex header_re = new Regex (@"^\.TH\s+" +
			//			     @"(?<title>(\S+|(""(\S+\s*)+"")))\s+" +
			//			     @"(?<section>\d+)\s+" + 
			//			     @"(?<date>(\S+|(""(\S+\s*)+"")))\s+" +
			//			     @"(?<source>(\S+|(""(\S+\s*)+"")))\s+" +
			//			     @"(?<manual>(\S+|(""(\S+\s*)+"")))\s*" +
			//			    "$");
			//
			// But there seem to be a number of broken man pages, and the current filter can be used 
			// for general troff pages.

			Regex header_regex = new Regex (@"^\.TH\s+(?<title>(\S+|(""(\S+\s*)+"")))\s*");
						    
			while ((line = reader.ReadLine ()) != null) {

				// Comment in man page
				if (line.StartsWith (".\\\""))
					continue;

				if (line.StartsWith (".TH ")) {
					MatchCollection matches = header_regex.Matches (line);
					
					if (matches.Count != 1) {
						Log.Error ("In title Expected 1 match but found {0} matches in '{1}'",
							   matches.Count, line);
						continue;
					}

					foreach (Match match in matches) {
						AddProperty (Beagle.Property.New ("dc:title", match.Groups ["title"].ToString ()));
					}
                      		} else {
                      			// This line is a "regular" string so strip out
					// some of the more common troff macros, ideally
					// we should handle more.

					line = line.Replace (".B", "");
					line = line.Replace (".BR", "");
					line = line.Replace (".HP", "");
					line = line.Replace (".IP", "");
					line = line.Replace (".PP", "");
					line = line.Replace (".SH", "");
					line = line.Replace (".TP", "");
					line = line.Replace ("\\-", "-");
					line = line.Replace ("\\fB", "");
					line = line.Replace ("\\fI", "");
					line = line.Replace ("\\fR", "");
					line = line.Replace ("\\(co", "(C)");
					
					if (String.IsNullOrEmpty (line))
						continue;

                      			AppendLine (line);
                      		}
			}  

			Finished ();
		}

		protected override void DoOpen (FileInfo info)
		{
			Stream stream = new FileStream (info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
			this.reader = new StreamReader (stream);
		}
		
		protected override void DoPull ()
		{
			ParseManFile (reader);
		}
	}
}
