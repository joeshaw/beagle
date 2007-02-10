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
	[PropertyKeywordMapping (Keyword="extension", PropertyName="beagle:FilenameExtension", IsKeyword=true, Description="File extension, e.g. extension:jpeg. Use extension: to search in files with no extension.")]
	[PropertyKeywordMapping (Keyword="ext", PropertyName="beagle:FilenameExtension", IsKeyword=true, Description="File extension, e.g. ext:jpeg. Use ext: to search in files with no extension.")]
	public class FileSystemQueryable : LuceneQueryable {

		static public new bool Debug = false;

		// History:
		// 1: Initially set to force a reindex due to NameIndex changes.
		// 2: Overhauled everything to use new lucene infrastructure.
		// 3: Switched to UTC for all times, changed the properties a bit.
		// 4: Changed the key of TextFilenamePropKey to beagle:Filename - it might be useful in clients.
		//    Make SplitFilenamePropKey unstored
		// 5: Keyword properies in the private namespace are no longer lower cased; this is required to
		//    offset the change in LuceneCommon.cs
		const int MINOR_VERSION = 5;

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

		private FileNameFilter filter;
		
		// This is just a copy of the LuceneQueryable's QueryingDriver
		// cast into the right type for doing internal->external Uri
		// lookups.
		private LuceneNameResolver name_resolver;

		//////////////////////////////////////////////////////////////////////////

		private Hashtable cached_uid_by_path = new Hashtable ();

		//////////////////////////////////////////////////////////////////////////

		public FileSystemQueryable () : base ("FileSystemIndex", MINOR_VERSION)
		{
			if (! Debug)
				Debug = (Environment.GetEnvironmentVariable ("BEAGLE_DEBUG_FSQ") != null);

			// Set up our event backend
			if (Inotify.Enabled) {
                                Logger.Log.Debug ("Starting Inotify FSQ file event backend");
                                event_backend = new InotifyBackend ();
                        } else {
                                Logger.Log.Debug ("Creating null FSQ file event backend");
				event_backend = new NullFileEventBackend ();
                        }

			tree_crawl_task = new TreeCrawlTask (this, new TreeCrawlTask.Handler (AddDirectory));
			tree_crawl_task.Source = this;

			file_crawl_task = new FileCrawlTask (this);
			file_crawl_task.Source = this;

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
			// FIXME: This is incorrect, but needed for DISABLE_XATTR
			// ExtendedAttribute.Supported only looks at homedirectory
			// There should be a similar check for all mount points or roots
			if (ExtendedAttribute.Supported)
				return new FileAttributesStore_Mixed (IndexDirectory, IndexFingerprint);
                        else
                                return new FileAttributesStore_Sqlite (IndexDirectory, IndexFingerprint);
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

		public static void AddStandardPropertiesToIndexable (Indexable indexable,
								     string    name, 
								     Guid      parent_id,
								     bool      mutable)
		{
			foreach (Property std_prop in Property.StandardFileProperties (name, mutable))
				indexable.AddProperty (std_prop);

			if (parent_id == Guid.Empty)
				return;
			
			string str = GuidFu.ToUriString (parent_id);
			// We use the uri here to recycle terms in the index,
			// since each directory's uri will already be indexed.
			Property prop = Property.NewUnsearched (Property.ParentDirUriPropKey, str);
			prop.IsMutable = mutable;
			indexable.AddProperty (prop);
		}

		public static void AddStandardPropertiesToIndexable (Indexable      indexable,
								     string         name,
								     DirectoryModel parent,
								     bool           mutable)
		{
			AddStandardPropertiesToIndexable (indexable,
							  name,
							  parent == null ? Guid.Empty : parent.UniqueId,
							  mutable);

			indexable.LocalState ["Parent"] = parent;
		}

		public static Indexable DirectoryToIndexable (string         path,
							      Guid           id,
							      DirectoryModel parent)
		{
			Indexable indexable;
			indexable = new Indexable (IndexableType.Add, GuidFu.ToUri (id));
			indexable.Timestamp = Directory.GetLastWriteTimeUtc (path);

			// If the directory was deleted, we'll bail out.
			if (! FileSystem.ExistsByDateTime (indexable.Timestamp))
				return null;

			indexable.MimeType = "inode/directory";
			indexable.NoContent = true;
			indexable.DisplayUri = UriFu.PathToFileUri (path);

			string name;
			if (parent == null)
				name = path;
			else
				name = Path.GetFileName (path);
			AddStandardPropertiesToIndexable (indexable, name, parent, true);

			Property prop;
			prop = Property.NewBool (Property.IsDirectoryPropKey, true);
			prop.IsMutable = true; // we want this in the secondary index, for efficiency
			indexable.AddProperty (prop);

			indexable.LocalState ["Path"] = path;

			return indexable;
		}

		public static Indexable FileToIndexable (string         path,
							 Guid           id,
							 DirectoryModel parent,
							 bool           crawl_mode)
		{
			Indexable indexable;

			indexable = new Indexable (IndexableType.Add, GuidFu.ToUri (id));
			indexable.Timestamp = File.GetLastWriteTimeUtc (path);

			// If the file was deleted, bail out.
			if (! FileSystem.ExistsByDateTime (indexable.Timestamp))
				return null;

			indexable.ContentUri = UriFu.PathToFileUri (path);
			indexable.DisplayUri = UriFu.PathToFileUri (path);
			indexable.Crawled = crawl_mode;
			indexable.Filtering = Beagle.IndexableFiltering.Always;

			AddStandardPropertiesToIndexable (indexable, Path.GetFileName (path), parent, true);

			indexable.LocalState ["Path"] = path;

			return indexable;
		}

		private static Indexable NewRenamingIndexable (string         name,
							       Guid           id,
							       DirectoryModel parent,
							       string last_known_path)
		{
			Indexable indexable;
			indexable = new Indexable (IndexableType.PropertyChange, GuidFu.ToUri (id));
			indexable.DisplayUri = UriFu.PathToFileUri (name);

			AddStandardPropertiesToIndexable (indexable, name, parent, true);

			indexable.LocalState ["Id"] = id;
			indexable.LocalState ["LastKnownPath"] = last_known_path;

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

		private void CacheDirectoryNameChange (Guid id, Guid new_parent_id, string new_name)
		{
			LuceneNameResolver.NameInfo info;
			info = name_info_by_id [id] as LuceneNameResolver.NameInfo;
			if (info != null) {
				info.ParentId = new_parent_id;
				info.Name = new_name;
			}
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

		private string UniqueIdToFileName (Guid id)
		{
			LuceneNameResolver.NameInfo info;
			info = name_resolver.GetNameInfoById (id);
			if (info == null)
				return null;
			return info.Name;
		}

		private void RegisterId (string name, DirectoryModel dir, Guid id)
		{
			cached_uid_by_path [Path.Combine (dir.FullName, name)] = id;
		}

		private void ForgetId (string path)
		{
			cached_uid_by_path.Remove (path);
		}

		// This works for files.  (It probably works for directories
		// too, but you should use one of the more efficient means
		// above if you know it is a directory.)
		private Guid NameAndParentToId (string name, DirectoryModel dir)
		{
			string path;
			path = Path.Combine (dir.FullName, name);

			Guid unique_id;
			if (cached_uid_by_path.Contains (path))
				unique_id = (Guid) cached_uid_by_path [path];
			else
				unique_id = name_resolver.GetIdByNameAndParentId (name, dir.UniqueId);

			return unique_id;
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Directory-related methods
		//

		private Hashtable dir_models_by_path = new Hashtable ();

		private DirectoryModel GetDirectoryModelByPath (string path)
		{
			DirectoryModel dir;

			lock (dir_models_by_path) {
				dir = dir_models_by_path [path] as DirectoryModel;
				if (dir != null)
					return dir;
			}

			// Walk each root until we find the correct path
			foreach (DirectoryModel root in roots) {
				dir = root.WalkTree (path);
				if (dir != null) {
					lock (dir_models_by_path)
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

			lock (dir_models_by_path)
				dir_models_by_path.Remove (expired_path);
		}

		public void AddDirectory (DirectoryModel parent, string name)
		{
			// Ignore the stuff we want to ignore.
			if (filter.Ignore (parent, name, true))
				return;

			// FIXME: ! parent.HasChildWithName (name)
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

			// If we're adding a root, this is probably a
			// long-running indexing task.  Set IsIndexing.
			IsIndexing = true;

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

			DateTime last_crawl;
			last_crawl = (attr == null) ? DateTime.MinValue : attr.LastWriteTime;

			Indexable indexable;
			indexable = DirectoryToIndexable (path, id, parent);

			if (indexable != null) {
				indexable.LocalState ["Name"] = name;
				indexable.LocalState ["LastCrawl"] = last_crawl;
				indexable.LocalState ["IsWalkable"] = is_walkable;

				Scheduler.Task task;
				task = NewAddTask (indexable);
				task.Priority = Scheduler.Priority.Delayed;
				ThisScheduler.Add (task);
			}
		}

		private bool RegisterDirectory (string name, DirectoryModel parent, FileAttributes attr)
		{
			string path;
			path = (parent == null) ? name : Path.Combine (parent.FullName, name);

			if (Debug)
				Logger.Log.Debug ("Registered directory '{0}' ({1})", path, attr.UniqueId);

			DateTime mtime = Directory.GetLastWriteTimeUtc (path);

			if (! FileSystem.ExistsByDateTime (mtime)) {
				Log.Debug ("Directory '{0}' ({1}) appears to have gone away", path, attr.UniqueId);
				return false;
			}

			DirectoryModel dir;
			if (parent == null)
				dir = DirectoryModel.NewRoot (big_lock, path, attr);
			else
				dir = parent.AddChild (name, attr);

			if (mtime > attr.LastWriteTime) {
				dir.State = DirectoryState.Dirty;
				if (Debug)
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

			return true;
		}

		private void ForgetDirectoryRecursively (DirectoryModel dir)
		{
			foreach (DirectoryModel child in dir.Children)
				ForgetDirectoryRecursively (child);

			if (dir.WatchHandle != null)
				event_backend.ForgetWatch (dir.WatchHandle);
			dir_models_by_id.Remove (dir.UniqueId);
			// We rely on the expire event to remove it from dir_models_by_path
		}

		private void RemoveDirectory (DirectoryModel dir)
		{
			Uri uri;
			uri = GuidFu.ToUri (dir.UniqueId);

			Indexable indexable;
			indexable = new Indexable (IndexableType.Remove, uri);
			indexable.DisplayUri = UriFu.PathToFileUri (dir.FullName);

			// Remember a copy of our external Uri, so that we can
			// easily remap it in the PostRemoveHook.
			indexable.LocalState ["RemovedUri"] = indexable.DisplayUri;

			// Forget watches and internal references
			ForgetDirectoryRecursively (dir);
			
			// Calling Remove will expire the path names,
			// so name caches will be cleaned up accordingly.
			dir.Remove ();

			Scheduler.Task task;
			task = NewAddTask (indexable); // We *add* the indexable to *remove* the index item
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
			if (dir == null) {
				Logger.Log.Warn ("Couldn't find DirectoryModel for directory moving to '{0}' in '{1}', so it was hopefully never there.",
						 new_name, new_parent.FullName);
				AddDirectory (new_parent, new_name);
				return;
			}

			if (dir.IsRoot)
				throw new Exception ("Can't move root " + dir.FullName);

			// We'll need this later in order to generate the
			// right change notification.
			string old_path;
			old_path = dir.FullName;
			
			if (new_parent != null && new_parent != dir.Parent)
				dir.MoveTo (new_parent, new_name);
			else
				dir.Name = new_name;

			// Remember this by path
			lock (dir_models_by_path)
				dir_models_by_path [dir.FullName] = dir;

			CacheDirectoryNameChange (dir.UniqueId, dir.Parent.UniqueId, new_name);

			Indexable indexable;
			indexable = NewRenamingIndexable (new_name,
							  dir.UniqueId,
							  dir.Parent, // == new_parent
							  old_path);
			indexable.LocalState ["OurDirectoryModel"] = dir;

			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Immediate;
			// Danger Will Robinson!
			// We need to use BlockUntilNoCollision to get the correct notifications
			// in a mv a b; mv b c; mv c a situation.
			// FIXME: And now that type no longer exists!
			ThisScheduler.Add (task);
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

			// We couldn't read our attribute back in for some
			// reason.  Complain loudly.
			if (attr == null) {
				Log.Error ("Unable to read attributes for recently crawled directory {0}", dir.FullName);
				dir.MarkAsClean ();
				return;
			}

			// We don't have to be super-careful about this since
			// we only use the FileAttributes mtime on a directory
			// to determine its initial state, not whether or not
			// its index record is up-to-date.
			attr.LastWriteTime = DateTime.UtcNow;

			// ...but we do use this to decide which order directories get
			// crawled in.
			dir.LastCrawlTime = DateTime.UtcNow;

			FileAttributesStore.Write (attr);
			dir.MarkAsClean ();
		}

		public void MarkDirectoryAsUncrawlable (DirectoryModel dir)
		{
			if (! dir.IsAttached)
				return;
			
			// If we managed to get set up a watch on this directory,
			// drop it.
			if (dir.WatchHandle != null) {
				event_backend.ForgetWatch (dir.WatchHandle);
				dir.WatchHandle = null;
			}

			dir.MarkAsUncrawlable ();
		}

		public void Recrawl (string path) 
		{
			// Try to find a directory model for the path specified
			// so that we can re-crawl it.
			DirectoryModel dir;
			dir = GetDirectoryModelByPath (path);

			bool path_is_registered = true;

			if (dir == null) {
				dir = GetDirectoryModelByPath (FileSystem.GetDirectoryNameRootOk (path));
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
								out string     last_known_path)
		{
			last_known_path = null;

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

			// If this was not indexed before, try again
			if (! attr.HasFilterInfo)
				return RequiredAction.Index;

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

			Mono.Unix.Native.Stat stat;
			try {
				Mono.Unix.Native.Syscall.stat (path, out stat);
			} catch (Exception ex) {
				Logger.Log.Debug (ex, "Caught exception stat-ing {0}", path);
				return RequiredAction.None;
			}

			DateTime last_write_time, last_attr_time;
			last_write_time = DateTimeUtil.UnixToDateTimeUtc (stat.st_mtime);
			last_attr_time = DateTimeUtil.UnixToDateTimeUtc (stat.st_ctime);

			if (attr.LastWriteTime != last_write_time) {
				if (Debug)
					Logger.Log.Debug ("*** Index it: MTime has changed ({0} vs {1})",
						DateTimeUtil.ToString (attr.LastWriteTime),
						DateTimeUtil.ToString (last_write_time));
				
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

			// If the inode ctime is newer than the last time we last
			// set file attributes, we might have been moved.  We don't
			// strictly compare times due to the fact that although
			// setting xattrs changes the ctime, if we don't have write
			// access our metadata will be stored in sqlite, and the
			// ctime will be at some point in the past.
			if (attr.LastAttrTime < last_attr_time) {
				if (Debug)
					Logger.Log.Debug ("*** CTime is newer, checking last known path ({0} vs {1})",
						DateTimeUtil.ToString (attr.LastAttrTime),
						DateTimeUtil.ToString (last_attr_time));

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
			action = DetermineRequiredAction (dir, name, attr, out last_known_path);

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
				indexable = FileToIndexable (path, unique_id, dir, true);
				break;

			case RequiredAction.Rename:
				indexable = NewRenamingIndexable (name, unique_id, dir,
								  last_known_path);
				break;

			case RequiredAction.Forget:
				FileAttributesStore.Drop (path);
				
				break;
			}

			return indexable;
		}

		public void AddFile (DirectoryModel dir, string name)
		{
			string path;
			path = Path.Combine (dir.FullName, name);

			if (! File.Exists (path))
				return;

			if (FileSystem.IsSpecialFile (path))
				return;
			
			if (filter.Ignore (dir, name, false))
				return;

			// If this file already has extended attributes,
			// make sure that the name matches the file
			// that is in the index.  If not, it could be
			// a copy of an already-indexed file and should
			// be assigned a new unique id.
			Guid unique_id = Guid.Empty;
			FileAttributes attr;
			attr = FileAttributesStore.Read (path);
			if (attr != null) {
				LuceneNameResolver.NameInfo info;
				info = name_resolver.GetNameInfoById (attr.UniqueId);
				if (info != null
				    && info.Name == name
				    && info.ParentId == dir.UniqueId)
					unique_id = attr.UniqueId;
			}

			if (unique_id == Guid.Empty)
				unique_id = Guid.NewGuid ();

			RegisterId (name, dir, unique_id);

			Indexable indexable;
			indexable = FileToIndexable (path, unique_id, dir, false);

			if (indexable != null) {
				Scheduler.Task task;
				task = NewAddTask (indexable);
				task.Priority = Scheduler.Priority.Immediate;
				ThisScheduler.Add (task);
			}
		}

		public void RemoveFile (DirectoryModel dir, string name)
		{
			// FIXME: We might as well remove it, even if it was being ignore.
			// Right?

			Guid unique_id;
			unique_id = NameAndParentToId (name, dir);
			if (unique_id == Guid.Empty) {
				Logger.Log.Info ("Could not resolve unique id of '{0}' in '{1}' for removal, it is probably already gone",
						 name, dir.FullName);
				return;
			}

			Uri uri, file_uri;
			uri = GuidFu.ToUri (unique_id);
			file_uri = UriFu.PathToFileUri (Path.Combine (dir.FullName, name));

			Indexable indexable;
			indexable = new Indexable (IndexableType.Remove, uri);
			indexable.DisplayUri = file_uri;
			indexable.LocalState ["RemovedUri"] = file_uri;

			Scheduler.Task task;
			task = NewAddTask (indexable);
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

			// We need to find the file's unique id.
			// We can't look at the extended attributes w/o making
			// assumptions about whether they follow around the
			// file (EAs) or the path (sqlite)...
			Guid unique_id;
			unique_id = NameAndParentToId (old_name, old_dir);
			if (unique_id == Guid.Empty) {
				// If we can't find the unique ID, we have to
				// assume that the original file never made it
				// into the index ---  thus we treat this as
				// an Add.
				AddFile (new_dir, new_name);
				return;
			}

			RegisterId (new_name, new_dir, unique_id);

			string old_path;
			old_path = Path.Combine (old_dir.FullName, old_name);

			ForgetId (old_path);

			// FIXME: I think we need to be more conservative when we seen
			// events in a directory that has not been fully scanned, just to
			// avoid races.  i.e. what if we are in the middle of crawling that
			// directory and haven't reached this file yet?  Then the rename
			// will fail.
			Indexable indexable;
			indexable = NewRenamingIndexable (new_name,
							  unique_id,
							  new_dir,
							  old_path);
			
			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Immediate;
			// Danger Will Robinson!
			// We need to use BlockUntilNoCollision to get the correct notifications
			// in a mv a b; mv b c; mv c a situation.
			// FIXME: And now AddType no longer exists
			ThisScheduler.Add (task);
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

		public void UpdateIsIndexing ()
		{
			// If IsIndexing is false, then the indexing had
			// finished previously and we don't really care about
			// this call anymore.  It can be reset to true if a new
			// root is added, however.
			if (this.IsIndexing == false)
				return;

			DirectoryModel next_dir = GetNextDirectoryToCrawl ();

			// If there are any "dirty" directories left, we're
			// still indexing.  If not, check our crawl tasks to
			// see if we're still working on the queue.
			if (next_dir != null)
				this.IsIndexing = (next_dir.State > DirectoryState.PossiblyClean);
			else
				this.IsIndexing = (file_crawl_task.IsActive || tree_crawl_task.IsActive);
		}

		//////////////////////////////////////////////////////////////////////////

		//
		// Our magic LuceneQueryable hooks
		//

		override protected void PostAddHook (Indexable indexable, IndexerAddedReceipt receipt)
		{
			// We don't have anything to do if we are dealing with a child indexable
			if (indexable.ParentUri != null)
				return;

			// If we just changed properties, remap to our *old* external Uri
			// to make notification work out property.
			if (indexable.Type == IndexableType.PropertyChange) {

				string last_known_path;
				last_known_path = (string) indexable.LocalState ["LastKnownPath"];
				receipt.Uri = UriFu.PathToFileUri (last_known_path);
				Logger.Log.Debug ("Last known path is {0}", last_known_path);

				// This rename is now in the index, so we no longer need to keep
				// track of the uid in memory.
				ForgetId (last_known_path);

				return;
			}

			string path;
			path = (string) indexable.LocalState ["Path"];
			if (Debug)
				Log.Debug ("PostAddHook for {0} ({1}) and receipt uri={2}", indexable.Uri, path, receipt.Uri);

			// Remap the Uri so that change notification will work properly
			receipt.Uri = UriFu.PathToFileUri (path);
		}

		override protected void PostRemoveHook (Indexable indexable, IndexerRemovedReceipt receipt)
		{
			// Find the cached external Uri and remap the Uri in the receipt.
			// We have to do this to make change notification work.
			Uri external_uri;
			external_uri = indexable.LocalState ["RemovedUri"] as Uri;
			if (external_uri == null)
				throw new Exception ("No cached external Uri for " + receipt.Uri);
			receipt.Uri = external_uri;
			ForgetId (external_uri.LocalPath);
		}

		override protected void PostChildrenIndexedHook (Indexable indexable,
								 IndexerAddedReceipt receipt,
								 DateTime Mtime)
		{
			// There is no business here for children or if only the property changed
			if (indexable.Type == IndexableType.PropertyChange ||
			    indexable.ParentUri != null)
				return;

			string path;
			path = (string) indexable.LocalState ["Path"];
			if (Debug)
				Log.Debug ("PostChildrenIndexedHook for {0} ({1}) and receipt uri={2}, (filter={3},{4})", indexable.Uri, path, receipt.Uri, receipt.FilterName, receipt.FilterVersion);

			ForgetId (path);

			DirectoryModel parent;
			parent = indexable.LocalState ["Parent"] as DirectoryModel;

			// The parent directory might have run away since we were indexed
			if (parent != null && ! parent.IsAttached)
				return;

			Guid unique_id;
			unique_id = GuidFu.FromUri (receipt.Uri);

			FileAttributes attr;
			attr = FileAttributesStore.ReadOrCreate (path, unique_id);

			attr.Path = path;
			// FIXME: Should timestamp be indexable.timestamp or parameter Mtime
			attr.LastWriteTime = indexable.Timestamp;
			
			attr.FilterName = receipt.FilterName;
			attr.FilterVersion = receipt.FilterVersion;

			if (indexable.LocalState ["IsWalkable"] != null) {
				string name;
				name = (string) indexable.LocalState ["Name"];

				if (! RegisterDirectory (name, parent, attr))
					return;
			}

			FileAttributesStore.Write (attr);
		}

		private bool RemapUri (Hit hit)
		{
			// Store the hit's internal uri in a property
			Property prop;
			prop = Property.NewUnsearched ("beagle:InternalUri",
						       UriFu.UriToEscapedString (hit.Uri));
			hit.AddProperty (prop);

			// Now assemble the path by looking at the parent and name
			string name = null, path, is_child;
			is_child = hit [Property.IsChildPropKey];

			if (is_child == "true")
				name = hit ["parent:" + Property.ExactFilenamePropKey];
			else
				name = hit [Property.ExactFilenamePropKey];

			if (name == null) {
				// If we don't have the filename property, we have to do a lookup
				// based on the guid.  This happens with synthetic hits produced by
				// index listeners.
				Guid hit_id;
				hit_id = GuidFu.FromUri (hit.Uri);
				path = UniqueIdToFullPath (hit_id);
			} else {
				string parent_id_uri = null;
				parent_id_uri = hit [Property.ParentDirUriPropKey];
				if (parent_id_uri == null)
					parent_id_uri = hit ["parent:" + Property.ParentDirUriPropKey];
				if (parent_id_uri == null)
					return false;

				Guid parent_id;
				parent_id = GuidFu.FromUriString (parent_id_uri);
			
				path = ToFullPath (name, parent_id);
				if (path == null)
					Logger.Log.Debug ("Couldn't find path of file with name '{0}' and parent '{1}'",
							  name, GuidFu.ToShortString (parent_id));
			}

			if (Debug)
				Log.Debug ("Resolved {0} to {1}", hit.Uri, path);

			if (path != null) {
				hit.Uri = UriFu.PathToFileUri (path);
				return true;
			}

			return false;
		}

		// Hit filter: this handles our mapping from internal->external uris,
		// and checks to see if the file is still there.
		override protected bool HitFilter (Hit hit)
		{
			Uri old_uri = hit.Uri;
			if (Debug)
				Log.Debug ("HitFilter ({0})", old_uri);

			if (! RemapUri (hit))
				return false;

			string path;
			path = hit.Uri.LocalPath;

			bool is_directory;
			bool exists = false;

			is_directory = hit.MimeType == "inode/directory";

			if (hit.MimeType == null && hit.Uri.IsFile && Directory.Exists (path)) {
				is_directory = true;
				exists = true;
			}

			if (! exists) {
				if (is_directory)
					exists = Directory.Exists (path);
				else
					exists = File.Exists (path);
			}

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

			// If child indexable, attach the relative URI at the end
			// Relative URI starts with '#'
			string is_child = hit [Property.IsChildPropKey];
			string fragment = null;
			if (is_child == "true") {
				hit.Uri = UriFu.PathToFileUri (path, old_uri.Fragment);
				hit.ParentUri = UriFu.PathToFileUri (path);
			}

			// Check the ignore status of the hit
			if (filter.Ignore (parent, Path.GetFileName (fragment == null ? path : fragment), is_directory))
				return false;

			return true;
		}

		override public string GetSnippet (string [] query_terms, Hit hit)
		{
			// Uri remapping from a hit is easy: the internal uri
			// is stored in a property.
			Uri uri = UriFu.EscapedStringToUri (hit ["beagle:InternalUri"]);

			string path = TextCache.UserCache.LookupPathRaw (uri);

			if (path == null)
				return null;

			// If this is self-cached, use the remapped Uri
			if (path == TextCache.SELF_CACHE_TAG)
				return SnippetFu.GetSnippetFromFile (query_terms, hit.Uri.LocalPath);

			path = Path.Combine (TextCache.UserCache.TextCacheDir, path);
			return SnippetFu.GetSnippetFromTextCache (query_terms, path);
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

			// If something goes wrong, just fail silently.
			if (dir == null)
				return;

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
				old_dir = GetDirectoryModelByPath (old_directory_name);
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
	
