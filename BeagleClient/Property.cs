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
	
	[XmlInclude (typeof (Property))]
		public class Property : IComparable, ICloneable {

		bool   isKeyword;
		bool   isSearched;
		string key;
		string value;

		[XmlAttribute]
		public bool IsKeyword {
			get { return isKeyword; }
			set { isKeyword = value; }
		}

		[XmlAttribute]
		public bool IsSearched {
			get { return isSearched; }
			set { isSearched = value; }
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

			return String.Compare (this.Key, other_property.Key);
		}

		public object Clone ()
		{
			object clone = this.MemberwiseClone ();

			return clone;
		}
		
		static public Property New (string key, object value)
		{
			Property p = new Property ();
			p.isKeyword = false;
			p.IsSearched = true;
			p.key = key;
			p.Value = value != null ? value.ToString () : null;
			return p;
		}

		static public Property NewKeyword (string key, object value)
		{
			Property p = Property.New (key, value);
			p.isKeyword = true;
			p.isSearched = true;
			return p;
		}

		static public Property NewUnsearched (string key, object value)
		{
			Property p = Property.New (key, value);
			p.isKeyword = true;
			p.isSearched = false;
			return p;
		}

		static public Property NewBool (string key, bool value)
		{
			return NewUnsearched (key, value ? "true" : "false");
		}

		static public Property NewFlag (string key)
		{
			return NewBool (key, true);
		}

		static public Property NewDate (string key, DateTime dt)
		{
			return NewUnsearched (key, StringFu.DateTimeToString (dt));
		}

		override public string ToString ()
		{
			return String.Format ("{0}={1}", Key, Value);
		}
	}
}
