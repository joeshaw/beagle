//
// Session.cs : Dumb structure corresponding to Xesam session
//
// Copyright (C) 2007 Arun Raghavan <arunissatan@gmail.com>
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
using System.Collections.Generic;

namespace Beagle {
	namespace Xesam {
		public class Session {
			private bool searchLive;
			private string[] hitFields;
			private string[] hitFieldsExtended;
			private uint hitSnippetLength;
			private string sortPrimary;
			private string sortSecondary;
			private string sortOrder;
			private string vendorId;
			private uint vendorVersion;
			private string vendorDisplay;
			private uint vendorXesam;
			private string[] vendorOntologyFields;
			private string[] vendorOntologyContents;
			private string[] vendorOntologySources;
			private string[] vendorExtensions;
			private string[][] vendorOntologies;
			private uint vendorMaxHits;

			private List<Search> searches = new List<Search>();

			public bool SearchLive {
				get { return searchLive; }
				set { searchLive = value; }
			}

			public string[] HitFields {
				get { return hitFields; }
				set { hitFields = value; }
			}

			public string[] HitFieldsExtended {
				get { return hitFieldsExtended; }
				set { hitFieldsExtended = value; }
			}

			public uint HitSnippetLength {
				get { return hitSnippetLength; }
				set { hitSnippetLength = value; }
			}

			public string SortPrimary {
				get { return sortPrimary; }
				set { sortPrimary = value; }
			}

			public string SortSecondary {
				get { return sortSecondary; }
				set { sortSecondary = value; }
			}

			public string SortOrder {
				get { return sortOrder; }
				set { sortOrder = value; }
			}

			public string VendorId {
				get { return vendorId; }
				set { vendorId = value; }
			}

			public uint VendorVersion {
				get { return vendorVersion; }
				set { vendorVersion = value; }
			}

			public string VendorDisplay {
				get { return vendorDisplay; }
				set { vendorDisplay = value; }
			}

			public uint VendorXesam {
				get { return vendorXesam; }
				set { vendorXesam = value; }
			}

			public string[] VendorExtensions {
				get { return vendorExtensions; }
				set { vendorExtensions = value; }
			}

			public string[] VendorOntologyFields {
				get { return vendorOntologyFields; }
				set { vendorOntologyFields = value; }
			}

			public string[] VendorOntologyContents {
				get { return vendorOntologyContents; }
				set { vendorOntologyContents = value; }
			}

			public string[] VendorOntologySources {
				get { return vendorOntologySources; }
				set { vendorOntologySources = value; }
			}

			public string[][] VendorOntologies {
				get { return vendorOntologies; }
				set { vendorOntologies = value; }
			}

			public uint VendorMaxHits {
				get { return vendorMaxHits; }
				set { vendorMaxHits = value; }
			}

			public Session ()
			{
				SearchLive = false;
				HitFields = new string[] { "xesam:url" ,
					"xesam:title",
					"xesam:author", 
					"dc:date", 
					"xesam:fileExtension" };
				HitFieldsExtended = new string[] { };
				HitSnippetLength = 200;
				SortPrimary = "xesam:score";
				SortSecondary = String.Empty;
				SortOrder = "descending";
				VendorId = "Unknown";
				VendorVersion = 0;
				VendorDisplay = "Unknown";
				VendorXesam = 90;
				VendorOntologyFields = new string[] { };
				VendorOntologyContents = new string[] { };
				VendorOntologySources = new string[] { };
				VendorExtensions = new string[] { };
				// FIXME: This is wrong. Needs to be fixed.
				VendorOntologies = new string[][] { };
				VendorMaxHits = 1000;
			}

			public Search CreateSearch (string searchID, string xmlQuery)
			{
				Search search = new Search (searchID, this, xmlQuery);
				searches.Add (search);
				return search;
			}

			public void Close ()
			{
				foreach (Search s in searches) {
					s.Close ();
				}
			}
		}
	}
}
