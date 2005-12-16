//
// Flavor.cs
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
using System.IO;
using System.Collections;

using Beagle.Util;

namespace Beagle.Daemon {

	public class FilterFlavor {

		private string uri = null;
		private string mime_type = null;
		private string extension = null;

		private int priority = 0;

		public string Uri { 
			get { return uri; }
			set { uri = IsWild (value) ? null : value; }
		}

		public string Extension {
			get { return extension; }
			set { extension = IsWild (value) ? null : value; }
		}

		public string MimeType {
			get { return mime_type; }
			set { mime_type = IsWild (value) ? null : value; }
		}

		public int Priority {
			get { return priority; }
			set { priority = value; }
		}

		public FilterFlavor (string uri, string extension, string mime_type, int priority) 
		{
			this.uri = uri;
			this.extension = extension;
			this.mime_type = mime_type;
			this.priority = priority;
		}

		private bool IsWild (string str)
		{
			if (str == null)
				return true;
			if (str == "")
				return false;
			foreach (char c in str)
				if (c != '*')
					return false;
			return true;
		}

		public bool IsMatch (Uri uri, string extension, string mime_type)
		{
			if (Uri != null && (uri == null || !StringFu.GlobMatch (Uri, uri.ToString ())))
				return false;

			if (Extension != null && (extension == null || !StringFu.GlobMatch (Extension, extension)))
				return false;

			if (MimeType != null && (mime_type == null || !StringFu.GlobMatch (MimeType, mime_type)))
				return false;

			return true;
		}

		public int Weight 
		{
			get {
				int weight = priority;

				if (Uri != null)
					weight += 1;				
				if (Extension != null)
					weight += 1;
				if (MimeType != null)
					weight += 1;

				return weight;
			}
		}

		////////////////////////////////////////////

		public override string ToString ()
		{
			string ret = "";

			if (Uri != null)
				ret += String.Format ("Uri: {0}", Uri);

			if (Extension != null)
				ret += String.Format ("Extension: {0}", Extension);

			if (MimeType != null)
				ret += String.Format ("MimeType: {0}", MimeType);

			return ret;
		}

		public class WeightComparer : IComparer 
		{
			public int Compare (object obj1, object obj2) 
			{
				FilterFlavor flav1 = (FilterFlavor) obj1;
				FilterFlavor flav2 = (FilterFlavor) obj2;

				return flav1.Weight.CompareTo (flav2.Weight);
			} 
		}

		public class Hasher : IHashCodeProvider
		{
			public int GetHashCode (object o)
			{
				return o.ToString ().GetHashCode ();
			}
		}

		static WeightComparer the_comparer = new WeightComparer ();
		static Hasher the_hasher = new Hasher ();

		public static Hashtable NewHashtable ()
		{
			return new Hashtable (the_hasher, the_comparer);
		}

		////////////////////////////////////////////

		static private ArrayList flavors = new ArrayList ();
		
		static public ArrayList Flavors {
			get { return flavors; }
		}

		public static FilterFlavor NewFromMimeType (string mime_type) {
			return new FilterFlavor (null, null, mime_type, 0);
		}

		public static FilterFlavor NewFromExtension (string extension) {
			return new FilterFlavor (null, extension, null, 0);
		}
	}
}
