//
// LuceneDriver.cs
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
// about Lucene's internals.
//

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using Beagle.Util;

namespace Beagle.Daemon {

	public class LuceneDriver {

		public delegate bool UriFilter (Uri uri);


		public delegate void ChangedHandler (LuceneDriver source,
						     ICollection list_of_added_uris,
						     ICollection list_of_removed_uris);

		public event ChangedHandler ChangedEvent;

		
		// 1: Original
		// 2: Changed format of timestamp strings
		// 3: Schema changed to be more Dashboard-Match-like
		// 4: Schema changed for files to include _Directory property
		// 5: Changed analyzer to support stemming.  Bumped version # to
		//    force everyone to re-index.
		// 6: lots of schema changes as part of the general refactoring
		private const int VERSION = 6;

		private Hashtable pending_by_uri = new Hashtable ();
		private int pending_adds = 0;
		private int pending_removals = 0;
		private int adds_since_last_optimization = 0;
		private int removals_since_last_optimization = 0;
		private bool optimizing = false;

		public LuceneDriver (string dir)
		{
			Setup (dir);
		}

		private void Setup (string dir)
		{
			string versionFile = Path.Combine (dir, "version");
			string fingerprintFile = Path.Combine (dir, "fingerprint");
			string lockDir = Path.Combine (dir, "Locks");
			string indexDir = Path.Combine (dir, "Index");
			string queueDir = Path.Combine (dir, "Queue");
			string indexTestFile = Path.Combine (indexDir, "segments");

			bool versionExists = File.Exists (versionFile);
			bool fingerprintExists = File.Exists (fingerprintFile);
			bool indexExists = File.Exists (indexTestFile);

			// Check the index's version number.  If it is wrong,
			// declare the index non-existent.
			if (versionExists && indexExists) {
				StreamReader sr = new StreamReader (versionFile);
				string versionStr = sr.ReadLine ();
				sr.Close ();
				
				if (versionStr != Convert.ToString (VERSION))
					indexExists = false;
			}

			// If there is no fingerprint file, declare the index
			// non-existent.
			if (indexExists && ! fingerprintExists)
				indexExists = false;

			// If the index seems to exist but contains dangling locks,
			// declare the index non-existent.
			if (indexExists) {
				DirectoryInfo lockDirInfo = new DirectoryInfo (lockDir);
				if (! lockDirInfo.Exists)
					indexExists = false;
				else {
					foreach (FileInfo info in lockDirInfo.GetFiles ()) {
						if (info.Name.IndexOf (".lock") != -1) {
							indexExists = false;
							break;
						}
					}
					if (! indexExists)
						log.Debug ("Found dangling locks in {0}", lockDir);
				}
			}

			if (indexExists) {
				// Read in the fingerprint
				StreamReader sr = new StreamReader (fingerprintFile);
				fingerprint = sr.ReadLine ();
				sr.Close ();

			} else {
				// Purge and rebuild the index's directory
				// structure.

				if (Directory.Exists (dir)) {
					log.Debug ("Purging {0}", dir);
					Directory.Delete (dir, true);
				}

				// Create all directories.
				Directory.CreateDirectory (dir);
				Directory.CreateDirectory (lockDir);
				Directory.CreateDirectory (indexDir);

				StreamWriter sw;

				// Generate a fingerprint and write it out
				fingerprint = DateTime.Now.Ticks.ToString ();
				sw = new StreamWriter (fingerprintFile, false);
				sw.WriteLine (fingerprint);
				sw.Close ();

				// Write out our version information
				sw = new StreamWriter (versionFile, false);
				sw.WriteLine ("{0}", VERSION);
				sw.Close ();
			}

			Lucene.Net.Store.FSDirectory store;
			store = Lucene.Net.Store.FSDirectory.GetDirectory (indexDir, false);
			store.TempDirectoryName = lockDir;

			Store = store;

			// Before we start, optimize the index.  We want to do
			// this every time at start-up to help avoid running
			// out of file descriptors if the beagled were to
			// crash or be shut down while in a non-optimized state.
			// (We do the optimization in a thread since it is I/O bound
			// and slows down start-up.)
			if (indexExists) {
				Thread opt_th = new Thread (new ThreadStart (Optimize));
				opt_th.Start ();
			} else {
				// This creates the index if it doesn't exist
				IndexWriter writer = new IndexWriter (Store, null, true);
				writer.Close ();
			}
		}

