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
			// Paths from the file:// indexer need (re)quoting. For example,
			// valid characters such as @ need to be converted to their hex
			// values.
			if (path.StartsWith ("file://")) {
				// Remove the file:// prefix
				path = path.Substring (7);

				return PathToFileUri (path);
			}
			
			// Currently, no other protocols need extra processing
			return new Uri (path, true);
		}

		static public String UriToSerializableString (Uri uri)
		{
			// The ToString() of a file:// URI is not always representative of
			// what it was constructed from. For example, it will return a
			// # (which was inputted as %23) as %23, whereas the more standard
			// behaviour for other escaped-characters is to return them as
			// their actual character. (e.g. %40 gets returned as @)
			// On the other hand, the LocalPath of a file:// URI does seem to
			// return the literal # so we use that instead.
			if (uri.IsFile)
				return Uri.UriSchemeFile + Uri.SchemeDelimiter + uri.LocalPath;
			else
				return uri.ToString ();
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
