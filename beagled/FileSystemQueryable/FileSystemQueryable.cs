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

		private static Logger log = Logger.Get ("FileSystemQueryable");

		private IFileEventBackend event_backend;
		private FileSystemModel model;

		public FileSystemQueryable () : base ("FileSystemIndex")
		{
		}

		public FileSystemModel Model {
			get { return model; }
		}

		//////////////////////////////////////////////////////////////////////////

		override protected IFileAttributesStore BuildFileAttributesStore (string index_fingerprint)
		{
			// A bit of a hack: since we need the event backend to construct
			// the FileSystemModel, we create it here.
			if (Inotify.Enabled) {
				Logger.Log.Debug ("Starting Inotify Backend");
				event_backend = new InotifyBackend ();
			} else {
				Logger.Log.Debug ("Starting FileSystemWatcher Backend");
				event_backend = new FileSystemWatcherBackend ();
			}

			// The FileSystemModel also implements IFileAttributesStore.
			model = new FileSystemModel (IndexDirectory, index_fingerprint, event_backend);

			model.NeedsScanEvent += new FileSystemModel.NeedsScanHandler (OnModelNeedsScan);
			model.NeedsCrawlEvent += new FileSystemModel.NeedsCrawlHandler (OnModelNeedsCrawl);

			SetUriRemappers (new LuceneDriver.UriRemapper (model.ToInternalUri),
					 new LuceneDriver.UriRemapper (model.FromInternalUri));

			return model;
		}

		//////////////////////////////////////////////////////////////////////////

		public static Indexable FileToIndexable (Uri file_uri, Uri internal_uri, bool crawl_mode)
		{
			Indexable indexable = new Indexable (file_uri);
			indexable.Uri = internal_uri;
			indexable.ContentUri = file_uri;
			indexable.Crawled = crawl_mode;
			indexable.Filtering = Beagle.IndexableFiltering.Always;
			return indexable;
		}

		//////////////////////////////////////////////////////////////////////////

		public void Add (string path, Scheduler.Priority priority)
		{
			FileSystemModel.RequiredAction action;
			string old_path;

			// Model.DetermineRequiredAction checks whether or not we should
			// ignore the path, so we don't need to do that separately.
			action = Model.DetermineRequiredAction (path, out old_path);
			
			if (action == FileSystemModel.RequiredAction.None)
				return;

			if (action == FileSystemModel.RequiredAction.Rename) {
				Rename (old_path, path);
				return;
			}

			Scheduler.Task task;
			Indexable indexable;
				
			Uri file_uri = UriFu.PathToFileUri (path);
			Uri internal_uri = model.ToInternalUri (file_uri);
			indexable = FileToIndexable (file_uri, internal_uri, false);
			task = NewAddTask (indexable);
			task.Priority = priority;
			
			ThisScheduler.Add (task);
		}

		public void Add (string path)
		{
			Add (path, Scheduler.Priority.Immediate);
		}

		public void Remove (string path, Scheduler.Priority priority)
		{
			// If we are ignoring this file, don't do anything.
			// This should be safe to do even if we weren't ignoring this
			// file previously --- if it is in the index and matches
			// a query, that hit will be filtered out by HitIsValid
			// since the file no longer exists.
			if (Model.Ignore (path))
				return;

			Uri uri = UriFu.PathToFileUri (path);
			Scheduler.Task task;
			task = NewRemoveTask (uri);
			task.Priority = priority;
			ThisScheduler.Add (task);
		}

		public void Remove (string path)
		{
			Remove (path, Scheduler.Priority.Immediate);
		}

		public void Rename (string old_path, string new_path, Scheduler.Priority priority)
		{
			bool ignore_old = Model.Ignore (old_path);
			bool ignore_new = Model.Ignore (new_path);

			// If we just want to ignore both paths, do nothing.
			if (ignore_old && ignore_new)
				return;

			// If we were ignoring the old path, treat this as an add
			if (ignore_old) {
				Add (new_path, priority);
				return;
			}

			// If we want to ignore the new path, treat this as a removal
			if (ignore_new) {
				Remove (old_path, priority);
				return;
			}

			Uri old_uri = UriFu.PathToFileUri (old_path);
			Uri new_uri = UriFu.PathToFileUri (new_path);
			Scheduler.Task task;
			task = NewRenameTask (old_uri, new_uri);
			task.Priority = priority;
			ThisScheduler.Add (task);
		}

		public void Rename (string old_path, string new_path)
		{
			Rename (old_path, new_path, Scheduler.Priority.Immediate);
		}

		//////////////////////////////////////////////////////////////////////////

		override protected void AbusiveRemoveHook (Uri internal_uri, Uri external_uri)
		{
			if (Debug)
				Logger.Log.Debug ("AbusiveRemoveHook: internal_uri={0} external_uri={1}",
						  internal_uri, external_uri);
			Model.DropUid (GuidFu.FromUri (internal_uri));
			this.FileAttributesStore.Drop (external_uri.LocalPath);
		}

		override protected void AbusiveRenameHook (Uri old_uri, Uri new_uri)
		{
			if (Debug)
				Logger.Log.Debug ("AbusiveRenameHook: old_uri={0}, new_uri={1}", old_uri, new_uri);

			// If the thing being renamed is a directory, we have to update
			// our model.
			FileSystemModel.Directory dir = Model.GetDirectoryByPath (old_uri.LocalPath);

			if (dir != null) {

				if (Debug)
					Logger.Log.Debug ("AbusiveRenameHook: found directory");
			
				string new_dirname = Path.GetDirectoryName (new_uri.LocalPath);
				string new_filename = Path.GetFileName (new_uri.LocalPath);

				FileSystemModel.Directory new_parent = Model.GetDirectoryByPath (new_dirname);

				if (Debug)
					Logger.Log.Debug ("AbusiveRenameHook: new_parent={0}", new_parent.FullName);
			
				if (dir.Name != new_filename) {
					if (Debug)
						Logger.Log.Debug ("AbusiveRenameHook: new name is {0}", new_filename);
					Model.Rename (dir, new_filename);
				}

				if (dir.Parent != new_parent) {
					if (Debug)
						Logger.Log.Debug ("AbusiveRenameHook: new parent is {0}", new_parent.FullName);
					Model.Move (dir, new_parent);
				}
			}
			
			// Attach the current time as the last index time.
			// We didn't actually index anything, of course, but we
			// need to update this time so that future ctime checks
			// will be accurate.
			// This will also make the necessary adjustments to
			// the unique ID store.
			FileAttributes attr = FileAttributesStore.Read (new_uri.LocalPath);
			if (attr != null) {
				attr.LastIndexedTime = DateTime.Now;
				FileAttributesStore.Write (attr);
			}
		}

		//////////////////////////////////////////////////////////////////////////

		// Filter out hits where the files seem to no longer exist.
		override protected bool HitIsValid (Uri uri)
		{
			return model.InternalUriIsValid (uri);
		}

		//////////////////////////////////////////////////////////////////////////

		// If our file system model contains elements that need to be crawled,
		// launch a crawling task.
		private void OnModelNeedsCrawl (FileSystemModel source)
		{
			CrawlTask task = new CrawlTask (this);
			ThisScheduler.Add (task, Scheduler.AddType.DeferToExisting);
		}

		private void OnModelNeedsScan (FileSystemModel source)
		{
			source.ScanAll ();
		}

		public void StartWorker ()
		{
			event_backend.Start (this);

			// FIXME: Shouldn't be hard-wired
			model.AddRoot (PathFinder.HomeDir);

			log.Info ("FileSystemQueryable start-up thread finished");
			
			// FIXME: Do we need to re-run queries when we are fully started?
		}

		public override void Start ()
		{
			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		//////////////////////////////////////////////////////////////////////////

		protected override ICollection DoBonusQuery (Query query, ICollection list_of_uris)
		{
			return model.Search (query, list_of_uris);
		}

		protected override double RelevancyMultiplier (Hit hit)
		{
			FileSystemInfo info = hit.FileSystemInfo;

			double days = (DateTime.Now - info.LastWriteTime).TotalDays;
			// Maximize relevancy if the file has been touched within the last seven days.
			if (0 <= days && days < 7)
				return 1.0;

			DateTime dt = info.LastAccessTime;
			if (dt < info.LastWriteTime)
				dt = info.LastWriteTime;

			return HalfLifeMultiplier (dt);
		}
	}
}
	
