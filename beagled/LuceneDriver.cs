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
// about Lucene.
//

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using BU = Beagle.Util;

namespace Beagle.Daemon {
	
	public class LuceneDriver {

		// 1: Original
		// 2: Changed format of timestamp strings
		// 3: Schema changed to be more Dashboard-Match-like
		// 4: Schema changed for files to include _Directory property
		// 5: Changed analyzer to support stemming.  Bumped version # to
		//    force everyone to re-index.
		// 6: lots of schema changes as part of the general refactoring
		private const int VERSION = 6;

		public LuceneDriver (Lucene.Net.Store.Directory store)
		{
			Store = store;
		}

		public LuceneDriver (string dir)
		{
			string versionFile = Path.Combine (dir, "version");
			string lockDir = Path.Combine (dir, "Locks");
			string indexDir = Path.Combine (dir, "Index");
			string indexTestFile = Path.Combine (indexDir, "segments");

			bool versionExists = File.Exists (versionFile);
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
			if (! indexExists) {
				// Purge the directory if it exists.
				if (Directory.Exists (dir))
					Directory.Delete (dir, true);

				// Create all directories.
				Directory.CreateDirectory (dir);
				Directory.CreateDirectory (lockDir);
				Directory.CreateDirectory (indexDir);

				// Write out our version information
				StreamWriter sw = new StreamWriter (versionFile, false);
				sw.WriteLine ("{0}", VERSION);
				sw.Close ();


			}

			Lucene.Net.Store.FSDirectory store;
			store = Lucene.Net.Store.FSDirectory.GetDirectory (indexDir, false);
			store.TempDirectoryName = lockDir;

			Store = store;

			if (! indexExists) {
				// Initialize a new index.
				IndexWriter writer = new IndexWriter (Store, null, true);
				writer.Close ();
			}
		}

		/////////////////////////////////////////////////////
		
		//
		// The Lucene Store
		//

		private Lucene.Net.Store.Directory ourStore = null;

		private Lucene.Net.Store.Directory Store {
			get { return ourStore; }

			set {
				if (ourStore != null)
					throw new Exception ("Attempt to attach a second store to a LuceneDriver");
				ourStore = (Lucene.Net.Store.Directory) value;
				StartQueue ();
			}
		}

		/////////////////////////////////////////////////////

		//
		// Public API
		//

		public void ScheduleAdd (Indexable indexable)
		{
			QueueItem item;
			item = new QueueItem ();
			item.IndexableToAdd = indexable;
			ScheduleQueueItem (item);
		}

		public void ScheduleDelete (Uri uri)
		{
			QueueItem item;
			item = new QueueItem ();
			item.UriToDelete = uri;
			ScheduleQueueItem (item);
		}

		private class HitInfo {
			public string Uri;
			public int Id;
			public Document Doc;
			public Versioned Versioned;
			public float Score;
		}

		public ICollection DoQuery (QueryBody body, ICollection listOfUris)
		{
			LNS.Searcher searcher = new LNS.IndexSearcher (Store);
			LNS.Query query = ToLuceneQuery (body, listOfUris);

			LNS.Hits luceneHits = searcher.Search (query);
			int nHits = luceneHits.Length ();

			if (nHits == 0) {
				searcher.Close ();
				return new Hit [0];
			}

			Hashtable byUri = new Hashtable ();

			// Pass #1: If we get multiple hits with the same Uri,
			// make sure that we throw out all but the most recent.
			for (int i = 0; i < nHits; ++i) {
				int id = luceneHits.Id (i);
				Document doc = luceneHits.Doc (i);
				string uri = UriFromLuceneDoc (doc);

				Versioned versioned = new Versioned ();
				FromLuceneDocToVersioned (doc, versioned);
				
				HitInfo other = (HitInfo) byUri [uri];
				if (other == null || other.Versioned.IsObsoletedBy (versioned)) {
					HitInfo info = new HitInfo ();
					info.Uri = uri;
					info.Id = luceneHits.Id (i);
					info.Doc = doc;
					info.Versioned = versioned;
					info.Score = luceneHits.Score (i);
					byUri [uri] = info;
				}
			}

			// Pass #2: Check that any Uris we get are the
			// most recent in the index.
			ArrayList filteredHits = new ArrayList ();
			foreach (HitInfo info in byUri.Values) {
				if (DocIsUpToDate (searcher, info.Uri, info.Id, info.Versioned)) {
					Hit hit = FromLuceneDocToHit (info.Doc, info.Id, info.Score);
					filteredHits.Add (hit);
				}
			}

			searcher.Close ();

			return filteredHits;
		}


