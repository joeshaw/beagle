//
// ManageIndex.cs
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
using System.Net;

using Beagle;
using Beagle.Util;
using Beagle.Daemon;

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;

namespace Beagle.Daemon 
{
	class ManageIndex 
	{
		static private LuceneIndexingDriver driver;

		static void Main (string [] args)
		{
			if (args.Length < 2)
				PrintUsage ();
			
			index_dir = (Path.IsPathRooted (args [0])) ? args [0] : Path.GetFullPath (args [0]);

			if (!Directory.Exists (index_dir)) {
				Console.WriteLine ("No such index directory: {0}", index_dir);
				Environment.Exit (1);
			}
			
			driver = new LuceneIndexingDriver (args [0]);

			switch (args [1]) {
			case "list":
				ExecuteList ();
				break;
			case "remove":
				ExecuteRemove (args [2]);
				break;
			case "merge":
				ExecuteMerge (args [2]);
				break;
			case "info":
				ExecuteInfo ();
				break;
			case "optimize":
				ExecuteOptimize ();
				break;
			default:
				Console.WriteLine ("Unknown command: {0}", args [1]);
				PrintUsage ();
				break;
			}
		}

		/////////////////////////////////////////////////////////
		
		static void PrintUsage ()
		{
			string usage = 
				"beagle-manage-index: Low-level Lucene index management\n" + 
				"Web page: http://www.gnome.org/projects/beagle\n" +
				"Copyright (C) 2004-2005 Novell, Inc.\n\n";
			
			usage += 
				"Usage: beagle-manage-index <index_path> <command> [OPTIONS]\n\n" + 
				"Commands:\n" + 
				"  list\t\t\t\tList all entries in the index.\n" + 
				"  remove <uri|tag>\t\tRemove entries corresponding to the criterias specified.\n" + 
				"  merge <index to merge>\tMerge another Lucene index into the target.\n" + 
				"  info\t\t\t\tPrint basic index information.\n" + 
				"  optimize\t\t\tOptimize index.\n";
			
			
			Console.WriteLine (usage);
			Environment.Exit (0);
		}

		/////////////////////////////////////////////////////////
		
		static void ExecuteList ()
		
{			LuceneDriver driver = new LuceneDriver (index_dir, true);
			
			IndexReader reader = IndexReader.Open (driver.Store);
			
			for (int i = 0; i < reader.NumDocs (); i++) {
				if (reader.IsDeleted (i))
					continue;
				Console.WriteLine (reader.Document (i));
			}
			
			reader.Close ();
		}

		/////////////////////////////////////////////////////////
		
		static void ExecuteRemove (string arg)
		{
			LuceneDriver driver = new LuceneDriver (index_dir);

			if (arg.IndexOf ("://") != -1) {
				Uri uri = new Uri (arg);
				ICollection hits = driver.DoQueryByUri (uri);
				
				if (hits == null || hits.Count == 0) {
					Console.WriteLine ("Uri not found in the index: {0}", uri);
					Environment.Exit (1);
				}
				
				driver.Remove (uri);
				driver.Flush ();
				
				Console.WriteLine ("Successfully removed Uri: {0}", uri);
			} else {
				IndexSearcher searcher = new IndexSearcher (driver.Store);
				BooleanQuery query = new BooleanQuery ();
				
				Term term = new Term ("prop:k:Tag", arg); // Argh
				TermQuery term_query = new TermQuery (term);
				query.Add (term_query, false, false);
				
				Hits hits = searcher.Search (query);
				int n_hits = hits.Length ();
				
				string uri;
				
				for (int i = 0; i < n_hits; ++i) {
					Document doc = hits.Doc (i);
					
					uri = doc.Get ("Uri");
					
					if (uri == null)
						continue;
					
					driver.Remove (UriFu.UriStringToUri (uri)); 
				}
				
				driver.Flush ();
				
				Console.WriteLine ("Successfully removed {0} items with tag: {1}", n_hits, arg);
			}
		}
		
		/////////////////////////////////////////////////////////
		
		static void ExecuteMerge (string index_to_merge) 
		{
			LuceneDriver driver = new LuceneDriver (index_dir);

			if (!Path.IsPathRooted (index_to_merge))
				index_to_merge = Path.GetFullPath (index_to_merge);
			
			if (!Directory.Exists (index_to_merge)) {
				Console.WriteLine ("Could not find index to merge: {0}", index_to_merge);
				Environment.Exit (1);
			}
			
			LuceneIndexingDriver driver_to_merge = new LuceneIndexingDriver (index_to_merge);
			
			Stopwatch watch = new Stopwatch ();
			watch.Start ();
			
			// Merge lucene index
			try {
				driver.Merge (driver_to_merge);
			} catch (Exception e) {
				Console.WriteLine ("Index merging failed: {0}", e);
				Environment.Exit (1);
			}
			
			// Merge file attributes stores
			try {
				FileAttributesStore_Sqlite store = new FileAttributesStore_Sqlite (driver.IndexDirectory, driver.Fingerprint);
				store.Merge (new FileAttributesStore_Sqlite (driver_to_merge.IndexDirectory, driver_to_merge.Fingerprint));
			} catch (Exception e) {
				Console.WriteLine ("Index merging failed: {0}", e);
				Environment.Exit (1);
			}
			
			watch.Stop ();
			
			Console.WriteLine ("Successfully merged index {0} into {1} in {2}", index_to_merge, driver.IndexDirectory, watch);
		}

		/////////////////////////////////////////////////////////
		
		static void ExecuteInfo ()
		{
			LuceneDriver driver = new LuceneDriver (index_dir, true);

			Console.WriteLine ("Total number of entries in index: {0}", driver.GetItemCount());
		}

		/////////////////////////////////////////////////////////
		
		static void ExecuteOptimize ()
		{
			LuceneDriver driver = new LuceneDriver (index_dir);

			Stopwatch watch = new Stopwatch ();
			watch.Start ();
			
			try {
				IndexWriter writer = new IndexWriter (driver.Store, null, false);
				writer.Optimize ();
				writer.Close ();
			} catch (Exception e) {
				Console.WriteLine ("Error optimizing index: {0}", driver.IndexDirectory);
				Environment.Exit (1);
			}
			watch.Stop ();
			
			Console.WriteLine ("Optimized index {0} in {1}", driver.IndexDirectory, watch);
		}
	}
}
