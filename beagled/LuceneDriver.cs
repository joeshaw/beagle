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
using System.Xml;

using Mono.Posix;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using Beagle.Util;

namespace Beagle.Daemon {

	public delegate void PostIndexHook (LuceneDriver driver, 
					    Uri uri);

	public class LuceneDriver {

		private static Beagle.Util.Logger log = Logger.Get ("lucene");

		// 1: Original
		// 2: Changed format of timestamp strings
		// 3: Schema changed to be more Dashboard-Match-like
		// 4: Schema changed for files to include _Directory property
		// 5: Changed analyzer to support stemming.  Bumped version # to
		//    force everyone to re-index.
		// 6: lots of schema changes as part of the general refactoring
		private const int VERSION = 6;

		private LuceneQueue queue;

		public LuceneDriver (string dir)
		{
			Setup (dir, false); 
		}

		public LuceneDriver (string dir, bool isQueuePersistent) 
		{
			Setup (dir, isQueuePersistent);
		}

		public void Setup (string dir, bool isQueuePersistent)
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
			// crash or be shut down before reaching the
			// sinceOptimizationThreshold.
			//
			// This creates the index if it doesn't exist
			IndexWriter writer = new IndexWriter (Store, null, !indexExists);
			if (indexExists)
				writer.Optimize ();
			writer.Close ();

			// Start an indexing queue
			queue = new LuceneQueue (this, isQueuePersistent, queueDir);
			Shutdown.AddQueue (queue);

			queue.Start ();
		}

		/////////////////////////////////////////////////////

		static object numberOfWorkersLock = new object ();
		static int numberOfWorkers = 0;

		static void WorkerBegin ()
		{
			lock (numberOfWorkersLock) {
				++numberOfWorkers;
			}
		}

		static void WorkerEnd ()
		{
			lock (numberOfWorkersLock) {
				--numberOfWorkers;
			}
		}

		static bool MultipleWorkers {
			get { 
				lock (numberOfWorkersLock) {
					return numberOfWorkers > 1;
				}
			}
		}
		

		/////////////////////////////////////////////////////

		//
		// The Index's Fingerprint
		//
		
		private string fingerprint = null;

		public string Fingerprint {
			get { return fingerprint; }
		}

