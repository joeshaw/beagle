//
// Indexable.cs
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
using System.Collections;
using System.IO;
using System.Text;

namespace Beagle {

	public class Indexable : Versioned {

		// The URI of the item being indexed.
		private String uri = null;
		
		// File, WebLink, MailMessage, IMLog, etc.
		private String type = null;

		// If applicable, otherwise set to null.
		private String mimeType = null;

		private Hashtable properties = new Hashtable (new CaseInsensitiveHashCodeProvider (),
							      new CaseInsensitiveComparer ());

		private String content = null;
		private String hotContent = null;

		private TextReader contentReader = null;
		private TextReader hotContentReader = null;

		private bool built = false;
		
		//////////////////////////

		virtual protected void DoBuild () { }

		public void Build ()
		{
			if (! built) {
				DoBuild ();
				built = true;
			}
		}

		//////////////////////////

		public String Uri { 
			get { return uri; }
			set { uri = value; }
		}

		public String Type {
			get { return type; }
			set { type = value; }
		}

		public String MimeType {
			get { return mimeType; }
			set { mimeType = value; }
		}

		//////////////////////////

		public IDictionary Properties {
			get { return properties; }
		}

		public ICollection Keys {
			get { return properties.Keys; }
		}

		public String this [String key] {
			get { return (String) properties [key]; }
			set {
				if (value == null || value == "") {
					if (properties.Contains (key))
						properties.Remove (key);
					return;
				}
				properties [key] = value as String;
			}
		}

		public String PropertiesAsString {
			get {
				StringBuilder propStr = null;
				foreach (String key in Keys) {
					if (propStr == null)
						propStr = new StringBuilder ("");
					else
						propStr.Append (" ");
					propStr.Append (this [key]);
				}
				return propStr == null ? "" : propStr.ToString ();
			}
		}

		//////////////////////////

		public String Content {
			get { return content; }
			set { content = value; }
		}

		public String HotContent {
			get { return hotContent; }
			set { hotContent = value; }
		}

		public TextReader ContentReader {
			get { return contentReader; }
			set { contentReader = value; }
		}

		public TextReader HotContentReader {
			get { return hotContentReader; }
			set { hotContentReader = value; }
		}

		//////////////////////////

		public override int GetHashCode ()
		{
			return uri.GetHashCode () ^ type.GetHashCode ();
		}
	}
}
