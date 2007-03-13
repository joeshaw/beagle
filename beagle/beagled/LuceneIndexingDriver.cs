//
// LuceneIndexingDriver.cs
//
// Copyright (C) 2004-2007 Novell, Inc.
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

using System;
using System.Collections;

using Lucene.Net.Documents;
using Lucene.Net.Index;

using Beagle.Util;
using Stopwatch = Beagle.Util.Stopwatch;

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
			: this (index_name, -1, build_usercache) { }

		public LuceneIndexingDriver (string index_name) 
			: this (index_name, -1, true) { }

		////////////////////////////////////////////////////////////////
		// DisableTextCache will disable text cache (surprise!) for the documents
		// indexed in this session of beagle-index-helper.
		// To consistently handle correct snippets where,
		// some sessions were called with disable-textcache and some without
		// when DisableTextCache is set, not only is no textcache is stored,
		// but any previous textcache entry is removed.
		// This is important, because otherwise snippets might be returned
		// for old versions of documents.
		// Note that, disable_textcache merely disables text-cache for documents indexed in the current session,
		// so you would still get snippets on documents that do not use
		// text cache for storing snippets, e.g. IM logs, KMail emails
		// and documents that were indexed in previous sessions.
		// To get rid of text cache completely, delete TextCache directory
		// and always run beagled with --disable-textcache.
		// Of course, if you ask to disable text-cache and then request snippets,
		// you will look dumb. But that's your choice. We love choice.
		private bool disable_textcache = false;
		public bool DisableTextCache {
			get { return disable_textcache; }
			set { disable_textcache = value; }
		}
	
		////////////////////////////////////////////////////////////////

		// We use this in the index helper so that we can report what's
		// going on if the helper spins the CPU.  The method will be
		// called with null parameters after filtering has finished.

		public delegate void FileFilterDelegate (Uri display_uri, Filter filter);
		public FileFilterDelegate FileFilterNotifier = null;

		////////////////////////////////////////////////////////////////

		private class DeferredInfo {
			public Indexable Indexable;
			public IndexerAddedReceipt Receipt;
			public Document PersistentPropDoc;
			public int Count;

			public DeferredInfo (Indexable indexable,
					     IndexerAddedReceipt receipt,
					     Document persistent_prop_doc,
					     int count)
			{
				Indexable = indexable;
				Receipt = receipt;
				PersistentPropDoc = persistent_prop_doc;
				Count = count;
			}
		}

		private Hashtable deferred_hash = UriFu.NewHashtable ();
		private ArrayList deferred_queue = new ArrayList ();

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
			// or property change in the request, get the Lucene
			// documents so we can move forward any persistent
			// properties (for adds) or all old properties (for
			// property changes).
			//
			// Then, for each add or remove in the request,
			// delete the associated documents from the index.
			// Note that we previously cached added documents so
			// that we can move persistent properties forward.

			Hashtable prop_change_docs = UriFu.NewHashtable ();
			Hashtable prop_change_children_docs = UriFu.NewHashtable ();
			TermDocs term_docs = secondary_reader.TermDocs ();
			int delete_count = 0;

			ICollection request_indexables = request.Indexables;

			foreach (Indexable indexable in request_indexables) {

				string uri_str = UriFu.UriToEscapedString (indexable.Uri);

				if (indexable.Type == IndexableType.Add ||
				    indexable.Type == IndexableType.PropertyChange) {

					Term term = new Term ("Uri", uri_str);
					term_docs.Seek (term);

					if (term_docs.Next ())
						prop_change_docs [indexable.Uri] = secondary_reader.Document (term_docs.Doc ());

					// We'll remove it for adds in the next block.
					if (indexable.Type == IndexableType.PropertyChange)
						secondary_reader.Delete (term);

					term = new Term ("ParentUri", uri_str);
					term_docs.Seek (term);

					while (term_docs.Next ()) {
						if (prop_change_children_docs [indexable.Uri] == null)
							prop_change_children_docs [indexable.Uri] = new ArrayList ();

						Document doc = secondary_reader.Document (term_docs.Doc ());
						string parent_uri_str = doc.Get ("ParentUri");
						Uri parent_uri = UriFu.EscapedStringToUri (parent_uri_str);

						ArrayList child_list = (ArrayList) prop_change_children_docs [parent_uri_str];

						child_list.Add (indexable.Uri);
					}
				}

				if (indexable.Type == IndexableType.Add ||
				    indexable.Type == IndexableType.Remove) {

					Logger.Log.Debug ("-{0}", indexable.DisplayUri);
					
					Term term;
					term = new Term ("Uri", uri_str);
					delete_count += primary_reader.Delete (term);
					secondary_reader.Delete (term);

					// When we delete an indexable, also delete any children.
					// FIXME: Shouldn't we also delete any children of children, etc.?
					term = new Term ("ParentUri", uri_str);
					delete_count += primary_reader.Delete (term);
					secondary_reader.Delete (term);

					// If this is a strict removal (and not a deletion that
					// we are doing in anticipation of adding something back),
					// queue up a removed receipt.
					if (indexable.Type == IndexableType.Remove) {
						IndexerRemovedReceipt r;
						r = new IndexerRemovedReceipt (indexable.Uri);
						receipt_queue.Add (r);
					}
				}
			}

			term_docs.Close ();

			if (HaveItemCount)
				AdjustItemCount (-delete_count);
			else
				SetItemCount (primary_reader);
			
			// We are now done with the readers, so we close them.
			primary_reader.Close ();
			secondary_reader.Close ();

			// FIXME: If we crash at exactly this point, we are in
			// trouble.  Items will have been dropped from the index
			// without the proper replacements being added.  We can
			// hopefully fix this when we move to Lucene 2.1.

			// Step #2: Make another pass across our list of indexables
			// and write out any new documents.

			if (text_cache != null)
				text_cache.BeginTransaction ();
				
			IndexWriter primary_writer, secondary_writer;
			primary_writer = new IndexWriter (PrimaryStore, IndexingAnalyzer, false);
			secondary_writer = null;

			foreach (Indexable indexable in request_indexables) {
				// If shutdown has been started, break here
				// FIXME: Some more processing will continue, a lot of them
				// concerning receipts, but the daemon will anyway ignore receipts
				// now, what is the fastest way to stop from here ?
				if (Shutdown.ShutdownRequested) {
					Log.Debug ("Shutdown initiated. Breaking while flushing indexables.");
					break;
				}
				
				// Receipts for removes were generated in the
				// previous block and require no further
				// processing.  Just skip over them here.
				if (indexable.Type == IndexableType.Remove)
					continue;

				IndexerAddedReceipt r;

				if (indexable.Type == IndexableType.PropertyChange) {

					Logger.Log.Debug ("+{0} (props only)", indexable.DisplayUri);

					r = new IndexerAddedReceipt (indexable.Uri);
					r.PropertyChangesOnly = true;
					receipt_queue.Add (r);

					Document doc;
					doc = prop_change_docs [indexable.Uri] as Document;

					Document new_doc;
					new_doc = RewriteDocument (doc, indexable);

					// Write out the new document...
					if (secondary_writer == null)
						secondary_writer = new IndexWriter (SecondaryStore, IndexingAnalyzer, false);
					secondary_writer.AddDocument (new_doc);

					IndexerIndexablesReceipt ir;

					// Get child property change indexables...
					ir = GetChildPropertyChange (prop_change_children_docs, indexable);
					if (ir != null)
						receipt_queue.Add (ir);

					continue; // ...and proceed to the next Indexable
				}

				// If we reach this point we know we are dealing with an IndexableType.Add

				if (indexable.Type != IndexableType.Add)
					throw new Exception ("When I said it was an IndexableType.Add, I meant it!");
				
				Filter filter = null;

				if (FileFilterNotifier != null)
					FileFilterNotifier (indexable.DisplayUri, null); // We don't know what filter yet.

				// If we have content, try to find a filter
				// which we can use to process the indexable.
				try {
					FilterFactory.FilterIndexable (indexable, (disable_textcache ? null : text_cache), out filter);
				} catch (Exception e) {
					Logger.Log.Error (e, "Unable to filter {0} (mimetype={1})", indexable.DisplayUri, indexable.MimeType);
					indexable.NoContent = true;
				}

				if (FileFilterNotifier != null)
					FileFilterNotifier (indexable.DisplayUri, filter); // Update with our filter

				Document persistent_prop_doc = (Document) prop_change_docs [indexable.Uri];
				bool deferred = false;

				r = new IndexerAddedReceipt (indexable.Uri);

				if (filter != null) {
					// Force the clean-up of temporary files, just in case.
					filter.Cleanup ();

					r.FilterName = filter.GetType ().ToString ();
					r.FilterVersion = filter.Version;

					if (filter.GeneratedIndexables.Count > 0) {

						Log.Debug ("Got {0} filter-generated indexable{1} from {2} (filtered with {3}); deferring until later",
							   filter.GeneratedIndexables.Count,
							   filter.GeneratedIndexables.Count > 1 ? "s" : "",
							   indexable.DisplayUri,
							   filter.GetType ().ToString ());

						IndexerDeferredReceipt dr;
						dr = new IndexerDeferredReceipt (indexable.Uri);
						receipt_queue.Add (dr);

						IndexerIndexablesReceipt ir;
						ir = new IndexerIndexablesReceipt (indexable.Uri, filter.GeneratedIndexables);
						receipt_queue.Add (ir);

						DeferredInfo di = new DeferredInfo (indexable, r, persistent_prop_doc, filter.GeneratedIndexables.Count);
						foreach (Indexable fi in filter.GeneratedIndexables)
							deferred_hash [fi.Uri] = di;
						deferred = true;
					}
				}

				// If we haven't deferred our receipt, add it to the index.
				if (! deferred) {
					Logger.Log.Debug ("+{0}", indexable.DisplayUri);
					AddDocumentToIndex (indexable, persistent_prop_doc, primary_writer, ref secondary_writer);
					receipt_queue.Add (r);

					// Lower the refcount on the deferred item, and
					// move it into the queue to be processed if all
					// the filter-generated indexables have been
					// indexed.
					DeferredInfo di = (DeferredInfo) deferred_hash [indexable.Uri];
					if (di != null) {
						di.Count--;
						deferred_hash.Remove (indexable.Uri);

						if (di.Count == 0)
							deferred_queue.Add (di);
					}
				}
				
				if (FileFilterNotifier != null)
					FileFilterNotifier (null, null); // reset

				// Clean up any temporary files associated with filtering this indexable.
				indexable.Cleanup ();

				// Remove any existing text cache for this item
				if (disable_textcache && text_cache != null)
					text_cache.Delete (indexable.Uri);
			}

			// Index any ready deferred items
			for (int i = 0; i < deferred_queue.Count; i++) {
				// We use for loop here rather than foreach so we can
				// append items to the end of the list without
				// exceptions being thrown.
				DeferredInfo di = (DeferredInfo) deferred_queue [i];

				Log.Debug ("+{0} (deferred)", di.Indexable.DisplayUri);
				AddDocumentToIndex (di.Indexable, di.PersistentPropDoc, primary_writer, ref secondary_writer);
				receipt_queue.Add (di.Receipt);

				DeferredInfo ref_di = (DeferredInfo) deferred_hash [di.Indexable.Uri];
				if (ref_di != null) {
					ref_di.Count--;
					deferred_hash.Remove (di.Indexable.Uri);

					if (ref_di.Count == 0)
						deferred_queue.Add (ref_di);
				}

				// Cleanup, and text cache maintenance.
				di.Indexable.Cleanup ();

				if (disable_textcache && text_cache != null)
					text_cache.Delete (di.Indexable.Uri);
			}
			deferred_queue.Clear ();

			if (text_cache != null)
				text_cache.CommitTransaction ();

			if (Shutdown.ShutdownRequested) {
				foreach (Indexable indexable in request_indexables)
					indexable.Cleanup ();

				primary_writer.Close ();
				if (secondary_writer != null)
					secondary_writer.Close ();
			
				return null;
			}

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

		private void AddDocumentToIndex (Indexable indexable,
						 Document persistent_prop_doc,
						 IndexWriter primary_writer,
						 ref IndexWriter secondary_writer)
		{
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

			secondary_doc = MergeDocuments (secondary_doc, persistent_prop_doc);

			if (secondary_doc != null) {
				if (secondary_writer == null)
					secondary_writer = new IndexWriter (SecondaryStore, IndexingAnalyzer, false);

				secondary_writer.AddDocument (secondary_doc);
			}

			AdjustItemCount (1);
		}

		// Since some parent properties maybe stored in child properties
		// as parent: property, any property change should be propagated
		// to all its children as well.
		private IndexerIndexablesReceipt GetChildPropertyChange (Hashtable children_docs,
									 Indexable parent)
		{
			if (! children_docs.Contains (parent.Uri))
				return null;

			IndexerIndexablesReceipt receipt;
			receipt = new IndexerIndexablesReceipt ();

			ArrayList child_uri_list = (ArrayList) children_docs [parent.Uri];
			ArrayList child_indexable_list = new ArrayList ();

			foreach (Uri uri in child_uri_list) {
				Indexable child_indexable;
				child_indexable = new Indexable (IndexableType.PropertyChange, uri);
				Log.Debug ("Creating property change child indexable for {1} (parent {0})", parent.Uri, uri);

				child_indexable.SetChildOf (parent);
				child_indexable_list.Add (child_indexable);
			}

			receipt.GeneratingUri = parent.Uri;
			receipt.Indexables = child_indexable_list;

			return receipt;
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
