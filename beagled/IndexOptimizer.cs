//
// IndexOptimizer.cs
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
using System.IO;

class IndexOptimizerTool {

	static void Main (string [] args)
	{
		foreach (string index_dir in args) {

			string test_file = Path.Combine (index_dir, "segments");
			string lock_dir = Path.Combine (Path.GetDirectoryName (index_dir), "Locks");

			if (Directory.Exists (index_dir)
			    && Directory.Exists (lock_dir)
			    && File.Exists (test_file)) {

				Console.WriteLine ("Optimizing Lucene Index at {0}", index_dir);

				Lucene.Net.Store.FSDirectory store;
				store = Lucene.Net.Store.FSDirectory.GetDirectory (index_dir, false);
				store.TempDirectoryName = lock_dir;

				Beagle.Util.Stopwatch sw = new Beagle.Util.Stopwatch ();
				sw.Start ();
				
				Lucene.Net.Index.IndexWriter writer;
				writer = new Lucene.Net.Index.IndexWriter (store, null, false);
				writer.Optimize ();
				writer.Close ();

				sw.Stop ();

				Console.WriteLine ("Optimized in {0}", sw);

			} else {

				Console.WriteLine ("{0} doesn't look like one of Beagle's Lucene indices.", index_dir);

			}
		}
	}

}
