//
// PropertyKeywordFu.cs
//
// Copyright (C) 2005 Debajyoti Bera
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

using Beagle;
using Beagle.Util;

namespace Beagle.Daemon {
	
	public class PropertyDetail {
		private PropertyType type;
		private string property_name;
		// a short description, so that frontends can get the description from here
		// and they dont need to guess the meaning of a query keyword (being too nice ?!)
		private string short_desc;
		
		public PropertyType Type { 
			get { return type;} 
		}

		public string PropertyName { 
			get { return property_name;} 
		}
		
		public string Description { 
			get { return short_desc; } 
		}

		public PropertyDetail (PropertyType type, string property_name) {
		    this.type = type;
		    this.property_name = property_name;
		    this.short_desc = "";
		}
		
		public PropertyDetail (PropertyType type, string property_name, string desc) {
		    this.type = type;
		    this.property_name = property_name;
		    this.short_desc = desc;
		}
	}
	
	public class PropertyKeywordFu {
		// mapping
		private static Hashtable property_table;

		// static class
		private PropertyKeywordFu () { }

		static PropertyKeywordFu () {
			PopulatePropertyTable ();
		}

		public static IEnumerable Keys {
			get { return property_table.Keys; }
		}

		public static IEnumerable Properties (string keyword) {
			if (! property_table.Contains (keyword))
				yield break;

			object o = property_table [keyword];
			if (o is PropertyDetail)
				yield return o;
			else if (o is ArrayList) {
				foreach (PropertyDetail detail in ((ArrayList) o))
					yield return detail;
			}
		}

		// FIXME handle i18n issues... user might use a i18n-ised string for "title"
		private static void PopulatePropertyTable () {
			property_table = new Hashtable ();
			
			// Mapping between human query keywords and beagle index property keywords
			// These are some of the standard mapping which is available to all backends and filters.
			
			property_table.Add ("title",
					    new PropertyDetail (PropertyType.Text, "dc:title", "Title"));
			
			property_table.Add ("creator",
					    new PropertyDetail (PropertyType.Text, "dc:creator", "Creator of the content"));

			property_table.Add ("author",
					    new PropertyDetail (PropertyType.Text, "dc:author", "Author of the content"));

			property_table.Add ("summary",
					    new PropertyDetail (PropertyType.Text, "dc:subject", "Brief description of the content"));

			property_table.Add ("source",
					    new PropertyDetail (PropertyType.Keyword, "beagle:Source", "Name of the backend"));

			property_table.Add ("type",
					    new PropertyDetail (PropertyType.Keyword, "beagle:HitType", "Hittype of the content e.g. File, IMLog, MailMessage"));

			property_table.Add ("mimetype",
					    new PropertyDetail (PropertyType.Keyword, "beagle:MimeType", "Mimetype of the content"));

			property_table.Add ("filetype",
					    new PropertyDetail (PropertyType.Keyword, "beagle:FileType", "Type of content for HitType File"));
					    
			property_table.Add ("host",
					    new PropertyDetail (PropertyType.Keyword, "fixme:host", "The host of this entitiy."));
		}

		public static void RegisterMapping (PropertyKeywordMapping mapping)
		{
			// If multiple mapping as registered, create an OR query for them
			// Store the multiple matchings in a list
			if (property_table.Contains (mapping.Keyword)) {
				object o = property_table [mapping.Keyword];
				if (o is ArrayList) {
					((ArrayList)o).Add (new PropertyDetail ( 
						mapping.IsKeyword ? PropertyType.Keyword : PropertyType.Text,
						mapping.PropertyName, 
						mapping.Description));
				} else if (o is PropertyDetail) {
					ArrayList list = new ArrayList (2);
					list.Add (o);
					list.Add (new PropertyDetail ( 
						mapping.IsKeyword ? PropertyType.Keyword : PropertyType.Text,
						mapping.PropertyName, 
						mapping.Description));
					property_table [mapping.Keyword] = list;
				}
				return;
			}

			property_table.Add (mapping.Keyword,
					    new PropertyDetail ( 
						mapping.IsKeyword ? PropertyType.Keyword : PropertyType.Text,
						mapping.PropertyName, 
						mapping.Description));
		}

		// return false if property not found!
		public static bool GetPropertyDetails (string keyword, out int num, out string[] name, out PropertyType[] type) {
			num = 0;
			name = null;
			type = null;

			if (! property_table.Contains (keyword))
				return false;

			object o = property_table [keyword];
			if (o is ArrayList) {
				ArrayList list = (ArrayList) o;
				num = list.Count;
				name = new string [num];
				type = new PropertyType [num];

				for (int i = 0; i < num; ++i) {
					PropertyDetail detail = (PropertyDetail) (list [i]);
					name [i] = detail.PropertyName;
					type [i] = detail.Type;
				}
			} else if (o is PropertyDetail) {
				num = 1;
				name = new string [num];
				type = new PropertyType [num];
				name [0] = ((PropertyDetail) o).PropertyName;
				type [0] = ((PropertyDetail) o).Type;
			}
			return true;
		}
	}
}
