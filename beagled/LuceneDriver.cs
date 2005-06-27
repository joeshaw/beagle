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
using System.Diagnostics;
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

	public class LuceneDriver : IIndexer {

		public delegate bool UriFilter (Uri uri);
		public delegate Uri UriRemapper (Uri uri);
		public delegate double RelevancyMultiplier (Hit hit);

		public event IIndexerChangedHandler ChangedEvent;
		public event IIndexerChildIndexableHandler ChildIndexableEvent;
		public event IIndexerUrisFilteredHandler UrisFilteredEvent;

		/////////////////////////////////////////////////////
		
		// 1: Original
		// 2: Changed format of timestamp strings
		// 3: Schema changed to be more Dashboard-Match-like
		// 4: Schema changed for files to include _Directory property
		// 5: Changed analyzer to support stemming.  Bumped version # to
		//    force everyone to re-index.
		// 6: lots of schema changes as part of the general refactoring
		// 7: incremented to force a re-index after our upgrade to lucene 1.4
		//    (in theory the file formats are compatible, we are seeing 'term
		//    out of order' exceptions in some cases)
		// 8: another forced re-index, this time because of massive changes
		//    in the file system backend (it would be nice to have per-backend
		//    versioning so that we didn't have to purge all indexes just
		//    because one changed)
		// 9: changed the way properties are stored, changed in conjunction
		//    with sane handling of multiple properties on hits.
		private const int MAJOR_VERSION = 9;
		private int minor_version = 0;

		private string top_dir;
		private Hashtable pending_by_uri = UriFu.NewHashtable ();
		private int pending_adds = 0;
		private int pending_removals = 0;
		private int cycles_since_last_optimization = 0;
		private bool optimizing = false;
		private int last_item_count = -1;

		public LuceneDriver (string index_name) : this (index_name, 0) { }

		public LuceneDriver (string index_name, int index_version)
		{
			Setup (index_name, index_version);
		}

		public string IndexDirectory {
			get { return top_dir; }
		}

		/////////////////////////////////////////////////////

		//
		// The Lucene Store
		//

		private Lucene.Net.Store.Directory ourStore = null;
		private string ourStorePath = null;
		

		public Lucene.Net.Store.Directory Store {
			get { return ourStore; }
		}

		public string StorePath {
			get { return ourStorePath; }
		}

		/////////////////////////////////////////////////////

		private void Setup (string index_name, int _minor_version)
		{			
			top_dir = Path.Combine (PathFinder.StorageDir, index_name); 
			
			string versionFile = Path.Combine (top_dir, "version");
			string fingerprintFile = Path.Combine (top_dir, "fingerprint");
			string lockDir = Path.Combine (top_dir, "Locks");
			string indexDir = Path.Combine (top_dir, "Index");
			string indexTestFile = Path.Combine (indexDir, "segments");

			bool versionExists = File.Exists (versionFile);
			bool fingerprintExists = File.Exists (fingerprintFile);
			bool indexExists = File.Exists (indexTestFile);

			if (_minor_version < 0)
				_minor_version = 0;
			minor_version = _minor_version;

			// Check the index's version number.  If it is wrong,
			// declare the index non-existent.
			if (versionExists && indexExists) {
				StreamReader sr = new StreamReader (versionFile);
				string versionStr = sr.ReadLine ();
				sr.Close ();

				int old_major_version, old_minor_version;
				int i = versionStr.IndexOf (".");

				if (i != -1) {
					old_major_version = Convert.ToInt32 (versionStr.Substring (0,i));
					old_minor_version = Convert.ToInt32 (versionStr.Substring (i+1));
				} else {
					old_major_version = Convert.ToInt32 (versionStr);
					old_minor_version = 0;
				}

				if (old_major_version != MAJOR_VERSION || old_minor_version != minor_version) {
					log.Debug ("Version mismatch in {0}", index_name);
					log.Debug ("Index has version {0}.{1}, expected {2}.{3}",
						   old_major_version, old_minor_version,
						   MAJOR_VERSION, minor_version);
					indexExists = false;
				}
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
						if (info.Name.IndexOf ("write.lock") != -1) {
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

				if (Directory.Exists (top_dir)) {
					log.Debug ("Purging {0}", top_dir);
					Directory.Delete (top_dir, true);
				}

				// Create all directories.
				Directory.CreateDirectory (top_dir);
				Directory.CreateDirectory (lockDir);
				Directory.CreateDirectory (indexDir);

				StreamWriter sw;

				// Generate a fingerprint and write it out
				fingerprint = Guid.NewGuid ().ToString ();
				sw = new StreamWriter (fingerprintFile, false);
				sw.WriteLine (fingerprint);
				sw.Close ();

				// Write out our version information
				sw = new StreamWriter (versionFile, false);
				sw.WriteLine ("{0}.{1}", MAJOR_VERSION, minor_version);
				sw.Close ();
			}

			Lucene.Net.Store.FSDirectory store;
			store = Lucene.Net.Store.FSDirectory.GetDirectory (indexDir, lockDir, false);
			ourStore = store;
			ourStorePath = indexDir;

			//Store = store;

			if (!indexExists) {
				// This creates the index if it doesn't exist
				IndexWriter writer = new IndexWriter (Store, null, true);
				writer.Close ();
			}

			if (Environment.GetEnvironmentVariable ("BEAGLE_OPTIMIZE_ON_STARTUP") != null) {
				cycles_since_last_optimization = 1000; // this can't be zero, or nothing will happen
				Optimize ();
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

		/////////////////////////////////////////////////////
		
		//
		// Public Indexing API
		//

		static object [] empty_collection = new object [0];

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

		public void Rename (Uri old_uri, Uri new_uri)
		{
			Logger.Log.Error ("**** LuceneDriver does not support Rename!");
			Logger.Log.Error ("**** old_uri={0}", old_uri);
			Logger.Log.Error ("**** new_uri={0}", new_uri);
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
			ArrayList filtered_uris;
			
			lock (pending_by_uri) {
				
				if (pending_by_uri.Count == 0)
					return;

				pending_uris = new ArrayList ();
				pending_indexables = new ArrayList ();
				added_uris = new ArrayList ();
				removed_uris = new ArrayList ();
				filtered_uris = new ArrayList ();

				// Move our indexables and remove requests out of the
				// hash and into local data structures.
				foreach (DictionaryEntry entry in pending_by_uri) {
					Uri uri = (Uri) entry.Key;
					Indexable indexable = (Indexable) entry.Value;

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

				term = new Term ("ParentUri", uri.ToString ());
				removal_count += reader.Delete (term);
			}
			last_item_count = reader.NumDocs ();
			reader.Close ();
			watch.Stop ();
			Log.Debug ("Lucene Delete: {0} {1} {2}", watch, pending_uris.Count,
				   pending_uris.Count / watch.ElapsedTime);


			// Step #2: Write out the pending adds
			watch.Restart ();
			IndexWriter writer = null;
			foreach (Indexable indexable in pending_indexables) {
				
				Log.Debug ("+ {0}", indexable.DisplayUri);

				Filter filter = null;

				try {
					FilterFactory.FilterIndexable (indexable, out filter);
				} catch (Exception e) {
					Log.Error ("Unable to filter {0} (mimetype={1})", indexable.DisplayUri, indexable.MimeType);
					Log.Error (e);
				}
				
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
					++last_item_count;
					++add_count;
				}

				if (filter != null) {
					filtered_uris.Add (FilteredStatus.New (indexable, filter));
				}

				if (filter != null && filter.ChildIndexables.Count > 0) {
					// Iterate across any indexables created by the
					// filter and set up the parent-child relationship.
					foreach (Indexable child in filter.ChildIndexables)
						child.SetChildOf (indexable);

					if (ChildIndexableEvent != null)
						ChildIndexableEvent ((Indexable[]) filter.ChildIndexables.ToArray (typeof (Indexable)));
				}
			}
			if (writer != null) 
				writer.Close ();
			watch.Stop ();
			Log.Debug ("Lucene Add: {0} {1} {2}", watch, pending_indexables.Count,
				   pending_indexables.Count / watch.ElapsedTime);
			

			// Step #3: Fire off an event telling what we just did.
			if (ChangedEvent != null) {
				ChangedEvent (this, added_uris, removed_uris, empty_collection);
			}

			if (filtered_uris.Count > 0 && UrisFilteredEvent != null) {
				UrisFilteredEvent ((FilteredStatus[]) filtered_uris.ToArray (typeof (FilteredStatus)));
			}

			lock (pending_by_uri)
				cycles_since_last_optimization++;

			if (NeedsOptimize)
				Optimize ();
		}

		private bool NeedsOptimize {
			get { 
				// FIXME: 19 is a totally arbitrary number.
				return cycles_since_last_optimization > 19;
			}
		}

		private void Optimize ()
		{
			// If nothing has happened since our last optimization,
			// do dothing.
			// If this index is already being optimized, don't
			// optimize it again.
			lock (pending_by_uri) {
				if (optimizing || cycles_since_last_optimization == 0)
					return;
				optimizing = true;
			}

			Log.Debug ("Optimizing {0}...", StorePath);

			Stopwatch watch = new Stopwatch ();
			watch.Start ();

			IndexWriter writer = new IndexWriter (Store, null, false);
			writer.Optimize ();
			writer.Close ();

			watch.Stop ();

			Log.Debug ("Optimization time for {0}: {1}", StorePath, watch);

			lock (pending_by_uri) {
				optimizing = false;
				cycles_since_last_optimization = 0;
			}
		}

		/////////////////////////////////////////////////////

		// Returns the lowest matching score before the results are
		// truncated.
		public void DoQuery (Query               query,
				     IQueryResult        result,
				     ICollection         search_subset, // should be internal uris
				     ICollection         bonus_uris,    // should be internal uris
				     UriFilter           uri_filter,
				     UriRemapper         uri_remapper, // map to external uris
				     RelevancyMultiplier relevancy_multiplier)
		{
			double t_lucene;
			double t_assembly;

			LNS.Query lucene_query = ToLuceneQuery (query, search_subset, bonus_uris);
			if (lucene_query == null)
				return;

			Stopwatch sw = new Stopwatch ();
			sw.Start ();
			IndexReader reader = IndexReader.Open (Store);
			LNS.Searcher searcher = new LNS.IndexSearcher (reader);
			LNS.Hits hits = searcher.Search (lucene_query);
			sw.Stop ();

			t_lucene = sw.ElapsedTime;

			//////////////////////////////////////

			sw.Reset ();
			sw.Start ();

			int n_hits = hits.Length ();
			if (n_hits == 0)
				return;

			for (int i = 0; i < n_hits; ++i) {
				Document doc = hits.Doc (i);

				if (uri_filter != null) {
					Uri uri = UriFromLuceneDoc (doc);
					if (! uri_filter (uri))
						continue;
				}

				double score = (double) hits.Score (i);

				if (result.WillReject (score)) {
					log.Debug ("Terminating DoQuery at {0} of {1} (score={2})", i, n_hits, score);
					break;
				}

				Hit hit = FromLuceneDocToHit (doc, hits.Id (i), score);
				if (uri_remapper != null)
					hit.Uri = uri_remapper (hit.Uri);

				if (relevancy_multiplier != null) {
					double m = relevancy_multiplier (hit);
					hit.ScoreMultiplier = (float) m;
				}
				result.Add (hit);
			}

			sw.Stop ();
			
			t_assembly = sw.ElapsedTime;

			//////////////////////////////////////

			searcher.Close ();
			reader.Close ();

			log.Debug ("{0}: n_hits={1} lucene={2:0.00}s assembly={3:0.00}s",
				   StorePath, n_hits, t_lucene, t_assembly);
		}
		
		// FIXME: This should support Uri filtering, Uri remapping, etc.
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

		// We cache the number of documents in the index when readers are
		// available, so calls to GetItemCount will return immediately
		// if the driver has been flushed or queried.
		public int GetItemCount ()
		{
			if (last_item_count < 0) {
				IndexReader reader = IndexReader.Open (Store);
				last_item_count = reader.NumDocs ();
				reader.Close ();
			}
			return last_item_count;
		}


		///////////////////////////////////////////////////////////////////////////////////////

		//
		// Code to map to/from Lucene data types
		//

		private void AddPropertyToLuceneDocument (Document doc, Property prop)
		{
			if (prop.Value == null)
				return;

			Field f;

			if (prop.IsSearched) {
				f = new Field (prop.IsKeyword ? "PropertyKeyword" : "PropertyText",
					       prop.Value,
					       false,              // never stored
					       true,               // always index
					       ! prop.IsKeyword);  // only tokenize non-keywords
				doc.Add (f);
			}

			f = new Field (String.Format ("prop:{0}:{1}",
						      prop.IsKeyword  ? "k" : "_",
						      prop.Key),
				       prop.Value,
				       true,               // always store
				       true,               // always index
				       ! prop.IsKeyword);  // only tokenize non-keywords
			doc.Add (f);
		}

		private Property FieldToProperty (Field field)
		{
			string name = field.Name ();
			if (name.Length < 7 || ! name.StartsWith ("prop:"))
				return null;

			string key = name.Substring (7);
			string value = field.StringValue ();
			
			if (name [5] == 'k')
				return Property.NewKeyword (key, value);
			else if (name [6] == 's')
				return Property.NewUnsearched (key, value);
			else
				return Property.New (key, value);
		}

		private Document ToLuceneDocument (Indexable indexable)
		{			
			Document doc = new Document ();
			Field f;
			String str;
			TextReader reader;

			// First we add the Indexable's 'canonical' properties
			// to the Document.
			
			f = Field.Keyword ("Uri", UriFu.UriToSerializableString (indexable.Uri));
			doc.Add (f);

			f = Field.Keyword ("Type", indexable.Type);
			doc.Add (f);

			if (indexable.ParentUri != null) {
				f = Field.Keyword ("ParentUri", UriFu.UriToSerializableString (indexable.ParentUri));
				doc.Add (f);
			}
			
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
			
			if (! indexable.NoContent) {
				
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
			}

			foreach (Property prop in indexable.Properties)
				AddPropertyToLuceneDocument (doc, prop);
			
			return doc;
		}

		static public LNS.Query ToUriQuery (ICollection list_of_uris, UriRemapper remapper)
		{
			if (list_of_uris == null || list_of_uris.Count == 0)
				return null;

			LNS.BooleanQuery query = new LNS.BooleanQuery ();
			int max_clauses = LNS.BooleanQuery.GetMaxClauseCount ();
			int clause_count = 0;

			foreach (Uri original_uri in list_of_uris) {
				Uri uri = original_uri;
				if (remapper != null)
					uri = remapper (uri);
				//Logger.Log.Debug ("ToUriQuery: {0} => {1}", original_uri, uri);
				Term term = new Term ("Uri", uri.ToString ()); // FIXME: Do we need some UriFu here?
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

			return query;
		}

		static public LNS.Query ToUriQuery (ICollection list_of_uris)
		{
			return ToUriQuery (list_of_uris, null);
		}

		static private LNS.Query NewTokenizedQuery (string field, string text)
		{
			ArrayList tokens = new ArrayList ();

			// Use the analyzer to extract the query's tokens.
			// This code is taken from Lucene's query parser.
			// We use the standard Analyzer.
			TokenStream source = LuceneDriver.Analyzer.TokenStream (field, new StringReader (text));
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

			return q;
		}

		// search_subset limits the score of our search to that set of Uris
		// bonus_uris are always matched by the query
		private LNS.Query ToLuceneQuery (Query query,
						 ICollection search_subset,
						 ICollection bonus_uris)
		{
			LNS.BooleanQuery body_query = null;
			LNS.Query        search_subset_query = null;
			LNS.Query        bonus_uris_query = null;
			LNS.BooleanQuery mime_type_query = null;
			LNS.BooleanQuery hit_type_query = null;

			body_query = new LNS.BooleanQuery ();

			bool used_any_part = false;
			
			foreach (QueryPart part in query.Parts) {
				
				LNS.BooleanQuery part_query = new LNS.BooleanQuery ();
				LNS.Query part_query_override = null;
				LNS.Query subquery = null;

				bool used_this_part = false;
				
				if (part.TargetIsAll || part.TargetIsText) {
					
					subquery = NewTokenizedQuery ("Text", part.Text); 
					if (subquery != null) {
						part_query.Add (subquery, false, false);
						used_this_part = true;
					}

					subquery = NewTokenizedQuery ("HotText", part.Text);
					if (subquery != null) {
						subquery.SetBoost (1.75f);
						part_query.Add (subquery, false, false);
						used_this_part = true;
					}
				}

				if (part.TargetIsAll || part.TargetIsProperties) {
					subquery = NewTokenizedQuery ("PropertyText", part.Text);
					if (subquery != null) {
						subquery.SetBoost (1.75f);
						part_query.Add (subquery, false, false);
						used_this_part = true;
					}
				}

				if (part.TargetIsSpecificProperty) {
					
					string prop_name;
					prop_name = String.Format ("prop:{0}:{1}",
								   part.IsKeyword ? 'k' : '_',
								   part.Target);
					
					if (part.IsKeyword) {
						Term term = new Term (prop_name, part.Text);
						subquery = new LNS.TermQuery (term);
					} else {
						subquery = NewTokenizedQuery (prop_name, part.Text);
					}
					
					// Instead of the boolean query, just use the subquery.
					if (subquery != null) {
						part_query_override = subquery;
						used_this_part = true;
					}
				}
				
				if (used_this_part) {
					if (part_query_override == null)
						part_query_override = part_query;
					body_query.Add (part_query_override, part.IsRequired, part.IsProhibited);
					used_any_part = true;
				}
			}

			if (! used_any_part)
				return null;
		
			search_subset_query = ToUriQuery (search_subset, null);

			bonus_uris_query = ToUriQuery (bonus_uris, null);
				
			if (query.MimeTypes.Count > 0) {
				mime_type_query = new LNS.BooleanQuery ();
				foreach (string mime_type in query.MimeTypes) {
					Term t = new Term ("MimeType", mime_type);
					LNS.Query q = new LNS.TermQuery (t);
					mime_type_query.Add (q, false, false);
				}
			}

			if (query.HasHitTypes) {
				hit_type_query = new LNS.BooleanQuery ();
				foreach (string hit_type in query.HitTypes) {
					Term t = new Term ("Type", hit_type);
					LNS.Query q = new LNS.TermQuery (t);
					hit_type_query.Add (q, false, false);
				}
			}

			//
			// Now we combine the various parts into one big query.
			//

			LNS.BooleanQuery total_query = new LNS.BooleanQuery ();

			// If we have hit types or mime types, those must be matched
			if (mime_type_query != null)
				total_query.Add (mime_type_query, true, false);
			if (hit_type_query != null)
				total_query.Add (hit_type_query, true, false);

			// We also must match the "content query":
			// (body_query OR bonus_uris_query) AND search_subset_query

			LNS.Query content_query = null;

			if (body_query != null && bonus_uris_query != null) {
				LNS.BooleanQuery q = new LNS.BooleanQuery ();
				q.Add (body_query, false, false);
				q.Add (bonus_uris_query, false, false);
				content_query = q;
			} else if (body_query != null) {
				content_query = body_query;
			} else if (bonus_uris_query != null) {
				content_query = bonus_uris_query;
			}

			if (content_query != null && search_subset_query != null) {
				LNS.BooleanQuery q = new LNS.BooleanQuery ();
				q.Add (content_query, true, false);
				q.Add (search_subset_query, true, false);
				content_query = q;
			} else if (search_subset_query != null) {
				content_query = search_subset_query;
			}

			if (content_query != null)
				total_query.Add (content_query, true, false);

			return total_query;
		}
		
		static private Uri UriFromLuceneDoc (Document doc)
		{
			string uri = doc.Get ("Uri");
			if (uri == null)
				throw new Exception ("Got document from Lucene w/o a URI!");
			return UriFu.UriStringToUri (uri);
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

		private Hit FromLuceneDocToHit (Document doc, int id, double score)
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

			str = doc.Get ("ParentUri");
			if (str != null)
				hit.ParentUri = UriFu.UriStringToUri (str);
			
			hit.MimeType = doc.Get ("MimeType");

			hit.Source = "lucene";
			hit.ScoreRaw = score;
			
			foreach (Field ff in doc.Fields ()) {
				Property prop = FieldToProperty (ff);
				if (prop != null)
					hit.AddProperty (prop);
			}
			
			return hit;
		}


		/////////////////////////////////////////////////////

		//
		// A common, shared analyzer
		//

		private class BeagleNoiseFilter : TokenFilter {
			
			static int total_count = 0;
			static int noise_count = 0;

			TokenStream token_stream;

			public BeagleNoiseFilter (TokenStream input) : base (input)
			{
				token_stream = input;
			}

			// FIXME: we should add some heuristics that are stricter
			// but explicitly try to avoid filtering out dates,
			// phone numbers, etc.
			private static bool IsNoise (string text)
			{
				// Anything really long is almost certainly noise.
				if (text.Length > 30) 
					return true;

				// Look at how often we switch between numbers and letters.
				// Scoring:
				// <letter> <digit>   1
				// <digit> <letter>   1
				// <x> <punct>+ <x>   1
				// <x> <punct>+ <y>   2
				const int transitions_cutoff = 4;
				int last_type = -1, last_non_punct_type = -1, first_type = -1;
				bool has_letter = false, has_digit = false, has_punctuation = false;
				int transitions = 0;
				for (int i = 0; i < text.Length && transitions < transitions_cutoff; ++i) {
					char c = text [i];
					int type = -1;
					if (Char.IsLetter (c)) {
						type = 1;
						has_letter = true;
					} else if (Char.IsDigit (c)) {
						type = 2;
						has_digit = true;
					} else if (Char.IsPunctuation (c)) {
						type = 3;
						has_punctuation = true;
					}
					
					if (type != -1) {
						
						if (type != last_type) {
							if (last_type == 3) {
								if (type != last_non_punct_type)
									++transitions;
							} else {
								++transitions;
							}
						}

						if (first_type == -1)
							first_type = type;

						last_type = type;
						if (type != 3)
							last_non_punct_type = type;
					}
				}

				// If we make too many transitions, it must be noise.
				if (transitions >= transitions_cutoff) 
					return true;

				// If we consist of nothing but digits and punctuation, treat it
				// as noise if it is too long.
				if (transitions == 1 && first_type != 1 && text.Length > 10)
					return true;

				// We are very suspicious of long things that make lots of
				// transitions
				if (transitions > 3 && text.Length > 10) 
					return true;

				// Beware of anything long that contains a little of everything.
				if (has_letter && has_digit && has_punctuation && text.Length > 10)
					return true;

				//Logger.Log.Debug ("BeagleNoiseFilter accepted '{0}'", text);
				return false;
				
			}

			public override Lucene.Net.Analysis.Token Next ()
			{
				Lucene.Net.Analysis.Token token;
				while ( (token = token_stream.Next ()) != null) {
#if false
					if (total_count > 0 && total_count % 5000 == 0)
						Logger.Log.Debug ("BeagleNoiseFilter filtered {0} of {1} ({2:0.0}%)",
								  noise_count, total_count, 100.0 * noise_count / total_count);
#endif
					++total_count;
					if (IsNoise (token.TermText ())) {
						++noise_count;
						continue;
					}
					return token;
				}
				return null;
			}
		}

		// This is just a standard analyzer combined with the Porter stemmer.
		// FIXME: This assumes everything being indexed is in English!
		private class BeagleAnalyzer : StandardAnalyzer {
			public override TokenStream TokenStream (String fieldName, TextReader reader)
			{
				TokenStream outstream = base.TokenStream (fieldName, reader);
				if (fieldName == "Text" || fieldName == "HotText")
					outstream = new BeagleNoiseFilter (outstream);
				outstream = new PorterStemFilter (outstream);
				return outstream;
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
		// Access to the Stemmer
		//

		static PorterStemmer stemmer = new PorterStemmer ();

		static public string Stem (string str)
		{
			return stemmer.Stem (str);
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

		/////////////////////////////////////////////////////

		// Expose some information for debugging and analytical purposes.

		public void WriteIndexTermFrequencies (TextWriter writer)
		{
			IndexReader reader = IndexReader.Open (Store);
			TermEnum term_enum = reader.Terms ();

			Term term;
			while (term_enum.Next ()) {
				term = term_enum.Term ();
				int freq = term_enum.DocFreq ();
				writer.WriteLine ("{0} {1} {2}", term.Field (), freq, term.Text ());

			}
			reader.Close ();
		}
	}
}