		/////////////////////////////////////////////////////

		//
		// The log
		//

		private static Logger log = Logger.Get ("lucene");

		private static Logger Log {
			get { return log; }
		}

		/////////////////////////////////////////////////////

		//
		// The Index's Fingerprint and up-to-date checking
		//
		
		private string fingerprint = null;

		public string Fingerprint {
			get { return fingerprint; }
		}

		private IFileAttributesStore fa_store = null;

		public IFileAttributesStore FileAttributesStore {
			get { return fa_store; }
			set { fa_store = value; }
		}

		public bool IsUpToDate (string path)
		{
			if (fa_store == null)
				return false;

			FileAttributes attr = fa_store.Read (path);

			// FIXME: This check is incomplete
			return attr != null
				&& attr.Path == path
				&& FileSystem.GetLastWriteTime (path) <= attr.LastWriteTime;
		}

		public void AttachTimestamp (string path, DateTime mtime)
		{
			if (fa_store == null)
				return;

			FileAttributes attr = new FileAttributes ();

			attr.UniqueId = "Foo"; // FIXME!
			attr.Path = path;
			attr.LastWriteTime = mtime;
			attr.LastIndexedTime = DateTime.Now;

			if (! fa_store.Write (attr)) {
				log.Warn ("Couldn't store file attributes for {0}", path);
			}
		}

		/////////////////////////////////////////////////////
		
		//
		// The Lucene Store
		//

		private Lucene.Net.Store.Directory ourStore = null;

		public Lucene.Net.Store.Directory Store {
			get { return ourStore; }

			set {
				if (ourStore != null)
					throw new Exception ("Attempt to attach a second store to a LuceneDriver");
				ourStore = (Lucene.Net.Store.Directory) value;
			}
		}

		/////////////////////////////////////////////////////

		//
		// Public Indexing API
		//

		public void Add (Indexable indexable)
		{
			Uri uri = indexable.Uri;

			lock (pending_by_uri) {
				if (pending_by_uri.Contains (uri) && pending_by_uri [uri] == null)
					--pending_removals;
				pending_by_uri [uri] = indexable;
				++pending_adds;
			}
		}

		public void Remove (Uri uri)
		{
			lock (pending_by_uri) {
				if (pending_by_uri [uri] != null)
					--pending_adds;
				pending_by_uri [uri] = null;
				++pending_removals;
			}
		}

		public int PendingAdds {
			get { return pending_adds; }
		}

		public int PendingRemovals {
			get { return pending_removals; }
		}

