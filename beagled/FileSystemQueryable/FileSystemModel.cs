//
// FileSystemModel.cs
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

using System;
using System.Collections;
using System.Text;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public class FileSystemModel : IFileAttributesStore {

		static public bool Debug = true;

		public enum State {
			Clean         = 0,
			PossiblyClean = 1, // It was clean last time we checked...
			Unknown       = 2,
			Dirty         = 3,
			Unscanned     = 4
		}

		public class Directory : IComparable {
			
			protected object big_lock;
			protected string name;
			protected Guid unique_id;
			protected string parent_name; // non-null only for roots of directory trees
			protected State state = State.Unscanned;
			protected object watch_handle;
			protected DateTime last_crawl_time;
			protected DateTime last_activity_time;

			protected string cached_full_name;
			protected int cached_depth = -1;

			protected Directory parent;
			protected Hashtable children = null;

			protected Directory (object big_lock)
			{
				this.big_lock = big_lock;
			}

			public string Name {
				get { return name; }
			}

			public string FullName {
				get { 
					lock (big_lock) {
						if (cached_full_name == null) {
							string directly_above = parent_name;
							if (directly_above == null && parent != null) {
								// FIXME: what if parent is null
								directly_above = parent.FullName;
							}
							if (directly_above == null)
								directly_above = "(null)";
							cached_full_name = System.IO.Path.Combine (directly_above, name);
						}
						return cached_full_name;
					}
				}
			}


			public bool FullNameIsCached { // Sort of hacky
				get { return cached_full_name != null; }
			}

			public Guid UniqueId {
				get { return unique_id; }
			}
		
			public int Depth {
				get {
					lock (big_lock) {
						if (cached_depth < 0) {
							if (parent == null)
								cached_depth = 0;
							else
								cached_depth = 1 + parent.Depth;
						}
						return cached_depth;
					}
				}
			}

			public State State {
				get { return state; }
			}

			public bool NeedsCrawl {
				get { 
					lock (big_lock) {
						return state == State.Dirty
							|| state == State.Unknown
							|| state == State.PossiblyClean
							|| state == State.Unscanned;
					}
				}
			}

			public object WatchHandle {
				get { return watch_handle; }
			}

			public bool IsWatched {
				get { return watch_handle != null; }
			}

			public Directory Parent {
				get { return parent; }
			}
			
			public bool IsRoot {
				get { return parent_name != null; }
			}

			public DateTime LastCrawlTime {
				get { return last_crawl_time; }
			}

			public DateTime LastActivityTime {
				get { return last_activity_time; }
			}

			///////////////////////////////////////////////////////////

			public Directory GetChildByName (string child_name)
			{
				lock (big_lock) {
					if (children != null)
						return children [child_name] as Directory;
					return null;
				}
			}

			public bool HasChildWithName (string child_name)
			{
				lock (big_lock) {
					return children != null && children.Contains (child_name);
				}
			}

			public int ChildCount {
				get { 
					lock (big_lock) {
						return children != null ? children.Count : 0;
					}
				}
			}

			// We want to always return an empty list if we have no children, not null
			static object [] empty_list = new object [0];
			public ICollection Children {
				get { 
					lock (big_lock) {
						return children != null ? children.Values : empty_list;
					}
				}
			}

			public int CompareTo_Unlocked (object obj)
			{
				Directory other = obj as Directory;
				if (other == null)
					return 1;

				int cmp;

				cmp = DateTime.Compare (this.last_activity_time,
							other.last_activity_time);
				if (cmp != 0)
					return cmp;

				cmp = this.state - other.state;
				if (cmp != 0)
					return cmp;

				cmp = DateTime.Compare (other.last_crawl_time,
							this.last_crawl_time);
				if (cmp != 0)
					return cmp;

				cmp = other.Depth - this.Depth;
				if (cmp != 0)
					return cmp;
				
				return other.Name.CompareTo (this.Name);
			}

			public int CompareTo (object obj)
			{
				lock (big_lock)
					return CompareTo_Unlocked (obj);
			}
			

			///////////////////////////////////////////////////////////

			public void Spew ()
			{
				lock (big_lock) {
					Console.WriteLine ("    Name: {0}", this.Name);
					Console.WriteLine ("   State: {0}", this.State);
					Console.WriteLine ("FullName: {0}", this.FullName);
					Console.WriteLine ("   Depth: {0}", this.Depth);
				}
			}
			
		}
	

		private class DirectoryPrivate : Directory {

			public DirectoryPrivate (object big_lock) : base (big_lock)
			{

			}

			public string RootParentName {
				get { return parent_name; }
			}

			public void SetState (State state)
			{
				this.state = state;
			}

			public void SetLastCrawlTime (DateTime dt)
			{
				this.last_crawl_time = dt;
			}

			public void SetWatchHandle (object handle)
			{
				this.watch_handle = handle;
			}

			public void SetFromFileAttributes (FileAttributes attr)
			{
				unique_id = attr.UniqueId;
				last_crawl_time = attr.LastIndexedTime;
			}

			public void ReportActivity ()
			{
				last_activity_time = DateTime.Now;
			}

			public void InitRoot (string parent_name, string name)
			{
				this.parent_name = parent_name;
				this.name = name;
			}

			public void Rename_Unlocked (string new_name)
			{
				if (name != new_name) {
					
					if (Parent != null) {
						DirectoryPrivate priv_parent = Parent as DirectoryPrivate;
						priv_parent.children.Remove (name);
						priv_parent.children [new_name] = this;
					} else {
						// FIXME: This is a root that is being renamed
						// We should handle that case.
					}

					name = new_name;
					ClearCachedName_Unlocked ();
				}
			}

			protected void ClearCached_Unlocked () 
			{
				if (cached_full_name != null || cached_depth >= 0) {
					cached_full_name = null;
					cached_depth = -1;
				}

				foreach (DirectoryPrivate priv in Children)
					priv.ClearCached_Unlocked ();
			}

			protected void ClearCachedName_Unlocked () 
			{
				if (cached_full_name != null) {
					cached_full_name = null;
					foreach (DirectoryPrivate priv in Children)
						priv.ClearCachedName_Unlocked ();
				}
			}

			public void Detatch_Unlocked ()
			{
				if (parent != null) {
					((DirectoryPrivate) parent).children.Remove (Name);
					parent = null;
					ClearCached_Unlocked ();
				}
			}
	
			public void AddChild_Unlocked (Directory new_child)
			{
				DirectoryPrivate new_child_priv = (DirectoryPrivate) new_child;

				if (new_child.IsRoot)
					throw new Exception ("Attempt to add a root directory as a child: " + new_child.FullName);
				if (new_child_priv.parent != null)
					throw new Exception ("Attempt to add an already-attached directory as a child: " + new_child.FullName);
				if (this == new_child) 
					throw new Exception ("Attempt to add " + Name + " as a child to itself");
				
				if (children != null && children.Contains (new_child.Name)) {
					string msg = String.Format ("Can't add '{0}' below '{1}', a subdir of that name already exists",
								    new_child.Name, FullName);
					throw new Exception (msg);
				}

				new_child_priv.parent = this;
				
				if (children == null)
					children = new Hashtable ();
				children [new_child.Name] = new_child;
			}

			public void PutDirectoriesInArray_Unlocked (ArrayList array)
			{
				if (NeedsCrawl)
					array.Add (this);
				if (this.children != null) {
					foreach (DirectoryPrivate subdir in this.children.Values)
						subdir.PutDirectoriesInArray_Unlocked (array);
				}
			}

			public void SetAllToUnknown_Unlocked ()
			{
				if (state == State.Clean)
					state = State.Unknown;
				if (this.children == null)
					return;
				foreach (DirectoryPrivate subdir in this.children.Values)
					subdir.SetAllToUnknown_Unlocked ();
			}
		}
		

		///////////////////////////////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////////


		object big_lock = new object ();
		ArrayList roots = new ArrayList ();
		Hashtable path_cache = new Hashtable ();
		Hashtable by_unique_id = new Hashtable ();
		ArrayList to_be_crawled = new ArrayList ();
		FileNameFilter filter;

		IFileEventBackend event_backend;

		UniqueIdStore unique_id_store;
		FileAttributesStore backing_store;
		FileAttributesStore fa_store;

		public FileSystemModel (string index_directory,
					string index_fingerprint,
					IFileEventBackend event_backend)
		{
			this.event_backend = event_backend;

			unique_id_store = new UniqueIdStore (index_directory, index_fingerprint);
			filter = new FileNameFilter (this);

			IFileAttributesStore backing_store_i;
			backing_store_i = new FileAttributesStore_Mixed (index_directory, index_fingerprint);
			backing_store = new FileAttributesStore (backing_store_i);

			// This let's us access our own implementation of the FileAttributesStore
			// using the convenience routines in FileAttributesStore.
			fa_store = new FileAttributesStore (this);
		}

		public void LoadConfiguration () 
		{
			if (Conf.Indexing.IndexHomeDir)
				AddRoot (PathFinder.HomeDir);
			
			foreach (string root in Conf.Indexing.Roots)
				AddRoot (root);

			Conf.Subscribe (typeof (Conf.IndexingConfig), OnConfigurationChanged);
		}

		private void OnConfigurationChanged (Conf.Section section)
		{
			ArrayList roots_wanted = new ArrayList (Conf.Indexing.Roots);
			
			if (Conf.Indexing.IndexHomeDir)
				roots_wanted.Add (PathFinder.HomeDir);
			
			IList roots_to_add, roots_to_remove;

			ArrayFu.IntersectListChanges (roots_wanted, RootsAsPaths, out roots_to_add, out roots_to_remove);

			foreach (string root in roots_to_remove)
				RemoveRoot (root);

			foreach (string root in roots_to_add)
				AddRoot (root);
		}

		// I'd rather not expose these, but we really can't avoid it.
		public UniqueIdStore UniqueIdStore {
			get { return unique_id_store; }
		}

		public FileAttributesStore FileAttributesStore {
			get { return fa_store; }
		}

		public ICollection Roots {
			// We return a copy of the list of roots to avoid locking issues.
			// In practice this shouldn't be a problem.
			get { lock (big_lock) return roots.Clone () as ArrayList; }
		}

		private IList RootsAsPaths {
			get {
				ArrayList roots = new ArrayList ();

				foreach (Directory root in Roots) {
					roots.Add (root.FullName);
				}

				return roots;
			}
		}

		public Directory AddRoot (string path)
		{
			// Remove trailing directory separators, which cause Path.FileName
			// to return the empty string.
			// (The below would be a stupid way to implement this if we ever expected
			// to have to remove multiple trailing separators.)
			while (path.Length > 0 && path [path.Length-1] == System.IO.Path.DirectorySeparatorChar)
				path = path.Substring (0, path.Length-1);

			DirectoryPrivate root = new DirectoryPrivate (big_lock);
			root.InitRoot (System.IO.Path.GetDirectoryName (path), 
				       System.IO.Path.GetFileName (path));
			by_unique_id [root.UniqueId] = root;
			
			FileAttributes attr = backing_store.ReadOrCreate (path);
			root.SetFromFileAttributes (attr);
			unique_id_store.AddRoot (attr.UniqueId, path, true);
			
			lock (big_lock) {
				// FIXME: We also should make sure the path is not a parent or child
				// of any existing root.
				foreach (Directory existing_root in roots)
					if (existing_root.FullName == root.FullName)
						return existing_root;

				Logger.Log.Debug ("Adding root {0}", path);
				roots.Add (root);
				to_be_crawled.Add (root);

				path_cache [root.FullName] = root;
			}

			ScanSubdirs (root);
			EnsureWatched (root);
			NeedsCrawlEvent (this);
			return root;
		}

		public bool RemoveRoot (string path) 
		{
			return RemoveRoot (GetDirectoryByPath (path));
		}

		public bool RemoveRoot (Directory root) 
		{
			DirectoryPrivate priv = (DirectoryPrivate) root;
			
			lock (big_lock) {
				if (!roots.Contains (root))
					return false;

				Logger.Log.Debug ("Removing root {0}", root.FullName);

				roots.Remove (root);
				RecursivelyRemove_Unlocked (priv);
			}
			
			return true;
		}

		///////////////////////////////////////////////////////////////////////////

		public Directory GetDirectoryByUniqueId (Guid uid)
		{
			return by_unique_id [uid] as Directory;
		}

		public Directory GetDirectoryByPath (string path)
		{
			// FIXME: We should probably canonize the path and
			// make sure that it is not relative.
			lock (big_lock) {
				Directory dir = path_cache [path] as Directory;
				if (dir == null) {

					// Find the directory by walking the appropriate tree

					string orig_path = path;
					
					// First, split the path up
					ArrayList path_parts = new ArrayList ();
					while (path != null) {
						string part = System.IO.Path.GetFileName (path);
						if (part.Length == 0) {
							path_parts.Add (path);
							break;
						}
						path = System.IO.Path.GetDirectoryName (path);
						path_parts.Add (part);
					}
					path_parts.Reverse ();


					int i = 0;

					// FIXME: FH
					
					// Next, find the correct root
					path = "";
					for (i = 0; i < path_parts.Count && dir == null; ++i) {
						path = System.IO.Path.Combine (path, (string) path_parts [i]);
						foreach (Directory root in roots) {
							if (root.FullName == path) {
								dir = root;
								break;
							}
						}
					}

					// Now walk down the root to find the directory
					for (; i < path_parts.Count && dir != null; ++i) {
						dir = dir.GetChildByName ((string) path_parts [i]);
					}

					// If we found it, cache it
					if (dir != null)
						path_cache [orig_path] = dir;
				}

				return dir;
			}
		}

		private void RecursivelyRemove_Unlocked (DirectoryPrivate priv)
		{
			if (priv.WatchHandle != null)
				event_backend.ForgetWatch (priv.WatchHandle);				

			if (priv.FullNameIsCached)
				path_cache.Remove (priv.FullName);

			to_be_crawled.Remove (priv);
			by_unique_id.Remove (priv.UniqueId);
			unique_id_store.Drop (priv.UniqueId);

			foreach (Directory subdir in new ArrayList (priv.Children))
				RecursivelyRemove_Unlocked ((DirectoryPrivate) subdir);

			priv.Detatch_Unlocked ();
		}

		private void RecursivelyRemoveFromPathCache_Unlocked (DirectoryPrivate priv)
		{
			if (priv.FullNameIsCached) {
				path_cache.Remove (priv.FullName);
				foreach (Directory subdir in priv.Children)
					RecursivelyRemoveFromPathCache_Unlocked ((DirectoryPrivate) subdir);
			}
		}

		///////////////////////////////////////////////////////////////////////////

		// This works around a mono bug: the DateTimes that we get out of stat
		// don't correctly account for daylight savings time.  We declare the two
		// dates to be equal if:
		// (1) They actually are equal
		// (2) The first date is exactly one hour ahead of the second
		static private bool DatesAreTheSame (DateTime system_io_datetime, DateTime stat_datetime)
		{
			double t = (system_io_datetime - stat_datetime).TotalSeconds;
			return Math.Abs (t) < 1e-5 || Math.Abs (t-3600) < 1e-5;
		}

		public enum RequiredAction {
			None,
			Index,
			Rename
		}
		
		// When we return RequiredAction.Rename, previous_path is set to the last
		// known name.  Otherwise it is null.
		public RequiredAction DetermineRequiredAction (string path, out string previous_path)
		{
			if (Debug)
				Logger.Log.Debug ("*** What should we do with {0}?", path);
			previous_path = null;

			if (Ignore (path)) {
				if (Debug)
					Logger.Log.Debug ("*** Ignoring {0}", path);
				return RequiredAction.None;
			}

			FileAttributes attr = this.backing_store.Read (path);
			if (attr == null) {
				if (Debug)
					Logger.Log.Debug ("*** No attributes on {0}", path);
				return RequiredAction.Index;
			}

			Mono.Posix.Stat stat;
			try {
				Mono.Posix.Syscall.stat (path, out stat);
			} catch (Exception ex) {
				Logger.Log.Debug ("Caught exception stating {0}", path);
				Logger.Log.Debug (ex);
				return RequiredAction.None;
			}

			// If the file has changed since we put on the
			// attributes, index.
			if (! DatesAreTheSame (attr.LastWriteTime, stat.MTime)) {
				if (Debug)
					Logger.Log.Debug ("*** mtime has changed on {0}", path);
				
				// If the mtime has changed and the path doesn't match the
				// unique ID, the file was probably copied.  Drop the file attributes
				// so that a new unique ID will be assigned when we index.
				if (PathFromUid (attr.UniqueId) != path)
					this.backing_store.Drop (path);

				return RequiredAction.Index;
			}

			// If the inode data has changed since it was last
			// indexed, we might have been moved or copied.
			if (! DatesAreTheSame (attr.LastIndexedTime, stat.CTime)) {
				string path_from_uid = PathFromUid (attr.UniqueId);
				if (Debug)
					Logger.Log.Debug ("CTime check {0} {1} '{2}'", attr.LastIndexedTime, stat.CTime, path_from_uid);
				if (path_from_uid == null) {
					if (Debug)
						Logger.Log.Debug ("*** Unfamiliar Uid, indexing {0}", path);
					return RequiredAction.Index;
				} else if (path_from_uid != path) {
					
					// Check to see the file mentioned in path_from_uid still exists.
					// If so, path_from_uid is probably a copy of path and should be
					// indexed separately.
					// FIXME: This probably shouldn't happen --- if it is a copy, then the
					// mtime must have changed, right?  Maybe path_from_uid is actually the
					// copy, and we should strip it's attrs and treat this as a rename.

					bool looks_like_copy = false;
					try {
						FileAttributes other_attr = this.Read (path_from_uid);
						if (other_attr != null && other_attr.UniqueId == attr.UniqueId) {
							Logger.Log.Debug ("*** '{0}' looks like a copy of '{1}'", path, path_from_uid);
							looks_like_copy = true;
						}
					} catch (Exception ex) {
						Logger.Log.Debug ("*** Caught exception while inspecting '{0}'", path_from_uid);
						Logger.Log.Debug (ex);
					}

					// If we think this is a copy, clear out the attributes and treat it like
					// a new file.
					if (looks_like_copy) {
						if (Debug)
							Logger.Log.Debug ("*** Stripping attributes from {0} and requesting new indexing", path);
						this.backing_store.Drop (path);
						return RequiredAction.Index;
					}
					
					if (Debug)
						Logger.Log.Debug ("*** Path has changed, renaming {0} => {1}", previous_path, path);
					previous_path = path_from_uid;
					return RequiredAction.Rename;
				}
			}

			// FIXME: This does not take in to account that we might have a better matching filter to use now
			// That, however, is kind of expensive to figure out since we'd have to do mime-sniffing and shit.
			if (attr.FilterName != null && attr.FilterVersion > 0) {
				int current_filter_version = FilterFactory.GetFilterVersion (attr.FilterName);

				if (current_filter_version > attr.FilterVersion) {
					if (Debug)
						Logger.Log.Debug ("*** Newer filter version found for filter {0}, re-indexing", attr.FilterName);
					return RequiredAction.Index;
				}
			}

			if (Debug)
				Logger.Log.Debug ("*** Doing nothing to {0}", path);

			return RequiredAction.None;
		}

		// We use this to test whether or not we need to work around
		// http://bugzilla.ximian.com/show_bug.cgi?id=71214
		private static bool MonoWillBreakThisPath (string path)
		{
			Uri uri = UriFu.PathToFileUri (path);
			return uri.LocalPath != path; // This assumes that the path is absolute.
		}
		
		public bool Ignore (string path)
		{
			return filter.Ignore (path) || FileSystem.IsSymLink (path) || MonoWillBreakThisPath (path);
		}

		///////////////////////////////////////////////////////////////////////////
		
		public bool ScanSubdirs (Directory dir)
		{
			bool ret = true;
			lock (big_lock) {
				DirectoryPrivate priv = (DirectoryPrivate) dir;

				ArrayList known_children = null;
				if (dir.State != State.Unscanned)
					known_children = new ArrayList (dir.Children);

				System.IO.DirectoryInfo info = new System.IO.DirectoryInfo (priv.FullName);

				// It's the call to GetDirectoryInfos() that may
				// trigger the exception caught below.
				try {
					foreach (System.IO.DirectoryInfo subinfo in DirectoryWalker.GetDirectoryInfos (info)) {
						if (known_children != null)
							known_children.Remove (subinfo.Name);

						if (Ignore (subinfo.FullName))
							continue;

						Directory child = priv.GetChildByName (subinfo.Name);

						// We don't know about the child, in which case we need to add it
						if (child == null)
							AddChild_Unlocked (priv, subinfo.Name);

						// Or, we already know about the child, but we might want to scan it anyway
						else if (child.NeedsCrawl && !to_be_crawled.Contains (child))
							to_be_crawled.Add (child);
					}
				} catch (System.IO.DirectoryNotFoundException e) {
					Logger.Log.Warn ("Skipping over {0}: {1}", priv.FullName, e.Message);
					RecursivelyRemove_Unlocked (priv);
					ret = false;
				}

				if (known_children != null) {
					foreach (string lost_child_name in known_children) {
						Directory lost_child = priv.GetChildByName (lost_child_name);
						RecursivelyRemove_Unlocked ((DirectoryPrivate) lost_child);
					}
				}
			}

			return ret;
		}

		///////////////////////////////////////////////////////////////////////////

		public delegate void NeedsCrawlHandler (FileSystemModel source);
		
		public event NeedsCrawlHandler NeedsCrawlEvent;

		public Directory GetNextDirectoryToCrawl ()
		{
			Directory next_to_crawl = null;
			lock (big_lock) {
				foreach (DirectoryPrivate candidate in to_be_crawled)
					if (next_to_crawl == null || candidate.CompareTo_Unlocked (next_to_crawl) > 0)
						next_to_crawl = candidate;
			}

			return next_to_crawl;
		}

		public int GetUncrawledCounts ()
		{
			return to_be_crawled.Count;
		}

		public ICollection GetAllDirectories ()
		{
			ArrayList array = new ArrayList ();
			lock (big_lock) {
				foreach (DirectoryPrivate root in roots)
					root.PutDirectoriesInArray_Unlocked (array);
				array.Sort ();
				array.Reverse ();
			}
			return array;
		}

		public void MarkAsCrawled (Directory dir, DateTime crawl_time)
		{
			DirectoryPrivate priv = (DirectoryPrivate) dir;

			lock (big_lock) {
				to_be_crawled.Remove (priv);
				if (! priv.NeedsCrawl)
					return;

				// FIXME: What if the directory changes between now and the
				// crawl time... there is a race here.
				priv.SetLastCrawlTime (crawl_time);

				if (priv.IsWatched)
					// If we've crawled while having watches in place, the
					// directory is definately clean
					priv.SetState (State.Clean);
				else
					// Unwatched directory can never be clean
					priv.SetState (State.PossiblyClean);

				FileAttributes attr = backing_store.ReadOrCreate (priv.FullName);
				attr.LastIndexedTime = priv.LastCrawlTime;

				// FIXME: We should check the return value and make sure that
				// the write succeeds.  (But what is the right behavior if it
				// fails?)
				backing_store.Write (attr);
				
			}
		}

		public void ReportActivity (Directory dir)
		{
			lock (big_lock) {
				((DirectoryPrivate) dir).ReportActivity ();
			}
		}

		// This is used by the FSW backend when a subdirectory
		// below a watched directory reports a change.
		public void ReportChanges (Directory dir)
		{
			lock (big_lock) {
				DirectoryPrivate priv = (DirectoryPrivate) dir;
				priv.SetState (State.Dirty);
				priv.ReportActivity ();
			}
		}

		// Set entire filesystem state to unknown, and fire a recrawl operation
		public void SetAllToUnknown ()
		{
			lock (big_lock) {
				foreach (DirectoryPrivate root in roots)
					root.SetAllToUnknown_Unlocked ();
			}

			NeedsCrawlEvent (this);
		}

		// Create watches if not already present
		public void EnsureWatched (Directory dir)
		{
			lock (big_lock) {
				if (dir.IsWatched)
					return;

				DirectoryPrivate priv = (DirectoryPrivate) dir;
				priv.SetWatchHandle (event_backend.CreateWatch (priv.FullName));
			}
		}

		public void DropOpenWatch (Directory dir)
		{
			lock (big_lock) {
				if (dir.IsWatched)
					return;

				DirectoryPrivate priv = (DirectoryPrivate) dir;
				priv.SetWatchHandle (event_backend.DropOpenWatch (priv.FullName, priv.WatchHandle));
			}
		}

		///////////////////////////////////////////////////////////////////////////

		public void MarkAsFiltered (string path, string filter_name, int filter_version)
		{
			FileAttributes attr = backing_store.ReadOrCreate (path);

			if (attr == null)
				return;

			attr.FilterName = filter_name;
			attr.FilterVersion = filter_version;

			try {
				Write (attr);
			} catch (Exception ex) {
				Logger.Log.Error ("Error writing filtered status attributes: {0}", path);
				Logger.Log.Error (ex);
			}
		}

		///////////////////////////////////////////////////////////////////////////

		private void AddChild_Unlocked (DirectoryPrivate parent, string child_name)
		{
			DirectoryPrivate child = new DirectoryPrivate (big_lock);
			child.Rename_Unlocked (child_name);
			parent.AddChild_Unlocked (child);
			by_unique_id [child.UniqueId] = child;

			FileAttributes attr = backing_store.ReadOrCreate (child.FullName);
			child.SetFromFileAttributes (attr);
			unique_id_store.Add (child.UniqueId, parent.UniqueId, child.Name, true);
			to_be_crawled.Add (child);
			EnsureWatched (child);
		}

		public void AddChild (Directory parent, string child_name)
		{
			DirectoryPrivate priv = (DirectoryPrivate) parent;
			lock (big_lock) {
				AddChild_Unlocked (priv, child_name);
			}
		}

		public void Delete (string path)
		{
			Directory dir = GetDirectoryByPath (path);
			if (dir != null)
				Delete (dir);
		}

		public void Delete (Directory dir)
		{			
			DirectoryPrivate priv = (DirectoryPrivate) dir;
			
			lock (big_lock) {
				RecursivelyRemove_Unlocked (priv);
			}
		}

		public void Rename (Directory dir, string new_name)
		{
			DirectoryPrivate priv = (DirectoryPrivate) dir;

			lock (big_lock) {
				RecursivelyRemoveFromPathCache_Unlocked (priv);
				priv.Rename_Unlocked (new_name);

				// Write the new name out to the store
				unique_id_store.Add (priv.UniqueId, 
						     priv.Parent != null ? priv.Parent.UniqueId : Guid.Empty,
						     new_name,
						     true);
			}
		}

		public void Move (Directory dir, Directory new_parent)
		{
			DirectoryPrivate priv = (DirectoryPrivate) dir;
			DirectoryPrivate new_parent_priv = (DirectoryPrivate) new_parent;
			
			lock (big_lock) {
				RecursivelyRemoveFromPathCache_Unlocked (priv);
				priv.Detatch_Unlocked ();
				new_parent_priv.AddChild_Unlocked (priv);

				// Write the new parent dir out to the store
				unique_id_store.Add (priv.UniqueId, new_parent.UniqueId, priv.Name, true);
			}
		}

		public void DoSpew (Directory dir)
		{
			StringBuilder builder = new StringBuilder ();
			builder.Append (dir.IsRoot ? ">>> " : "    ");
			builder.Append (' ', dir.Depth * 4);
			builder.Append (dir.IsRoot ? dir.FullName : dir.Name);
			Console.WriteLine (builder.ToString ());
			foreach (Directory subdir in dir.Children)
				DoSpew (subdir);
		}

		public void Spew ()
		{
			lock (big_lock) {
				foreach (Directory dir in roots) {
					DoSpew (dir);
					Console.WriteLine ();
				}
			}
		}

		//////////////////////////////////////////////////////////////////////////////////

		public string PathFromUid (Guid uid)
		{
			return unique_id_store.GetPathById (uid);
		}

		public Guid PathToUid (string path)
		{
			Guid unique_id;

			// FIXME: This is probably racey.
			if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path)) {
				FileAttributes attr;
				attr = backing_store.ReadOrCreate (path);
				unique_id = attr.UniqueId;

				if (! unique_id_store.IsCached (attr.UniqueId)) {
					string dir_name = System.IO.Path.GetDirectoryName (attr.Path);
					string file_name = System.IO.Path.GetFileName (attr.Path);
					Directory dir = GetDirectoryByPath (dir_name);
					unique_id_store.Add (attr.UniqueId, dir.UniqueId, file_name, false);
				}
			} else {
				// Maybe the file got deleted.  If so, try to get it from
				// the unique id store.
				string dir_name = System.IO.Path.GetDirectoryName (path);
				string file_name = System.IO.Path.GetFileName (path);
				Directory dir = GetDirectoryByPath (dir_name);
				unique_id = unique_id_store.GetIdByNameAndParentId (file_name, dir.UniqueId);
			}

			return unique_id;
		}
		
		// Map between internal and external Uris.

		public Uri PathToInternalUri (string path)
		{
			return GuidFu.ToUri (PathToUid (path));
		}

		public Uri ToInternalUri (Uri external_uri)
		{
			return PathToInternalUri (external_uri.LocalPath);
		}

		public Uri FromInternalUri (Uri internal_uri)
		{
			return unique_id_store.GetFileUriByUidUri (internal_uri);
		}

		public bool InternalUriIsValid (Uri internal_uri)
		{
			string path = unique_id_store.GetPathByUidUri (internal_uri);

			if (path == null)
				return false;

			if (Ignore (path))
				return false;
			
			return System.IO.File.Exists (path) || System.IO.Directory.Exists (path);
		}

		//////////////////////////////////////////////////////////////////////////////////

		//
		// Implementation of IFileAttributesStore
		//

		public FileAttributes Read (string path)
		{
			FileAttributes attr;
			attr = backing_store.Read (path);
			return attr;
		}

		public bool Write (FileAttributes attr)
		{
			// Excludes might have been modified after the indexing process
			// was started.
			if (Ignore (attr.Path))
				return false;

			bool write_rv = backing_store.Write (attr);

			// Since directories are always explicitly cached,
			// this check is equivalent to checking that
			// attr.Path is a file and not a directory.
			if (! unique_id_store.IsCached (attr.UniqueId)) {
				// When writing out our FileAttributes, add the
				// file and unique id to the UniqueIdStore.
				string dir_name = System.IO.Path.GetDirectoryName (attr.Path);
				string file_name = System.IO.Path.GetFileName (attr.Path);
				Directory dir = GetDirectoryByPath (dir_name);
				unique_id_store.Add (attr.UniqueId, dir.UniqueId, file_name, false);
			}

			return write_rv;
		}

		public void Drop (string path)
		{
			Guid unique_id = PathToUid (path);
			unique_id_store.Drop (unique_id);

			backing_store.Drop (path);
		}

		//////////////////////////////////////////////////////////////////////////////////

		public void DropUid (Guid unique_id)
		{
			unique_id_store.Drop (unique_id);
		}
		
	}
}
