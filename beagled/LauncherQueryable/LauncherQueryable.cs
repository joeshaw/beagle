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
		int polling_interval_in_hours = 1;

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

			// Add KDE dirs
			foreach (string kde_dir in KdeUtils.KdeLocations) {
				if (kde_dir == null || kde_dir == String.Empty)
					continue;

				string share_dir = Path.Combine (kde_dir, "share");
				Dirs.Add (Path.Combine(share_dir, "applications"));
			}
			Dirs.Add (Path.Combine (PathFinder.HomeDir, ".local/share/applications"));
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
			log.Info ("Scanning Launchers");
			Stopwatch timer = new Stopwatch ();
			int launchers_found = 0;
			foreach (String dir in Dirs)
				launchers_found += CrawlLaunchers (dir);

			log.Info ("Found {0} Launchers in {1}", launchers_found, timer);
		}

		private void CrawlHook (Scheduler.Task task)
		{
			Crawl ();
			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddHours (this.polling_interval_in_hours);
		}

		// Crawl the specified directory and all subdirectories, indexing all
		// discovered launchers. If Inotify is available, every directory 
		// scanned will be watched.
		private int CrawlLaunchers (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			if (! root.Exists)
				return 0;
			int fileCount = 0;

			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				if (Inotify.Enabled) {
					Inotify.Subscribe (dir.FullName, OnInotifyEvent, Inotify.EventType.Create | Inotify.EventType.Modify);
				}

				foreach (FileInfo file in dir.GetFiles ()) {
					IndexLauncher (file, Scheduler.Priority.Delayed);
					++fileCount;
				}
				
				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}

			return fileCount;
		}

		private void OnInotifyEvent (Inotify.Watch watch,
					    string path,
					    string subitem,
					    string srcpath,
					    Inotify.EventType type)
		{
			if (subitem == "")
				return;

			string fullPath = Path.Combine (path, subitem);

			if ((type & Inotify.EventType.Create) != 0 && (type & Inotify.EventType.IsDirectory) != 0) {
				CrawlLaunchers (fullPath);
				return;
			}
			if ((type & Inotify.EventType.Modify) != 0) {
				IndexLauncher (new FileInfo (fullPath), Scheduler.Priority.Immediate);
				return;
			}
		}

		private void IndexLauncher (FileInfo file, Scheduler.Priority priority)
		{
			if ((! file.Exists)
			    || (this.FileAttributesStore.IsUpToDate (file.FullName)))
				return;
			
			/* Check to see if file is a launcher */
			if (Beagle.Util.VFS.Mime.GetMimeType (file.FullName) != "application/x-desktop")
				return;

			StreamReader reader;

			try {
				reader = new StreamReader (file.Open (FileMode.Open, FileAccess.Read, FileShare.Read));
			} catch (Exception e) {
				log.Warn ("Could not open '{0}': {1}", file.FullName, e.Message);
				return;
			}

			if (reader.ReadLine () != "[Desktop Entry]") {
				reader.Close ();
				return;
			}

			/* I'm convinced it is a launcher */
			Indexable indexable = new Indexable (UriFu.PathToFileUri (file.FullName));

			indexable.Timestamp = file.LastWriteTime;
			indexable.Type = "Launcher";
			indexable.MimeType = "application/x-desktop";
			
			// desktop files must have a name
			bool have_name = false;

			String line;
			while ((line = reader.ReadLine ()) != null)  {
				string [] sline = line.Split ('=');
				if (sline.Length != 2)
					continue;

				// FIXME: We shouldnt really search fields that are in other locales than the current should we?

				if (sline [0].Equals ("Icon") || sline [0].Equals ("Exec")) {
					indexable.AddProperty (Beagle.Property.NewUnsearched ("fixme:" + sline[0], sline[1]));
				} else if (sline [0].StartsWith ("Name")) {
					if (sline [0] == "Name")
						have_name = true;
					indexable.AddProperty (Beagle.Property.NewKeyword ("fixme:" + sline[0], sline[1]));
				} else if (sline[0].StartsWith ("Comment")) {
					   indexable.AddProperty (Beagle.Property.New ("fixme:" + sline[0], sline[1]));
				}
			}
			reader.Close ();
			
			if (have_name) {
				    Scheduler.Task task = NewAddTask (indexable);
				    task.Priority = priority;
				    ThisScheduler.Add (task);
			}
		}
	}
}
