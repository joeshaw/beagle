//
// IndexingServiceQueryable.cs
//
// Copyright (C) 2005 Novell, Inc.
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

// The IndexingService has two modes of operation: one is through the standard
// message-passing system and one where a slightly-structured file is dropped
// into a known location on the filesystem.
//
// (1) Messaging: An IndexingServiceRequest message is sent containing URIs of
// items to remove and Indexables to add.  This is more reliable, and is best
// for clients which will also be utilizing Beagle for searching.
//
// (2) Files: The file to be indexed is dropped into the ~/.beagle/ToIndex
// directory.  Another file with the same name prepended with a period is
// also dropped into the directory.  In that file is the metadata for the
// file being indexed.  The first line is the URI of the data being indexed.
// The second line is the hit type.  The third line is the mime type.  Then
// there are zero or more properties in the form "type:key=value", where
// "type" is either 't' for text or 'k' for keyword.  This method is a lot
// easier to use, but requires that Beagle have inotify support enabled to
// work.

using System;
using System.Collections;
using System.IO;
using System.Threading;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.IndexingServiceQueryable {

	[QueryableFlavor (Name="IndexingService", Domain=QueryDomain.Local, RequireInotify=false)]
	public class IndexingServiceQueryable : LuceneQueryable {

		public IndexingServiceQueryable () : base ("IndexingServiceIndex")
		{
			Server.RegisterRequestMessageHandler (typeof (IndexingServiceRequest), new Server.RequestMessageHandler (HandleMessage));
		}

		public override void Start ()
		{
			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			string index_path = Path.Combine (PathFinder.StorageDir, "ToIndex");

			if (!Directory.Exists (index_path))
				Directory.CreateDirectory (index_path);

			if (Inotify.Enabled)
				Inotify.Subscribe (index_path, OnInotifyEvent, Inotify.EventType.CloseWrite);

			Logger.Log.Info ("Setting up an initial crawl of the IndexingService directory");

			IndexableGenerator generator = new IndexableGenerator (GetIndexables (index_path));
			Scheduler.Task task = NewAddTask (generator);
			task.Tag = "IndexingService initial crawl";
			ThisScheduler.Add (task);
		}

		private IEnumerable GetIndexables (string path)
		{
			foreach (FileInfo file in DirectoryWalker.GetFileInfos (path)) {
				if (file.Name [0] == '.')
					continue;

				if (File.Exists (Path.Combine (file.DirectoryName, "." + file.Name)))
					yield return FileToIndexable (file);
			}

			yield break;
		}

		private Indexable FileToIndexable (FileInfo data_file)
		{
			FileInfo meta_file = new FileInfo (Path.Combine (data_file.DirectoryName, "." + data_file.Name));
			FileStream meta_stream;

			try {
				meta_stream = meta_file.Open (FileMode.Open, FileAccess.Read, FileShare.Read);
			} catch (FileNotFoundException) {
				// The meta file disappeared before we could
				// open it.
				return null;
			}

			StreamReader reader = new StreamReader (meta_stream);
			
			// First line of the file is a URI
			string line = reader.ReadLine ();
			Uri uri;

			try {
				uri = new Uri (line);
			} catch (Exception e) {
				Logger.Log.Warn (e, "IndexingService: Unable to parse URI in {0}:", meta_file.FullName);
				meta_stream.Close ();
				return null;
			}

			Indexable indexable = new Indexable (uri);
			indexable.Timestamp = data_file.LastWriteTimeUtc;
			indexable.ContentUri = UriFu.PathToFileUri (data_file.FullName);
			indexable.DeleteContent = true;

			// Second line is the hit type
			line = reader.ReadLine ();
			if (line == null) {
				Logger.Log.Warn ("IndexingService: EOF reached trying to read hit type from {0}",
						 meta_file.FullName);
				meta_stream.Close ();
				return null;
			} else if (line != String.Empty)
				indexable.HitType = line;

			// Third line is the mime type
			line = reader.ReadLine ();
			if (line == null) {
				Logger.Log.Warn ("IndexingService: EOF reached trying to read mime type from {0}",
						 meta_file.FullName);
				meta_stream.Close ();
				return null;
			} else if (line != String.Empty)
				indexable.MimeType = line;

			// Following lines are properties in "t:key=value" format
			do {
				line = reader.ReadLine ();

				if (line != null && line != String.Empty) {
					bool keyword = false;

					if (line[0] == 'k')
						keyword = true;
					else if (line[0] != 't') {
						Logger.Log.Warn ("IndexingService: Unknown property type: '{0}'", line[0]);
						continue;
					}

					int i = line.IndexOf ('=');

					if (i == -1) {
						Logger.Log.Warn ("IndexingService: Unknown property line: '{0}'", line);
						continue;
					}
					
					// FIXME: We should probably handle date types
					if (keyword) {
						indexable.AddProperty (Property.NewUnsearched (line.Substring (2, i - 2),
											    line.Substring (i + 1)));
					} else {
						indexable.AddProperty (Property.New (line.Substring (2, i - 2),
										     line.Substring (i + 1)));
					}
				}
			} while (line != null);

			indexable.LocalState ["MetaFile"] = meta_file;
			
			// Ok, we're finished with the meta file.  It will be
			// deleted in PostAddHook ().
			meta_stream.Close ();

			return indexable;
		}

		// Bleh, we need to keep around a list of pending items to be
		// indexed so that we don't actually index it twice because
		// the order of the creation of the data file and meta file
		// isn't defined.
		private ArrayList pending_files = new ArrayList ();

		private void OnInotifyEvent (Inotify.Watch watch,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (subitem == "")
				return;
			
			if (subitem[0] == '.') {
				string data_file = Path.Combine (path, subitem.Substring (1));

				lock (pending_files) {
					if (File.Exists (data_file) && ! pending_files.Contains (data_file)) {
						pending_files.Add (data_file);
						IndexFile (new FileInfo (data_file));
					}
				}
			} else {
				string meta_file = Path.Combine (path, "." + subitem);
				string data_file = Path.Combine (path, subitem);

				lock (pending_files) {
					if (File.Exists (meta_file) && ! pending_files.Contains (data_file)) {
						pending_files.Add (data_file);
						IndexFile (new FileInfo (data_file));
					}
				}
			}
		}

		private void IndexFile (FileInfo data_file)
		{
			Indexable indexable = FileToIndexable (data_file);

			if (indexable == null) // The file disappeared
				return;

			Scheduler.Task task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}

		protected override void PostAddHook (Indexable indexable, IndexerAddedReceipt receipt)
		{
			FileInfo meta_file = indexable.LocalState ["MetaFile"] as FileInfo;
			if (meta_file == null)
				return;

			meta_file.Delete ();

			lock (pending_files)
				pending_files.Remove (indexable.ContentUri.LocalPath);
		}

		private class IndexableGenerator : IIndexableGenerator {
			private IEnumerator to_add_enumerator;
			private int count = -1, done_count = 0;

			public IndexableGenerator (IEnumerable to_add)
			{
				this.to_add_enumerator = to_add.GetEnumerator ();
			}

			public IndexableGenerator (ICollection to_add) : this ((IEnumerable) to_add)
			{
				this.count = to_add.Count;
			}

			public Indexable GetNextIndexable ()
			{
				return to_add_enumerator.Current as Indexable;
			}
			
			public bool HasNextIndexable ()
			{
				++done_count;
				return to_add_enumerator.MoveNext ();
			}

			public string StatusName {
				get { 
					if (count == -1)
						return String.Format ("IndexingService: {0}", done_count);
					else
						return String.Format ("IndexingService: {0} of {1}", done_count, count);
				}
			}

			public void PostFlushHook ()
			{ }
		}

		private ResponseMessage HandleMessage (RequestMessage msg)
		{
			IndexingServiceRequest isr = (IndexingServiceRequest) msg;

			LuceneQueryable backend = this;

			if (isr.Source != null) {
				Queryable target = QueryDriver.GetQueryable (isr.Source);

				if (target == null) {
					string err = String.Format ("Unable to find backend matching '{0}'", isr.Source);

					Log.Error (err);
					return new ErrorResponse (err);
				}

				if (! (target.IQueryable is LuceneQueryable)) {
					string err = String.Format ("Backend '{0}' is not an indexed backend", isr.Source);

					Log.Error (err);
					return new ErrorResponse (err);
				}

				backend = (LuceneQueryable) target.IQueryable;
			}

			foreach (Uri uri in isr.ToRemove) {
				Log.Debug ("IndexingService: Removing {0}", uri);
				Scheduler.Task task = backend.NewRemoveTask (uri);
				ThisScheduler.Add (task);
			}

			// FIXME: There should be a way for the request to control the
			// scheduler priority of the task.

			if (isr.ToAdd.Count > 0) {
				Log.Debug ("IndexingService: Adding {0} indexables.", isr.ToAdd.Count);
				IIndexableGenerator ind_gen = new IndexableGenerator (isr.ToAdd);
				Scheduler.Task task = backend.NewAddTask (ind_gen);
				task.Priority = Scheduler.Priority.Immediate;
				ThisScheduler.Add (task);
			}

			// FIXME: There should be an asynchronous response  (fired by a Scheduler.Hook)
			// that fires when all of the items have been added to the index.
			
			// No response
			return new EmptyResponse ();
		}

	}

}
