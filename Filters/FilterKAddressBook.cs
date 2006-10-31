//
// FilterKAddressBook.cs
//
// Copyright (C) 2006 Debajyoti Bera <dbera.web@gmail.com>
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
using Beagle.Util;

namespace Beagle.Filters {

	public class FilterKAddressBook : Beagle.Filters.FilterKCal {

		public FilterKAddressBook ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType (ICalParser.KabcMimeType));
			SnippetMode = false;
			if (vCard_property_mapping == null)
				SetupPropertyMapping ();
		}

		private static Hashtable vCard_property_mapping = null;
		override protected Hashtable KCalPropertyMapping {
			get { return vCard_property_mapping; }
		}

		private void SetupPropertyMapping () {
			vCard_property_mapping = new Hashtable ();
			// KCalProperty (name, comma_sep, keyword, text_or_date)
			vCard_property_mapping ["FN"] = new KCalProperty ("vCard:FN", false, false, KCalType.Text);
			vCard_property_mapping ["NICKNAME"] = new KCalProperty ("vCard:NICKNAME", true, false,KCalType.Text);
			vCard_property_mapping ["BDAY"] = new KCalProperty ("vCard:BDAY", true, false, KCalType.Date);
			vCard_property_mapping ["TITLE"] = new KCalProperty ("vCard:TITLE", false, false, KCalType.Text);
			vCard_property_mapping ["ROLE"] = new KCalProperty ("vCard:ROLE", false, false, KCalType.Text);
			vCard_property_mapping ["CATEGORIES"] = new KCalProperty ("vCard:CATEGORIES", false, false, KCalType.Text);
			vCard_property_mapping ["NAME"] = new KCalProperty ("vCard:NAME", false, false, KCalType.Text);
			vCard_property_mapping ["NOTE"] = new KCalProperty ("vCard:NOTE", false, false, KCalType.Text);
			vCard_property_mapping ["REV"] = new KCalProperty ("dc:date", true, false, KCalType.Date);
			vCard_property_mapping ["CLASS"] = new KCalProperty ("vCard:CLASS", false, true, KCalType.Text);
			vCard_property_mapping ["UID"] = new KCalProperty ("vCard:UID", false, true, KCalType.Text);
			vCard_property_mapping ["EMAIL"] = new KCalProperty ("vCard:EMAIL", false, true, KCalType.Special);
			vCard_property_mapping ["TEL"] = new KCalProperty ("vCard:TEL", false, true, KCalType.Text);
			vCard_property_mapping ["URL"] = new KCalProperty ("vCard:URL", false, true,KCalType.Text);
		}

		override protected string GetPropertyName (string prop_name, ArrayList paramlist)
		{
			string mapped_prop_name =
				((KCalProperty)vCard_property_mapping [prop_name]).property_name;

			switch (prop_name) {
			case "TEL":
				foreach (KCalPropertyParameter vcpp in paramlist) {
					if (vcpp.param_name == "TYPE") {
						if (vcpp.param_value == "WORK")
							return mapped_prop_name + ":WORK";
						else if (vcpp.param_value == "HOME")
							return mapped_prop_name + ":HOME";
					}
				}
				break;
			}

			return mapped_prop_name;
		}

		override protected void ProcessPropertySpecial (string prop_name,
								ArrayList paramlist,
								string prop_value)
		{
			if (prop_name == "EMAIL") {
				foreach (KCalPropertyParameter vcpp in paramlist) {
					if (vcpp.param_name == "TYPE" &&
					    vcpp.param_value == "PREF")
						// Default email
						AddProperty (Beagle.Property.New (
							     "vCard:PREFEMAIL",
							     prop_value));
				}
				AddProperty (Beagle.Property.New ("vCard:EMAIL", prop_value));
			}
		}
	}
}
