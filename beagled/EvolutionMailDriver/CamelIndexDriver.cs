//
// CamelIndexDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//
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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Mono.Posix;

using Beagle.Util;
using Camel = Beagle.Util.Camel;

namespace Beagle.Daemon {

	internal class CamelIndex : IDisposable {
		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_text_index_new (string path, int mode);

		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_index_words (IntPtr index);

		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_index_find (IntPtr index, string word);

		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_index_cursor_next (IntPtr cursor);

		[DllImport ("libcamel.so.0")]
		extern static void camel_object_unref (IntPtr obj);

		private IntPtr index = IntPtr.Zero;

		public CamelIndex (string path)
		{
			this.index = camel_text_index_new (path, (int) OpenFlags.O_RDONLY);

			if (this.index == IntPtr.Zero)
				throw new ArgumentException ();
		}

		~CamelIndex ()
		{
			if (this.index != IntPtr.Zero)
				camel_object_unref (this.index);
		}

		public void Dispose ()
		{
			if (this.index != IntPtr.Zero)
				camel_object_unref (this.index);
			GC.SuppressFinalize (this);
		}

		private static string GetUid (IntPtr cursor)
		{
			IntPtr uid_ptr = camel_index_cursor_next (cursor);

			if (uid_ptr == IntPtr.Zero)
				return null;
			else
				return Marshal.PtrToStringAnsi (uid_ptr);
		}

		public ArrayList Match (string folderName, IList words)
		{
			ArrayList matches = null;

			foreach (string word in words) {
				ArrayList word_matches = new ArrayList ();

				IntPtr cursor = camel_index_find (this.index, word);

				string uid;
				while ((uid = GetUid (cursor)) != null) {
					Uri uri = EvolutionMailQueryable.EmailUri ("local@local", folderName, uid);
					word_matches.Add (uri);
				}

				if (matches == null)
					matches = word_matches;
				else {
					// Remove duplicates
					foreach (Uri m in matches) {
						foreach (Uri m2 in word_matches) {
							if (m2 == m)
								matches.Remove (m);
						}
					}
				}

				camel_object_unref (cursor);
			}

			return matches;
		}
	}

	internal class CamelIndexDriver {

		private static Logger log = Logger.Get ("mail");

		private EvolutionMailQueryable queryable;
		private LuceneDriver driver;
		private QueryBody queryBody;
		private IQueryResult queryResult;
		private Hashtable recentHits = new Hashtable ();
		private SortedList watched = new SortedList ();
		private ArrayList indexes = new ArrayList ();
		private Hashtable indexStatus = new Hashtable ();

		public CamelIndexDriver (EvolutionMailQueryable queryable, LuceneDriver driver,
					 QueryBody body, IQueryResult result)
		{
			this.queryable = queryable;
			this.driver = driver;
			this.queryBody = body;
			this.queryResult = result;

			string home = Environment.GetEnvironmentVariable ("HOME");
			string local_path = Path.Combine (home, ".evolution/mail/local");

			Inotify.Event += OnInotifyEvent;
			Watch (local_path);

			QueryResult queryResult = (QueryResult) result;
			queryResult.CancelledEvent += OnResultCancelled;

			Shutdown.ShutdownEvent += OnShutdown;
		}

		private void OnResultCancelled (QueryResult source)
		{
			this.queryBody = null;
			this.queryResult = null;
		}

		private void OnShutdown ()
		{
			this.queryBody = null;
			this.queryResult = null;
		}

		private void Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			if (! root.Exists)
				return;

			Queue queue = new Queue ();
			queue.Enqueue (root);
			this.indexes.Clear ();

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				int wd = Inotify.Watch (dir.FullName,
							Inotify.EventType.CreateSubdir
							| Inotify.EventType.Modify
							| Inotify.EventType.MovedTo);
				watched [wd] = dir.FullName;
				this.indexes.AddRange (Directory.GetFiles (dir.FullName, "*.ibex.index"));

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}
		}

		private void Ignore (string path)
		{
			Inotify.Ignore (path);
			watched.RemoveAt (watched.IndexOfValue (path));

			this.indexes.Clear ();
			foreach (string p in this.watched.Values)
				this.indexes.AddRange (Directory.GetFiles (p, "*.ibex.index"));
		}

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     Inotify.EventType type,
					     uint cookie)
		{
			if (subitem == "" || ! watched.Contains (wd))
				return;

			string fullPath = Path.Combine (path, subitem);

			switch (type) {
				
			case Inotify.EventType.CreateSubdir:
				Watch (fullPath);
				break;

			case Inotify.EventType.DeleteSubdir:
				Ignore (fullPath);
				break;

			// Ok, some explanation needed here.  The .ibex.index files get modified,
			// but you get out-of-sync errors if you try to read them before the
			// summary is updated.  So what we do is: if the .ibex.index file has
			// been changed, add it to the hash table with a value of false.  When
			// the summary gets updated, change the value to true.  When the
			// index is updated again, we know that it's ok to rerun the query.
			case Inotify.EventType.MovedTo:
				
				if (Path.GetExtension (fullPath) == ".ev-summary") {
					string indexPath = Path.ChangeExtension (fullPath, ".ibex.index");

					if (this.indexStatus.ContainsKey (indexPath)) {
						// Second pass - the summary
						this.indexStatus [indexPath] = true;
					}
				}
				break;

			case Inotify.EventType.Modify:

				if (Path.GetExtension (fullPath) == ".index") {
					if (!this.indexStatus.ContainsKey (fullPath)) {
						// First pass - the index file changes
						this.indexStatus [fullPath] = false;
					} else if ((bool) this.indexStatus [fullPath] == true) {
						// Third pass - the index file changes again
						log.Debug ("Index {0} has changed, rerunning query", fullPath);
						this.indexStatus.Remove (fullPath);
						this.Start ();
					}
				}
				break;
			}
		}

		public void Start ()
		{
			ArrayList matches = new ArrayList ();
			ArrayList hits = new ArrayList ();

			foreach (string idx_path in this.indexes) {
				// gets rid of the ".index" from the file.  camel expects it to just end in ".ibex"
				string path = Path.ChangeExtension (idx_path, null);

				CamelIndex index;

				try {
					index = new CamelIndex (path);
				} catch (DllNotFoundException e) {
					log.Info ("Couldn't load libcamel.so.  You probably need to set $LD_LIBRARY_PATH");
					return;
				} catch {
					// If an index is invalid, just skip it.
					continue;
				}

				// Index files are in the form "foo.ibex.index", so we need to remove the extension
				// twice.
				string folderPath = Path.ChangeExtension (Path.ChangeExtension (idx_path, null), null);
				string folderName = EvolutionMailQueryable.GetLocalFolderName (new FileInfo (folderPath));

				matches.AddRange (index.Match (folderName, this.queryBody.Text));
				index.Dispose ();
			}			

			hits.AddRange (this.driver.DoQueryByUri (matches));

			ArrayList filteredHits = new ArrayList ();
			Hashtable newRecentHits = new Hashtable ();
			foreach (Hit hit in hits) {
				if (!this.recentHits.ContainsKey (hit.Uri))
					filteredHits.Add (hit);
				else
					this.recentHits.Remove (hit.Uri);

				newRecentHits [hit.Uri] = hit;
			}

			this.queryResult.Add (filteredHits);
			this.queryResult.Subtract (this.recentHits.Keys);

			this.recentHits = newRecentHits;
		}
	}
}
