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
using System.Reflection;
using System.IO;

using Beagle.Daemon;
using BU = Beagle.Util;


namespace Beagle.Daemon.FileSystemQueryable {

	[QueryableFlavor (Name="FileSystemQueryable", Domain=QueryDomain.Local)]
	public class FileSystemQueryable : LuceneQueryable {

		static void IndexFile (LuceneDriver driver, FileSystemInfo fsinfo)
		{
			Uri uri = BU.UriFu.PathToFileUri (fsinfo.FullName);
			FilteredIndexable indexable = new FilteredIndexable (uri);
			driver.ScheduleAddAndMark (indexable, fsinfo);
		}

		static void IndexFile (LuceneDriver driver, string path)
		{
			if (File.Exists (path))
				IndexFile (driver, new FileInfo (path));
			else if (Directory.Exists (path))
				IndexFile (driver, new DirectoryInfo (path));
		}

		static void RemoveFileFromIndex (LuceneDriver driver, string path)
		{
			Uri uri = BU.UriFu.PathToFileUri (path);
			driver.ScheduleDelete (uri);
		}

		private class FileSystemCrawler : Crawler {

			LuceneDriver driver;
			FileNameFilter filter;

			public FileSystemCrawler (LuceneDriver _driver, FileNameFilter _filter) : base (_driver.Fingerprint)
			{
				driver = _driver;
				filter = _filter;
			}

			protected override bool SkipByName (FileSystemInfo fsinfo)
			{
				return filter.Ignore (fsinfo);
			}

			protected override void CrawlFile (FileSystemInfo fsinfo)
			{
				IndexFile (driver, fsinfo);
			}
		}

		private class FileSystemIndexerImpl : Beagle.FileSystemIndexerProxy {

			LuceneDriver driver;
			Crawler crawler;

			public FileSystemIndexerImpl (LuceneDriver _driver, Crawler _crawler)
			{
				driver = _driver;
				crawler = _crawler;
			}
			

			public override void Index (string path)
			{
				IndexFile (driver, path);
			}

			public override void Delete (string path)
			{
				RemoveFileFromIndex (driver, path);
			}

			public override void Crawl (string path, int maxDepth)
			{
				if (Directory.Exists (path)) {
					DirectoryInfo dir = new DirectoryInfo (path);
					crawler.ScheduleCrawl (dir, maxDepth);
				}
			}
		}

		private FileNameFilter filter;
		private FileSystemCrawler crawler;
		private FileSystemIndexerImpl indexer;
		private FileSystemEventMonitor monitor;

		public FileSystemQueryable () : base ("FileSystemQueryable",
						      Path.Combine (PathFinder.RootDir, "FileSystemIndex"))
		{
			filter = new FileNameFilter ();
			crawler = new FileSystemCrawler (Driver, filter);
			indexer = new FileSystemIndexerImpl (Driver, crawler);
			DBusisms.Service.RegisterObject (indexer, Beagle.DBusisms.FileSystemIndexerPath);

			// Set up file system monitor
			monitor  = new FileSystemEventMonitor ();
			string home = Environment.GetEnvironmentVariable ("HOME");
			ImportantDirectory (home);
			ImportantDirectory (Path.Combine (home, "Desktop"));
			ImportantDirectory (Path.Combine (home, "Documents"));

			monitor.FileSystemEvent += OnFileSystemEvent;
		}

		public void ImportantDirectory (string path)
		{
			DirectoryInfo dir = new DirectoryInfo (path);
			if (dir.Exists) {
				monitor.Subscribe (dir, false);
				crawler.ScheduleCrawl (dir, 0);
			}
		}

		private void OnFileSystemEvent (FileSystemEventMonitor source, FileSystemEventType eventType,
						string oldPath, string newPath)
		{
			Console.WriteLine ("Got event {0} {1} {2}", eventType, oldPath, newPath);

			if (eventType == FileSystemEventType.Changed
			    || eventType == FileSystemEventType.Created) {

				if (filter.Ignore (newPath)) {
					Console.WriteLine ("Ignoring {0}", newPath);
					return;
				}

				IndexFile (Driver, newPath);

			} else if (eventType == FileSystemEventType.Deleted) {

				if (filter.Ignore (oldPath)) {
					Console.WriteLine ("Ignoring {0}", oldPath);
					return;
				}

				RemoveFileFromIndex (Driver, oldPath);

			} else {

				Console.WriteLine ("Unhandled!");
			}
		}
	}
}
