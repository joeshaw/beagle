//
// IndexDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// This should be the only piece of source code that knows anything
// about Lucene.
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


namespace Dewey {

	public class IndexDriver {

		public IndexDriver () { }

		//////////////////////////
		
		public bool Debug = true;
		
		protected void Spew (String str)
		{
			if (Debug)
				Console.WriteLine (str);
		}

		protected void Spew (String formatStr, params object[] formatArgs)
		{
			Spew (String.Format (formatStr, formatArgs));
		}

		//////////////////////////

		static private String TimestampToString (DateTime dt)
		{
			return Convert.ToString (dt.Ticks);
		}
		
		static private DateTime StringToTimestamp (String str)
		{
			return new DateTime (Convert.ToInt64 (str));
		}

		static private String RevisionToString (long rev)
		{
			return Convert.ToString (rev);
		}

		static private long StringToRevision (String str)
		{
			return Convert.ToInt64 (str);
		}

		//////////////////////////

		private String IndexPath {
			get {
				String homedir = Environment.GetEnvironmentVariable ("HOME");
				return Path.Combine (homedir, ".dewey");
			}
		}
		
		private Analyzer NewAnayzer ()
		{
			return new StandardAnalyzer ();
		}

		//////////////////////////
	
		private Document ToLuceneDocument (Indexable indexable)
		{
			Document doc = new Document ();

			// First we add the Indexable's canonical metadata fields
			// to the Document.

			Field f;
			
			f = Field.Keyword ("Uri", indexable.Uri);
			doc.Add (f);
			
			f = Field.Keyword ("Domain", indexable.Domain);
			doc.Add (f);

			f = Field.Keyword ("MimeType", indexable.MimeType);
			doc.Add (f);
	    
			if (indexable.ValidTimestamp) {
				f = Field.Keyword ("Timestamp",
						   TimestampToString (indexable.Timestamp));
				doc.Add (f);
			}
			
			if (indexable.ValidRevision) {
				f = Field.UnIndexed ("Revision",
						     RevisionToString (indexable.Revision));
				doc.Add (f);
			}
			
			// Next we actually open the Indexable and extract the content
			// fields and the content-related metadata.
			
			String str;
			
			indexable.Open ();
			
			str = indexable.Content;
			if (str != null) {
				f = Field.UnStored ("Content", str);
				doc.Add (f);
			}
			
			str = indexable.HotContent;
			if (str != null) {
				f = Field.UnStored ("HotContent", str);
				doc.Add (f);
			}
			
			str = indexable.Metadata;
			if (str != null) {
				f = Field.UnStored ("MetaData", str);
				doc.Add (f);
			}
			
			foreach (String key in indexable.MetadataKeys) {
				// Non-canonical metadata keys are always lower case.
				f = Field.Text (key.ToLower (), indexable [key]);
				doc.Add (f);
			}
			
			indexable.Close ();
			
			return doc;
		}
		
		private LNS.Query ToLuceneQuery (Query query, Analyzer analyzer)
		{
			LNS.BooleanQuery luceneQuery = new LNS.BooleanQuery ();
			
			String queryStr = query.AbusivePeekInsideQuery;
			
			LNS.Query hotQuery;
			hotQuery = QueryParser.Parse (queryStr, "HotContent", analyzer);
			hotQuery.SetBoost (2.0f);
			luceneQuery.Add (hotQuery, false, false);
			
			LNS.Query metaQuery;
			metaQuery = QueryParser.Parse (queryStr, "MetaData", analyzer);
			metaQuery.SetBoost (1.5f);
			luceneQuery.Add (metaQuery, false, false);
			
			LNS.Query contentQuery;
			contentQuery = QueryParser.Parse (queryStr, "Content", analyzer);
			luceneQuery.Add (contentQuery, false, false);
			
			return luceneQuery;
		}

