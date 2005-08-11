//
// LuceneIndexingDriver.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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

	public class LuceneIndexingDriver : LuceneCommon, IIndexer {

		Hashtable pending_by_uri = UriFu.NewHashtable ();

		public LuceneIndexingDriver (string index_name, int minor_version) : base (index_name, minor_version)
		{
			if (Exists ())
				Open ();
			else
				Create ();
		}

		public LuceneIndexingDriver (string index_name) : this (index_name, 0)
		{ }

		////////////////////////////////////////////////////////////////

		//
		// Implementation of the IIndexer interface
		//

		public void Add (Indexable indexable)
		{
			lock (pending_by_uri) {
				Indexable existing_indexable;
				existing_indexable = pending_by_uri [indexable.Uri] as Indexable;

				// If we already have an Indexable queued up and this is a property-change
				// only Indexable, just change the original Indexable's properties.
				if (existing_indexable != null && indexable.PropertyChangesOnly) {
					existing_indexable.MergeProperties (indexable);
					return;
				}

				pending_by_uri [indexable.Uri] = indexable;
			}
		}
		
		public void Remove (Uri uri)
		{
			lock (pending_by_uri) {
				pending_by_uri [uri] = null;
			}
		}

		public void Rename (Uri old_uri, Uri new_uri)
		{
			// FIXME!
		}

		public IndexerReceipt [] FlushAndBlock ()
		{
			ArrayList receipt_queue;

			lock (pending_by_uri) {

				receipt_queue = new ArrayList ();
				
				// Step #1: Delete all items with the same URIs
				// as our pending items from the index.

				IndexReader primary_reader, secondary_reader;
				primary_reader = IndexReader.Open (PrimaryStore);
				secondary_reader = IndexReader.Open (SecondaryStore);
				
				LNS.BooleanQuery prop_change_query = null;

				int delete_count = 0;

				foreach (DictionaryEntry entry in pending_by_uri) {
					Uri uri = entry.Key as Uri;
					Indexable indexable = entry.Value as Indexable;

					// If this indexable only contains property changes,
					// all we do at this point is assemble the query that we will
					// use to retrieve the current property values.  We'll ultimately
					// need to delete the existing secondary documents, but not
					// until we've loaded them...
					if (indexable != null && indexable.PropertyChangesOnly) {
						if (prop_change_query == null)
							prop_change_query = new LNS.BooleanQuery ();
						prop_change_query.Add (UriQuery ("Uri", uri), false, false);
						continue;
					}

					Logger.Log.Debug ("-{0}", uri);
					
					Term term;
					term = new Term ("Uri", UriFu.UriToSerializableString (uri));
					delete_count += primary_reader.Delete (term);
					if (secondary_reader != null)
						secondary_reader.Delete (term);

					// When we delete an indexable, also delete any children.
					// FIXME: Shouldn't we also delete any children of children, etc.?
					term = new Term ("ParentUri", UriFu.UriToSerializableString (uri));
					delete_count += primary_reader.Delete (term);
					if (secondary_reader != null)
						secondary_reader.Delete (term);

					// If this is a strict removal (and not a deletion that
					// we are doing in anticipation of adding something back),
					// queue up a removed event.
					if (indexable == null) {
						IndexerRemovedReceipt r;
						r = new IndexerRemovedReceipt (uri);
						receipt_queue.Add (r);
					}
				}

				if (HaveItemCount)
					AdjustItemCount (-delete_count);
				else
					SetItemCount (primary_reader);

				// If we have are doing any property changes,
				// we read in the current secondary documents
				// and store them in a hash table for use
				// later.  Then we delete the current
				// secondary documents.
				Hashtable current_docs = null;
				if (prop_change_query != null) {
					current_docs = UriFu.NewHashtable ();

					LNS.IndexSearcher secondary_searcher;
					secondary_searcher = new LNS.IndexSearcher (secondary_reader);

					LNS.Hits hits;
					hits = secondary_searcher.Search (prop_change_query);

					ArrayList delete_terms;
					delete_terms = new ArrayList ();

					int N;
					N = hits.Length ();
					for (int i = 0; i < N; ++i) {
						Document doc;
						doc = hits.Doc (i);

						Uri doc_uri;
						doc_uri = GetUriFromDocument (doc);

						current_docs [doc_uri] = doc;
						
						Term term;
						term = new Term ("Uri", UriFu.UriToSerializableString (doc_uri));
						delete_terms.Add (term);
					}

					secondary_searcher.Close ();

					foreach (Term term in delete_terms)
						secondary_reader.Delete (term);
				}

				// FIXME: Would we gain more "transactionality" if we didn't close
				// the readers until later?  Would that even be possible, or will
				// it create locking problems?
				primary_reader.Close ();
				secondary_reader.Close ();


				// Step #2: Write out the pending adds.

				if (text_cache != null)
					text_cache.BeginTransaction ();
				
				IndexWriter primary_writer, secondary_writer;
				primary_writer = new IndexWriter (PrimaryStore, IndexingAnalyzer, false);
				secondary_writer = null;

				foreach (Indexable indexable in pending_by_uri.Values) {

					if (indexable == null)
						continue;

					IndexerAddedReceipt r;
					r = new IndexerAddedReceipt (indexable.Uri);
					r.Properties = indexable.Properties;
					
					// Handle property changes
					if (indexable.PropertyChangesOnly) {
						Logger.Log.Debug ("+{0} (props only)", indexable.DisplayUri);

						Document current_doc;
						current_doc = current_docs [indexable.Uri] as Document;

						Document new_doc;
						new_doc = RewriteDocument (current_doc, indexable);

						// Write out the new document...
						if (secondary_writer == null)
							secondary_writer = new IndexWriter (SecondaryStore, IndexingAnalyzer, false);
						secondary_writer.AddDocument (new_doc);

						r.PropertyChangesOnly = true;
						receipt_queue.Add (r);

						continue; // ...and proceed to the next Indexable
					}

					Logger.Log.Debug ("+{0}", indexable.DisplayUri);

					Filter filter = null;

					try {
						FilterFactory.FilterIndexable (indexable, text_cache, out filter);
					} catch (Exception e) {
						Logger.Log.Error ("Unable to filter {0} (mimetype={1})", indexable.DisplayUri, indexable.MimeType);
						Logger.Log.Error (e);
						indexable.NoContent = true;
					}
					
					Document primary_doc = null, secondary_doc = null;

					try {
						BuildDocuments (indexable, out primary_doc, out secondary_doc);
						primary_writer.AddDocument (primary_doc);
					} catch (Exception ex) {

						// If an exception was thrown, something bad probably happened
						// while we were filtering the content.  Set NoContent to true
						// and try again.

						Logger.Log.Debug ("First attempt to index {0} failed", indexable.DisplayUri);
						Logger.Log.Debug (ex);

						indexable.NoContent = true;

						try {
							BuildDocuments (indexable, out primary_doc, out secondary_doc);
							primary_writer.AddDocument (primary_doc);
						} catch (Exception ex2) {
							Logger.Log.Debug ("Second attempt to index {0} failed, giving up...", indexable.DisplayUri);
							Logger.Log.Debug (ex2);
						}
					}

					if (filter != null) {
						r.FilterName = filter.GetType ().ToString ();
						r.FilterVersion = filter.Version;
					}
					
					receipt_queue.Add (r);
					
					if (secondary_doc != null) {
						if (secondary_writer == null)
							secondary_writer = new IndexWriter (SecondaryStore, IndexingAnalyzer, false);

						secondary_writer.AddDocument (secondary_doc);
					}

					AdjustItemCount (1);
				}

				if (text_cache != null)
					text_cache.CommitTransaction ();

#if false
				// FIXME: always optimize
				Logger.Log.Debug ("Optimizing");
				primary_writer.Optimize ();
				if (secondary_writer != null)
					secondary_writer.Optimize ();
#endif

				// Step #3. Close our writers and return the events to
				// indicate what has happened.
				
				primary_writer.Close ();
				if (secondary_writer != null)
					secondary_writer.Close ();

				pending_by_uri.Clear ();

				IndexerReceipt [] receipt_array;
				receipt_array = new IndexerReceipt [receipt_queue.Count];
				for (int i = 0; i < receipt_queue.Count; ++i)
					receipt_array [i] = (IndexerReceipt) receipt_queue [i];
				
				return receipt_array;
			}
		}

		public void Flush ()
		{
			// FIXME: Right now we don't support a non-blocking flush,
			// but it would be easy enough to do it in a thread.

			IndexerReceipt [] receipts;

			receipts = FlushAndBlock ();

			if (FlushEvent != null) {
				if (receipts != null)
					FlushEvent (this, receipts); // this returns the receipts to anyone who cares
				FlushEvent (this, null);             // and this indicates that we are all done
			}
		}


		public event IIndexerFlushHandler FlushEvent;

		////////////////////////////////////////////////////////////////

		public void Optimize ()
		{
			IndexWriter writer;

			writer = new IndexWriter (PrimaryStore, null, false);
			writer.Optimize ();
			writer.Close ();

			if (SecondaryStore != null) {
				writer = new IndexWriter (SecondaryStore, null, false);
				writer.Optimize ();
				writer.Close ();
			}
		}

		public void Merge (LuceneCommon index_to_merge)
		{
                        // FIXME: Error recovery

			// Merge the primary index
			IndexWriter primary_writer;
			Lucene.Net.Store.Directory[] primary_store = {index_to_merge.PrimaryStore};
			primary_writer = new IndexWriter (PrimaryStore, null, false);

			primary_writer.AddIndexes (primary_store);
			primary_writer.Close ();

			// Merge the secondary index
			IndexWriter secondary_writer;
			Lucene.Net.Store.Directory[] secondary_store = {index_to_merge.SecondaryStore};
			secondary_writer = new IndexWriter (SecondaryStore, null, false);

			secondary_writer.AddIndexes (secondary_store);
			secondary_writer.Close ();
		}
	}
}
