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
using System.Globalization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;


namespace Dewey {

	public class IndexDriver {

		// 1: Original
		// 2: Changed format of timestamp strings
		private const int VERSION = 2;

		public IndexDriver ()
		{
			BootstrapIndex ();
		}

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

		private const String timeFormat = "yyyyMMddHHmmss";

		static private String TimestampToString (DateTime dt)
                {
			return dt.ToString (timeFormat);
                }
                 
                static private DateTime StringToTimestamp (String str)
                {
			return DateTime.ParseExact (str, timeFormat, CultureInfo.CurrentCulture);
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

		private String RootDir {
			get {
				String homedir = Environment.GetEnvironmentVariable ("HOME");
				String dir = Path.Combine (homedir, ".dewey");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				// FIXME: We should set some reasonable permissions on the
				// .dewey directory.
				return dir;
			}
		}

		private String IndexDir {
			get {
				String dir = Path.Combine (RootDir, "Index");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				return dir;
			}
		}

		// FIXME: We don't actually use the lockdir for anything, since
		// Lucene's decision about where to put locks is tangled up in
		// the Store.FSDirectory code.  For now, it isn't worth the
		// trouble.
		private String LockDir {
			get {
				String dir = Path.Combine (RootDir, "Locks");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				return dir;
			}
		}

		private void BootstrapIndex ()
		{
			// Look to see if there are any signs of an existing index
			// with the correct version tag.  If everything looks OK,
			// just return.

			String indexTestFile = Path.Combine (IndexDir, "segments");
			String versionFile = Path.Combine (RootDir, "indexVersion");

			bool indexExists = File.Exists (indexTestFile);
			bool versionExists = File.Exists (versionFile);
			if (indexExists && versionExists) {
				StreamReader sr = new StreamReader (versionFile);
				String line = sr.ReadLine ();
				if (line == Convert.ToString (VERSION))
					return;
			}

			if (! indexExists)
				Spew ("Creating index.");
			else if (! versionExists)
				Spew ("No version information.  Purging index.");
			else
				Spew ("Index format is obsolete.  Purging index.");

			// If this looks like an old-style (pre-.dewey/Index) set-up,
			// blow away everything in sight.
			if (File.Exists (Path.Combine (RootDir, "segments")))
				Directory.Delete (RootDir, true);
			else {
				// Purge exist index-related directories.
				Directory.Delete (IndexDir, true);
				Directory.Delete (LockDir, true);
			}

			// Initialize a new index.
			IndexWriter writer = new IndexWriter (IndexDir, null, true);
			writer.Close ();

			// Write out the correct version information.
			StreamWriter sw = new StreamWriter (versionFile);
			sw.WriteLine (Convert.ToString (VERSION));
			sw.Close ();
		}

		//////////////////////////
		
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

			indexable.Open ();

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
			IndexReader reader = IndexReader.Open (IndexDir);
			foreach (String uri in uris) {
				Term term = new Term ("Uri", uri);
				Spew ("Removing {0}", uri);
				reader.Delete (term);
			}
			reader.Close ();
		}

		Random optimizeTest = new Random ();
		private void DoInsert (IEnumerable indexables)
		{
			Analyzer analyzer = NewAnayzer ();
			IndexWriter writer = new IndexWriter (IndexDir, analyzer, false);
			
			int count = 0;
			foreach (Indexable indexable in indexables) {
				Spew ("Inserting {0}", indexable.Uri);
				Document doc = ToLuceneDocument (indexable);
				writer.AddDocument (doc);
				++count;
			}
			// optimization is expensive
			// FIXME: this is just asking for trouble
			const int INSERTS_PER_OPTIMIZATION = 100;
			if (optimizeTest.Next (INSERTS_PER_OPTIMIZATION) < count) {
				Spew ("Optimizing...");
				writer.Optimize ();
				Spew ("Optimization complete.");
			}
			writer.Close ();
		}

		// Add a set of items to the index
		public void Add (IEnumerable indexables)
		{
			ArrayList preloaded = new ArrayList ();
			ArrayList toBeDeleted = new ArrayList ();
			ArrayList toBeInserted = new ArrayList ();

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexDir);

