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

		private static Logger log = Logger.Get ("FileSystemQueryable");

		private FileSystemModel model;
		private IFileEventBackend event_backend;

		public FileSystemQueryable () : base ("FileSystemIndex")
		{
			
		}

		public FileSystemModel Model {
			get { return model; }
		}

		//////////////////////////////////////////////////////////////////////////

		override protected IFileAttributesStore BuildFileAttributesStore (string index_fingerprint)
		{
			return new FileAttributesStore_Mixed (IndexDirectory, index_fingerprint);
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

			if (this.FileAttributesStore.IsUpToDate (path))
				return false;
			
			return true;
		}

		public static Indexable FileToIndexable (string path, bool crawl_mode)
		{
			Uri uri = UriFu.PathToFileUri (path);
			return new FilteredIndexable (uri, crawl_mode);
		}

		//////////////////////////////////////////////////////////////////////////

		public void Add (string path)
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

		public void Remove (string path)
		{
			Uri uri = UriFu.PathToFileUri (path);
			Scheduler.Task task;
			task = NewRemoveTask (uri);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
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

		// If our file system model contains elements that need to be crawled,
		// launch a crawling task.
		private void OnModelNeedsCrawl (FileSystemModel source)
		{
			CrawlTask task = new CrawlTask (this, source);
			ThisScheduler.Add (task, Scheduler.AddType.DeferToExisting);
		}

		private void OnModelNeedsScan (FileSystemModel source)
		{
			source.ScanAll ();
		}

		public void StartWorker ()
		{
			if (Inotify.Enabled)
				event_backend = new InotifyBackend ();
			else
				event_backend = new FileSystemWatcherBackend ();

			model = new FileSystemModel (this.FileAttributesStore, event_backend);
			model.NeedsScanEvent += new FileSystemModel.NeedsScanHandler (OnModelNeedsScan);
			model.NeedsCrawlEvent += new FileSystemModel.NeedsCrawlHandler (OnModelNeedsCrawl);

			event_backend.Start (this);

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
	