		private Hit FromLuceneHit (LNS.Hits luceneHits, int i)
		{
			Hit hit = new Hit ();
			
			Document doc = luceneHits.Doc (i);
			String str;
			
			str = doc.Get ("Uri");
			if (str == null)
				throw new Exception ("Got hit from Lucene w/o a URI!");
			hit.Uri = str;

			str = doc.Get ("Domain");
			if (str != null)
				hit.Domain = str;
			
			str = doc.Get ("MimeType");
			if (str == null)
				throw new Exception ("Got hit from Lucene w/o a MimeType");
			hit.MimeType = str;
			
			str = doc.Get ("Timestamp");
			if (str != null)
				hit.Timestamp = StringToTimestamp (str);
			
			str = doc.Get ("Revision");
			if (str != null)
				hit.Revision = StringToRevision (str);
			
			hit.Source = "lucene";
			hit.Score = luceneHits.Score (i);
			
			foreach (Field ff in doc.Fields ()) {
				String key = ff.Name ();
				// Non-canonical metadata keys are always lower case.
				if (key == key.ToLower ())
					hit [key] = ff.StringValue ();
			}
			
			hit.lockdown ();
			return hit;
		}
		
		//////////////////////////

		private void DoDelete (IEnumerable uris) 
		{
			IndexReader reader = IndexReader.Open (IndexPath);
			foreach (String uri in uris) {
				Term term = new Term ("Uri", uri);
				Spew ("Removing {0}", uri);
				reader.Delete (term);
			}
			reader.Close ();
		}

		private void DoInsert (IEnumerable indexables, bool optimize)
		{
			Analyzer analyzer = NewAnayzer ();
			IndexWriter writer = new IndexWriter (IndexPath, analyzer, false);
			
			foreach (Indexable indexable in indexables) {
				Spew ("Inserting {0}", indexable.Uri);
				Document doc = ToLuceneDocument (indexable);
				writer.AddDocument (doc);
			}
			// optimization is expensive
			if (optimize)
				writer.Optimize ();
			writer.Close ();
		}

		// Add a set of items to the index
		public void Add (IEnumerable indexables)
		{
			if (! Directory.Exists (IndexPath)) {
				Directory.CreateDirectory (IndexPath);
				// Initialize the index
				IndexWriter writer = new IndexWriter (IndexPath, null, true);
				writer.Close ();
			}
			
			ArrayList toBeDeleted = new ArrayList ();
			ArrayList toBeInserted = new ArrayList ();

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexPath);
			
			// If we've been handed multiple Indexables with the same Uri,
			// try to do something intelligent.
			Hashtable byUri = new Hashtable ();
			foreach (Indexable indexable in indexables) {
				if (byUri.Contains (indexable.Uri)) {
					Indexable prev = (Indexable) byUri [indexable.Uri];
					// FIXME: This isn't quite the right logic.  And what
					// about objects with Revisions instead of Timestamps?
					if (prev.ValidTimestamp
					    && indexable.ValidTimestamp
					    && prev.Timestamp < indexable.Timestamp)
						byUri [indexable.Uri] = indexable;
				} else {
					byUri [indexable.Uri] = indexable;
				}
			}
			
