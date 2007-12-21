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

using ICSharpCode.SharpZipLib.GZip;

namespace Beagle.Filters {

	// FIXME: Right now we don't handle pages with just one line like:
	//   .so man3/strcpy.3
	// Which is in strncpy.3.gz and points to strcpy.3.gz

	public class FilterMan : Beagle.Daemon.Filter {

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

		private static Regex header_regex = new Regex (@"^\.TH\s+(?<title>(\S+|(""(\S+\s*)+"")))\s*", RegexOptions.Compiled);

		public FilterMan ()
		{
			// 1:Separate compressed man page filter
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

		protected void ParseManFile (TextReader reader)
		{
			string line = null;
						    
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

					// If it is tempting to use a regex to do these replacements, profile it first.
					// I found multiple Replace()s more efficient that a large Regex. - dBera
					line = line.Replace (".B", String.Empty);
					line = line.Replace (".BR", String.Empty);
					line = line.Replace (".HP", String.Empty);
					line = line.Replace (".IP", String.Empty);
					line = line.Replace (".I", String.Empty);
					line = line.Replace (".PP", String.Empty);
					line = line.Replace (".SH", String.Empty);
					line = line.Replace (".TP", String.Empty);
					line = line.Replace ("\\-", "-");
					line = line.Replace ("\\fB", String.Empty);
					line = line.Replace ("\\fI", String.Empty);
					line = line.Replace ("\\fP", String.Empty);
					line = line.Replace ("\\fR", String.Empty);
					line = line.Replace ("\\(co", "(C)");
					
					if (String.IsNullOrEmpty (line))
						continue;

                      			AppendLine (line);
                      		}
			}  

			Finished ();
		}

		protected override void DoPullProperties ()
		{
			ParseManFile (base.TextReader);
		}
	}

	public class FilterCompressedMan : FilterMan {

		public FilterCompressedMan () : base ()
		{
		}

		protected override void RegisterSupportedTypes ()
		{
			// FIXME: Hardcoded path is ok ?
			AddSupportedFlavor (new FilterFlavor ("file:///usr/share/man/*", ".gz", null, 1));
		}

		StreamReader reader = null;

		protected override void DoOpen (FileInfo info)
		{
			try {
				GZipInputStream stream = new GZipInputStream (Stream);
				reader = new StreamReader (stream);
			} catch (Exception e) {
				Log.Error (e, "Error in opening compressed man page");
				if (reader != null)
					reader.Close ();
				Error ();
			}
		}

		protected override void DoPullProperties ()
		{
			ParseManFile (reader);
		}

		protected override void DoClose ()
		{
			if (reader != null)
				reader.Close ();
		}
	}
}
