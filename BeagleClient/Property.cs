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
using BU = Beagle.Util;
using System.Xml.Serialization;

namespace Beagle {
	
	[XmlInclude (typeof (Property))]
	public class Property {

		bool   isKeyword;
		string key;
		string value;

		[XmlAttribute]
		public bool IsKeyword {
			get { return isKeyword; }
			set { isKeyword = value; }
		}

		[XmlAttribute]
		public string Key {
			get { return key; }
			set { key = value; }
		}

		[XmlAttribute]
		public string Value {
			get { return value; }
			set { this.value = value; }
		}
		
		/////////////////////////////////////

		public Property () { }
		
		static public Property New (string key, object value)
		{
			Property p = new Property ();
			p.isKeyword = false;
			p.key = key;
			p.value = value != null ? value.ToString () : null;
			return p;
		}

		static public Property NewKeyword (string key, object value)
		{
			Property p = Property.New (key, value);
			p.isKeyword = true;
			return p;
		}

		static public Property NewBool (string key, bool value)
		{
			return NewKeyword (key, value ? "true" : "false");
		}

		static public Property NewFlag (string key)
		{
			return NewBool (key, true);
		}

		static public Property NewDate (string key, DateTime dt)
		{
			return NewKeyword (key, BU.StringFu.DateTimeToString (dt));
		}
	}
}
