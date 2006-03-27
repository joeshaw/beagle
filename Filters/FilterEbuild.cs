//
// FilterEbuild.cs
//
// Copyright (C) 2006 Pat Double <pat@patdouble.com>
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
using System.Text.RegularExpressions;
using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterEbuild : Beagle.Daemon.Filter {
		static Regex metadata_pattern = new Regex ("\\s*(?<key>([A-Z_]+))\\s*=\\s*\"(?<value>(.*))\"\\s*");
		static Regex einfo_pattern = new Regex ("\\s*(einfo|ewarn)\\s+\"(?<message>(.*))\"\\s*");
		static Regex package_pattern = new Regex ("(?<name>([^0-9]+))-(?<version>(.+)).ebuild");

		public FilterEbuild () 
		{
			AddSupportedFlavor (FilterFlavor.NewFromExtension (".ebuild"));
		}

		override protected void DoOpen (FileInfo file) 
		{
			Match match = package_pattern.Match (file.Name);
			String pkgname = match.Groups ["name"].ToString();
			if (pkgname.Length > 0)
				AddProperty (Beagle.Property.New ("dc:title", pkgname));
			
			String version = match.Groups ["version"].ToString();
			if (version.Length > 0)
				AddProperty (Beagle.Property.NewKeyword ("fixme:version", version));

			StreamReader reader = new StreamReader (new FileStream (file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));

			string str = null;
			while ((str = reader.ReadLine ()) != null) {
				// Skip comments
				if (str.StartsWith ("#"))
					continue;

				// Handle line continuation
				string str2 = null;
				while (str.Trim ().EndsWith ("\\") && ((str2 = reader.ReadLine ()) != null) ) {
					str = str.Trim ();
					if (str.Length == 1)
						str = str2;
					else
						str = str.Substring (0, str.Length - 1) + " " + str2.Trim ();
				}

				if (str.Length == 0)
					continue;

				// check for meta data
				MatchCollection matches;
				matches = metadata_pattern.Matches (str);
				if (matches.Count > 0) {
					foreach (Match the_match in matches) {
						String key = the_match.Groups ["key"].ToString ();
						String value = the_match.Groups ["value"].ToString ();
						if (key.Equals ("DESCRIPTION"))
							AddProperty (Beagle.Property.New ("dc:description", value));
						else if (key.Equals ("LICENSE"))
							AddProperty (Beagle.Property.New ("dc:rights", value));
						else if (key.Equals ("HOMEPAGE"))
							AddProperty (Beagle.Property.New ("dc:source", value));
					}
				} else {
					// check for einfo/ewarn
					matches = einfo_pattern.Matches (str);
					if (matches.Count > 0) {
						foreach (Match the_match in matches)
							AppendText (the_match.Groups ["message"].ToString ());
					}
				}
			}
			Finished ();
		}
	}
}
