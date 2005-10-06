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

using Beagle.Util;

// FIXME crude hack, the list of property names should be registered by queryables
// while calling AddNewKeyword etc.

namespace Beagle.Daemon {
	
	class PropertyDetail {
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

		// FIXME handle i18n issues... user might use a i18n-ised string for "title"
		private static void PopulatePropertyTable () {
			property_table = new Hashtable ();
			
			// Mapping between human query keywords and beagle index property keywords
			// ---------------- Dublin Core mappings --------------------
			
			// title - corresponds to dc:title
			// FIXME there are alteast two candidates for title: dc:title, fixme:title
			property_table.Add ("title",
					    new PropertyDetail (PropertyType.Text, "dc:title", "Title"));
			
			// creator
			property_table.Add ("creator",
					    new PropertyDetail (PropertyType.Text, "dc:author", "Creator of the content"));

			// author
			property_table.Add ("author",
					    new PropertyDetail (PropertyType.Text, "dc:author", "Author of the content"));

			// artist
			property_table.Add ("artist",
					    new PropertyDetail (PropertyType.Text, "fixme:artist", "Artists"));

			// date of content creation
			property_table.Add ("date",
					    new PropertyDetail (PropertyType.Date, "dc:date", "Date of creation"));

			// ---------------- content mappings for media ------------------------
			
			// album
			property_table.Add ("album",
					    new PropertyDetail (PropertyType.Text, "fixme:album", "Album of the media"));

			// ---------------- beaglequery mappings --------------------
			
			property_table.Add ("extension",
					    new PropertyDetail (PropertyType.Keyword, "bq:extension", "File extension"));

			// FIXME add more mappings to support more query
		}

		// return false if property not found!
		public static bool GetPropertyDetails (string keyword, out string name, out PropertyType type) {
			PropertyDetail property_detail = property_table [keyword] as PropertyDetail;
			name = (property_detail == null ? null		    : property_detail.PropertyName);
			type = (property_detail == null ? PropertyType.Text : property_detail.Type);
			return (property_detail != null);
		}
	}
}