		public bool IsUpToDate (FileSystemInfo fsinfo)
		{
			return ExtendedAttribute.Check (fsinfo, fingerprint);
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
			}
		}

		/////////////////////////////////////////////////////

		//
		// Public API
		//

		public void ScheduleAdd (Indexable indexable, int priority, PostIndexHook hook)
		{
			QueueItem item;
			item = new QueueItem ();
			item.Priority = priority;
			item.IndexableToAdd = indexable;
			item.PostIndexHook += hook;

			queue.ScheduleQueueItem (item);
		}

		public void ScheduleAdd (Indexable indexable, int priority)
		{
			ScheduleAdd (indexable, priority, null);
		}

		public void ScheduleAdd (Indexable indexable)
		{
			ScheduleAdd (indexable, 0, null);
		}

		public void ScheduleAddAndMark (Indexable indexable, int priority, FileSystemInfo fsinfo)
		{
			MarkClosure closure = new MarkClosure (fsinfo, log);
			ScheduleAdd (indexable, priority, new PostIndexHook (closure.Hook));
		}

		public bool ScheduleAddFile (FileSystemInfo fsinfo, int priority)
		{
			// Skip symlinks.  FIXME: This is probably the wrong thing
			// to do, but it doesn't seem possible to attach
			// extended attributes to them.
			if (IsSymLink (fsinfo)) {
				log.Debug ("{0} is a symlink... skipping", fsinfo.FullName);				
				return false;
			}

			// If the file appears to be up-to-date, don't do
			// anything.
			if (IsUpToDate (fsinfo)) {
				log.Debug ("{0} appears to be up-to-date", fsinfo.FullName);
				return false;
			}

			log.Debug ("Scheduled {0}", fsinfo.FullName);

			Uri uri = UriFu.PathToFileUri (fsinfo.FullName);
			FilteredIndexable indexable = new FilteredIndexable (uri);
			ScheduleAddAndMark (indexable, priority, fsinfo);
			
			return true;
		}

		public void ScheduleDelete (Uri uri, int priority, PostIndexHook hook)
		{
			QueueItem item;
			item = new QueueItem ();
			item.Priority = priority;
			item.UriToDelete = uri;
			item.PostIndexHook += hook;
			log.Debug ("Scheduling deletion of {0}", uri);
			queue.ScheduleQueueItem (item);
		}

		public void ScheduleDelete (Uri uri, int priority)
		{
			ScheduleDelete (uri, priority, null);
		}

		public void ScheduleDelete (Uri uri)
		{
			ScheduleDelete (uri, 0, null);
		}

		public void ScheduleDeleteFile (string path, int priority)
		{
			Uri uri = UriFu.PathToFileUri (path);
			ScheduleDelete (uri, priority, null);
		}

		private class HitInfo {
			public Uri Uri;
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
				Uri uri = UriFromLuceneDoc (doc);

				// If this is a file Uri and the file doesn't exist
				// on the system, filter the hit out of the
				// query results and schedule the removal of that
				// Uri from the index.
				if (uri.IsFile
				    && ! File.Exists (uri.LocalPath)
				    && ! Directory.Exists (uri.LocalPath)) {

					log.Debug ("{0} is file!", uri);
					// Do a low-priority delete --- it shouldn't matter
					// if this lingers in the index for a bit, right?
					ScheduleDelete (uri, -1000);

					continue;
				}

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
		// The MarkClosure class
		//

		private class MarkClosure {
			FileSystemInfo info;
			Logger log;
			DateTime mtime;
			
			public MarkClosure (FileSystemInfo _info, Logger _log)
			{
				info = _info;
				log = _log;
				// We cache the file's mtime...
				mtime = info.LastWriteTime;
			}

			public void Hook (LuceneDriver driver, Uri uri)
			{
				// ...and then use that mtime when marking the file.
				// Maybe this is just paranoia, but I don't want to miss
				// file changes between indexing and marking the file.
				try {
					ExtendedAttribute.Mark (info, driver.Fingerprint, mtime);
				} catch (Exception e) {
					if (info.Exists && log != null)
						log.Debug (e);
				}
			}
		}


		/////////////////////////////////////////////////////

		//
		// The PersistClosure class
		//

		private class PersistClosure {
			string filename;
			
			public PersistClosure (string filename) 
			{
				this.filename = filename;
			}

			public void Hook (LuceneDriver driver, Uri uri)
			{
				File.Delete (filename);
			}
		}


		/////////////////////////////////////////////////////
		
		//
		// Queue implementation
		//

		private class QueueItem : IComparable {
			public uint SequenceNumber;
			public int  Priority;

			public Indexable IndexableToAdd;
			public Uri UriToDelete;
			public event PostIndexHook PostIndexHook;
			public bool IsSilent = false;

			public Uri Uri {
				get { 
					if (IndexableToAdd != null)
						return IndexableToAdd.Uri;
					return UriToDelete;
				}
			}

			public bool IsAdd {
				get { return IndexableToAdd != null; }
			}

			public bool IsDelete {
				get { return UriToDelete != null; }
			}

			override public string ToString ()
			{
				return String.Format ("[QueueItem: {0} {1}]",
						      IsAdd ? "Add" : "Delete", Uri);
			}

			public void WriteToXml (XmlTextWriter writer) 
			{
				writer.WriteStartElement ("queueitem");
				writer.WriteAttributeString ("sequenceno",
							     SequenceNumber.ToString ());
				writer.WriteAttributeString ("priority",
							     Priority.ToString ());
				if (UriToDelete != null)
					writer.WriteAttributeString ("todelete", 
								     UriToDelete.ToString ());
				if (IndexableToAdd != null) {
					writer.WriteAttributeString ("hasindexable", "true");
					IndexableToAdd.WriteToXml (writer);
				}

				writer.WriteEndElement ();
			}

			public void ReadFromXml (XmlTextReader reader)
			{
				reader.Read ();
				string str;

				SequenceNumber = uint.Parse (reader.GetAttribute ("sequenceno"));
				Priority = int.Parse (reader.GetAttribute ("priority"));
				str = reader.GetAttribute ("todelete");
				if (str == null)
					UriToDelete = null;
				else
					UriToDelete = new Uri (str, true);
				
				if (reader.GetAttribute ("hasindexable") == "true") {
					IndexableToAdd = Indexable.NewFromXml (reader);
				}


			}

			public void RunHook (LuceneDriver driver) 
			{
				if (PostIndexHook != null)
					PostIndexHook (driver, Uri);
			}

			public int CompareTo (object o) 
			{
				if (o == null) return 1;
			
				if (o is QueueItem)
					return (int)((int)this.SequenceNumber - (int)((QueueItem)o).SequenceNumber);
				else 
					throw new ArgumentException ();
			}	
		}

		private class LuceneQueue : ThreadedPriorityQueue {

			private LuceneDriver driver;
			
			private uint NextSequenceNumber = 0;
			private Hashtable seqnoByUri = new Hashtable ();
			
			private Hashtable pendingByUri = new Hashtable ();
			private int pendingAdds = 0;
			private int pendingDeletes = 0;
			private const int pendingAddThreshold = 21;
			private const int pendingDeleteThreshold = 83;

			private bool isPersistent;
			string queueDir;
			
			int sinceOptimization = 0;
			const int sinceOptimizationThreshold = 117;

			public LuceneQueue (LuceneDriver _driver,
					    bool persistent,
					    string queueDir)
			{
				driver = _driver;
				Log = LuceneDriver.log;
				isPersistent = persistent;
				this.queueDir = queueDir;

				if (persistent) {
					Directory.CreateDirectory (queueDir);
					RestoreQueue ();
				}
			}

			private QueueItem RestoreQueueItem (string filename)
			{
				try {
					FileStream f = new FileStream (filename,
								       System.IO.FileMode.Open,
								       FileAccess.Read);
					StreamReader sr = new StreamReader (f); 
					
					XmlTextReader reader = new XmlTextReader (sr);
					QueueItem item = new QueueItem ();
					item.ReadFromXml (reader);
					reader.Close ();
					sr.Close ();
					f.Close ();

					return item;
				} catch (Exception e) {
					Logger.Log.Warn ("Unable to restore queued indexable: {0}", e);
					return null;
				}

			}

			private void RestoreQueue () 
			{
				string[] files = Directory.GetFiles (queueDir);
				ArrayList items = new ArrayList ();
				foreach (string file in files) {
					QueueItem item = RestoreQueueItem (file);
					PersistClosure closure = new PersistClosure (file);
					item.PostIndexHook += closure.Hook;
					
					if (item != null)
						items.Add (item);
				}
				items.Sort ();

				foreach (QueueItem item in items) {
					// Reschedule without persisting again
					ScheduleQueueItem (item, false);
				}

				Logger.Log.Debug ("Restored {0} unindexed items to the queue", items.Count);
			}

			private string PersistQueueItem (QueueItem item)
			{
				string filename = Path.Combine (queueDir, Guid.NewGuid ().ToString ());

				FileStream f = new FileStream (filename,
							       System.IO.FileMode.Create,
							       FileAccess.ReadWrite);
				StreamWriter sw = new StreamWriter (f); 
				
				XmlTextWriter writer = new XmlTextWriter (sw);
				item.WriteToXml (writer);
				writer.Close ();
				sw.Close ();
				f.Close ();

				return filename;
			}

			public void ScheduleQueueItem (QueueItem item,
						       bool persist)
			{
				lock (this) {
					item.SequenceNumber = NextSequenceNumber;
					++NextSequenceNumber;
				}
				
				if (persist) {
					string filename = PersistQueueItem (item);
					PersistClosure closure = new PersistClosure (filename);
					item.PostIndexHook += closure.Hook;
				}

				Enqueue (item, item.Priority);
			}

			public void ScheduleQueueItem (QueueItem item) 
			{
				ScheduleQueueItem (item, isPersistent);
			}
			
			public void Flush ()
			{
				if (pendingByUri.Count == 0)
					return;

				Log.Debug ("Flushing...");

				Stopwatch watch = new Stopwatch ();

				ArrayList pending = new ArrayList (pendingByUri.Values);

				ArrayList idsToDelete = new ArrayList ();

				// Step #1:  Convert the URIs of all pending items
				// to Id numbers and store them in idsToDelete
				watch.Restart ();
				LNS.BooleanQuery uriQuery = new LNS.BooleanQuery ();
				foreach (QueueItem item in pending) {
					Uri uri = item.Uri;
					if (! item.IsSilent)
						Log.Debug ("- {0}", uri);
					Term term = new Term ("Uri", uri.ToString ());
					LNS.Query termQuery = new LNS.TermQuery (term);
					uriQuery.Add (termQuery, false, false);
				}
				LNS.Searcher searcher = new LNS.IndexSearcher (driver.Store);
				LNS.Hits uriHits = searcher.Search (uriQuery);
				for (int i = 0; i < uriHits.Length (); ++i) {
					int id = uriHits.Id (i);
					idsToDelete.Add (id);
				}
				searcher.Close ();
				watch.Stop ();
				Log.Debug ("Step #1: {0} {1} {2}", watch, pending.Count, watch.ElapsedTime / pending.Count);

				// Step #2: Walk across the list of ids and delete all
				// of those documents.
				watch.Restart ();
				IndexReader reader;
				reader = IndexReader.Open (driver.Store);
				foreach (int id in idsToDelete)
					reader.Delete (id);
				reader.Close ();
				watch.Stop ();
				Log.Debug ("Step #2: {0}", watch);

				// Step #3: Write out the pending adds
				watch.Restart ();
				IndexWriter writer = null;
				foreach (QueueItem item in pending) {
					if (! item.IsAdd)
						continue;

					Indexable indexable = item.IndexableToAdd;
					
					Document doc = null;
					if (! item.IsSilent)
						Log.Debug ("+ {0}", indexable.Uri);
					try {
						doc = driver.ToLuceneDocument (indexable);
					} catch (Exception e) {
						Log.Error ("Unable to convert {0} (type={1}) to a lucene document",
							   indexable.Uri, indexable.Type);
						Log.Error (e);
					}

					if (doc != null) {
						if (writer == null)
							writer = new IndexWriter (driver.Store, Analyzer, false);
						writer.AddDocument (doc);
						++sinceOptimization;
					}
				}
				if (writer != null) {
					if (sinceOptimization > sinceOptimizationThreshold) {
						Log.Debug ("Threshold Optimize ({0})", sinceOptimization);
						writer.Optimize ();
						sinceOptimization = 0;
					}
					writer.Close ();
				}
				watch.Stop ();
				Log.Debug ("Step #3: {0}", watch);

				// Step #4: 
				// (a) Call the post-index hooks.
				// (b) Broadcast our notifications.
				// (c) Store the sequence numbers by Uri
				watch.Restart ();
				foreach (QueueItem item in pending) {

					item.RunHook (driver);

					if (! item.IsSilent) {
						if (item.IsAdd) {
							if (driver.AddedEvent != null)
								driver.AddedEvent (driver, item.Uri);
						} else if (item.IsDelete) {
							if (driver.DeletedEvent != null)
								driver.DeletedEvent (driver, item.Uri);
						}
					}

					seqnoByUri [item.Uri] = item.SequenceNumber;
				}
				watch.Stop ();
				Log.Debug ("Step #4: {0}", watch);
				
				// Step #6: Clear the list of pending items.
				pendingByUri.Clear ();
				pendingAdds = 0;
				pendingDeletes = 0;
			}
			
			///////////////////////////////////////////////////////
			

			override protected void ProcessQueueItem (object obj)
			{
				QueueItem item = (QueueItem) obj;
				QueueItem oldItem = null;

				// Check to make sure that another item w/ the
				// same Uri and a larger sequence number
				// wasn't already processed.
				if (seqnoByUri.Contains (item.Uri)) {
					uint seqno = (uint) seqnoByUri [item.Uri];
					if (seqno > item.SequenceNumber) {
						Log.Debug ("Rejected {0} by seqno", item.Uri);
						return;
					}
				}

				oldItem = pendingByUri [item.Uri] as QueueItem;
				if (oldItem == null || item.SequenceNumber > oldItem.SequenceNumber) {
				
					pendingByUri [item.Uri] = item;
					
					if (oldItem != null) {
						Log.Debug ("Superceding previously queued item {0}", item.Uri);
						if (oldItem.IsAdd)
							--pendingAdds;
						else if (oldItem.IsDelete)
							--pendingDeletes;
					}
				
					if (item.IsAdd)
						++pendingAdds;
					else if (item.IsDelete)
						++pendingDeletes;
				} else {
					Log.Debug ("Dropped duplicate {0}", item.Uri);
				}

				// We always flush after an elevated-priority item is processed.
				if (item.Priority > 0
				    || pendingAdds >= pendingAddThreshold
				    || pendingDeletes >= pendingDeleteThreshold) {

					string reason;
					if (item.Priority > 0)
						reason = "Priority";
					else
						reason = "Threshold";

					int pa = pendingAdds;
					int pd = pendingDeletes;
					
					Stopwatch watch = new Stopwatch ();
					watch.Start ();
					Flush ();
					watch.Stop ();

					Log.Debug ("{0} Flush (add={1}, delete={2}), {3}",
							  reason, pa, pd, watch);
				}
			}

			override protected int PostProcessSleepDuration ()
			{
				return 50;
			}

			override protected int EmptyQueueTimeoutDuration ()
			{
				Log.Debug ("pendingCount={0}, sinceOptimization={1}",
					   pendingByUri.Count, sinceOptimization);
				if (pendingByUri.Count > 0)
					return 1000; // After 1s, flush
				else if (sinceOptimization > 0)
					return 5000; // After 5s, optimize
				return 0;
			}

			override protected void EmptyQueueTimeout ()
			{
				// If the queue is empty, there is no longer
				// a risk of processing a queue items out of
				// sequence because of priorities.
				seqnoByUri.Clear ();

				if (pendingByUri.Count != 0) {
					
					int pa = pendingAdds;
					int pd = pendingDeletes;

					Stopwatch watch = new Stopwatch ();
					watch.Start ();
					Flush ();
					watch.Stop ();
					Log.Debug ("Opportunistic Flush (add={0}, delete={1}), {2}",
						   pa, pd, watch);
					
				} else if (sinceOptimization > 0) {

					Stopwatch watch = new Stopwatch ();
					watch.Start ();
					IndexWriter writer;
					writer = new IndexWriter (driver.Store, null, false);
					writer.Optimize ();
					writer.Close ();
					watch.Stop ();
					Log.Debug ("Opportunistic Optimize ({0}), {1}",
						   sinceOptimization, watch);

					sinceOptimization = 0;
				}
					
			}
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

		// Sanity-check a Document against the Index:  Make sure that
		// there isn't some other more recent document with the same Uri.
		private bool DocIsUpToDate (LNS.Searcher searcher,
					    Uri          docUri,
					    int          docId,
					    Versioned    docVersioned)
		{
			// First, find documents with the same Uri.
			Term uriTerm = new Term ("Uri", docUri.ToString ());
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
					log.Warn ("Matched obsolete document with Uri '{0}'", docUri);
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

		// Check if a file is a symlink.
		private static bool IsSymLink (FileSystemInfo info)
		{
			Stat stat = new Stat ();
			Syscall.lstat (info.FullName, out stat);
			int mode = (int) stat.Mode & (int)StatModeMasks.TypeMask;
			return mode == (int) StatMode.SymLink;
		}
	}
}
