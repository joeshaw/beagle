//
// LauncherQueryable.cs
//
// Copyright (C) 2004 Joe Gasiorek
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
using System.Text;
using System.Diagnostics;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.LauncherQueryable {

	[QueryableFlavor (Name="Launcher", Domain=QueryDomain.Local, RequireInotify=false)]
	public class LauncherQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("LauncherQueryable");
		ArrayList Dirs;
		Hashtable watched = new Hashtable ();
		string home;
		FileStream LauncherDB;
		int polling_interval_in_hours = 1;
		LauncherCrawler crawler;

		public LauncherQueryable () : base ("LauncherIndex")
		{
			Dirs = new ArrayList ();
			// Add GNOME dirs
			string path = Path.Combine (ExternalStringsHack.GnomePrefix, "share");
			Dirs.Add (Path.Combine (path, "applications"));
			Dirs.Add (Path.Combine (path, "gnome/apps"));
			Dirs.Add (Path.Combine (path, "control-center-2.0"));
			Dirs.Add (Path.Combine (path, "control-center"));
			Dirs.Add (Path.Combine (PathFinder.HomeDir, ".gnome2/panel2.d/default/launchers"));

			// FIXME:  Add KDE dirs
			// home = Environment.GetEnvironmentVariable ("KDEDIR");
			// Dirs.Add (home + "share/services/");
			// Dirs.Add (home + "share/apps/");
			// Dirs.Add (home + "share/applications/");
		}

		override protected IFileAttributesStore BuildFileAttributesStore (string index_fingerprint) 
		{
			return new FileAttributesStore_Mixed (IndexDirectory, Driver.Fingerprint);
		}

		public override void Start ()
		{
			base.Start ();

			log.Info ("Starting launcher backend");
			Stopwatch timer = new Stopwatch ();
			timer.Start ();

			if (Inotify.Enabled) {
				Inotify.Event += OnInotifyEvent;

				log.Info ("Scanning Launchers");
				int launchers_found = 0;
				foreach (String dir in Dirs)
					launchers_found += Watch (dir);

				log.Info ("Found {0} Launchers in {1}", launchers_found, timer);
			}

			this.crawler = new LauncherCrawler (Dirs);
			Crawl ();

			if (!Inotify.Enabled) {
				Scheduler.Task task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlHook));
				task.Tag = "Crawling system launchers";
				ThisScheduler.Add (task);
			}

			timer.Stop ();
			log.Info ("Launcher backend worker thread done in {0}", timer);
		}

		private void Crawl ()
		{
			this.crawler.Crawl ();
  	                
			foreach (FileInfo file in crawler.Launchers)
				IndexLauncher (file, Scheduler.Priority.Delayed);
		}

		private void CrawlHook (Scheduler.Task task)
		{
			Crawl ();
			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddHours (this.polling_interval_in_hours);
		}

		private int Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			if (! root.Exists)
				return 0;
			int fileCount = 0;

			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				int wd = Inotify.Watch (dir.FullName, Inotify.EventType.CreateSubdir | Inotify.EventType.Modify);
				watched [wd] = true;
				foreach (FileInfo file in dir.GetFiles ()) {
					IndexLauncher (file, Scheduler.Priority.Delayed);
					++fileCount;
				}
				
				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}

			return fileCount;
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
				case Inotify.EventType.Modify:
					IndexLauncher (new FileInfo (fullPath), Scheduler.Priority.Immediate);
					break;
			}
		}

		private void IndexLauncher (FileInfo file, Scheduler.Priority priority)
		{
			if ((! file.Exists)
			    || (this.FileAttributesStore.IsUpToDate (file.FullName)))
				return;
			
			/* Check to see if file is a launcher */
			if (Beagle.Util.VFS.Mime.GetMimeType (file.ToString ()) != "application/x-desktop")
				return;
			StreamReader reader = new StreamReader (file.Open (FileMode.Open, FileAccess.Read, FileShare.Read));
			if (reader.ReadLine () != "[Desktop Entry]") {
				reader.Close ();
				return;
			}
				
			/* I'm convinced it is a launcher */
			Indexable indexable = new Indexable (UriFu.PathToFileUri (file.FullName));

			indexable.Timestamp = file.LastWriteTime;
			indexable.Type = "Launcher";
			indexable.MimeType = "application/x-desktop";

			String line;
			while ((line = reader.ReadLine ()) != null)  {
				string [] sline = line.Split ('=');
				if (sline.Length != 2)
					continue;
				if (!sline[1].Equals (""))  {
					// FIXME:  add other language support
					if ((sline[0].Equals ("Exec")) || (sline[0].Equals ("Icon")) || (sline[0].Equals ("Name")) || (sline[0].Equals ("Comment"))) {
						StringBuilder property = new StringBuilder ("fixme:");
						indexable.AddProperty (Beagle.Property.NewKeyword (property.Append(sline[0]).ToString (), sline[1]));
					}
				}
			}
			reader.Close ();
			Scheduler.Task task = NewAddTask (indexable);
			task.Priority = priority;
			ThisScheduler.Add (task);
		}
	}
}