		public void Flush ()
		{
			ArrayList pending_uris;
			ArrayList pending_indexables;

			ArrayList added_uris;
			ArrayList removed_uris;
			
			lock (pending_by_uri) {
				
				if (pending_by_uri.Count == 0)
					return;

				pending_uris = new ArrayList ();
				pending_indexables = new ArrayList ();
				added_uris = new ArrayList ();
				removed_uris = new ArrayList ();

				// Move our indexables and remove requests out of the
				// hash and into local data structures.
				foreach (DictionaryEntry entry in pending_by_uri) {
					Uri uri = (Uri) entry.Key;
					Indexable indexable = (Indexable) entry.Value;

					// Filter out indexables with
					// non-transient file ContentUris that
					// appear to be up-to-date.
					if (indexable != null
					    && indexable.IsNonTransient
					    && IsUpToDate (indexable.ContentUri.LocalPath))
						continue;
					
					pending_uris.Add (uri);
					if (indexable != null)
						pending_indexables.Add (indexable);
					
					if (indexable != null)
						added_uris.Add (uri);
					else
						removed_uris.Add (uri);
				}

				pending_adds = 0;
				pending_removals = 0;
				pending_by_uri.Clear ();
			}

			int add_count = 0;
			int removal_count = 0;

			Log.Debug ("Flushing...");

			Stopwatch watch = new Stopwatch ();
				
			// Step #1: Delete all items with the same URIs
			// as our pending items from the index.
			watch.Restart ();
			IndexReader reader = IndexReader.Open (Store);
			foreach (Uri uri in pending_uris) {
				log.Debug ("- {0}", uri);
				Term term = new Term ("Uri", uri.ToString ());
				reader.Delete (term);
				++removal_count;
			}
			reader.Close ();
			watch.Stop ();
			//Log.Debug ("Step #1: {0} {1} {2}", watch, pending_uris.Count,
			//	   watch.ElapsedTime / pending_uris.Count);

			
			// Step #2: Cache non-transient content mtimes
			Hashtable mtimes = new Hashtable ();
			foreach (Indexable indexable in pending_indexables) {
				if (indexable.IsNonTransient) {
					try {
						string path = indexable.ContentUri.LocalPath;
						mtimes [path] = FileSystem.GetLastWriteTime (path);
					} catch { }
				}
			}


			// Step #3: Write out the pending adds
			watch.Restart ();
			IndexWriter writer = null;
			foreach (Indexable indexable in pending_indexables) {
				
				Log.Debug ("+ {0}", indexable.Uri);
				
				Document doc = null;
				try {
					doc = ToLuceneDocument (indexable);
				} catch (Exception e) {
					Log.Error ("Unable to convert {0} (type={1}) to a lucene document",
						   indexable.Uri, indexable.Type);
					Log.Error (e);
				}
				
				if (doc != null) {
					if (writer == null)
						writer = new IndexWriter (Store, Analyzer, false);
					writer.AddDocument (doc);
					++add_count;
				}
			}
			if (writer != null) 
				writer.Close ();
			watch.Stop ();
			//Log.Debug ("Step #3: {0}", watch);
			
			// Step #4: Mark added non-transient ContentUri files.
			watch.Restart ();
			foreach (Indexable indexable in pending_indexables) {
				if (indexable.ContentUri.IsFile && ! indexable.DeleteContent) {
					string path = indexable.ContentUri.LocalPath;
					if (mtimes.Contains (path))
						AttachTimestamp (path, (DateTime) mtimes [path]);
				}
			}
			watch.Stop ();
			//Log.Debug ("Step #4: {0}", watch);

			if (ChangedEvent != null) {
				ChangedEvent (this, added_uris, removed_uris);
			}

			lock (pending_by_uri) {
				adds_since_last_optimization += add_count;
				removals_since_last_optimization += removal_count;
			}
		}

		public bool NeedsOptimize {
			get { 
				// FIXME: 47 is a totally arbitrary number.
				return adds_since_last_optimization + removals_since_last_optimization > 47;
			}
		}

		public void Optimize ()
		{
			// If nothing has happened since our last optimization,
			// do dothing.
			// If this index is already being optimized, don't
			// optimize it again.
			lock (pending_by_uri) {
				if (optimizing || (adds_since_last_optimization == 0
						   && removals_since_last_optimization == 0))
					return;
				optimizing = true;
			}

			IndexWriter writer = new IndexWriter (Store, null, false);
			writer.Optimize ();
			writer.Close ();

			lock (pending_by_uri) {
				optimizing = false;
				adds_since_last_optimization = 0;
				removals_since_last_optimization = 0;
			}
		}

		/////////////////////////////////////////////////////

