//
// Indexer.cs
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
using DBus;

namespace Beagle {

	public class Indexer 
	{
		private static object theIndexerLock = new object ();
		static private IndexerProxy theIndexer = null;

		private static IndexerProxy TheIndexer {
			get {
				lock (theIndexerLock) {
					if (theIndexer == null)
						theIndexer = (IndexerProxy) DBusisms.Service.GetObject (typeof (IndexerProxy), DBusisms.IndexerPath);
				}
				return theIndexer;
			}
		}

		public static void Index (Indexable indexable)
		{
			indexable.StoreStream ();
			TheIndexer.Index (indexable.ToXml ());
		}

		public static void Delete (Uri uri)
		{
			TheIndexer.Delete (uri.ToString ());
		}

		public static void Crawl (string path, int maxDepth)
		{
			TheIndexer.Crawl (path, maxDepth);
		}
	}
}
