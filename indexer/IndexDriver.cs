//
// IndexDriver.cs
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


namespace Beagle {

	public class IndexDriver : IQueryable {

		// 1: Original
		// 2: Changed format of timestamp strings
		// 3: Schema changed to be more Dashboard-Match-like
		// 4: Schema changed for files to include _Directory property
		// 5: Changed analyzer to support stemming.  Bumped version # to
		//    force everyone to re-index.
		private const int VERSION = 5;

		private Beagle.Util.Logger log = null;

		//////////////////////////

		public IndexDriver ()
		{
			lock (this) {
				if (log == null) {
					string logPath = Path.Combine (PathFinder.LogDir,
								       "Index");
					log = new Beagle.Util.Logger (logPath);
				}
			}

			BootstrapIndex ();

			Lucene.Net.Store.FSDirectory.Logger = log;
			Lucene.Net.Store.FSDirectory.TempDirectoryName = LockDir;
			IndexWriter.WRITE_LOCK_TIMEOUT = 30/*seconds*/ * 1000;
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

		private String IndexDir {
			get {
				String dir = Path.Combine (PathFinder.RootDir, "Index");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				return dir;
			}
		}

		private String LockDir {
			get {
				String dir = Path.Combine (PathFinder.RootDir, "Locks");
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
			String versionFile = Path.Combine (PathFinder.RootDir, "indexVersion");

			bool indexExists = File.Exists (indexTestFile);
			bool versionExists = PathFinder.HaveAppData ("__Index", "version");
			
			if (indexExists && versionExists) {
				String line = PathFinder.ReadAppDataLine ("__Index", "version");
				if (line == Convert.ToString (VERSION))
					return;
			}

			if (! indexExists)
				log.Log ("Creating index.");
			else if (! versionExists)
				log.Log ("No version information.  Purging index.");
			else
				log.Log ("Index format is obsolete.  Purging index.");

			// If this looks like an old-style (pre-.beagle/Index) set-up,
			// blow away everything in sight.
			if (File.Exists (Path.Combine (PathFinder.RootDir, "segments")))
				Directory.Delete (PathFinder.RootDir, true);
			else {
				// Purge exist index-related directories.
				Directory.Delete (IndexDir, true);
				Directory.Delete (LockDir, true);
			}

			// Initialize a new index.
			IndexWriter writer = new IndexWriter (IndexDir, null, true);
			writer.Close ();

			// Write out the correct version information.
			PathFinder.WriteAppDataLine ("__Index", "version", Convert.ToString (VERSION));
		}

		//////////////////////////

		// This is just a standard analyzer combined with the Porter stemmer.
		// FIXME: This assumes everything being indexed is in English!
		private class BeagleAnalyzer : StandardAnalyzer {
			public override TokenStream TokenStream (String fieldName, TextReader reader)
			{
				return new PorterStemFilter (base.TokenStream (fieldName, reader));
			}
		}
		
		private Analyzer NewAnalyzer ()
		{
			return new BeagleAnalyzer ();
		}

		//////////////////////////

		private String ToLucenePropertyKey (String key)
		{
			return "_" + key;
		}
	
		private Document ToLuceneDocument (Indexable indexable)
		{
			indexable.Build ();

			Document doc = new Document ();
			Field f;
			String str;

			// First we add the Indexable's 'canonical' properties
			// to the Document.
			
			f = Field.Keyword ("Uri", indexable.Uri);
			doc.Add (f);
			
			f = Field.Keyword ("Type", indexable.Type);
			doc.Add (f);

			if (indexable.MimeType != null) {
				f = Field.Keyword ("MimeType", indexable.MimeType);
				doc.Add (f);
			}
	    
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

			if (indexable.ContentReader != null) {
				f = Field.Text ("Content", indexable.ContentReader);
				doc.Add (f);
			} else if (indexable.Content != null) {
				f = Field.UnStored ("Content", indexable.Content);
				doc.Add (f);
			}
			
			if (indexable.HotContentReader != null) {
				f = Field.Text ("HotContent", indexable.HotContentReader);
				doc.Add (f);
			} else if (indexable.HotContent != null) {
				f = Field.UnStored ("HotContent", indexable.HotContent);
				doc.Add (f);
			}
			
			if (indexable.PropertiesAsString != null) {
				f = Field.UnStored ("Properties", indexable.PropertiesAsString);
				doc.Add (f);
			}
			
			foreach (String key in indexable.Keys) {
				// Namespace keys before storing them in the document.
				String docKey = ToLucenePropertyKey (key);
				String value = indexable [key];
		
				// If the key starts with _, treat it as
				// a keyword.
				if (key.Length > 0 && key [0] == '_')
					f = Field.Keyword (docKey, value);
				else
					f = Field.Text (docKey, value);
				doc.Add (f);
			}
			
			return doc;
		}

		private LNS.Query ToCoreLuceneQuery (Query    query,
						     string   field,
						     Analyzer analyzer)
		{
			LNS.BooleanQuery luceneQuery = null;
			foreach (string part in query.Parts) {

				// Use the analyzer to extract the query's tokens.
				// Code taken from Lucene's query parser.
				TokenStream source = analyzer.TokenStream (field, new StringReader (part));
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
					if (luceneQuery == null)
						luceneQuery = new LNS.BooleanQuery ();
					luceneQuery.Add (q, true, false);
				}
			}
			return luceneQuery;

		}
		
		private LNS.Query ToLuceneQuery (Query query)
		{
			Analyzer analyzer = NewAnalyzer ();
			LNS.BooleanQuery luceneQuery = new LNS.BooleanQuery ();
			
			LNS.Query metaQuery;
			metaQuery = ToCoreLuceneQuery (query, "Properties", analyzer);
			if (metaQuery == null)
				return null;
			metaQuery.SetBoost (2.5f);
			luceneQuery.Add (metaQuery, false, false);

			LNS.Query hotQuery;
			hotQuery = ToCoreLuceneQuery (query, "HotContent", analyzer);
			if (hotQuery == null)
				return null;
			hotQuery.SetBoost (1.75f);
			luceneQuery.Add (hotQuery, false, false);
		
			LNS.Query contentQuery;
			contentQuery = ToCoreLuceneQuery (query, "Content", analyzer);
			if (contentQuery == null)
				return null;
			luceneQuery.Add (contentQuery, false, false);
			
			return luceneQuery;
		}

		private Hit FromLuceneHit (LNS.Hits luceneHits, int i)
		{
			Hit hit = new Hit ();

			hit.Id = luceneHits.Id (i);
			
			Document doc = luceneHits.Doc (i);
			String str;
			
			str = doc.Get ("Uri");
			if (str == null)
				throw new Exception ("Got hit from Lucene w/o a URI!");
			hit.Uri = str;

			str = doc.Get ("Type");
			if (str == null)
				throw new Exception ("Got hit from Lucene w/o a Type!");
			hit.Type = str;
			
			hit.MimeType = doc.Get ("MimeType");

			str = doc.Get ("Timestamp");
			if (str != null)
				hit.Timestamp = StringToTimestamp (str);
			
			str = doc.Get ("Revision");
			if (str != null)
				hit.Revision = StringToRevision (str);
			
			hit.Source = "lucene";
			hit.ScoreRaw = luceneHits.Score (i);
			
			foreach (Field ff in doc.Fields ()) {
				String key = ff.Name ();
				// Non-property metadata keys always start with _.
				if (key.Length > 1 && key [0] == '_') {
					String realKey = key.Substring (1);
					hit [realKey] = ff.StringValue ();
				}
			}
			
			return hit;
		}

		private Hit[] FromLuceneHits (LNS.Hits luceneHits)
		{
			int nHits = luceneHits.Length ();
			Hit[] hits = new Hit[nHits];
			for (int i = 0; i < nHits; ++i)
				hits [i] = FromLuceneHit (luceneHits, i);
			return hits;
		}

		//////////////////////////

		public void QuickAdd (ICollection indexables)
		{
			if (indexables.Count == 0)
				return;

			Analyzer analyzer = NewAnalyzer ();
			IndexWriter writer = new IndexWriter (IndexDir, analyzer, false);
			
			foreach (Indexable indexable in indexables) {
				log.Log ("Inserting {0} (type={1})",
					 indexable.Uri, indexable.Type);
				Document doc = null;
				try {
					doc = ToLuceneDocument (indexable);
				} catch (Exception e) {
					log.Log (e);
				}
				if (doc != null)
					writer.AddDocument (doc);
			}
			writer.Close ();
		}


		public void QuickAdd (Indexable indexable)
		{
			QuickAdd (new Indexable [1] { indexable });
		}

		
		public void Remove (ICollection hits) 
		{
			if (hits.Count == 0)
				return;

			IndexReader reader = IndexReader.Open (IndexDir);
			foreach (Hit hit in hits) {
				log.Log ("Removing {0}", hit.Uri);
				reader.Delete (hit.Id);
			}
			reader.Close ();
		}

		public void Remove (Hit hit)
		{
			Remove (new Hit [1] { hit });
		}


		// Add a set of items to the index
		public void Add (ICollection indexables)
		{
			if (indexables.Count == 0)
				return;

			ArrayList toBeRemoved = new ArrayList ();
			ArrayList toBeAdded = new ArrayList ();

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexDir);

			// If we've been handed multiple Indexables with the same Uri,
			// try to do something intelligent.
			Hashtable byUri = new Hashtable ();
			foreach (Indexable indexable in indexables) {
				Indexable prev = (Indexable) byUri [indexable.Uri];
				if (indexable.IsNewerThan (prev))
					byUri [indexable.Uri] = indexable;
			}
			
			foreach (Indexable indexable in indexables) {

				// Skip duplicates
				if (byUri [indexable.Uri] != indexable)
					continue;
				
				Term term = new Term ("Uri", indexable.Uri);
				LNS.Query uriQuery = new LNS.TermQuery (term);
				LNS.Hits uriHits = searcher.Search (uriQuery);
				int nHits = uriHits.Length ();
				
				bool needsInsertion = true;
				
				for (int i = 0; i < nHits; ++i) {
					Hit hit = FromLuceneHit (uriHits, i);
					if (indexable.IsNewerThan (hit)) {
						// Schedule the old document's removal
						toBeRemoved.Add (hit);
					} else {
						needsInsertion = false;
						break;
					}
				}
						
				if (needsInsertion) {
					if (nHits > 0)
						log.Log ("Re-scheduling {0}", indexable.Uri);
					else
						log.Log ("Scheduling {0}", indexable.Uri);
					toBeAdded.Add (indexable);
				} else {
					log.Log ("Skipping {0}", indexable.Uri);
				}
			}

			Remove (toBeRemoved);
			QuickAdd (toBeAdded);
		}
	
