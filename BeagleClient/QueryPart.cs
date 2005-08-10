//
// QueryPart.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Collections;
using System.Xml.Serialization;

using BU = Beagle.Util;

namespace Beagle {

	public enum QueryPartLogic {
		Required,
		Optional,
		Prohibited
	};

	[XmlInclude (typeof (QueryPart_Text)),
	 XmlInclude (typeof (QueryPart_Property)),
	 XmlInclude (typeof (QueryPart_DateRange)),
	 XmlInclude (typeof (QueryPart_Human)),
	 XmlInclude (typeof (QueryPart_Or))]
	abstract public class QueryPart {

		private QueryPartLogic logic = QueryPartLogic.Optional;

		public QueryPart ()
		{ }

		public QueryPartLogic Logic {
			get { return logic; }
			set { logic = value; }
		}
	}

	public class QueryPart_Text : QueryPart {

		private string text;
		private bool search_full_text = true;
		private bool search_properties = true;

		public QueryPart_Text ()
		{ }

		public string Text {
			get { return text; }
			set { text = value; }
		}

		public bool SearchFullText {
			get { return search_full_text; }
			set { search_full_text = value; }
		}

		public bool SearchTextProperties {
			get { return search_properties; }
			set { search_properties = value; }
		}
	}

	public class QueryPart_Property : QueryPart {

		public const string AllProperties = "_all";

		private PropertyType type;
		private string key;
		private string value;

		public QueryPart_Property ()
		{ }

		public PropertyType Type {
			get { return type; }
			set { type = value; }
		}

		public string Key {
			get { return key; }
			set { key = value; }
		}

		public string Value {
			get { return value; }
			set { this.value = value; } // ugh
		}
	}

	public class QueryPart_DateRange : QueryPart {

		public const string AllProperties = "_all";

		private string key = AllProperties;
		private DateTime start_date;
		private DateTime end_date;

		public QueryPart_DateRange ()
		{ }
		
		public string Key {
			get { return key; }
			set { key = value; }
		}

		public DateTime StartDate {
			get { return start_date; }
			set { start_date = value; }
		}

		public DateTime EndDate {
			get { return end_date; }
			set { end_date = value; }
		}
	}

	public class QueryPart_Human : QueryPart {

		private string query_string;

		public QueryPart_Human ()
		{ }

		public string QueryString {
			get { return query_string; }
			set { query_string = value; }
		}
	}

	public class QueryPart_Or : QueryPart {
		
		private ArrayList sub_parts = new ArrayList ();

		public QueryPart_Or ()
		{ }

		[XmlArray ("SubParts")]
		[XmlArrayItem (ElementName="SubPart", Type=typeof (QueryPart))]
		public ArrayList SubParts_ShouldBePrivateSoPleaseDontUseThis {
			get { return sub_parts; }
		}

		[XmlIgnore]
		public ICollection SubParts {
			get { return sub_parts; }
		}

		public void Add (QueryPart part)
		{
			sub_parts.Add (part);
		}
	}
}
