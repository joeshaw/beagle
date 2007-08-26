//
// MasterDelete.cs
//
// Copyright (C) 2005 Novell, Inc.
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

using Beagle;
using Beagle.Daemon;

class MasterDeleteTool {

	static void Main (string[] args)
	{
		if (args.Length != 2) {
			Console.WriteLine ("Usage: beagle-master-delete-button /path/to/index-dir uri-to-delete");
			return;
		}

		string index_dir = args [0];
		Uri uri_to_delete = new Uri (args [1], false);

		LuceneQueryingDriver driver = new LuceneQueryingDriver (index_dir, -1, true);

		LuceneIndexingDriver indexer = new LuceneIndexingDriver (index_dir, false);

		Indexable indexable = new Indexable (uri_to_delete);
		indexable.Type = IndexableType.Remove;

		IndexerRequest request = new IndexerRequest ();
		request.Add (indexable);

		IndexerReceipt [] receipts = indexer.Flush (request);
		if (receipts == null || receipts.Length == 0) {
			Console.WriteLine ("Uri {0} not found in the index in {1}",
					   uri_to_delete, index_dir);
			return;
		}

		IndexerRemovedReceipt r = receipts [0] as IndexerRemovedReceipt;
		if (r == null || r.NumRemoved == 0) {
			Console.WriteLine ("Uri {0} not found in the index in {1}",
					   uri_to_delete, index_dir);
			return;
		}

		Console.WriteLine ("Uri {0} deleted", uri_to_delete);
	}
}
