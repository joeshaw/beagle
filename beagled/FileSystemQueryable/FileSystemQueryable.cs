//
// FileSystemQueryable.cs
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

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;


using Beagle.Daemon;
using Beagle.Util;


namespace Beagle.Daemon.FileSystemQueryable {

	internal class DirectoryIndexableGenerator : IIndexableGenerator {
		private DirectoryInfo dir_info;
		private IEnumerator files = null; 
		private FileSystemQueryable queryable;

		public DirectoryIndexableGenerator (FileSystemQueryable q,
						    DirectoryInfo info)
		{
			queryable = q;
			dir_info = info;
		}

		public Indexable GetNextIndexable ()
		{
			FileInfo f = files.Current as FileInfo;
			return FileSystemQueryable.FileToIndexable (f.FullName, true);
		}

		public bool HasNextIndexable ()
		{
			if (files == null)
				files = dir_info.GetFiles ().GetEnumerator ();
			
			bool has_next = files.MoveNext ();
			while (has_next) {
				FileInfo f = files.Current as FileInfo;
				if (queryable.FileNeedsIndexing (f.FullName)) 
					return true;
				has_next = files.MoveNext ();
			}
			return false;
		}

		public string StatusName {
			get {
				return dir_info.Name;
			}
		}

		public override bool Equals (object o)
		{
			DirectoryIndexableGenerator generator = o as DirectoryIndexableGenerator;

			if (generator == null) 
				return false;

			if (Object.ReferenceEquals (this, generator))
			    return true;

			if (this.dir_info.FullName == generator.dir_info.FullName)
				return true;
			else
				return false;

		}
	}

	[QueryableFlavor (Name="Files", Domain=QueryDomain.Local)]
	public class FileSystemQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("FileSystemQueryable");

		public FileSystemQueryable () : base ("FileSystemIndex")
		{
			Inotify.Event += OnInotifyEvent;
		}

		//////////////////////////////////////////////////////////////////////////

		private FileNameFilter filter = new FileNameFilter ();

		private bool IgnoreFile (string path)
		{
			if (FileSystem.IsSymLink (path))
				return true;

			if (filter.Ignore (path))
				return true;
			
			return false;
		}

		public bool FileNeedsIndexing (string path)
		{
			if (! FileSystem.Exists (path))
				return false;

			if (FileSystem.IsSymLink (path))
				return false;

			if (filter.Ignore (path))
				return false;

			if (Driver.IsUpToDate (path))
				return false;
			
			return true;
		}

		public static Indexable FileToIndexable (string path, bool crawl_mode)
		{
			Uri uri = UriFu.PathToFileUri (path);
			return new FilteredIndexable (uri, crawl_mode);
		}

		//////////////////////////////////////////////////////////////////////////

		private void Add (string path)
		{
			if (FileNeedsIndexing (path)) {
				Scheduler.Task task;
				Indexable indexable;

				indexable = FileToIndexable (path, false);
				task = NewAddTask (indexable);
				task.Priority = Scheduler.Priority.Immediate;
				
				ThisScheduler.Add (task);
			}
		}

		private void Remove (string path)
		{
			Uri uri = UriFu.PathToFileUri (path);
			Scheduler.Task task;
			task = NewRemoveTask (uri);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}

