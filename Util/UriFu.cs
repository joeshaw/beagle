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
			string scheme, path;
			StringBuilder builder = new StringBuilder ();

			scheme = uri.Scheme;
			path = StringFu.HexEscape (uri.LocalPath);

			// XmlSerializer is happy to serialize 'odd' characters, but doesn't
			// like to deserialize them. So we encode all 'odd' characters now.
			for (i = 0; i < path.Length; i++)
				if ((path [i] < '!') || (path [i] > '~' && path [i] < 256))
					builder.Append (Uri.HexEscape (path [i]));
				else
					builder.Append (path [i]);

			if (scheme == "uid")
				builder.Insert (0, ':');
			else
				builder.Insert (0, Uri.SchemeDelimiter);

			builder.Insert (0, scheme);
			builder.Append (uri.Fragment);
			
			return builder.ToString ();
		}

		// Stolen from Mono SVN 20050319
		// Fixes bug where non-ASCII characters couldn't be decoded
		// FIXME: Go back to using Uri.HexUnescape when new Mono 1.1.5+ is 
		// readily available.
		public static char HexUnescape (string pattern, ref int index) 
		{
			if (pattern == null) 
				throw new ArgumentException ("pattern");
				
			if (index < 0 || index >= pattern.Length)
				throw new ArgumentOutOfRangeException ("index");

			if (!Uri.IsHexEncoding (pattern, index))
				return pattern [index++];

			int stage = 0;
			int c = 0;
			int b = 0;
			bool looped = false;
			do {
				index++;
				int msb = Uri.FromHex (pattern [index++]);
				int lsb = Uri.FromHex (pattern [index++]);
				b = (msb << 4) + lsb;
				if (!Uri.IsHexEncoding (pattern, index)) {
					if (looped)
						c += (b - 0x80) << ((stage - 1) * 6);
					else
						c = b;
					break;
				} else if (stage == 0) {
					if (b < 0xc0)
						return (char) b;
					else if (b < 0xE0) {
						c = b - 0xc0;
						stage = 2;
					} else if (b < 0xF0) {
						c = b - 0xe0;
						stage = 3;
					} else if (b < 0xF8) {
						c = b - 0xf0;
						stage = 4;
					} else if (b < 0xFB) {
						c = b - 0xf8;
						stage = 5;
					} else if (b < 0xFE) {
						c = b - 0xfc;
						stage = 6;
					}
					c <<= (stage - 1) * 6;
				} else {
					c += (b - 0x80) << ((stage - 1) * 6);
				}
				stage--;
				looped = true;
			} while (stage > 0);
			
			return (char) c;
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
