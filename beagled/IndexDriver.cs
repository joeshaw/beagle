//
// IndexDriver.cs
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

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public class IndexDriver : IQueryable {
		const string propPrefix = "prop:";

		private Beagle.Util.Logger log = null;

		private Lucene.Net.Store.Directory store = null;

		private int sinceOptimization = 0;
		private const int sinceOptimizationThreshold = 100;

		//////////////////////////

		public IndexDriver (Lucene.Net.Store.Directory _store) {
			lock (this) {
				if (log == null) {
					string logPath = Path.Combine (PathFinder.LogDir,
								       "Index");
					log = new Beagle.Util.Logger (logPath);
				}
			}

			store = _store;

			IndexWriter.WRITE_LOCK_TIMEOUT = 30/*seconds*/ * 1000;
		}

		//////////////////////////
		
		static private String RevisionToString (long rev)
		{
			return Convert.ToString (rev);
		}

		static private long StringToRevision (String str)
		{
			return Convert.ToInt64 (str);
		}

		//////////////////////////

		private Lucene.Net.Store.Directory IndexStore {
			get {
				return store;
			}
		}

		protected Beagle.Util.Logger Log {
			get {
				return log;
			}
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
			return propPrefix + key;
		}

		private Document ToLuceneDocument (Indexable indexable)
		{			
			Document doc = new Document ();
			Field f;
			String str;
			TextReader reader;

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
				str = BU.StringFu.DateTimeToString (indexable.Timestamp);
				f = Field.Keyword ("Timestamp", str);
				doc.Add (f);
			}
			
			if (indexable.ValidRevision) {
				f = Field.UnIndexed ("Revision",
						     RevisionToString (indexable.Revision));
				doc.Add (f);
			}
			
			reader = indexable.GetTextReader ();
			if (reader != null) {
				f = Field.Text ("Text", reader);
				doc.Add (f);
			}
			
			reader = indexable.GetHotTextReader ();
			if (reader != null) {
				f = Field.Text ("HotText", reader);
				doc.Add (f);
			}

			f = Field.UnStored ("PropertiesText",
					    indexable.TextPropertiesAsString);
			doc.Add (f);

			// FIXME: We shouldn't apply stemming, etc. when dealing
			// with this field.
			f = Field.UnStored ("PropertiesKeyword",
					    indexable.KeywordPropertiesAsString);
			doc.Add (f);
			
			// FIXME: We need to deal with duplicate properties in some
			// sort of sane way.
			foreach (Property prop in indexable.Properties) {
				if (prop.Value != null) {
					f = Field.Keyword (ToLucenePropertyKey (prop.Key),
							   prop.Value);
					doc.Add (f);
				}
			}
			
			return doc;
		}

		private LNS.Query ToCoreLuceneQuery (QueryBody body,
						     string    field,
						     Analyzer  analyzer)
		{
			LNS.BooleanQuery luceneQuery = null;
			foreach (string text in body.Text) {

				// Use the analyzer to extract the query's tokens.
				// Code taken from Lucene's query parser.
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
					if (luceneQuery == null)
						luceneQuery = new LNS.BooleanQuery ();
					luceneQuery.Add (q, true, false);
				}
			}
			return luceneQuery;

		}
		
		private LNS.Query ToLuceneQuery (QueryBody body)
		{
			LNS.BooleanQuery luceneQuery = new LNS.BooleanQuery ();
			
			if (body.Text.Count > 0) {
				Analyzer analyzer = NewAnalyzer ();
				LNS.BooleanQuery contentQuery = new LNS.BooleanQuery ();

				LNS.Query propTQuery;
				propTQuery = ToCoreLuceneQuery (body, "PropertiesText", analyzer);
				if (propTQuery != null) {
					propTQuery.SetBoost (2.5f);
					contentQuery.Add (propTQuery, false, false);
				}

				LNS.Query propKQuery;
				propKQuery = ToCoreLuceneQuery (body, "PropertiesKeyword", analyzer);
				if (propKQuery != null) {
					propKQuery.SetBoost (2.5f);
					contentQuery.Add (propKQuery, false, false);
				}
				
				LNS.Query hotQuery;
				hotQuery = ToCoreLuceneQuery (body, "HotText", analyzer);
				if (hotQuery != null) {
					hotQuery.SetBoost (1.75f);
					contentQuery.Add (hotQuery, false, false);		
				}
				
				LNS.Query textQuery;
				textQuery = ToCoreLuceneQuery (body, "Text", analyzer);
				if (textQuery != null) {
					contentQuery.Add (textQuery, false, false);
				}

				luceneQuery.Add (contentQuery, true, false);
			}

			// If mime types are specified, we must match one of them.
			if (body.MimeTypes.Count > 0) {
				LNS.BooleanQuery mimeTypeQuery = new LNS.BooleanQuery ();
				foreach (string mimeType in body.MimeTypes) {
					Term t = new Term ("MimeType", mimeType);
					LNS.Query q = new LNS.TermQuery (t);
					mimeTypeQuery.Add (q, false, false);
				}
				luceneQuery.Add (mimeTypeQuery, true, false);
			}
			
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
				hit.Timestamp = BU.StringFu.StringToDateTime (str);
			
			str = doc.Get ("Revision");
			if (str != null)
				hit.Revision = StringToRevision (str);
			
			hit.Source = "lucene";
			hit.ScoreRaw = luceneHits.Score (i);
			
			foreach (Field ff in doc.Fields ()) {
				String key = ff.Name ();
				// Non-core properties always start with prop:.
				if (key.StartsWith (propPrefix)) {
					String realKey = key.Substring (propPrefix.Length);
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
			IndexWriter writer = new IndexWriter (IndexStore, analyzer, false);
			
			foreach (Indexable indexable in indexables) {
				Document doc = null;
				try {
					doc = ToLuceneDocument (indexable);
				} catch (Exception e) {
					log.Log (e);
					Console.WriteLine ("unable to convert {0} (type={1}) to a lucene document",
							   indexable.Uri, indexable.Type);
					Console.WriteLine (e.Message);
					Console.WriteLine (e.StackTrace);
				}
				if (doc != null) {
					writer.AddDocument (doc);
					++sinceOptimization;
				}
				if (sinceOptimization > sinceOptimizationThreshold) {
					Optimize (writer);
				}
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

			IndexReader reader = IndexReader.Open (IndexStore);
			foreach (Hit hit in hits) {
				log.Log ("Removing {0}", hit.Uri);
				reader.Delete (hit.Id);
				++sinceOptimization;
			}
			reader.Close ();

			if (sinceOptimization > sinceOptimizationThreshold)
				Optimize ();
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

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexStore);

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

		private void Optimize (IndexWriter writer)
		{
			if (sinceOptimization == 0)
				return;

			bool createdWriter = false;
			if (writer == null) {
				writer = new IndexWriter (IndexStore, NewAnalyzer (), false);
				createdWriter = true;
			}

			log.Log ("Beginning optimization");
			writer.Optimize ();
			log.Log ("Optimization complete");
			
			if (createdWriter)
				writer.Close ();

			sinceOptimization = 0;
		}

		private void Optimize ()
		{
			Optimize (null);
		}

		///////////////////////////////////////////////////////

		public Hit FindByUri (String uri)
		{			
			Term term = new Term ("Uri", uri);
			LNS.Query uriQuery = new LNS.TermQuery (term);

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexStore);
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

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexStore);
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

		public bool AcceptQuery (QueryBody body)
		{
			if (! body.AllowsDomain (QueryDomain.Local)) {
				return false;
			}

			return true;
		}

		public void DoQuery (QueryBody body, IQueryResult result)
		{
			LNS.Query luceneQuery = ToLuceneQuery (body);
			if (luceneQuery == null)
				return;

			LNS.Searcher searcher = new LNS.IndexSearcher (IndexStore);
			LNS.Hits luceneHits = searcher.Search (luceneQuery);

			int nHits = luceneHits.Length ();
			Hashtable seen = new Hashtable ();

			for (int i = 0; i < nHits; ++i) {
				Hit hit = FromLuceneHit (luceneHits, i);
				
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