			// First, pre-load all Indexables.  Since Preload can block,
			// the Preloads should be done in multiple threads.
			foreach (Indexable indexable in indexables) {
				try {
					if (indexable.NeedPreload)
						indexable.Preload ();
					preloaded.Add (indexable);
				} catch {
					// If the Preload throws an exception, just 
					// forget about it.
				}
			}
			
			// If we've been handed multiple Indexables with the same Uri,
			// try to do something intelligent.
			Hashtable byUri = new Hashtable ();
			foreach (Indexable indexable in preloaded) {
				Indexable prev = (Indexable) byUri [indexable.Uri];
				if (prev == null || prev.IsObsoletedBy (indexable))
					byUri [indexable.Uri] = indexable;
			}
			
			foreach (Indexable indexable in byUri.Values) {
				
				Term term = new Term ("Uri", indexable.Uri);
				LNS.Query uriQuery = new LNS.TermQuery (term);
				LNS.Hits uriHits = searcher.Search (uriQuery);
				int nHits = uriHits.Length ();
				
				bool needsInsertion = true;
				
				for (int i = 0; i < nHits; ++i) {
					
					Document doc = uriHits.Doc (i);
					String oldTsStr = doc.Get ("Timestamp");
					String oldRevStr = doc.Get ("Revision");
					
					// If there is no timestamp or revision #, always
					// re-index
					if (oldTsStr == null && oldRevStr == null)
						continue;
					
					// First, try comparing the timestamps.
					if (oldTsStr != null) {
						DateTime oldTs = StringToTimestamp (oldTsStr);
						if (! indexable.IsNewerThan (oldTs)) {
							needsInsertion = false;
							break;
						}
					}
					
					// Next, try comparing the revisions.
					if (oldRevStr != null) {
						long oldRev = StringToRevision (oldRevStr);
						if (! indexable.IsNewerThan (oldRev)) {
							needsInsertion = false;
							break;
						}
					}
				}
		    
				if (needsInsertion) {
					if (nHits > 0) {
						Spew ("Re-scheduling {0}", indexable.Uri);
						toBeDeleted.Add (indexable.Uri);
					} else {
						Spew ("Scheduling {0}", indexable.Uri);
					}
					toBeInserted.Add (indexable);
				} else {
					Spew ("Skipping {0}", indexable.Uri);
				}
			}

			if (toBeDeleted.Count > 0)
				DoDelete (toBeDeleted);

			if (toBeInserted.Count > 0)
				DoInsert (toBeInserted);
		}
	
		// Add a single item to the index
		public void Add (Indexable indexable)
		{
			Add (new Indexable[] { indexable });
		}

		public IEnumerable Query (Query query)
		{
			Analyzer analyzer = NewAnayzer ();
			LNS.Query luceneQuery = ToLuceneQuery (query, analyzer);

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexDir);
			LNS.Hits luceneHits = searcher.Search (luceneQuery);
			int nHits = luceneHits.Length ();

			Hashtable seen = new Hashtable ();
			ArrayList hits = new ArrayList ();

			for (int i = 0; i < nHits; ++i) {
				Hit hit = FromLuceneHit (luceneHits, i);
				bool valid = true;

				// If the same Uri comes back more than once, filter
				// out all but the most recent one.
				Hit prev = (Hit) seen [hit.Uri];
				if (prev != null) {
					if (prev.IsObsoletedBy (hit)) {
						// FIXME: prev needs to be removed from the index
					} else {
						// FIXME: hit needs to be removed from the index
						continue;
					}
				}
				seen [hit.Uri] = hit;

				// Check that file:// hits still exist and haven't
				// changed since they were last indexed.
				if (hit.Uri.StartsWith ("file://")) {
					String path = hit.Uri.Substring (7);
					FileInfo info = new FileInfo (path);
		    
					if (info.Exists) {
						if (hit.IsObsoletedBy (info.LastWriteTime)) {
							Spew ("Out-of-date {0}", hit.Uri);
							// For now, out-of-date hits just pass through.
							
							// FIXME: We need to do something more clever here.
							// Ideally we would extract the content from the
							// file and write it into a in-memory Index,
							// then redo the query.  The in-memory Index could
							// then be written out to disk.
						}
					} else {
						// File has disappeared since being indexed.
						// FIXME: remove it from the index.
						Spew ("Lost {0}", hit.Uri);
						continue;
					}
				}


				hits.Add (hit);
			}

			searcher.Close ();

			return hits;
		}
	}

}
