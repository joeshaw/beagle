//
// NameIndex.cs
//
// Copyright (C) 2005 Novell, Inc.
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

//
// Remember when I said that only LuceneDriver.cs should be the only source
// code that knew about Lucene internals?  I lied.
//

using System;
using System.Collections;
using System.IO;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using Beagle.Util;

namespace Beagle.Daemon {

	public class NameIndex {

		static public bool Debug = true;

		// This is just a standard analyzer combined with the Porter stemmer.
		// FIXME: This assumes everything being indexed is in English!
		private class NameAnalyzer : StandardAnalyzer {
			public override TokenStream TokenStream (String fieldName, TextReader reader)
			{
				TokenStream outstream = base.TokenStream (fieldName, reader);
				outstream = new PorterStemFilter (outstream);
				return outstream;
			}
		}

		// If Name is null, this is a removal, otherwise it is an add.
		private class PendingOperation {
			public Guid UniqueId;
			public string Name;
		}

		const int VERSION = 1;

		Lucene.Net.Store.FSDirectory store;
		Analyzer analyzer;
		string store_path;

		Hashtable pending = new Hashtable ();

		int adds_since_last_optimize = 0;
		const int optimize_threshold = 5000;

		public NameIndex (string directory, string fingerprint)
		{
			string top_dir = Path.Combine (directory, "NameIndex");
			string index_dir = Path.Combine (top_dir, "Index");
			string lock_dir = Path.Combine (top_dir, "Locks");
			string version_file = Path.Combine (top_dir, "version");
			string fingerprint_file = Path.Combine (top_dir, "fingerprint");
			string index_test_file = Path.Combine (index_dir, "segments");

			bool version_exists = File.Exists (version_file);
			bool fingerprint_exists = File.Exists (fingerprint_file);
			bool index_exists = File.Exists (index_test_file);

			// Check the index's version number.  If it is wrong,
			// declare the index non-existent.
			if (version_exists && index_exists) {
				StreamReader sr = new StreamReader (version_file);
				string version_str = sr.ReadLine ();
				sr.Close ();

				if (version_str != Convert.ToString (VERSION))
					index_exists = false;
			}

			// Check the fingerprint.  If it is wrong, declare the
			// index non-existent.
			if (index_exists && fingerprint_exists) {
				StreamReader sr = new StreamReader (fingerprint_file);
				string fingerprint_from_file = sr.ReadLine ();
				sr.Close ();
				if (fingerprint == null) {
					fingerprint = fingerprint_from_file;
					index_exists = true;
				} else if (fingerprint_from_file != fingerprint)
					index_exists = false;
			} else {
				index_exists = false;
			}

			// If our index doesn't exist, purge and rebuild the directory
			// structure.
			if (! index_exists) {

				if (Directory.Exists (top_dir)) {
					Logger.Log.Debug ("Purging {0}", top_dir);
					Directory.Delete (top_dir, true);
				}

				Directory.CreateDirectory (top_dir);
				Directory.CreateDirectory (index_dir);
				Directory.CreateDirectory (lock_dir);

				StreamWriter sw = new StreamWriter (fingerprint_file, false);
				sw.WriteLine (fingerprint);
				sw.Close ();

				sw = new StreamWriter (version_file, false);
				sw.WriteLine (VERSION);
				sw.Close ();
			}


			store = Lucene.Net.Store.FSDirectory.GetDirectory (index_dir, lock_dir, false);
			store_path = index_dir;

			analyzer = new NameAnalyzer ();

			if (! index_exists) {
				// This creates the index if it doesn't exist
				IndexWriter writer = new IndexWriter (store, null, true);
				writer.Close ();
			}
		}

		private Document ToLuceneDocument (PendingOperation p)
		{
			Document doc;
			Field f;

			doc = new Document ();

			f = Field.Keyword ("Uid", GuidFu.ToShortString (p.UniqueId));
			doc.Add (f);

			f = Field.Text ("Name", p.Name);
			doc.Add (f);

			string name_noext = Path.GetFileNameWithoutExtension (p.Name);
			if (name_noext != p.Name) {
				f = Field.UnStored ("NoExt", name_noext);
				doc.Add (f);
			}

			string name_split = String.Join (" ", StringFu.FuzzySplit (name_noext));
			if (name_split != name_noext) {
				f= Field.UnStored ("Split", name_split);
				doc.Add (f);
			}

			return doc;

		}

