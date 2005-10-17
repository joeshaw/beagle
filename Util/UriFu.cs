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
using System.Text;

namespace Beagle.Util {

	public class UriFu {

		private UriFu () { } // class is static

		static public Uri PathToFileUri (string path)
		{
			string uriStr = StringFu.PathToQuotedFileUri (path);
			return new Uri (uriStr, true);
		}

		static public Uri UriStringToUri (string path)
		{
			// Our current hackery attempts to serialize Uri strings in
			// escaped and constructable form, so we don't require any
			// extra processing on deserialization right now.
			return new Uri (path, true);
		}

		static public String UriToSerializableString (Uri uri)
		{
			int i;
			string path;
			StringBuilder builder = new StringBuilder ();

			if (uri.IsFile)
				path = Uri.UriSchemeFile + Uri.SchemeDelimiter
					+ StringFu.HexEscape (uri.LocalPath);
			else
				path = uri.ToString ();

			// XmlSerializer is happy to serialize 'odd' characters, but doesn't
			// like to deserialize them. So we encode all 'odd' characters now.
			for (i = 0; i < path.Length; i++)
				if ((path [i] < '!') || (path [i] > '~' && path [i] < 256))
					builder.Append (Uri.HexEscape (path [i]));
				else
					builder.Append (path [i]);

			if (uri.IsFile)
				builder.Append (uri.Fragment);
			
			return builder.ToString ();
		}

		static public String LocalPathFromUri (Uri uri)
		{
			if (uri == null)
				return "";
			// FIXME: Can we assume "a directory", if it is not a file?
			// If so, return the path of that directory.
			if (uri.IsFile) 
				return uri.LocalPath;
			else
				return "";
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

		public class Comparer : IComparer
		{
			public int Compare(object uri1, object uri2)
			{
				return String.Compare(uri1.ToString(), uri2.ToString());
			}
		}

		public class Hasher : IHashCodeProvider
		{
			public int GetHashCode(object o)
			{
				return o.ToString().GetHashCode();
			}
		}

		static Comparer the_comparer = new Comparer ();
		static Hasher the_hasher = new Hasher ();

		// Returns a hash table that does the right thing when
		// the key is a Uri.
		static public Hashtable NewHashtable ()
		{
			return new Hashtable (the_hasher, the_comparer);
		}

		//////////////////////////////////

		static public string UrisToString (ICollection list_of_uris)
		{
			StringBuilder sb = new StringBuilder ("!@#");

			foreach (Uri uri in list_of_uris) {
				sb.Append (" ");
				sb.Append (UriToSerializableString (uri).Replace (" ", "%20"));
			}

			return sb.ToString ();
		}

		static public ICollection StringToUris (string list_of_uris_as_string)
		{
			string [] parts = list_of_uris_as_string.Split (' ');

			if (parts.Length == 0 || parts [0] != "!@#")
				return null;

			ArrayList uri_array = new ArrayList ();
			for (int i = 1; i < parts.Length; ++i) {
				try {
					Uri uri = UriStringToUri (parts [i]);
					uri_array.Add (uri);
				} catch (Exception ex) {
					Logger.Log.Debug ("Caught exception converting '{0}' to a Uri", parts [i]);
				}
			}
			
			return uri_array;
				
		}

	}

}
