//
// Property.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle {

	public enum PropertyType {
		Text     = 1,
		Keyword  = 2,
		Date     = 3
	}
	
	public class Property : IComparable, ICloneable {
		
		PropertyType type;
		string       key;
		string       value;
		bool         is_searched;
		bool         is_mutable;
		bool	     is_stored;

		[XmlAttribute]
		public PropertyType Type {
			get { return type; }
			set { type = value; }
		}
		
		[XmlAttribute]
		public string Key {
			get { return key; }
			set { this.key = StringFu.CleanupInvalidXmlCharacters (value); }
		}

		[XmlAttribute]
		public string Value {
			get { return value; }
			set { this.value = StringFu.CleanupInvalidXmlCharacters (value); }
		}

		// If IsSearched is true, this property will can be matched by a
		// general match-any-propety query.
		// You can always query against the specific property, even if
		// IsSearched is false.
		[XmlAttribute]
		public bool IsSearched {
			get { return is_searched; }
			set { is_searched = value; }
		}

		// When IsMutable is true, the property is stored in the secondary
		// index so that it can more efficiently be changed later on.
		[XmlAttribute]
		public bool IsMutable {
			get { return is_mutable; }
			set { is_mutable = value; }
		}

		// When IsStored is false, the property wont be stored in the index
		[XmlAttribute]
		public bool IsStored {
			get { return is_stored; }
			set { is_stored = value; }
		}

		/////////////////////////////////////

		public Property () { }

		public int CompareTo (object other)
		{
			// By convention, a non-null object always
			// compares greater than null.
			if (other == null)
				return 1;

			Property other_property = other as Property;

			// If the other object is not a Property, compare the
			// two objects by their hash codes.
			if (other_property == null)
				return this.GetHashCode ().CompareTo (other.GetHashCode ());

			int rv;
			rv = String.Compare (this.Key, other_property.Key);
			if (rv != 0)
				return rv;

			return String.Compare (this.Value, other_property.Value);
		}

		public object Clone ()
		{
			return this.MemberwiseClone ();
		}

		static public Property New (string key, string value)
		{
			if (value == null)
				return null;

			Property p = new Property ();
			p.type = PropertyType.Text;
			p.Key = key;
			p.Value = value;
			p.is_searched = true;
			p.is_stored = true;
			return p;
		}

		static public Property NewKeyword (string key, object value)
		{
			if (value == null)
				return null;

			Property p = new Property ();
			p.type = PropertyType.Keyword;
			p.Key = key;
			p.Value = value.ToString ();
			p.is_searched = true;
			p.is_stored = true;
			return p;
		}

		static public Property NewUnsearched (string key, object value)
		{		
			if (value == null)
				return null;

			Property p = new Property ();
			p.type = PropertyType.Keyword;
			p.Key = key;
			p.Value = value.ToString ();
			p.is_searched = false;
			p.is_stored = true;
			return p;
		}

		static public Property NewUnstored (string key, object value)
		{		
			if (value == null)
				return null;

			Property p = new Property ();
			p.type = PropertyType.Keyword;
			p.Key = key;
			p.Value = value.ToString ();
			p.is_searched = false;
			p.is_stored = false;
			return p;
		}

		static public Property NewBool (string key, bool value)
		{
			return Property.NewUnsearched (key, value ? "true" : "false");
		}

		static public Property NewFlag (string key)
		{
			return NewBool (key, true);
		}

		static public Property NewDate (string key, DateTime dt)
		{
			Property p = new Property ();
			p.type = PropertyType.Date;
			p.Key = key;
			p.Value = StringFu.DateTimeToString (dt);
			p.is_searched = true;
			p.is_stored = true;
			return p;
		}

		static public Property NewDateFromString (string key, string value)
		{
			if (value == null)
				return null;

			Property p = new Property ();
			p.type = PropertyType.Date;
			p.Key = key;
			// FIXME: Should probably check that value is a valid date string.
			p.Value = value;
			p.is_searched = true;
			p.is_stored = true;
			return p;
		}

		override public string ToString ()
		{
			return String.Format ("{0}={1}", Key, Value);
		}
	}
}