		// Add a single item to the index
		public void Add (Indexable indexable)
		{
			Add (new Indexable[] { indexable });
		}

		public void Optimize ()
		{
			log.Log ("Beginning optimization");
			IndexWriter writer = new IndexWriter (IndexDir, NewAnalyzer (), false);
			writer.Optimize ();
			writer.Close ();
			log.Log ("Optimization complete");
		}

		///////////////////////////////////////////////////////

		public Hit FindByUri (String uri)
		{
			Term term = new Term ("Uri", uri);
			LNS.Query uriQuery = new LNS.TermQuery (term);

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexDir);
			LNS.Hits uriHits = searcher.Search (uriQuery);

			int nHits = uriHits.Length ();
			Hit hit = null;

			if (nHits > 0) {
				hit = FromLuceneHit (uriHits, 0);
				for (int i = 1; i < nHits; ++i) {
					Hit altHit = FromLuceneHit (uriHits, i);
					if (altHit.IsNewerThan (hit))
						hit = altHit;
				}
			}

			searcher.Close ();

			return hit;
		}

		// Returns all hits that exactly match.
		public Hit[] FindByProperty (String key, String value)
		{
			Term term = new Term (ToLucenePropertyKey (key), value);
			LNS.Query luceneQuery = new LNS.TermQuery (term);

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexDir);
			LNS.Hits luceneHits = searcher.Search (luceneQuery);
			Hit[] hits = FromLuceneHits (luceneHits);
			searcher.Close ();

