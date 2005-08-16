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

	[QueryableFlavor (Name="Files", Domain=QueryDomain.Local, RequireInotify=false)]
	public class FileSystemQueryable : LuceneQueryable {

		static public bool Debug = true;

		private const string OldExternalUriPropKey = LuceneCommon.UnindexedNamespace + "OldExternalUri";
		private const string SplitFilenamePropKey = "beagle:Filename";
		public const string ExactFilenamePropKey = "beagle:ExactFilename";
		public const string ParentDirUriPropKey = LuceneQueryingDriver.PrivateNamespace + "ParentDirUri";
		public const string IsDirectoryPropKey = LuceneQueryingDriver.PrivateNamespace + "IsDirectory";

		// History:
		// 1: Initially set to force a reindex due to NameIndex changes.
		// 2: Overhauled everything to use new lucene infrastructure.
		const int MINOR_VERSION = 2;

		private object big_lock = new object ();

		private IFileEventBackend event_backend;

		// This is the task that walks the tree structure
		private TreeCrawlTask tree_crawl_task;

		// This is the task that finds the next place that
		// needs to be crawled in the tree and spawns off
		// the appropriate IndexableGenerator.
		private FileCrawlTask file_crawl_task;

		private ArrayList roots = new ArrayList ();
		private ArrayList roots_by_path = new ArrayList ();

		// This is a cache of the external Uris of removed
		// objects, keyed on their internal Uris.  We use this
		// to remap Uris on removes.
		private Hashtable removed_uri_cache = UriFu.NewHashtable ();

		private FileNameFilter filter;
		
		// This is just a copy of the LuceneQueryable's QueryingDriver
		// cast into the right type for doing internal->external Uri
		// lookups.
		private LuceneNameResolver name_resolver;

		//////////////////////////////////////////////////////////////////////////

		private class PendingInfo {
			public Uri      Uri; // an internal uid: uri
			public string   Path;
			public bool     IsDirectory;
			public DateTime Mtime;

			// This is set when we are adding a subdirectory to a
			// given parent directory.
			public DirectoryModel Parent;

			public bool IsRoot { get { return Parent == null; } }
		}

		private Hashtable pending_info_cache = UriFu.NewHashtable ();

		//////////////////////////////////////////////////////////////////////////

		public FileSystemQueryable () : base ("FileSystemIndex", MINOR_VERSION)
		{
			// Set up our event backend
			if (Inotify.Enabled) {
                                Logger.Log.Debug ("Starting Inotify Backend");
                                event_backend = new InotifyBackend ();
                        } else {
                                Logger.Log.Debug ("Starting FileSystemWatcher Backend");
                                event_backend = new FileSystemWatcherBackend ();
                        }

			tree_crawl_task = new TreeCrawlTask (new TreeCrawlTask.Handler (AddDirectory));
			file_crawl_task = new FileCrawlTask (this);

			name_resolver = (LuceneNameResolver) Driver;
			PreloadDirectoryNameInfo ();

			// Setup our file-name filter
			filter = new FileNameFilter (this);

			// Do the right thing when paths expire
			DirectoryModel.ExpireEvent +=
				new DirectoryModel.ExpireHandler (ExpireDirectoryPath);
		}


		override protected IFileAttributesStore BuildFileAttributesStore ()
		{
			return new FileAttributesStore_Mixed (IndexDirectory, IndexFingerprint);
		}

		override protected LuceneQueryingDriver BuildLuceneQueryingDriver (string index_name,
										   int    minor_version,
										   bool   read_only_mode)
		{
			return new LuceneNameResolver (index_name, minor_version, read_only_mode);
		}

		public FileNameFilter Filter {
			get { return filter; }
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// This is where we build our Indexables
		//

		private static Indexable NewIndexable (Guid id)
		{
			// This used to do more.  Maybe it will again someday.
			Indexable indexable;
			indexable = new Indexable (GuidFu.ToUri (id));
			return indexable;
		}

		public static void AddStandardPropertiesToIndexable (Indexable indexable,
								     string    name, 
								     Guid      parent_id,
								     bool      mutable)
		{
			Property prop;

			prop = Property.NewKeyword (ExactFilenamePropKey, name);
			prop.IsMutable = mutable;
			indexable.AddProperty (prop);
			
			string str;
			str = Path.GetFileNameWithoutExtension (name);
			str = StringFu.FuzzyDivide (str);
			prop = Property.New (SplitFilenamePropKey, str);
			prop.IsMutable = mutable;
			indexable.AddProperty (prop);

			if (parent_id == Guid.Empty)
				return;
			
			str = GuidFu.ToUriString (parent_id);
			// We use the uri here to recycle terms in the index,
			// since each directory's uri will already be indexed.
			prop = Property.NewKeyword (ParentDirUriPropKey, str);
			prop.IsMutable = mutable;
			indexable.AddProperty (prop);
		}

		public static Indexable DirectoryToIndexable (string path, Guid id, Guid parent_id)
		{
			Indexable indexable;
			indexable = NewIndexable (id);
			indexable.MimeType = "inode/directory";
			indexable.NoContent = true;
			indexable.Timestamp = Directory.GetLastWriteTime (path);

			string name;
			if (parent_id == Guid.Empty)
				name = path;
			else
				name = Path.GetFileName (path);
			AddStandardPropertiesToIndexable (indexable, name, parent_id, true);

			Property prop;
			prop = Property.NewBool (IsDirectoryPropKey, true);
			prop.IsMutable = true; // we want this in the secondary index, for efficiency
			indexable.AddProperty (prop);

			return indexable;
		}

		public static Indexable FileToIndexable (string path,
							 Guid   id,
							 Guid   parent_id,
							 bool   crawl_mode)
		{
			Indexable indexable;
			indexable = NewIndexable (id);
			indexable.ContentUri = UriFu.PathToFileUri (path);
			indexable.Crawled = crawl_mode;
			indexable.Filtering = Beagle.IndexableFiltering.Always;

			AddStandardPropertiesToIndexable (indexable, Path.GetFileName (path), parent_id, true);

			return indexable;
		}

		private static Indexable NewRenamingIndexable (string name,
							       Guid   id,
							       Guid   parent_id,
							       string last_known_path)
		{
			Indexable indexable;
			indexable = new Indexable (GuidFu.ToUri (id));
			indexable.PropertyChangesOnly = true;

			AddStandardPropertiesToIndexable (indexable, name, parent_id, true);

			Property prop;
			prop = Property.NewKeyword (OldExternalUriPropKey,
						    StringFu.PathToQuotedFileUri (last_known_path));
			prop.IsMutable = true; // since this is a property-change-only Indexable
			indexable.AddProperty (prop);

			return indexable;
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Mapping from directory ids to paths
		//

		private Hashtable dir_models_by_id = new Hashtable ();
		private Hashtable name_info_by_id = new Hashtable ();

		// We fall back to using the name information in the index
		// until we've fully constructed our set of DirectoryModels.
		private void PreloadDirectoryNameInfo ()
		{
			ICollection all;
			all = name_resolver.GetAllDirectoryNameInfo ();
			foreach (LuceneNameResolver.NameInfo info in all)
				name_info_by_id [info.Id] = info;
		}

		// This only works for directories.
		private string UniqueIdToDirectoryName (Guid id)
		{
			DirectoryModel dir;
			dir = dir_models_by_id [id] as DirectoryModel;
			if (dir != null)
				return dir.FullName;

			LuceneNameResolver.NameInfo info;
			info = name_info_by_id [id] as LuceneNameResolver.NameInfo;
			if (info != null) {
				if (info.ParentId == Guid.Empty) // i.e. this is a root
					return info.Name;
				else {
					string parent_name;
					parent_name = UniqueIdToDirectoryName (info.ParentId);
					if (parent_name == null)
						return null;
					return Path.Combine (parent_name, info.Name);
				}
			}

			return null;
		}

		private string ToFullPath (string name, Guid parent_id)
		{
			// This is the correct behavior for roots.
			if (parent_id == Guid.Empty)
				return name;

			string parent_name;
			parent_name = UniqueIdToDirectoryName (parent_id);
			if (parent_name == null)
				return null;

			return Path.Combine (parent_name, name);
		}

		// This works for both files and directories.
		private string UniqueIdToFullPath (Guid id)
		{
			// First, check if it is a directory.
			string path;
			path = UniqueIdToDirectoryName (id);
			if (path != null)
				return path;

			// If not, try to pull name information out of the index.
			LuceneNameResolver.NameInfo info;
			info = name_resolver.GetNameInfoById (id);
			if (info == null)
				return null;
			return ToFullPath (info.Name, info.ParentId);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Directory-related methods
		//

		private Hashtable dir_models_by_path = new Hashtable ();

		private DirectoryModel GetDirectoryModelByPath (string path)
		{
			DirectoryModel dir;
			dir = dir_models_by_path [path] as DirectoryModel;
			if (dir != null)
				return dir;

			// Walk each root until we find the correct path
			foreach (DirectoryModel root in roots) {
				dir = root.WalkTree (path);
				if (dir != null) {
					dir_models_by_path [path] = dir;
					break;
				}
			}

			return dir;
		}

		private void ExpireDirectoryPath (string expired_path, Guid unique_id)
		{
			if (Debug) 
				Logger.Log.Debug ("Expired '{0}'", expired_path);

			DirectoryModel dir = (DirectoryModel) dir_models_by_id [unique_id];
			if (dir != null && dir.WatchHandle != null)
				event_backend.ForgetWatch (dir.WatchHandle);
			
			dir_models_by_path.Remove (expired_path);
			dir_models_by_id.Remove (unique_id);
		}

		public void AddDirectory (DirectoryModel parent, string name)
		{
			// Ignore the stuff we want to ignore.
			if (filter.Ignore (parent, name, true))
				return;

			if (parent != null && parent.HasChildWithName (name))
				return;

			string path;
			path = (parent == null) ? name : Path.Combine (parent.FullName, name);

			if (Debug)
				Logger.Log.Debug ("Adding directory '{0}'", path, name);

			if (! Directory.Exists (path)) {
				Logger.Log.Error ("Can't add directory: '{0}' does not exist", path);
				return;
			}

			FileAttributes attr;
			attr = FileAttributesStore.Read (path);

			// Note that we don't look at the mtime of a directory when
			// deciding whether or not to index it.
			bool needs_indexing = false;
			if (attr == null) {
				// If it has no attributes, it definitely needs
				// indexing.
				needs_indexing = true;
			} else {
				// Make sure that it still has the same name as before.
				// If not, we need to re-index it.
				// We can do this since we preloaded all of the name
				// info in the directory via PreloadDirectoryNameInfo.
				string last_known_name;
				last_known_name = UniqueIdToDirectoryName (attr.UniqueId);
				if (last_known_name != path) {
					Logger.Log.Debug ("'{0}' now seems to be called '{1}'", last_known_name, path);
					needs_indexing = true;
				}
			}
			
			// If we can't descend into this directory, we want to
			// index it but not build a DirectoryModel for it.
			// FIXME: We should do the right thing when a
			// directory's permissions change.
			bool is_walkable;
			is_walkable = DirectoryWalker.IsWalkable (path);
			if (! is_walkable)
				Logger.Log.Debug ("Can't walk '{0}'", path);
			
			if (needs_indexing)
				ScheduleDirectory (name, parent, attr, is_walkable);
			else if (is_walkable)
				RegisterDirectory (name, parent, attr);
		}

		public void AddRoot (string path)
		{
			path = StringFu.SanitizePath (path);
			Logger.Log.Debug ("Adding root: {0}", path);

			if (roots_by_path.Contains (path)) {
				Logger.Log.Error ("Trying to add an existing root: {0}", path);
				return;
			}

			// We need to have the path key in the roots hashtable
			// for the filtering to work as we'd like before the root 
			// is actually added.
			roots_by_path.Add (path);

			AddDirectory (null, path);
		}

		public void RemoveRoot (string path)
		{
			Logger.Log.Debug ("Removing root: {0}", path);

			if (! roots_by_path.Contains (path)) {
				Logger.Log.Error ("Trying to remove a non-existing root: {0}", path);
				return;
			}
				
			// Find our directory model for the root
			DirectoryModel dir;
			dir = GetDirectoryModelByPath (path);

			if (dir == null) {
				Logger.Log.Error ("Could not find directory-model for root: {0}", path);
				return;
			}

			// FIXME: Make sure we're emptying the crawler task of any sub-directories 
			// to the root we're removing. It's not a big deal since we do an Ignore-check
			// in there, but it would be nice.

			roots_by_path.Remove (path);
			roots.Remove (dir);

			// Clean out the root from our directory cache.
			RemoveDirectory (dir);
		}

		private void ScheduleDirectory (string         name,
						DirectoryModel parent,
						FileAttributes attr,
						bool           is_walkable)
		{
			string path;
			path = (parent == null) ? name : Path.Combine (parent.FullName, name);

			Guid id;
			id = (attr == null) ? Guid.NewGuid () : attr.UniqueId;

			Guid parent_id;
			parent_id = (parent == null) ? Guid.Empty : parent.UniqueId;

			DateTime last_crawl;
			last_crawl = (attr == null) ? DateTime.MinValue : attr.LastWriteTime;

			Indexable indexable;
			indexable = DirectoryToIndexable (path, id, parent_id);

			PendingInfo info;
			info = new PendingInfo ();
			info.Uri = indexable.Uri;
			info.Path = path;
			info.Parent = parent;
			info.Mtime  = last_crawl;

			// We only set the IsDirectory flag if it is actually
			// walkable.  The IsDirectory flag is what is used to
			// decide whether or not to call RegisterDirectory
			// in the PostAddHook.  Thus non-walkable directories
			// will be indexed but will not have DirectoryModels
			// created for them.
			info.IsDirectory = is_walkable;

			pending_info_cache [info.Uri] = info;

			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Delayed;
			ThisScheduler.Add (task);
		}

		private void RegisterDirectory (string name, DirectoryModel parent, FileAttributes attr)
		{
			string path;
			path = (parent == null) ? name : Path.Combine (parent.FullName, name);

			if (Debug)
				Logger.Log.Debug ("Registered directory '{0}' ({1})", path, attr.UniqueId);

			DirectoryModel dir;
			if (parent == null)
				dir = DirectoryModel.NewRoot (big_lock, path, attr);
			else
				dir = parent.AddChild (name, attr);

			if (Directory.GetLastWriteTime (path) > attr.LastWriteTime) {
				dir.State = DirectoryState.Dirty;
				Logger.Log.Debug ("'{0}' is dirty", path);
			}

			if (Debug) {
				if (dir.IsRoot)
					Logger.Log.Debug ("Created model '{0}'", dir.FullName);
				else
					Logger.Log.Debug ("Created model '{0}' with parent '{1}'", dir.FullName, dir.Parent.FullName);
			}

			// Add any roots we create to the list of roots
			if (dir.IsRoot)
				roots.Add (dir);

			// Add the directory to our by-id hash, and remove any NameInfo
			// we might have cached about it.
			dir_models_by_id [dir.UniqueId] = dir;
			name_info_by_id.Remove (dir.UniqueId);

			// Start watching the directory.
			dir.WatchHandle = event_backend.CreateWatch (path);
			
			// Schedule this directory for crawling.
			if (tree_crawl_task.Add (dir))
				ThisScheduler.Add (tree_crawl_task);

			// Make sure that our file crawling task is active,
			// since presumably we now have something new to crawl.
			ActivateFileCrawling ();
		}

		private void RemoveDirectory (DirectoryModel dir)
		{
			Uri uri;
			uri = GuidFu.ToUri (dir.UniqueId);

			// Cache a copy of our external Uri, so that we can
			// easily remap it in the PostRemoveHook.
			Uri external_uri;
			external_uri = UriFu.PathToFileUri (dir.FullName);
			removed_uri_cache [uri] = external_uri;

			// Calling Remove will expire the path names,
			// so name caches will be cleaned up accordingly.
			dir.Remove ();

			Scheduler.Task task;
			task = NewRemoveTask (GuidFu.ToUri (dir.UniqueId));
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}

		public void RemoveDirectory (string path)
		{
			DirectoryModel dir = GetDirectoryModelByPath (path);
				if (dir != null)
					RemoveDirectory (dir);
		}

		private void MoveDirectory (DirectoryModel dir, 
					    DirectoryModel new_parent, // or null if we are just renaming
					    string new_name)
		{
			// We'll need this later in order to generate the
			// right change notification.
			string old_path;
			old_path = dir.FullName;
			
			if (new_parent != null && new_parent != dir.Parent)
				dir.MoveTo (new_parent, new_name);
			else
				dir.Name = new_name;

			Guid parent_id;
			parent_id = dir.IsRoot ? Guid.Empty : dir.Parent.UniqueId;

			Indexable indexable;
			indexable = NewRenamingIndexable (new_name,
							  dir.UniqueId,
							  parent_id,
							  old_path);

			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Immediate;
			// Danger Will Robinson!
			// We need to use BlockUntilNoCollision to get the correct notifications
			// in a mv a b; mv b c; mv c a situation.
			ThisScheduler.Add (task, Scheduler.AddType.BlockUntilNoCollision);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// This code controls the directory crawl order
		//

		private DirectoryModel StupidWalk (DirectoryModel prev_best, DirectoryModel contender)
		{
			if (contender.NeedsCrawl) {
				if (prev_best == null || prev_best.CompareTo (contender) < 0)
					prev_best = contender;
			}

			foreach (DirectoryModel child in contender.Children)
				prev_best = StupidWalk (prev_best, child);

			return prev_best;
		}

		public DirectoryModel GetNextDirectoryToCrawl ()
		{
			DirectoryModel next_dir = null;
			
			foreach (DirectoryModel root in roots)
				next_dir = StupidWalk (next_dir, root);

			return next_dir;
		}

		public void DoneCrawlingOneDirectory (DirectoryModel dir)
		{
			if (! dir.IsAttached)
				return;

			FileAttributes attr;
			attr = FileAttributesStore.Read (dir.FullName);

			// We don't have to be super-careful about this since
			// we only use the FileAttributes mtime on a directory
			// to determine its initial state, not whether or not
			// its index record is up-to-date.
			attr.LastWriteTime = DateTime.Now;

			FileAttributesStore.Write (attr);
			dir.MarkAsClean ();
		}

		public void Recrawl (string path) 
		{
			// Try to find a directory model for the path specified
			// so that we can re-crawl it.
			DirectoryModel dir;
			dir = GetDirectoryModelByPath (path);

			bool path_is_registered = true;

			if (dir == null) {
				dir = GetDirectoryModelByPath (Path.GetDirectoryName (path));
				path_is_registered = false;

				if (dir == null) {
					Logger.Log.Debug ("Unable to get directory-model for path: {0}", path);
					return;
				}
			}
			
			Logger.Log.Debug ("Re-crawling {0}", dir.FullName);
			
			if (tree_crawl_task.Add (dir))
				ThisScheduler.Add (tree_crawl_task);
			
			if (path_is_registered)
				Recrawl_Recursive (dir, DirectoryState.PossiblyClean);

			ActivateFileCrawling ();
			ActivateDirectoryCrawling ();
		}

		public void RecrawlEverything ()
		{
			Logger.Log.Debug ("Re-crawling all directories");
			
			foreach (DirectoryModel root in roots)
				Recrawl_Recursive (root, DirectoryState.PossiblyClean);
			
			ActivateFileCrawling ();
			ActivateDirectoryCrawling ();
		}
		
		private void Recrawl_Recursive (DirectoryModel dir, DirectoryState state)
		{
			dir.State = state;
			tree_crawl_task.Add (dir);
			foreach (DirectoryModel sub_dir in dir.Children) 
				Recrawl_Recursive (sub_dir, state);
		}

		private void ActivateFileCrawling ()
		{
			if (! file_crawl_task.IsActive)
				ThisScheduler.Add (file_crawl_task);
		}

		private void ActivateDirectoryCrawling ()
		{
			if (! tree_crawl_task.IsActive)
				ThisScheduler.Add (tree_crawl_task);
		}
		
		//////////////////////////////////////////////////////////////////////////

		//
		// File-related methods
		//

		private enum RequiredAction {
			None,
			Index,
			Rename,
			Forget
		}
		
		private RequiredAction DetermineRequiredAction (DirectoryModel dir,
								string         name,
								FileAttributes attr,
								out string     last_known_path,
								out DateTime   mtime)
		{
			last_known_path = null;
			mtime = DateTime.MinValue;

			string path;
			path = Path.Combine (dir.FullName, name);

			if (Debug)
				Logger.Log.Debug ("*** What should we do with {0}?", path);

			if (filter.Ignore (dir, name, false)) {
				// If there are attributes on the file, we must have indexed
				// it previously.  Since we are ignoring it now, we should strip
				// any file attributes from it.
				if (attr != null) {
					if (Debug)
						Logger.Log.Debug ("*** Forget it: File is ignored but has attributes");
					return RequiredAction.Forget;
				}
				if (Debug)
					Logger.Log.Debug ("*** Do nothing: File is ignored");
				return RequiredAction.None;
			}

			if (attr == null) {
				if (Debug)
					Logger.Log.Debug ("*** Index it: File has no attributes");
				return RequiredAction.Index;
			}

			// FIXME: This does not take in to account that we might have a better matching filter to use now
			// That, however, is kind of expensive to figure out since we'd have to do mime-sniffing and shit.
			if (attr.FilterName != null && attr.FilterVersion > 0) {
				int current_filter_version;
				current_filter_version = FilterFactory.GetFilterVersion (attr.FilterName);

				if (current_filter_version > attr.FilterVersion) {
					if (Debug)
						Logger.Log.Debug ("*** Index it: Newer filter version found for filter {0}", attr.FilterName);
					return RequiredAction.Index;
				}
			}

			Mono.Posix.Stat stat;
			try {
				Mono.Posix.Syscall.stat (path, out stat);
			} catch (Exception ex) {
				Logger.Log.Debug ("Caught exception stat-ing {0}", path);
				Logger.Log.Debug (ex);
				return RequiredAction.None;
			}
			mtime = stat.MTime;

			if (! DatesAreTheSame (attr.LastWriteTime, mtime)) {
				if (Debug)
					Logger.Log.Debug ("*** Index it: MTime has changed");
				
				// If the file has been copied, it will have the
				// original file's EAs.  Thus we have to check to
				// make sure that the unique id in the EAs actually
				// belongs to this file.  If not, replace it with a new one.
				// (Thus touching & then immediately renaming a file can
				// cause its unique id to change, which is less than
				// optimal but probably can't be helped.)
				last_known_path = UniqueIdToFullPath (attr.UniqueId);
				if (path != last_known_path) {
					if (Debug)
						Logger.Log.Debug ("*** Name has also changed, assigning new unique id");
					attr.UniqueId = Guid.NewGuid ();
				}
				
				return RequiredAction.Index;
			}

			// If the inode ctime is different that the time we last
			// set file attributes, we might have been moved or copied.
			if (! DatesAreTheSame (attr.LastAttrTime, stat.CTime)) {
				if (Debug)
					Logger.Log.Debug ("*** CTime has changed, checking last known path");

				last_known_path = UniqueIdToFullPath (attr.UniqueId);

				if (last_known_path == null) {
					if (Debug)
						Logger.Log.Debug ("*** Index it: CTime has changed, but can't determine last known path");
					return RequiredAction.Index;
				}

				// If the name has changed but the mtime
				// hasn't, the only logical conclusion is that
				// the file has been renamed.
				if (path != last_known_path) {
					if (Debug)
						Logger.Log.Debug ("*** Rename it: CTime and path has changed");
					return RequiredAction.Rename;
				}
			}
			
			// We don't have to do anything, which is always preferable.
			if (Debug)
				Logger.Log.Debug ("*** Do nothing");
			return RequiredAction.None;	
		}

		// This works around a mono bug: the DateTimes that we get out of stat
		// don't correctly account for daylight savings time.  We declare the two
		// dates to be equal if:
		// (1) They actually are equal
		// (2) The first date is exactly one hour ahead of the second
		static private bool DatesAreTheSame (DateTime system_io_datetime, DateTime stat_datetime)
		{
			const double epsilon = 1e-5;
			double t = (system_io_datetime - stat_datetime).TotalSeconds;
			return Math.Abs (t) < epsilon || Math.Abs (t-3600) < epsilon;
		}

		// Return an indexable that will do the right thing with a file
		// (or null, if the right thing is to do nothing)
		public Indexable GetCrawlingFileIndexable (DirectoryModel dir, string name)
		{
			string path;
			path = Path.Combine (dir.FullName, name);

			FileAttributes attr;
			attr = FileAttributesStore.Read (path);
			
			RequiredAction action;
			string last_known_path;
			DateTime mtime;
			action = DetermineRequiredAction (dir, name, attr, out last_known_path, out mtime);

			if (action == RequiredAction.None)
				return null;

			Guid unique_id;
			if (attr != null)
				unique_id = attr.UniqueId;
			else
				unique_id = Guid.NewGuid ();
			
			Indexable indexable = null;

			switch (action) {

			case RequiredAction.Index:
				indexable = FileToIndexable (path, unique_id, dir.UniqueId, true);
				if (mtime == DateTime.MinValue)
					mtime = File.GetLastWriteTime (path);
				break;

			case RequiredAction.Rename:
				indexable = NewRenamingIndexable (name, unique_id, dir.UniqueId,
								  last_known_path);
				break;

			case RequiredAction.Forget:
				FileAttributesStore.Drop (path);
				
				break;
			}

			if (indexable != null) {
				PendingInfo info;
				info = new PendingInfo ();
				info.Uri = indexable.Uri;
				info.Path = path;
				info.IsDirectory = false;
				info.Mtime = mtime;
				info.Parent = dir;
				pending_info_cache [info.Uri] = info;
			}

			return indexable;
		}

		public void AddFile (DirectoryModel dir, string name)
		{
			string path;
			path = Path.Combine (dir.FullName, name);

			if (! File.Exists (path))
				return;
			
			if (filter.Ignore (dir, name, false))
				return;

			FileAttributes attr;
			attr = FileAttributesStore.Read (path);

			Guid unique_id;
			unique_id = (attr != null) ? attr.UniqueId : Guid.NewGuid ();

			Indexable indexable;
			indexable = FileToIndexable (path, unique_id, dir.UniqueId, false);

			PendingInfo info;
			info = new PendingInfo ();
			info.Uri = indexable.Uri;
			info.Path = path;
			info.IsDirectory = false;
			info.Mtime = File.GetLastWriteTime (path);
			info.Parent = dir;
			pending_info_cache [info.Uri] = info;

			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}

		public void RemoveFile (DirectoryModel dir, string name)
		{
			// FIXME: We might as well remove it, even if it was being ignore.
			// Right?

			Guid unique_id;
			unique_id = name_resolver.GetIdByNameAndParentId (name, dir.UniqueId);
			if (unique_id == Guid.Empty) {
				Logger.Log.Warn ("Couldn't find unique id for '{0}' in '{1}' ({2})",
						 name, dir.FullName, dir.UniqueId);
				return;
			}

			Uri uri, file_uri;
			uri = GuidFu.ToUri (unique_id);
			file_uri = UriFu.PathToFileUri (Path.Combine (dir.FullName, name));
			removed_uri_cache [uri] = file_uri;

			Scheduler.Task task;
			task = NewRemoveTask (uri);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}

		public void MoveFile (DirectoryModel old_dir, string old_name,
				      DirectoryModel new_dir, string new_name)
		{
			bool old_ignore, new_ignore;
			old_ignore = filter.Ignore (old_dir, old_name, false);
			new_ignore = filter.Ignore (new_dir, new_name, false);

			if (old_ignore && new_ignore)
				return;

			// If our ignore-state is changing, synthesize the appropriate
			// action.

			if (old_ignore && ! new_ignore) {
				AddFile (new_dir, new_name);
				return;
			}

			if (! old_ignore && new_ignore) {
				RemoveFile (new_dir, new_name);
				return;
			}

			string old_path;
			old_path = Path.Combine (old_dir.FullName, old_name);

			// We need to find the file's unique id.
			// We can't look at the extended attributes w/o making
			// assumptions about whether they follow around the
			// file (EAs) or the path (sqlite)... so we go straight
			// to the name resolver.
			
			Guid unique_id;
			unique_id = name_resolver.GetIdByNameAndParentId (old_name, old_dir.UniqueId);
			if (unique_id == Guid.Empty) {
				Logger.Log.Warn ("Couldn't find unique id for '{0}' in '{1}' ({2})",
						 old_name, old_dir.FullName, old_dir.UniqueId);
				return;
			}

			// FIXME: I think we need to be more conservative when we seen
			// events in a directory that has not been fully scanned, just to
			// avoid races.  i.e. what if we are in the middle of crawling that
			// directory and haven't reached this file yet?  Then the rename
			// will fail.
			Indexable indexable;
			indexable = NewRenamingIndexable (new_name,
							  unique_id,
							  new_dir.UniqueId,
							  old_path);
			
			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Immediate;
			// Danger Will Robinson!
			// We need to use BlockUntilNoCollision to get the correct notifications
			// in a mv a b; mv b c; mv c a situation.
			ThisScheduler.Add (task, Scheduler.AddType.BlockUntilNoCollision);
		}

		//////////////////////////////////////////////////////////////////////////
		
		// Configuration stuff

		public IList Roots {
			get {
				return roots_by_path;
			}
		}
		
		private void LoadConfiguration () 
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
			ArrayFu.IntersectListChanges (roots_wanted, Roots, out roots_to_add, out roots_to_remove);

			foreach (string root in roots_to_remove)
				RemoveRoot (root);

			foreach (string root in roots_to_add)
				AddRoot (root);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Our magic LuceneQueryable hooks
		//

		override protected void PostAddHook (IndexerAddedReceipt receipt)
		{
			// If we just changed properties, remap to our *old* external Uri
			// to make notification work out property.
			if (receipt.PropertyChangesOnly) {

				// FIXME: This linear search sucks --- we should
				// be able to use the fact that they are sorted.
				foreach (Property prop in receipt.Properties) {
					if (prop.Key == OldExternalUriPropKey) {
						receipt.Uri = UriFu.UriStringToUri (prop.Value);
						break;
					}
				}
				
				return;
			}

			PendingInfo info;
			info = pending_info_cache [receipt.Uri] as PendingInfo;
			pending_info_cache.Remove (receipt.Uri);

			// The parent directory might have run away since we were indexed
			if (info.Parent != null && !info.Parent.IsAttached)
				return;

			Guid unique_id;
			unique_id = GuidFu.FromUri (receipt.Uri);

			FileAttributes attr;
			attr = FileAttributesStore.ReadOrCreate (info.Path, unique_id);
			attr.Path = info.Path;
			attr.LastWriteTime = info.Mtime;
			
			attr.FilterName = receipt.FilterName;
			attr.FilterVersion = receipt.FilterVersion;
			
			if (info.IsDirectory) {
				string name;
				if (info.Parent == null)
					name = info.Path;
				else
					name = Path.GetFileName (info.Path);
				RegisterDirectory (name, info.Parent, attr);
			}

			FileAttributesStore.Write (attr);

			// Remap the Uri so that change notification will work properly
			receipt.Uri = UriFu.PathToFileUri (info.Path);
		}

		override protected void PostRemoveHook (IndexerRemovedReceipt receipt)
		{
			// Find the cached external Uri and remap the Uri in the receipt.
			// We have to do this to make change notification work.
			Uri external_uri;
			external_uri = removed_uri_cache [receipt.Uri] as Uri;
			if (external_uri == null)
				throw new Exception ("No cached external Uri for " + receipt.Uri);

			removed_uri_cache.Remove (receipt.Uri);
			
			receipt.Uri = external_uri;
		}

		private bool RemapUri (Hit hit)
		{
			string name, parent_id_uri;
			name = hit [ExactFilenamePropKey];
			if (name == null)
				return false;
			parent_id_uri = hit [ParentDirUriPropKey];
			if (parent_id_uri == null)
				return false;
			
			Guid parent_id;
			parent_id = GuidFu.FromUriString (parent_id_uri);
			
			string path;
			path = ToFullPath (name, parent_id);

			// Store the hit's internal uri in a property
			Property prop;
			prop = Property.NewKeyword ("beagle:InternalUri",
						    UriFu.UriToSerializableString (hit.Uri));
			hit.AddProperty (prop);

			hit.Uri = UriFu.PathToFileUri (path);

			return true;
		}

		// Hit filter: this handles our mapping from internal->external uris,
		// and checks to see if the file is still there.
		override protected bool HitFilter (Hit hit)
		{
			if (! RemapUri (hit))
				return false;

			string path;
			path = hit.Uri.LocalPath;

			bool is_directory = (hit.MimeType == "inode/directory");

			bool exists;
			if (is_directory)
				exists = Directory.Exists (path);
			else
				exists = File.Exists (path);

			// If the file doesn't exist, we do not schedule a removal and
			// return false.  This is to avoid "losing" files if they are
			// in a directory that has been renamed but which we haven't
			// scanned yet... if we dropped them from the index, they would
			// never get re-indexed (or at least not until the next time they
			// were touched) since they would still be stamped with EAs
			// indicating they were up-to-date.  And that would be bad.
			// FIXME: It would be safe if we were in a known state, right?
			// i.e. every DirectoryModel is clean.
			if (! exists)
				return false;

			// Fetch the parent directory model from our cache to do clever 
			// filtering to determine if we're ignoring it or not.
			DirectoryModel parent;
			parent = GetDirectoryModelByPath (Path.GetDirectoryName (path));

			// Check the ignore status of the hit
			if (filter.Ignore (parent, Path.GetFileName (path), is_directory))
				return false;

			return true;
		}

		override public string GetSnippet (string [] query_terms, Hit hit)
		{
			// Uri remapping from a hit is easy: the internal uri
			// is stored in a property.
			Uri uri;
			uri = UriFu.UriStringToUri (hit ["beagle:InternalUri"]);

			string path;
			path = TextCache.UserCache.LookupPathRaw (uri);

			if (path == null)
				return null;

			// If this is self-cached, use the remapped Uri
			if (path == TextCache.SELF_CACHE_TAG)
				path = hit.Uri.LocalPath;

			return SnippetFu.GetSnippetFromFile (query_terms, path);
		}

		override public void Start ()
		{
			base.Start ();
			
			event_backend.Start (this);

			LoadConfiguration ();

			Logger.Log.Debug ("Done starting FileSystemQueryable");
		}

		//////////////////////////////////////////////////////////////////////////

		// These are the methods that the IFileEventBackend implementations should
		// call in response to events.
		
		public void ReportEventInDirectory (string directory_name)
		{
			DirectoryModel dir;
			dir = GetDirectoryModelByPath (directory_name);

			// We only use this information to prioritize the order in which
			// we crawl directories --- so if this directory doesn't
			// actually need to be crawled, we can safely ignore it.
			if (! dir.NeedsCrawl)
				return;

			dir.LastActivityTime = DateTime.Now;

			Logger.Log.Debug ("Saw event in '{0}'", directory_name);
		}

		public void HandleAddEvent (string directory_name, string file_name, bool is_directory)
		{
			Logger.Log.Debug ("*** Add '{0}' '{1}' {2}", directory_name, file_name,
					  is_directory ? "(dir)" : "(file)");
			
			DirectoryModel dir;
			dir = GetDirectoryModelByPath (directory_name);
			if (dir == null) {
				Logger.Log.Warn ("HandleAddEvent failed: Couldn't find DirectoryModel for '{0}'", directory_name);
				return;
			}

			if (is_directory)
				AddDirectory (dir, file_name);
			else
				AddFile (dir, file_name);
		}

		public void HandleRemoveEvent (string directory_name, string file_name, bool is_directory)
		{
			Logger.Log.Debug ("*** Remove '{0}' '{1}' {2}", directory_name, file_name,
					  is_directory ? "(dir)" : "(file)");

			if (is_directory) {
				string path;
				path = Path.Combine (directory_name, file_name);

				DirectoryModel dir;
				dir = GetDirectoryModelByPath (path);
				if (dir == null) {
					Logger.Log.Warn ("HandleRemoveEvent failed: Couldn't find DirectoryModel for '{0}'", path);
					return;
				}

				dir.WatchHandle = null;
				RemoveDirectory (dir);
			} else {
				DirectoryModel dir;
				dir = GetDirectoryModelByPath (directory_name);
				if (dir == null) {
					Logger.Log.Warn ("HandleRemoveEvent failed: Couldn't find DirectoryModel for '{0}'", directory_name);
					return;
				}
				
				RemoveFile (dir, file_name);
			}
		}

		public void HandleMoveEvent (string old_directory_name, string old_file_name,
					     string new_directory_name, string new_file_name,
					     bool is_directory)
		{
			Logger.Log.Debug ("*** Move '{0}' '{1}' -> '{2}' '{3}' {4}",
					  old_directory_name, old_file_name,
					  new_directory_name, new_file_name,
					  is_directory ? "(dir)" : "(file)");

			if (is_directory) {
				DirectoryModel dir, new_parent;
				dir = GetDirectoryModelByPath (Path.Combine (old_directory_name, old_file_name));
				new_parent = GetDirectoryModelByPath (new_directory_name);
				MoveDirectory (dir, new_parent, new_file_name);
				return;
			} else {
				DirectoryModel old_dir, new_dir;
				old_dir = GetDirectoryModelByPath (new_directory_name);
				new_dir = GetDirectoryModelByPath (new_directory_name);
				MoveFile (old_dir, old_file_name, new_dir, new_file_name);
			}
		}

		public void HandleOverflowEvent ()
		{
			Logger.Log.Debug ("Queue overflows suck");
		}

	}
}
	
