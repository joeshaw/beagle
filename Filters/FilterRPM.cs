//
// FilterRPM.cs
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
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Xml;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Filters {
	public class FilterRPM : Beagle.Daemon.Filter {
		
		private class RpmPropertyInfo {
			public string property_name;
			public bool is_keyword;

			public RpmPropertyInfo (string property_name, bool is_keyword)
			{
				this.property_name = property_name;
				this.is_keyword = is_keyword;
			}
    		}
    		
    		private static Hashtable hash_property_list;
    		static FilterRPM ()
    		{
		    hash_property_list = new Hashtable ();

		    // mapping between rpm tagname and beagle property name
    		    hash_property_list ["Name"]          = new RpmPropertyInfo ("dc:title", false);
    		    hash_property_list ["Version"]       = new RpmPropertyInfo ("fixme:version", true);
    		    hash_property_list ["Release"]       = new RpmPropertyInfo ("fixme:release", true);
    		    hash_property_list ["Summary"]       = new RpmPropertyInfo ("dc:subject", false);
    		    hash_property_list ["Description"]   = new RpmPropertyInfo ("dc:description", false);
    		    hash_property_list ["License"]       = new RpmPropertyInfo ("dc:rights", false);
    		    hash_property_list ["Group"]         = new RpmPropertyInfo ("fixme:group", false);
		    hash_property_list ["Url"]           = new RpmPropertyInfo ("dc:source", true);
    		    hash_property_list ["Os"]            = new RpmPropertyInfo ("fixme:os", false);
    		    hash_property_list ["Arch"]          = new RpmPropertyInfo ("fixme:arch", false);
    		    hash_property_list ["Changelogtext"] = new RpmPropertyInfo ("fixme:changelog", false);
    		}

		public FilterRPM ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-rpm"));
		}

		protected override void DoPullProperties ()
		{
			SafeProcess pc = new SafeProcess ();
			pc.Arguments = new string [] { "rpm", "-qp", "--queryformat", "[%{*:xml}\n]", FileInfo.FullName };
			pc.RedirectStandardOutput = true;
			pc.RedirectStandardError = true;
			
			try {
				pc.Start ();
			} catch (SafeProcessException e) {
				Log.Warn (e.Message);
				Error ();
				return;
			}

			XmlTextReader reader = new XmlTextReader (new StreamReader (pc.StandardOutput));
			reader.WhitespaceHandling = WhitespaceHandling.None;

			try {
				ParseRpmTags (reader);
			} catch (XmlException e) {
				Logger.Log.Warn ("FilterRPM: Error parsing output of rpmquery: {0}", e.Message);
				Error ();
			} finally {
				reader.Close ();
				pc.Close ();
			}

			Finished ();
		}

		private void ParseRpmTags (XmlTextReader reader)
		{
			RpmPropertyInfo prop_info = null;
	
			reader.Read ();
			while (reader.Read ()) {
				if (reader.IsEmptyElement || ! reader.IsStartElement ())
					continue;
				else if (reader.Name != "rpmTag") {
					reader.Skip ();
					continue;
				}
				string attr_name = reader ["name"];
				//Logger.Log.Debug ("Read element:" + reader.Name + " - " + attr_name);

				// store basenames values as Text - they are like the "text"-content of rpm files
				if (attr_name == "Basenames") {
					ReadStringValues (reader, true, null);
					continue;
				}

				prop_info = (RpmPropertyInfo) hash_property_list [attr_name];
				if (prop_info != null)
					ReadStringValues (reader, false, prop_info);
			}
		}

		private void ReadStringValues (XmlTextReader reader, bool store_as_text, RpmPropertyInfo prop_info)
		{
			reader.ReadStartElement ();

			while (reader.IsStartElement ()) {
				//Logger.Log.Debug ("        Reading value for:" + reader.Name);
				if (reader.IsEmptyElement)
					reader.Skip ();
				
				string content = HtmlAgilityPack.HtmlEntity.DeEntitize (reader.ReadInnerXml ());
				//Logger.Log.Debug (prop_info.property_name 
				//	+ (prop_info.is_keyword ? " (keyword)" : " (text)") 
				//	+ " = [" + content + "]");
				
				if (store_as_text) {
					AppendText (content);
					AppendWhiteSpace ();
					continue;
				}

				if (prop_info.is_keyword)
					AddProperty (Beagle.Property.New (prop_info.property_name, content));
				else
					AddProperty (Beagle.Property.NewUnsearched (prop_info.property_name, content));
			}

			//Logger.Log.Debug ("    Done reading values. Now at " + 
			//	(reader.IsStartElement () ? "" : "/") + reader.Name);
		}
	}
}
