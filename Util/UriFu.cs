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

namespace Beagle.Util {

	public class UriFu {

		private UriFu () { } // class is static

		static public Uri PathToFileUri (string path)
		{
			string uriStr = StringFu.PathToQuotedFileUri (path);
			return new Uri (uriStr, true);
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

	}

}
