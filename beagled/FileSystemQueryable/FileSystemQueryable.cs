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

	[QueryableFlavor (Name="Files", Domain=QueryDomain.Local)]
	public class FileSystemQueryable : LuceneQueryable, IFlowControl {

		private static Logger log = Logger.Get ("FileSystemQueryable");

		private enum DirectoryState {
			Initializing, // Known, but still being set up
			Scanning,     // We are in the process of scanning this directory
			              // to set watches on the subdirectories.
			Watched,      // Directory is being watched
			Backoff,      // This directory is busy, so we aren't trying to keep up with it.
		}

		private class WatchedDirectory : IComparable {

			private string path;
			private int path_length;
			private int wd;
			private string computed_hash;
			private DirectoryState state = DirectoryState.Initializing;
			private FrequencyStatistics statistics = new FrequencyStatistics ();

			private bool is_dirty;
			private DateTime dirty_time;
			
			public WatchedDirectory (string path)
			{
				this.path = path;
				this.wd = Inotify.Watch (path, 
							 InotifyEventType.Open
							 | InotifyEventType.CreateSubdir
							 | InotifyEventType.DeleteSubdir
							 | InotifyEventType.DeleteFile
							 | InotifyEventType.CloseWrite
							 | InotifyEventType.Ignored
							 | InotifyEventType.QueueOverflow);

				// compute the path length
				this.path_length = -1;

				this.computed_hash = DirectoryHash.ComputeHash (path);
				
				do {
					++this.path_length;
					path = System.IO.Path.GetDirectoryName (path);
				} while (path != null);

				// Directories start out dirty, but with their dirty
				// time set as far as possible into the future.
				// Since dirty directories are processed in order
				// of their dirty-times, this means that these are
				// always at the end of the queue.
				this.is_dirty = true;
				this.dirty_time = DateTime.MaxValue;
				
			}

			public string Path {
				get { return path; }
			}

			public int PathLength {
				get { return path_length; }
			}

			public int WatchDescriptor {
				get { return wd; }
			}

			public DirectoryState State {
				get { return state; }
				set { state = value; }
			}

			public FrequencyStatistics Statistics {
				get { return statistics; }
			}

			public bool Murky {
				get { return is_dirty && dirty_time == DateTime.MaxValue; }
			}

			public bool Dirty {
				get { return is_dirty; }
				set {
					if (value && (! is_dirty || dirty_time == DateTime.MaxValue))
						dirty_time = DateTime.Now;
					is_dirty = value;
				}
			}

			// Return true if we should continue processing the
			// event, false if we should filter it.
			public bool ProcessEvent (InotifyEventType event_type)
			{
				if (event_type == InotifyEventType.DeleteFile
				    || event_type == InotifyEventType.CloseWrite) {
					statistics.AddEvent ();
					//Console.WriteLine ("{0}: {1}", Path, statistics.EstimatedFrequency);
				}

								
				switch (state) {

				case DirectoryState.Initializing:
					return false;

				case DirectoryState.Scanning:
					return event_type == InotifyEventType.Open;

				case DirectoryState.Watched:
					return true;

				case DirectoryState.Backoff:
					if (event_type == InotifyEventType.DeleteFile
					    || event_type == InotifyEventType.CloseWrite) {
						Dirty = true;
						return false;
					}

					return true;
				}

				return true;
			}

			public int CompareTo (object obj)
			{
				WatchedDirectory other = obj as WatchedDirectory;
				if (other == null)
					return 1;

				// Dirty items always come first
				if (is_dirty != other.is_dirty) {
					return is_dirty ? -1 : +1;
				}
				
				int cmp;

				// Sort in increasing order of dirty-time
				cmp = dirty_time.CompareTo (other.dirty_time);
				if (cmp != 0)
					return 0;

				// Sort in increasing order of path length
				cmp = path_length.CompareTo (other.path_length);
				if (cmp != 0)
					return 0;

				// Finally, sort in dictionary order.
				// This is sort of gratuitous, but it seems
				// nice to avoid ties.
				return path.CompareTo (other.path);
			}

		} // end of class WatchedDirectory

		//////////////////////////////////////////////////////////////////////////

		private Hashtable dir_by_path = new Hashtable ();
		private Hashtable dir_by_wd = new Hashtable ();
		private ArrayList all_dirty = new ArrayList ();
		private FileNameFilter filter = new FileNameFilter ();


		public FileSystemQueryable () : base (Path.Combine (PathFinder.RootDir, "FileSystemIndex"))
		{
			Inotify.InotifyEvent += new InotifyHandler (OnInotifyEvent);

			// Hack: we implement some flow control for the driver in this class
			Driver.FlowControl = this as IFlowControl;
		}

		// Do a breadth-first traversal starting at root,
		// setting up watches as we go.
		private void Scan (string root)
		{
			Queue queue = new Queue ();
			int count = 0;

			log.Info ("Scanning {0}", root);
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();
			
			queue.Enqueue (root);

			while (queue.Count > 0) {
				++count;
				string path = (string) queue.Dequeue ();
			
				DirectoryInfo info = new DirectoryInfo (path);
				if (info.Exists) {
					WatchedDirectory dir = new WatchedDirectory (path);
					dir_by_path [dir.Path] = dir;
					dir_by_wd [dir.WatchDescriptor] = dir;
					AddDirty (dir);

					dir.State = DirectoryState.Scanning;					

					foreach (DirectoryInfo subinfo in info.GetDirectories ()) {
						if (! filter.Ignore (subinfo.FullName)
						    && ! FileSystem.IsSymLink (subinfo.FullName))
							queue.Enqueue (subinfo.FullName);
					}
					
					dir.State = DirectoryState.Watched;
				}
			}

			stopwatch.Stop ();

			log.Info ("Scanned {0} director{1} in {2}", 
				  count, count == 1 ? "y" : "ies", stopwatch);
		}
		

		//////////////////////////////////////////////////////////////////////////

		private void AddDirty (WatchedDirectory dir)
		{
			if (! dir.Dirty)
				return;

			int i = all_dirty.BinarySearch (dir);
			if (i < 0)
				i = ~i;
			all_dirty.Insert (i, dir);
		}

		private bool HaveDirty ()
		{
			int i = 0;
			while (i < all_dirty.Count) {
				WatchedDirectory dir;
				dir = all_dirty [i] as WatchedDirectory;
				if (dir.Dirty)
					return true;
				++i;
			}
			return false;
		}

		private WatchedDirectory GetNextDirty ()
		{
			// Non-dirty stuff can end up left in the queue,
			// so we need to filter it out.
			int i = 0;
			WatchedDirectory dir = null;
			while (i < all_dirty.Count) {
				dir = all_dirty [i] as WatchedDirectory;
				++i;
				if (dir.Dirty)
					break;
			}
			all_dirty.RemoveRange (0, i);
			return dir;
		}

		//////////////////////////////////////////////////////////////////////////

		// Filter out hits where the files seem to no longer exist.
		override protected bool HitIsValid (Hit hit)
		{
			string path = hit.Uri.LocalPath;
			return File.Exists (path) || Directory.Exists (path);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// File indexing
		//
		
		private bool ScheduleAddFile (string path, int priority)
		{
			if (! FileSystem.Exists (path))
				return false;

			if (FileSystem.IsSymLink (path))
				return false;

			if (filter.Ignore (path))
				return false;

			if (Driver.IsUpToDate (path))
				return false;

			Uri uri = UriFu.PathToFileUri (path);
			Indexable indexable = new FilteredIndexable (uri);

			Driver.ScheduleAdd (indexable, priority);

			return true;
		}

		private void ScheduleDeleteFile (string path, int priority)
		{
			Uri uri = UriFu.PathToFileUri (path);
			Driver.ScheduleDelete (uri, priority);
		}

		private bool ScheduleAddDirectory (string path, int priority)
		{
			if (! Directory.Exists (path))
				return false;

			if (FileSystem.IsSymLink (path))
				return false;

			bool did_work = false;
			
			DirectoryInfo dir = new DirectoryInfo (path);
			foreach (FileSystemInfo info in dir.GetFileSystemInfos ())
				did_work |= ScheduleAddFile (info.FullName, priority);

			return did_work;
			
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Flow Control
		//

		public void PostProcessSleepInit ()
		{

		}

		public double PostProcessSleepDuration ()
		{
			return 0;
		}

		public int PostProcessSleepThreshold ()
		{
			return 0;
		}

		public void IdleInit ()
		{

		}

		public double IdleTimeoutDuration ()
		{
			if (! HaveDirty ())
				return 0;

			double idle_time = 0;

			// FIXME: This is probably actually a bad thing
			// to do, since it will keep spinning up the
			// hard drive.
			if (SystemInformation.UsingBattery)
				idle_time += 60;

			if (SystemInformation.ScreenSaverRunning
			    || SystemInformation.InputIdleTime > /*5*60*/ 20)
				return idle_time;

			double load_avg = SystemInformation.LoadAverageOneMinute;
			idle_time += load_avg * 30; // fairly silly

			return idle_time;
		}

		public void IdleTimeout ()
		{
			WatchedDirectory dir = GetNextDirty ();
			if (dir == null)
				return;
			ScheduleAddDirectory (dir.Path, dir.Murky ? 0 : 100);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Handle Inotify Events
		//

		private void OnInotifyEvent (int              wd,
					     string           path,
					     string           subitem,
					     InotifyEventType type,
					     int              cookie)
		{
			// Clean up after removed/ignored directories
			if (type == InotifyEventType.Ignored) {
				dir_by_path.Remove (path);
				dir_by_wd.Remove (wd);
				return;
			}

			WatchedDirectory dir = dir_by_wd [wd] as WatchedDirectory;
			if (dir == null)
				return;

			string full_path = Path.Combine (path, subitem);
			if (filter.Ignore (full_path))
				return;

			if (! dir.ProcessEvent (type))
				return;

			//Console.WriteLine ("+++ {0} {1}", full_path, type);
			
			// Handle a few simple event types
			switch (type) {

			case InotifyEventType.CreateSubdir:
				Scan (full_path);
				return;

			case InotifyEventType.QueueOverflow:
				// If the queue overflows, we can't make any
				// assumptions about the state of the file system.
				// FIXME!
				return;
			}


			// If events are coming in too fast, stop watching this directory
			// for a while.

			// If we see activity in a murky directory, mark it as dirty so that
			// the crawl will happen sooner.
			if (dir.Murky) {
				dir.Dirty = true;
				AddDirty (dir);
			}

			// Handle the events that trigger indexing operations.
			switch (type) {

			case InotifyEventType.DeleteSubdir:
			case InotifyEventType.DeleteFile:
				ScheduleDeleteFile (full_path, 100);
				break;

			case InotifyEventType.CloseWrite:
				ScheduleAddFile (full_path, 100);
				break;

			}
		}

		//////////////////////////////////////////////////////////////////////////

		public void StartWorker ()
		{
			string home = Environment.GetEnvironmentVariable ("HOME");

			Scan (home);

			// Crawl a few important directories right away, just to be sure.
			// FIXME: This list shouldn't be hard-wired
			ScheduleAddDirectory (home, 0);
			ScheduleAddDirectory (Path.Combine (home, "Desktop"), 0);
			ScheduleAddDirectory (Path.Combine (home, "Documents"), 0);

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
			StringBuilder builder = new StringBuilder ();
			string str;

#if false
			builder.Append ("Crawl Queue:\n");
			str = String.Format ("{0} pending director{1} to crawl.\n",
					     crawlQ.PendingCount,
					     crawlQ.PendingCount == 1 ? "y" : "ies");
			builder.Append (str);
			crawlQ.GetHumanReadableStatus (builder);
#endif

			builder.Append ("\n\n");
			builder.Append ("Indexing Queue:\n");
			builder.Append (base.GetHumanReadableStatus ());

			return builder.ToString ();
		}


	}
}
	