			foreach (Indexable indexable in byUri.Values) {
				
				Term term = new Term ("Uri", indexable.Uri);
				LNS.Query uriQuery = new LNS.TermQuery (term);
				LNS.Hits uriHits = searcher.Search (uriQuery);
				int nHits = uriHits.Length ();
				
				if (nHits > 1) {
					throw new Exception (String.Format ("Got {0} hits on Uri {1}",
									    nHits, indexable.Uri));
				} else if (nHits == 1) {
					String oldTsStr = uriHits.Doc (0).Get ("Timestamp");
					String oldRevStr = uriHits.Doc (0).Get ("Revision");
					
					bool isSupercededBy = false;
					
					// If there is no timestamp or revision #, always
					// re-index
					if (oldTsStr == null && oldRevStr == null)
						isSupercededBy = true;
					
					// First, try comparing the timestamps.
					if (! isSupercededBy
					    && oldTsStr != null
					    && indexable.ValidTimestamp) {
						DateTime oldTs = StringToTimestamp (oldTsStr);
						if (oldTs < indexable.Timestamp)
							isSupercededBy = true;
					}
					
					// Next, try comparing the revisions.
					if (! isSupercededBy
					    && oldRevStr != null
					    && indexable.ValidRevision) {
						long oldRev = StringToRevision (oldRevStr);
						if (oldRev < indexable.Revision)
							isSupercededBy = true;
					}
		    
					if (isSupercededBy) {
						toBeDeleted.Add (indexable.Uri);
						toBeInserted.Add (indexable);
						Spew ("Re-scheduling {0}", indexable.Uri);
					} else {
						Spew ("Skipping {0}", indexable.Uri);
					}
		    
				} else {
					toBeInserted.Add (indexable);
					Spew ("Scheduling {0}", indexable.Uri);
				}
			}

			if (toBeDeleted.Count > 0)
				DoDelete (toBeDeleted);

			if (toBeInserted.Count > 0)
				DoInsert (toBeInserted, true);
		}
	
		// Add a single item to the index
		public void Add (Indexable indexable)
		{
			Add (new Indexable[] { indexable });
		}

		public IEnumerable Query (Query query)
		{
			return Query (query, 0);
		}

		private IEnumerable Query (Query query, int step)
		{

			if (step > 0)
				Spew ("Query Step {0}", step);

			Analyzer analyzer = NewAnayzer ();
			LNS.Query luceneQuery = ToLuceneQuery (query, analyzer);

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexPath);
			LNS.Hits luceneHits = searcher.Search (luceneQuery);
			int nHits = luceneHits.Length ();

			ArrayList hits = new ArrayList ();
			ArrayList toBeDeleted = new ArrayList ();
			ArrayList toBeInserted = new ArrayList ();

			for (int i = 0; i < nHits; ++i) {
				Hit hit = FromLuceneHit (luceneHits, i);
				bool valid = true;

				// Check that file:// hits still exist and haven't
				// changed since they were last indexed.
				if (hit.Uri.StartsWith ("file://")) {
					String path = hit.Uri.Substring (7);
					FileInfo info = new FileInfo (path);
		    
					if (info.Exists) {
						if (hit.ValidTimestamp
						    && hit.Timestamp < info.LastWriteTime) {
							Spew ("Out-of-date {0}", hit.Uri);
							valid = false;
							toBeDeleted.Add (hit.Uri);
							try {
								Indexable indexable = new IndexableFile (path);
								toBeInserted.Add (indexable);
							} catch {
								// If we get an exception, we couldn't figure
								// out how to filter the file.  In that case,
								// we just drop the file from the index and
								// throw away the hit.
								Spew ("Couldn't re-index {0}", hit.Uri);
							}
						}
					} else {
						// File has disappeared since being indexed.
						// FIXME: remove it from the index.
						Spew ("Lost {0}", hit.Uri);
						valid = false;
						toBeDeleted.Add (hit.Uri);
					}
				}

				if (valid)
					hits.Add (hit);
			}

			searcher.Close ();

			// If our index appears to be out-of-date, update the index
			// as necessary.  If documents changed, re-do the query.
			// This is fairly ugly, but we can probably get away with it
			// for now because queries are so very, very fast.
			//
			// FIXME: can we generate a score for a Document against a
			// Query in-memory, without going to the index?  Poking around
			// in Lucene, I didn't see an obvious way to do it.
			//
			// FIXME: Doing this assumes that you can write back to any index
			// that you can query.
			if (toBeDeleted.Count > 0)
				DoDelete (toBeDeleted);
			if (toBeInserted.Count > 0) {
				// To speed things up, we don't re-optimize the index.
				DoInsert (toBeInserted, false);
				return Query (query, step+1);
			}

			return hits;
		}
	}

}