			return hits;
		}

		
		///////////////////////////////////////////////////////

		///
		/// IQueryable interface
		///

		public String Name {
			get { return "Lucene"; }
		}

		public bool AcceptQuery (Query query)
		{
			return true;
		}

		public void Query (Query query, IQueryResult result)
		{
			LNS.Query luceneQuery = ToLuceneQuery (query);
			if (luceneQuery == null)
				return;

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexDir);
			LNS.Hits luceneHits = searcher.Search (luceneQuery);

			int nHits = luceneHits.Length ();
			Hashtable seen = new Hashtable ();

			for (int i = 0; i < nHits; ++i) {
				Hit hit = FromLuceneHit (luceneHits, i);
				
				// Filter out missing files.
				if (hit.Uri.StartsWith ("file://")) {
					String path = hit.Uri.Substring ("file://".Length);
					if (! File.Exists (path))
						continue;
				}

				// FIXME: Should check that file:// Uris are unchanged, and do
				// something smart if they aren't.
				
				// If the same Uri comes back more than once, filter
				// out all but the most recent one.
				//
				// FIXME: The same Uri could back for, say, web history
				// and Google.  In that case, Google will always win,
				// even though web history will probably be more relevant.
				// Maybe the most relevant match should win if the
				// types/sources are different?
				Hit prev = (Hit) seen [hit.Uri];
				if (hit.IsNewerThan (prev))
					seen [hit.Uri] = hit;
			}

			searcher.Close ();
			result.Add (seen.Values);
		}
	}

}
