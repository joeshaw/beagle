//
// WatchedDirectory.cs
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
using System.IO;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public enum DirectoryState {
		Initializing, // Known, but still being set up
		Scanning,     // We are in the process of scanning this directory
		              // to set watches on the subdirectories.
		Watched,      // Directory is being watched
		Backoff,      // This directory is busy, so we aren't trying to keep up with it.
	}

	public class WatchedDirectory : IComparable {

		private string path;
		private int path_length;
		private int wd;
		private DirectoryState state = DirectoryState.Initializing;
		private FrequencyStatistics statistics = new FrequencyStatistics ();

		private bool is_dirty;
		private DateTime dirty_time;
		private DateTime last_crawl_time;
			
		public WatchedDirectory (LuceneDriver driver, string path)
		{
			DirectoryInfo info = new DirectoryInfo (path);

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

			if (ExtendedAttribute.CheckFingerprint (this.path, driver.Fingerprint))
				this.last_crawl_time = LastCrawlTime.Get (this.path);
			else
				this.last_crawl_time = DateTime.MinValue;

			// If a directory has obviously changed since the last
			// time we crawled it, move it up in the queue.  Setting
			// the last_crawl_time to Date.Value is the equivalent
			// of declaring it to be uncrawled.
			// (This is not a foolproof way of detecting changes,
			// because of the crazy unix rules for setting mtimes
			// on directories.)
			if (last_crawl_time < info.LastWriteTime) {
				last_crawl_time = DateTime.MinValue;
			}
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

		public bool Dirty {
			get { return is_dirty; }
			set {
				if (value && (! is_dirty || dirty_time == DateTime.MaxValue))
					dirty_time = DateTime.Now;
				is_dirty = value;
			}
		}

		public DateTime DirtyTime {
			get { return dirty_time; }
		}

		public DateTime LastCrawl {
			get { return last_crawl_time; }
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
			
			//Console.WriteLine ("{0} {1} {2}", path, state, event_type);
								
			switch (state) {

			case DirectoryState.Initializing:
				return false;

			case DirectoryState.Scanning:
				return event_type != InotifyEventType.Open;

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
				return cmp;

			// Then sort in increasing order of crawl-time
			cmp = last_crawl_time.CompareTo (other.last_crawl_time);
			if (cmp != 0)
				return cmp;

			// Sort in increasing order of path length.
			// We give priority to short directories over longer
			// ones, on the theory that stuff higher up in the
			// directory structure is more likely to be important.
			cmp = path_length.CompareTo (other.path_length);
			if (cmp != 0)
				return cmp;

			// Finally, sort in dictionary order.
			// This is sort of gratuitous, but it seems
			// nice to avoid ties.
			return path.CompareTo (other.path);
		}

	}
}