		public void Add (Guid unique_id, string name)
		{
			if (unique_id == Guid.Empty) {
				string msg = String.Format ("Attempt to add '{0}' to the NameIndex with unique_id=Guid.Empty", name);
				//throw new Exception (msg);
				Logger.Log.Debug (msg);
				return;
			}

			if (Debug && name != null)
				Logger.Log.Debug ("NameIndex.Add: {0} '{1}'",
						  GuidFu.ToShortString (unique_id), name);

			PendingOperation p = new PendingOperation ();
			p.UniqueId = unique_id;
			p.Name = name;
			pending [p.UniqueId] = p;
		}


		public void Remove (Guid unique_id)
		{
			if (unique_id == Guid.Empty) {
				string msg = "Attempt to remove unique_id=Guid.Empty from the NameIndex";
				//throw new Exception ("Attempt to remove unique_id=Guid.Empty from the NameIndex");
				Logger.Log.Debug (msg);
				return;
			}

			if (Debug)
				Logger.Log.Debug ("NameIndex.Remove: {0}",
						  GuidFu.ToShortString (unique_id));

			Add (unique_id, null);
		}

		public void Flush ()
		{
			if (pending.Count == 0) {
				if (Debug)
					Logger.Log.Debug ("NameIndex.Flush: nothing to do");
				return;
			}

			if (Debug)
				Logger.Log.Debug ("NameIndex.Flush: starting");

			Stopwatch sw = new Stopwatch ();
			sw.Start ();

			// This code:
			// (1) Makes sure there is only one record per uid for things we are adding
			// (2) Deletes rid of things we are removing
			IndexReader reader = IndexReader.Open (store);
			foreach (PendingOperation p in pending.Values) {
				Term term = new Term ("Uid", GuidFu.ToShortString (p.UniqueId));
				reader.Delete (term);
			}
			reader.Close ();


			bool did_optimize = false;
			IndexWriter writer = new IndexWriter (store, analyzer, false);

			foreach (PendingOperation p in pending.Values) {

				if (p.Name == null)
					continue;

				Document doc = ToLuceneDocument (p);
				writer.AddDocument (doc);

				++adds_since_last_optimize;
				if (adds_since_last_optimize > optimize_threshold) {
					writer.Optimize ();
					adds_since_last_optimize = 0;
					did_optimize = true;
				}
			}

			writer.Close ();

			sw.Stop ();
			
			if (Debug)
				Logger.Log.Debug ("NameIndex.Flush: Add{0} of {1} took {2}",
						  did_optimize ? "+Optimize" : "",
						  pending.Count,
						  sw);

			pending.Clear ();
		}

		///////////////////////////////////////////////////////////////////////////////////////////

		private LNS.Query ToCoreLuceneQuery (QueryBody body, string field)
		{
			LNS.BooleanQuery lucene_query = null;
			foreach (string text_orig in body.Text) {
				string text = text_orig;

				if (text == null || text == "")
					continue;

				bool minus_sign = false;
				if (text [0] == '-') {
					text = text.Substring (1);
					minus_sign = true;
				}

				// Use the analyzer to extract the query's tokens.
				// This code is taken from Lucene's query parser.
				// We use the standard Analyzer.
				TokenStream source = analyzer.TokenStream (field, new StringReader (text));
				ArrayList tokens = new ArrayList ();

				while (true) {
					Lucene.Net.Analysis.Token t;
					try {
						t = source.Next ();
					} catch (IOException) {
						t = null;
					}
					if (t == null)
						break;
					tokens.Add (t.TermText ());
				}
				try {
					source.Close ();
				} catch (IOException) { 
					// ignore
				}

				LNS.Query q = null;
				if (tokens.Count == 1) {
					Term t = new Term (field, (string) tokens [0]);
					q = new LNS.TermQuery (t);
				} else if (tokens.Count > 1) {
					q = new LNS.PhraseQuery ();
					foreach (string tokenStr in tokens) {
						Term t = new Term (field, tokenStr);
						((LNS.PhraseQuery) q).Add (t);
					}
				}

				if (q != null) {
					if (lucene_query == null)
						lucene_query = new LNS.BooleanQuery ();
					lucene_query.Add (q, !minus_sign, minus_sign);
				}
			}
			return lucene_query;
		}

