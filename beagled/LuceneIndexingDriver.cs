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

		object flush_lock = new object ();

		public LuceneIndexingDriver (string index_name, int minor_version, bool build_usercache) 
			: base (index_name, minor_version)
		{
			if (Exists ())
				Open ();
			else
				Create ();

			if (build_usercache)
				text_cache = TextCache.UserCache;
		}

		public LuceneIndexingDriver (string index_name, int minor_version)
			: this (index_name, minor_version, true) { }
		
		public LuceneIndexingDriver (string index_name, bool build_usercache)
			: this (index_name, 0, build_usercache) { }

		public LuceneIndexingDriver (string index_name) 
			: this (index_name, 0, true) { }
	
		////////////////////////////////////////////////////////////////

		// We use this in the index helper so that we can report what's
		// going on if the helper spins the CPU.  The method will be
		// called with null parameters after filtering has finished.

		public delegate void FileFilterDelegate (Uri display_uri, Filter filter);
		public FileFilterDelegate FileFilterNotifier = null;

		////////////////////////////////////////////////////////////////

		//
		// Implementation of the IIndexer interface
		//

		public IndexerReceipt [] Flush (IndexerRequest request)
		{
			// This is just to keep a big block of code from being
			// indented an extra eight spaces.
			lock (flush_lock)
				return Flush_Unlocked (request);
		}

		private IndexerReceipt [] Flush_Unlocked (IndexerRequest request)
		{
			ArrayList receipt_queue;
			receipt_queue = new ArrayList ();

			IndexReader primary_reader, secondary_reader;
			primary_reader = IndexReader.Open (PrimaryStore);
			secondary_reader = IndexReader.Open (SecondaryStore);

			// Step #1: Make our first pass over the list of
			// indexables that make up our request.  For each add
			// or remove in the request, delete the associated
			// items from the index.  Assemble a query that will
			// be used to find the secondary documents for any
			// property change requests.

			LNS.BooleanQuery prop_change_query = null;
			LNS.BooleanQuery prop_change_children_query = null;
			int delete_count = 0;

			ICollection request_indexables = request.Indexables;

			foreach (Indexable indexable in request_indexables) {

				switch (indexable.Type) {

				case IndexableType.Add:
				case IndexableType.Remove:

					string uri_str;
					uri_str = UriFu.UriToEscapedString (indexable.Uri);

					Logger.Log.Debug ("-{0}", indexable.DisplayUri);
					
					Term term;
					term = new Term ("Uri", uri_str);
					delete_count += primary_reader.Delete (term);
					if (secondary_reader != null)
						secondary_reader.Delete (term);

					// When we delete an indexable, also delete any children.
					// FIXME: Shouldn't we also delete any children of children, etc.?
					term = new Term ("ParentUri", uri_str);
					delete_count += primary_reader.Delete (term);
					if (secondary_reader != null)
						secondary_reader.Delete (term);

					// If this is a strict removal (and not a deletion that
					// we are doing in anticipation of adding something back),
					// queue up a removed receipt.
					if (indexable.Type == IndexableType.Remove) {
						IndexerRemovedReceipt r;
						r = new IndexerRemovedReceipt (indexable.Uri);
						receipt_queue.Add (r);
					}

					break;

				case IndexableType.PropertyChange:
					if (prop_change_query == null) {
						prop_change_query = new LNS.BooleanQuery ();
						prop_change_children_query = new LNS.BooleanQuery ();
					}

					prop_change_query.Add (UriQuery ("Uri", indexable.Uri), false, false);
					prop_change_children_query.Add (UriQuery ("ParentUri", indexable.Uri), false, false);
					break;
				}
			}

			if (HaveItemCount)
				AdjustItemCount (-delete_count);
			else
				SetItemCount (primary_reader);
			
			// Step #2: If we have are doing any property changes,
			// we read in the current secondary documents and
			// store them in a hash table for use later.  Then we
			// delete the current secondary documents.
			Hashtable prop_change_docs = null;
			Hashtable prop_change_children_docs = null;
			if (prop_change_query != null) {
				prop_change_docs = UriFu.NewHashtable ();

				LNS.IndexSearcher secondary_searcher;
				secondary_searcher = new LNS.IndexSearcher (secondary_reader);

				LNS.Hits hits;
				hits = secondary_searcher.Search (prop_change_query);

				ArrayList delete_terms;
				delete_terms = new ArrayList ();

				int N = hits.Length ();
				Document doc;
				for (int i = 0; i < N; ++i) {
					doc = hits.Doc (i);
					
					string uri_str;
					uri_str = doc.Get ("Uri");

					Uri uri;
					uri = UriFu.EscapedStringToUri (uri_str);
					prop_change_docs [uri] = doc;
						
					Term term;
					term = new Term ("Uri", uri_str);
					delete_terms.Add (term);
				}

				secondary_searcher.Close ();

				foreach (Term term in delete_terms)
					secondary_reader.Delete (term);

				// Step #2.5: Find all child indexables for this document
				// Store them to send them later as IndexerChildIndexablesReceipts
				prop_change_children_docs = UriFu.NewHashtable ();

				hits = secondary_searcher.Search (prop_change_children_query);
				N = hits.Length ();

				for (int i = 0; i < N; ++i) {
					doc = hits.Doc (i);
					
					string uri_str, parent_uri_str;
					uri_str = doc.Get ("Uri");
					parent_uri_str = doc.Get ("ParentUri");

					Uri uri, parent_uri;
					uri = UriFu.EscapedStringToUri (uri_str);
					parent_uri = UriFu.EscapedStringToUri (parent_uri_str);

					if (! prop_change_children_docs.Contains (parent_uri)) {
						ArrayList c_list = new ArrayList ();
						prop_change_children_docs [parent_uri] = c_list;
					}

					ArrayList children_list = (ArrayList) prop_change_children_docs [parent_uri];
					children_list.Add (uri);
				}

				secondary_searcher.Close ();

			}

			// We are now done with the readers, so we close them.
			primary_reader.Close ();
			secondary_reader.Close ();

			// FIXME: If we crash at exactly this point, we are in
			// trouble.  Items will have been dropped from the index
			// without the proper replacements being added.

			// Step #3: Make another pass across our list of indexables
			// and write out any new documents.

			if (text_cache != null)
				text_cache.BeginTransaction ();
				
			IndexWriter primary_writer, secondary_writer;
			primary_writer = new IndexWriter (PrimaryStore, IndexingAnalyzer, false);
			secondary_writer = null;

			foreach (Indexable indexable in request_indexables) {
				
				if (indexable.Type == IndexableType.Remove)
					continue;

				IndexerAddedReceipt r;
				r = new IndexerAddedReceipt (indexable.Uri);
				receipt_queue.Add (r);

				if (indexable.Type == IndexableType.PropertyChange) {

					Logger.Log.Debug ("+{0} (props only)", indexable.DisplayUri);
					r.PropertyChangesOnly = true;

					Document doc;
					doc = prop_change_docs [indexable.Uri] as Document;

					Document new_doc;
					new_doc = RewriteDocument (doc, indexable);

					// Write out the new document...
					if (secondary_writer == null)
						secondary_writer = new IndexWriter (SecondaryStore, IndexingAnalyzer, false);
					secondary_writer.AddDocument (new_doc);

					// Add children property change indexables...
					AddChildrenPropertyChange (
						prop_change_children_docs,
						indexable,
						receipt_queue);

					continue; // ...and proceed to the next Indexable
				}

				// If we reach this point we know we are dealing with an IndexableType.Add

				if (indexable.Type != IndexableType.Add)
					throw new Exception ("When I said it was an IndexableType.Add, I meant it!");
				
				Logger.Log.Debug ("+{0}", indexable.DisplayUri);

				Filter filter = null;

				if (FileFilterNotifier != null)
					FileFilterNotifier (indexable.DisplayUri, null); // We don't know what filter yet.

				// If we have content, try to find a filter
				// which we can use to process the indexable.
				try {
					FilterFactory.FilterIndexable (indexable, text_cache, out filter);
				} catch (Exception e) {
					Logger.Log.Error (e, "Unable to filter {0} (mimetype={1})", indexable.DisplayUri, indexable.MimeType);
					indexable.NoContent = true;
				}

				if (FileFilterNotifier != null)
					FileFilterNotifier (indexable.DisplayUri, filter); // Update with our filter
					
				Document primary_doc = null, secondary_doc = null;

				try {
					BuildDocuments (indexable, out primary_doc, out secondary_doc);
					primary_writer.AddDocument (primary_doc);
				} catch (Exception ex) {
					
					// If an exception was thrown, something bad probably happened
					// while we were filtering the content.  Set NoContent to true
					// and try again -- that way it will at least end up in the index,
					// even if we don't manage to extract the fulltext.

					Logger.Log.Debug (ex, "First attempt to index {0} failed", indexable.DisplayUri);
					
					indexable.NoContent = true;
						
					try {
						BuildDocuments (indexable, out primary_doc, out secondary_doc);
						primary_writer.AddDocument (primary_doc);
					} catch (Exception ex2) {
						Logger.Log.Debug (ex2, "Second attempt to index {0} failed, giving up...", indexable.DisplayUri);
					}
				}
				
				if (filter != null) {

					// Force the clean-up of temporary files, just in case.
					filter.Cleanup ();

					r.FilterName = filter.GetType ().ToString ();
					r.FilterVersion = filter.Version;

					// Create a receipt containing any child indexables.
					if (filter.ChildIndexables.Count > 0) {
						Log.Debug ("Generated {0} child indexable{1} from {2} (filtered with {3})", filter.ChildIndexables.Count, filter.ChildIndexables.Count > 1 ? "s" : "", indexable.DisplayUri, r.FilterName);
						IndexerChildIndexablesReceipt cr;
						cr = new IndexerChildIndexablesReceipt (indexable, filter.ChildIndexables);
						receipt_queue.Add (cr);
					}
				}

				if (FileFilterNotifier != null)
					FileFilterNotifier (null, null); // reset
				
				if (secondary_doc != null) {
					if (secondary_writer == null)
						secondary_writer = new IndexWriter (SecondaryStore, IndexingAnalyzer, false);
					
					secondary_writer.AddDocument (secondary_doc);
				}
				
				AdjustItemCount (1);

				// Clean up any temporary files associated with filtering this indexable.
				indexable.Cleanup ();
			}

			if (text_cache != null)
				text_cache.CommitTransaction ();

			if (request.OptimizeIndex) {
				Stopwatch watch = new Stopwatch ();
				Logger.Log.Debug ("Optimizing {0}", IndexName);
				watch.Start ();
				primary_writer.Optimize ();
				if (secondary_writer == null)
					secondary_writer = new IndexWriter (SecondaryStore, IndexingAnalyzer, false);
				secondary_writer.Optimize ();
				watch.Stop ();
				Logger.Log.Debug ("{0} optimized in {1}", IndexName, watch);
			}
			
			// Step #4. Close our writers and return the events to
			// indicate what has happened.
				
			primary_writer.Close ();
			if (secondary_writer != null)
				secondary_writer.Close ();
			
			IndexerReceipt [] receipt_array;
			receipt_array = new IndexerReceipt [receipt_queue.Count];
			for (int i = 0; i < receipt_queue.Count; ++i)
				receipt_array [i] = (IndexerReceipt) receipt_queue [i];
			
			return receipt_array;
		}

		// Since some parent properties maybe stored in child properties
		// as parent: property, any property change should be propagated
		// to all its children as well.
		private void AddChildrenPropertyChange (
				Hashtable children_docs,
				Indexable parent,
				ArrayList receipt_queue)
		{
			if (! children_docs.Contains (parent.Uri))
				return;

			ArrayList children_list = (ArrayList) children_docs [parent.Uri];
			IndexerChildIndexablesReceipt child_r;
			child_r = new IndexerChildIndexablesReceipt ();
			ArrayList child_indexable_list = new ArrayList ();

			foreach (Uri uri in children_list) {
				Indexable child_indexable;
				child_indexable = new Indexable (IndexableType.PropertyChange, uri);
				Log.Debug ("Creating property change child indexable for {1} (parent {0})", parent.Uri, uri);

				child_indexable.SetChildOf (parent);
				child_indexable_list.Add (child_indexable);
			}

			child_r.Children = child_indexable_list;
			receipt_queue.Add (child_r);
		}
		
		////////////////////////////////////////////////////////////////

		public void OptimizeNow ()
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
