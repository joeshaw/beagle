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

	public class FilterEbuild : FilterPackage {
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

			PackageName = match.Groups ["name"].ToString();
			PackageVersion = match.Groups ["version"].ToString();

			if (PackageName.Length == 0 && PackageVersion.Length == 0)
				return;

			// get download file size
			FileInfo digest = new FileInfo (file.Directory.FullName + "/files/digest-" + PackageName + "-" + PackageVersion);
			if (digest.Exists) {
				long download_size = 0;
				StreamReader digest_reader = new StreamReader (new FileStream (digest.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
				string digest_line = null;
				while ((digest_line = digest_reader.ReadLine ()) != null) {
					string[] digest_parts = digest_line.Split (' ');
					if (digest_parts.Length < 4)
						continue;
					if (digest_parts[0].Equals ("MD5"))
						download_size += Int64.Parse (digest_parts[3]);
				}
				Size = download_size.ToString ();
				digest_reader.Close ();
			}
		}

		override protected void PullPackageProperties () 
		{
			string str = null;
			while ((str = TextReader.ReadLine ()) != null) {
				// Skip comments
				if (str.StartsWith ("#"))
					continue;

				// Handle line continuation
				string str2 = null;
				while (str.Trim ().EndsWith ("\\") && ((str2 = TextReader.ReadLine ()) != null) ) {
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
						String val = the_match.Groups ["value"].ToString ();
						if (key.Equals ("DESCRIPTION"))
							Summary = val; // Ebuild descriptions are short - use them as summary.
						else if (key.Equals ("LICENSE"))
							AddProperty (Beagle.Property.NewUnsearched ("dc:rights", val));
						else if (key.Equals ("HOMEPAGE"))
							Homepage = val;
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
		}
	}
}