		public void DoQuery (QueryBody body, IQueryResult result, ICollection list_of_uris, UriFilter uri_filter)
		{
			LNS.Searcher searcher = new LNS.IndexSearcher (Store);
			LNS.Query query = ToLuceneQuery (body, list_of_uris);

			LNS.Hits hits = searcher.Search (query);
			int n_hits = hits.Length ();

			if (n_hits == 0)
				return;

			ArrayList filtered_hits = new ArrayList ();
			for (int i = 0; i < n_hits; ++i) {
				Document doc = hits.Doc (i);
				if (uri_filter != null) {
					Uri uri = UriFromLuceneDoc (doc);
					if (! uri_filter (uri))
						continue;
				}
				Hit hit = FromLuceneDocToHit (doc, hits.Id (i), hits.Score (i));
				if (hit != null)
					filtered_hits.Add (hit);

				if ((i + 1) % 200 == 0) {
					result.Add (filtered_hits);
					filtered_hits = new ArrayList ();
				}
			}
			result.Add (filtered_hits);

			searcher.Close ();
		}

		public ICollection DoQueryByUri (ICollection list_of_uris)
		{
			LNS.BooleanQuery uri_query = new LNS.BooleanQuery ();
			LNS.Searcher searcher;
			LNS.Hits lucene_hits;
			ArrayList all_hits = new ArrayList ();

			int max_clauses = LNS.BooleanQuery.GetMaxClauseCount ();
			int clause_count = 0;

			foreach (Uri uri in list_of_uris) {
				Term term = new Term ("Uri", uri.ToString ());
				LNS.Query term_query = new LNS.TermQuery (term);
				uri_query.Add (term_query, false, false);
				++clause_count;

				if (clause_count == max_clauses) {
					searcher = new LNS.IndexSearcher (Store);
					lucene_hits = searcher.Search (uri_query);
					int n_hits = lucene_hits.Length ();

					for (int i = 0; i < n_hits; ++i) {
						Hit hit = FromLuceneDocToHit (lucene_hits.Doc (i),
									      lucene_hits.Id (i),
									      lucene_hits.Score (i));
						all_hits.Add (hit);
					}

					searcher.Close ();
					uri_query = new LNS.BooleanQuery ();
					clause_count = 0;
				}
			}

			if (clause_count > 0) {
				searcher = new LNS.IndexSearcher (Store);
				lucene_hits = searcher.Search (uri_query);
				int n_hits = lucene_hits.Length ();
				
				for (int i = 0; i < n_hits; ++i) {
					Hit hit = FromLuceneDocToHit (lucene_hits.Doc (i),
								      lucene_hits.Id (i),
								      lucene_hits.Score (i));
					all_hits.Add (hit);
				}

				searcher.Close ();
			}

			return all_hits;
		}

		public ICollection DoQueryByUri (Uri uri)
		{
			return DoQueryByUri (new Uri[1] { uri });
		}


		///////////////////////////////////////////////////////////////////////////////////////

		//
		// Code to map to/from Lucene data types
		//