		/////////////////////////////////////////////////////

		//
		// Indexing Events
		//

		public delegate void AddedHandler (LuceneDriver source, Uri uri);
		public event AddedHandler AddedEvent;

		public delegate void DeletedHandler (LuceneDriver source, Uri uri);
		public event DeletedHandler DeletedEvent;


		/////////////////////////////////////////////////////
		
		//
		// Queue implementation
		//

		private class QueueItem {
			public Indexable IndexableToAdd;
			public Uri UriToDelete;

			public bool IsAdd {
				get { return IndexableToAdd != null; }
			}

			public bool IsDelete {
				get { return UriToDelete != null; }
			}
		}
		
		Thread queueThread = null;
		object queueLock = new object ();
		Queue queue = new Queue ();

		private void ScheduleQueueItem (QueueItem item)
		{
			lock (queueLock) {
				queue.Enqueue (item);
				Monitor.Pulse (queueLock);
			}
		}

		// Process a single queue item.
		// In the interest of efficiency, we try to batch together
		// as many operations as possible.  
		private void ProcessQueueItem (QueueItem item)
		{
			if (item.IsDelete) {
				// We can't do interleaved additions and deletions,
				// so we have to flush if a delete item comes in
				// while adds are pending.
				if (HavePendingAdds) {
					FlushPendingDeletes ();
					FlushPendingAdds ();
				}
				AddPendingDelete (item.UriToDelete);
			} else if (item.IsAdd) {
				AddPendingDelete (item.IndexableToAdd.Uri);
				AddPendingAdd (item.IndexableToAdd);
			} else {
				Console.WriteLine ("Failed to process unknown/malformed QueueItem");
			}
		}

		// Called immediately after each queue item is processed.
		private void PostProcessQueue ()
		{

		}

		// Called when the queue is empty (and has been empty for a brief
		// period of time.)
		private void ProcessEmptyQueue ()
		{
			FlushPendingDeletes ();
			FlushPendingAdds ();
		}

		// Test whether or not we should continue processing queue items.
		private bool StopProcessingQueue ()
		{
			// For now, we never stop.
			return false;
		}

		private void WorkQueue ()
		{
			while (true) {

				// Get the next queue item.  If necessary,
				// wait until an item becomes available.
				QueueItem item = null;
				lock (queueLock) {
					if (queue.Count == 0)
						Monitor.Wait (queueLock);
					if (queue.Count > 0)
						item = (QueueItem) queue.Dequeue ();
				}

				// If we got a queue item, process it.
				if (item != null) {
					ProcessQueueItem (item);
					PostProcessQueue ();

					// If the queue is empty and stays empty for 137ms,
					// call ProcessEmptyQueue.  (I chose 137ms at random.
					// Why pretend that any value we stick in here isn't
					// completely arbitrary?)
					bool queueIsEmpty = false;
					lock (queueLock) {
						if (queue.Count == 0) {
							Monitor.Wait (queueLock, 137);
							if (queue.Count == 0)
								queueIsEmpty = true;
						}
					}
					if (queueIsEmpty)
						ProcessEmptyQueue ();
				}

				// Should we stop processing the queue and exit the thread?
				if (StopProcessingQueue ())
					break;
			}
		}

		// Launch a thread to handle the queue.
		private void StartQueue ()
		{
			lock (this) {
				if (queueThread != null)
					return;
				queueThread = new Thread (new ThreadStart (WorkQueue));
				queueThread.Start ();
			}
		}


		/////////////////////////////////////////////////////

		//
		// Queue Processing Helper Functions
		//

		// Adds

		ArrayList pendingAdds = new ArrayList ();

		private bool HavePendingAdds {
			get { return pendingAdds.Count > 0; }
		}

		private void AddPendingAdd (Indexable indexable) // an unfortunate method name...
		{
			pendingAdds.Add (indexable);
		}