		private void Crawl (WatchedDirectory dir)
		{
			DirectoryInfo info = new DirectoryInfo (dir.Path);
			if (! info.Exists)
				return;
			
			Scheduler.TaskGroup group = LastCrawlTime.NewTaskGroup (dir.Path, DateTime.Now);
			
			Scheduler.Task task;
			Indexable indexable;

			log.Info ("Crawling {0}", dir.Path);

			// Index the directory itself...
			if (FileNeedsIndexing (dir.Path)) {
				
				/* Enable crawl mode on the indexable */
				indexable = FileToIndexable (dir.Path, true);
				
				task = NewAddTask (indexable);
				task.AddTaskGroup (group);
				task.Priority = Scheduler.Priority.Delayed;
				task.SubPriority = 0;
				task.Description = "Indexing directory";

				ThisScheduler.Add (task);
			}
			

			DirectoryIndexableGenerator generator = new DirectoryIndexableGenerator (this, info);
			task = NewAddTask (generator);
			task.AddTaskGroup (group);
			task.Priority = Scheduler.Priority.Delayed;
			task.SubPriority = 0;
			task.Description = "Crawling " + dir.Path;
			ThisScheduler.Add (task);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Maintain a sorted list of directories marked as "dirty".
		// These are directories that need to be crawled, sorted in
		// an order that prioritizes never-before-crawled and
		// recently active directories.
		//

		private ArrayList dir_dirty = new ArrayList ();
		private Hashtable dir_dirty_hash = new Hashtable ();
		private bool dir_dirty_needs_sorting = false;

		private void AddDirtyDirectory (WatchedDirectory dir)
		{
			if (! dir.Dirty)
				return;

			lock (dir_dirty) {

				if (dir_dirty_hash.Contains (dir)) {
					// If we already know the directory is dirty,
					// assume something has changed and thus that
					// we need a re-sort.
					dir_dirty_needs_sorting = true;
				} else {
					// There is no point in insertion-sorting
					// if the list is in a possibly-unsorted state.
					if (dir_dirty_needs_sorting) {
						dir_dirty.Add (dir);
					} else {
						int i = dir_dirty.BinarySearch (dir);
						if (i < 0)
							i = ~i;
						dir_dirty.Insert (i, dir);
					}
					dir_dirty_hash [dir] = true;
				}

				if (dir_dirty.Count == 1)
					ScheduleCrawl ();
			}
		}

		private bool HaveDirtyDirectories {
			get { lock (dir_dirty) { return dir_dirty.Count > 0; } }
		}

		private WatchedDirectory GetNextDirtyDirectory ()
		{
			lock (dir_dirty) {
				if (dir_dirty.Count == 0)
					return null;

				WatchedDirectory dir;
				int i;

				if (dir_dirty_needs_sorting) {

					dir_dirty.Sort ();

					// Prune non-dirty directories off of the end.
					i = dir_dirty.Count - 1;
					while (i >= 0) {
						dir = dir_dirty [i] as WatchedDirectory;
						if (dir.Dirty)
							break;
						dir_dirty_hash.Remove (dir);
						--i;
					}
					if (i < dir_dirty.Count - 1)
						dir_dirty.RemoveRange (i+1, dir_dirty.Count - 1 - i);

					if (i == -1) // Everything disappeared
						return null;
				}

				// Find the first non-backed-off directory
				dir = null;
				for (i = 0; i < dir_dirty.Count; ++i) {
					dir = dir_dirty [i] as WatchedDirectory;
					if (dir.State == DirectoryState.Backoff) {
						dir = null;
					} else {
						dir_dirty.RemoveAt (0);
						dir_dirty_hash.Remove (dir);
						break;
					}
				}
				
				return dir;
			}
		}

		//////////////////////////////////////////////////////////////////////////

		private Hashtable dir_by_path = new Hashtable ();
		private Hashtable dir_by_wd = new Hashtable ();
		private ArrayList dir_array = new ArrayList ();

		public void Scan (string root)
		{
			log.Info ("Scanning {0}", root);

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			Queue queue = new Queue ();
			ArrayList scanned = new ArrayList ();
			int count = 0;

			queue.Enqueue (root);

			while (queue.Count > 0) {

				string path = (string) queue.Dequeue ();
				if (IgnoreFile (path))
					continue;

				++count;
			
				DirectoryInfo info = new DirectoryInfo (path);
				if (info.Exists) {
					WatchedDirectory dir = new WatchedDirectory (Driver, path);
					dir_by_path [dir.Path] = dir;
					dir_by_wd [dir.WatchDescriptor] = dir;
					dir_array.Add (dir);

					dir.State = DirectoryState.Scanning;					

					foreach (DirectoryInfo subinfo in info.GetDirectories ()) {
						if (! filter.Ignore (subinfo.FullName)
						    && ! FileSystem.IsSymLink (subinfo.FullName))
							queue.Enqueue (subinfo.FullName);
					}
					
					// Directories start out dirty
					AddDirtyDirectory (dir);

					scanned.Add (dir);
				}
			}

			foreach (WatchedDirectory dir in scanned)
				dir.State = DirectoryState.Watched;

			stopwatch.Stop ();

			log.Info ("Scanned {0} director{1} in {2}", 
				  count, count == 1 ? "y" : "ies", stopwatch);
			// FIXME: Do we need to re-run queries when we are fully started?
		}

		private void ScheduleCrawl ()
		{
			lock (dir_dirty) {
				if (dir_dirty.Count > 0) {
					Scheduler.Task task;
					task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlNextDirectory));
					task.Tag = "File System Crawler";
					task.Priority = Scheduler.Priority.Generator;
					task.Description = String.Format ("{0} directories need to be crawled",
									  dir_dirty.Count);

					ThisScheduler.Add (task, Scheduler.AddType.DeferToExisting);
				}
			}
		}

		private void CrawlNextDirectory (Scheduler.Task task)
		{
			WatchedDirectory dir;

			dir = GetNextDirtyDirectory ();

			if (dir != null) {
				Crawl (dir);
				dir.Dirty = false;
				
				task.Description = String.Format ("{0} directories need to be crawled",
								  dir_dirty.Count);
				task.Reschedule = true;
			}
		}

		//////////////////////////////////////////////////////////////////////////

		// Filter out hits where the files seem to no longer exist.
		override protected bool HitIsValid (Uri uri)
		{
			if (! uri.IsFile)
				return false;
			string path = uri.LocalPath;
			return File.Exists (path) || Directory.Exists (path);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Inotify-related code
		//

		private void OnInotifyEvent (int               wd,
					     string            path,
					     string            subitem,
					     Inotify.EventType type,
					     uint              cookie)
		{
			if (type == Inotify.EventType.QueueOverflow) {
				Inotify.Log.Warn ("The inotify queue overflowed!");
				// FIXME: Do the right thing
				return;
			}

			WatchedDirectory dir;
			dir = dir_by_wd [wd] as WatchedDirectory;
			if (dir == null)
				return;

			if (type != Inotify.EventType.Open)
				Inotify.Log.Debug ("FileSystemQueryable.OnInotifyEvent: type={0} path='{1}' subitem='{2}'",
						   type, path, subitem);

			if (! dir.ProcessEvent (type))
				return;

			string full_path = Path.Combine (path, subitem);

			// Handle a few simple event types
			switch (type) {

			case Inotify.EventType.CreateSubdir:
				Scan (full_path);
				return;

			case Inotify.EventType.QueueOverflow:
				// If the queue overflows, we can't make any
				// assumptions about the state of the file system.
				// FIXME: Do the right thing here.
				return;

			}

			// If events are coming in too fast, stop watching this directory
			// for a while.
			// FIXME: Do the right thing.

			// If we see activity in a dirty directory with the time set to DateTime.MaxValue,
			// mark it as dirty so that the crawl will happen sooner.
			if (dir.Dirty && dir.DirtyTime == DateTime.MaxValue) {
				log.Debug ("Flagging {0} as dirty", dir.Path);
				dir.Dirty = true;
				AddDirtyDirectory (dir);
			}

			// Handle the events that trigger indexing operations.
			switch (type) {

			case Inotify.EventType.DeleteSubdir:
			case Inotify.EventType.DeleteFile:
				Remove (full_path);
				break;

			case Inotify.EventType.CloseWrite:
				Add (full_path);
				break;

			}
		}

		//////////////////////////////////////////////////////////////////////////

		public void StartWorker ()
		{
			string home = Environment.GetEnvironmentVariable ("HOME");
			Scan (home);
			log.Info ("FileSystemQueryable start-up thread finished");
			
			// FIXME: Do we need to re-run queries when we are fully started?
		}

		public override void Start ()
		{
			base.Start ();

			Thread th = new Thread (new ThreadStart (StartWorker));
			th.Start ();
		}

		//////////////////////////////////////////////////////////////////////////

		public override string GetHumanReadableStatus ()
		{
			return "FIXME!";
		}
	}
}
	