		private Document ToLuceneDocument (Indexable indexable)
		{			
			indexable.Build ();
			
			Document doc = new Document ();
			Field f;
			String str;
			TextReader reader;

			// First we add the Indexable's 'canonical' properties
			// to the Document.
			
			f = Field.Keyword ("Uri", indexable.Uri.ToString ());
			doc.Add (f);

			f = Field.Keyword ("Type", indexable.Type);
			doc.Add (f);
			
			if (indexable.MimeType != null) {
				f = Field.Keyword ("MimeType", indexable.MimeType);
				doc.Add (f);
			}
			
			if (indexable.ValidTimestamp) {
				str = StringFu.DateTimeToString (indexable.Timestamp);
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
		
		private LNS.Query ToCoreLuceneQuery (QueryBody body, string field)
		{
			LNS.BooleanQuery luceneQuery = null;
			foreach (string text in body.Text) {

				// Use the analyzer to extract the query's tokens.
				// This code is taken from Lucene's query parser.
				// We use the standard Analyzer.
				TokenStream source = LuceneDriver.Analyzer.TokenStream (field, new StringReader (text));
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

		private LNS.Query ToLuceneQuery (QueryBody body,
						 ICollection listOfUris)
		{
			LNS.BooleanQuery luceneQuery = new LNS.BooleanQuery ();
			
			if (body.Text.Count > 0) {
				LNS.BooleanQuery contentQuery = new LNS.BooleanQuery ();

				LNS.Query propTQuery;
				propTQuery = ToCoreLuceneQuery (body, "PropertiesText");
				if (propTQuery != null) {
					propTQuery.SetBoost (2.5f);
					contentQuery.Add (propTQuery, false, false);
				}

				LNS.Query propKQuery;
				propKQuery = ToCoreLuceneQuery (body, "PropertiesKeyword");
				if (propKQuery != null) {
					propKQuery.SetBoost (2.5f);
					contentQuery.Add (propKQuery, false, false);
				}
				
				LNS.Query hotQuery;
				hotQuery = ToCoreLuceneQuery (body, "HotText");
				if (hotQuery != null) {
					hotQuery.SetBoost (1.75f);
					contentQuery.Add (hotQuery, false, false);		
				}
				
				LNS.Query textQuery;
				textQuery = ToCoreLuceneQuery (body, "Text");
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

			// If a list of Uris is specified, we must match one of them.
			if (listOfUris != null && listOfUris.Count > 0) {
				LNS.BooleanQuery uriQuery = new LNS.BooleanQuery ();
				foreach (Uri uri in listOfUris) {
					Term t = new Term ("Uri", uri.ToString ());
					LNS.Query q = new LNS.TermQuery (t);
					uriQuery.Add (q, false, false);
				}
				luceneQuery.Add (uriQuery, true, false);
			}

			return luceneQuery;
		}
		
		static private Uri UriFromLuceneDoc (Document doc)
		{
			string uri = doc.Get ("Uri");
			if (uri == null)
				throw new Exception ("Got document from Lucene w/o a URI!");
			return new Uri (uri, true);
		}

		static private void FromLuceneDocToVersioned (Document doc, Versioned versioned)
		{
			string str;

			str = doc.Get ("Timestamp");
			if (str != null)
				versioned.Timestamp = StringFu.StringToDateTime (str);
			
			str = doc.Get ("Revision");
			if (str != null)
				versioned.Revision = StringToRevision (str);

		}

		private Hit FromLuceneDocToHit (Document doc, int id, float score)
		{
			Hit hit = new Hit ();

			hit.Id = id;
			
			string str;

			FromLuceneDocToVersioned (doc, hit);
			
			hit.Uri = UriFromLuceneDoc (doc);

			str = doc.Get ("Type");
			if (str == null)
				throw new Exception ("Got hit from Lucene w/o a Type!");
			hit.Type = str;
			
			hit.MimeType = doc.Get ("MimeType");

			hit.Source = "lucene";
			hit.ScoreRaw = score;
			
			foreach (Field ff in doc.Fields ()) {
				string key = FromLucenePropertyKey (ff.Name ());
				if (key != null)
					hit [key] = ff.StringValue ();
			}
			
			return hit;
		}


		/////////////////////////////////////////////////////

		//
		// A common, shared analyzer
		//

		// This is just a standard analyzer combined with the Porter stemmer.
		// FIXME: This assumes everything being indexed is in English!
		private class BeagleAnalyzer : StandardAnalyzer {
			public override TokenStream TokenStream (String fieldName, TextReader reader)
			{
				return new PorterStemFilter (base.TokenStream (fieldName, reader));
			}
		}

		private static Analyzer theAnalyzer;

		private static Analyzer Analyzer {
			get { 
				if (theAnalyzer == null)
					theAnalyzer = new BeagleAnalyzer ();
				return theAnalyzer;
			}
		}

		/////////////////////////////////////////////////////

		//
		// Helpful little utility functions
		//

		static private String RevisionToString (long rev)
		{
			return Convert.ToString (rev);
		}

		static private long StringToRevision (String str)
		{
			return Convert.ToInt64 (str);
		}

		const string propPrefix = "prop:";

		private string ToLucenePropertyKey (string key)
		{
			return propPrefix + key;
		}

		private string FromLucenePropertyKey (string key)
		{
			if (key.StartsWith (propPrefix))
				return key.Substring (propPrefix.Length);
			return null;
		}
	}
}