		private void FlushPendingAdds ()
		{
			if (pendingAdds.Count == 0)
				return;

			IndexWriter writer = new IndexWriter (Store, Analyzer, false);
			foreach (Indexable indexable in pendingAdds) {
				Document doc = null;
				try {
					doc = ToLuceneDocument (indexable);
				} catch (Exception e) {
					Console.WriteLine ("unable to convert {0} (type={1}) to a lucene document",
							   indexable.Uri, indexable.Type);
					Console.WriteLine (e.Message);
					Console.WriteLine (e.StackTrace);
				}
				if (doc != null) {
					Console.WriteLine ("Adding {0}", indexable.Uri);
					writer.AddDocument (doc);
				}
			}

			// FIXME: We shouldn't optimize this often
			Console.WriteLine ("Optimizing");
			writer.Optimize ();
			Console.WriteLine ("Done Optimizing");
			writer.Close ();

			foreach (Indexable indexable in pendingAdds) {
				if (AddedEvent != null)
					AddedEvent (this, indexable.Uri);
			}

			pendingAdds.Clear ();
		}

		private Document ToLuceneDocument (Indexable indexable)
		{			
			FilteredIndexable filtered = indexable as FilteredIndexable;
			if (filtered != null) {
				filtered.Build ();
			}

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
		

		// Deletions

		private ArrayList pendingDeletes = new ArrayList ();

		private void AddPendingDelete (Uri uri)
		{
			pendingDeletes.Add (uri);
		}

		private void FlushPendingDeletes ()
		{
			if (pendingDeletes.Count == 0)
				return;

			ArrayList idsToDelete = new ArrayList ();

			// Get the ids of all documents with the given Uris.
			LNS.Searcher searcher = new LNS.IndexSearcher (Store);
			foreach (Uri uri in pendingDeletes) {
				Console.WriteLine ("Deleting {0}", uri);
				Term term = new Term ("Uri", uri.ToString ());
				LNS.Query uriQuery = new LNS.TermQuery (term);
				LNS.Hits uriHits = searcher.Search (uriQuery);
				for (int i = 0; i < uriHits.Length (); ++i) {
					int id = uriHits.Id (i);
					idsToDelete.Add (id);
				}
			}
			searcher.Close ();

			// Walk across the list of ids and delete all of those
			// documents.
			IndexReader reader;
			reader = IndexReader.Open (Store);
			foreach (int id in idsToDelete)
				reader.Delete (id);
			reader.Close ();

			// Fire off events to indicate what we just deleted.
			foreach (Uri uri in pendingDeletes) {
				if (DeletedEvent != null)
					DeletedEvent (this, uri);
			}

			// Clear our list of pending deletions
			pendingDeletes.Clear ();
		}


		/////////////////////////////////////////////////////

		//
		// Query Implementation
		//

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
		
		static private string UriFromLuceneDoc (Document doc)
		{
			string uri = doc.Get ("Uri");
			if (uri == null)
				throw new Exception ("Got document from Lucene w/o a URI!");
			return uri;
		}

		static private void FromLuceneDocToVersioned (Document doc, Versioned versioned)
		{
			string str;

			str = doc.Get ("Timestamp");
			if (str != null)
				versioned.Timestamp = BU.StringFu.StringToDateTime (str);
			
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
			
			hit.Uri = new Uri (UriFromLuceneDoc (doc), true);

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

		// Sanity-check a Document against the Index:  Make sure that
		// there isn't some other more recent document with the same Uri.
		private bool DocIsUpToDate (LNS.Searcher searcher,
					    string       docUri,
					    int          docId,
					    Versioned    docVersioned)
		{
			// First, find documents with the same Uri.
			Term uriTerm = new Term ("Uri", docUri);
			LNS.Query uriQuery = new LNS.TermQuery (uriTerm);
			LNS.Hits uriHits = searcher.Search (uriQuery);

			Versioned other = null;
			for (int i = 0; i < uriHits.Length (); ++i) {
				// Skip the hit under consideration
				if (uriHits.Id (i) == docId)
					continue;
				
				if (other == null)
					other = new Versioned ();

				FromLuceneDocToVersioned (uriHits.Doc (i), other);
				// Oops... this isn't supposed to happen.
				if (docVersioned.IsObsoletedBy (other)) {
					Console.WriteLine ("Matched obsolete document with Uri '{0}'", docUri);
					return false;
				}
			}

			return true;
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
