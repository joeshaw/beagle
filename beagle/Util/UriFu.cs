//
// UriFu.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Beagle.Util {

	public class UriFu {

		private UriFu () { } // class is static

		static public Uri PathToFileUri (string path)
		{
			return PathToFileUri (path, null);
		}

		static public Uri PathToFileUri (string path, string fragment)
		{
			return new Uri (PathToFileUriString (path, fragment), true);
		}

		static public string PathToFileUriString (string path)
		{
			return PathToFileUriString (path, null);
		}

		static public string PathToFileUriString (string path, string fragment)
		{
			string str = String.Concat (Uri.UriSchemeFile,
						    Uri.SchemeDelimiter,
						    StringFu.HexEscape (Path.GetFullPath (path)));
			if (fragment != null)
				str = str + fragment;
			return str;
		}

		static public Uri EscapedStringToUri (string path)
		{
			// Our current hackery attempts to serialize Uri strings in
			// escaped and constructable form, so we don't require any
			// extra processing on deserialization right now.
			return new Uri (path, true);
		}

		// UriBuilder is a piece of shit so we have to do this ourselves.
		static public string UriToEscapedString (Uri uri)
		{
			StringBuilder builder = new StringBuilder ();

			builder.Append (uri.Scheme);

			if (uri.ToString ().IndexOf (Uri.SchemeDelimiter) == uri.Scheme.Length)
				builder.Append (Uri.SchemeDelimiter);
			else
				builder.Append (':');

			if (uri.Host != String.Empty) {
				if (uri.UserInfo != String.Empty)
					builder.Append (uri.UserInfo + "@");

				builder.Append (uri.Host);
			}

			if (! uri.IsDefaultPort)
				builder.Append (":" + uri.Port);

			// Both PathAndQuery and Fragment are escaped for us
			builder.Append (uri.PathAndQuery);
			builder.Append (uri.Fragment);

			return builder.ToString ();
		}

		//////////////////////////////////

		static public bool Equals (Uri uri1, Uri uri2)
		{
			return uri1.ToString () == uri2.ToString ();
		}

		static public int Compare (Uri uri1, Uri uri2)
		{
			return String.Compare (uri1.ToString (), uri2.ToString ());
		}

		//////////////////////////////////

		public class EqualityComparer : IEqualityComparer
		{
			public new bool Equals (object uri1, object uri2)
			{
				return String.Equals (uri1.ToString (), uri2.ToString ());
			}

			public int GetHashCode (object o)
			{
				return o.ToString ().GetHashCode ();
			}
		}

		static EqualityComparer equality_comparer = new EqualityComparer ();

		// Returns a hash table that does the right thing when
		// the key is a Uri.
		static public Hashtable NewHashtable ()
		{
			return new Hashtable (equality_comparer);
		}

	}

}