		private LNS.Query ToLuceneQuery (QueryBody body, ICollection uris_to_search)
		{
			if (body.Text.Count == 0)
				return null;

			LNS.BooleanQuery query = new LNS.BooleanQuery ();
			
			query.Add (ToCoreLuceneQuery (body, "Name"),  false, false);
			query.Add (ToCoreLuceneQuery (body, "NoExt"), false, false);
			query.Add (ToCoreLuceneQuery (body, "Split"), false, false);

			// If a list of Uris is specified, we must match one of them.
			LNS.Query uri_query = LuceneDriver.ToUriQuery (uris_to_search);
			if (uri_query != null) {
				LNS.BooleanQuery combined_query = new LNS.BooleanQuery ();
				combined_query.Add (query, true, false);
				combined_query.Add (uri_query, true, false);
				combined_query = query;
			}

			return query;
		}

		// Return a collection of uid: Uris.
		public ICollection Search (QueryBody body, ICollection uris_to_search)
		{
			LNS.Query query = ToLuceneQuery (body, uris_to_search);
			if (query == null)
				return new string [0];
			
			IndexReader reader = IndexReader.Open (store);
			LNS.Searcher searcher = new LNS.IndexSearcher (reader);
			LNS.Hits hits = searcher.Search (query);

			int n_hits = hits.Length ();
			Uri [] uids = new Uri [n_hits];

			for (int i = 0; i < n_hits; ++i) {
				Document doc = hits.Doc (i);
				uids [i] = GuidFu.FromShortStringToUri (doc.Get ("Uid"));
			}

			// The call to searcher.Close () also closes the IndexReader
			searcher.Close ();

			return uids;
		}

		//////////////////////////////////////////////////////////////////////////////////

		// Pull data out of the NameIndex in bulk -- useful for sanity checks
		// and debugging

		public struct Record {
			public Guid   UniqueId;
			public string Name;
		}

		public Record [] GetManyByUniqueId (Guid [] unique_ids)
		{
			LNS.BooleanQuery query = new LNS.BooleanQuery ();
			int max_clauses = LNS.BooleanQuery.GetMaxClauseCount ();
			int clause_count = 0;

			foreach (Guid uid in unique_ids) {
				Term term = new Term ("Uid", GuidFu.ToShortString (uid));
				LNS.Query term_query = new LNS.TermQuery (term);
				query.Add (term_query, false, false);
				++clause_count;
				// If we have to many clases, nest the queries
				if (clause_count == max_clauses) {
					LNS.BooleanQuery new_query = new LNS.BooleanQuery ();
					new_query.Add (query, false, false);
					query = new_query;
					clause_count = 1;
				}
			}

			IndexReader reader = IndexReader.Open (store);
			LNS.Searcher searcher = new LNS.IndexSearcher (reader);
			LNS.Hits hits = searcher.Search (query);
			int n_hits = hits.Length ();

			Record [] records = new Record [n_hits];
			for (int i = 0; i < n_hits; ++i) {
				Document doc = hits.Doc (i);
				records [i].UniqueId = GuidFu.FromShortString (doc.Get ("Uid"));
				records [i].Name     = doc.Get ("Name");
			}

			// The call to searcher.Close () also closes the IndexReader
			searcher.Close ();

			return records;
		}

		//////////////////////////////////////////////////////////////////////////////////

		public void SpewIndex ()
		{
			IndexReader reader = IndexReader.Open (store);
			int N = reader.MaxDoc ();

			for (int i = 0; i < N; ++i) {
				if (! reader.IsDeleted (i)) {
					Document doc = reader.Document (i);
					Console.WriteLine (doc.Get ("Uid"));
				}
			}

			reader.Close ();
		}
	}
}
