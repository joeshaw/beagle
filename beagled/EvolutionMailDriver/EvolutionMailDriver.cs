
//
// EvolutionMailDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//
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
using System.Threading;

using Beagle.Util;
using Camel = Beagle.Util.Camel;

namespace Beagle.Daemon.EvolutionMailDriver {

	[QueryableFlavor (Name="Mail", Domain=QueryDomain.Local, RequireInotify=false)]
	public class EvolutionMailQueryable : LuceneQueryable {

		public int polling_interval_in_seconds = 60;

		public static Logger log = Logger.Get ("mail");
		private string local_path, imap_path;

		private SortedList watched = new SortedList ();
		private MailCrawler crawler;

		private object lockObj = new object ();
		private ArrayList AddedUris = new ArrayList ();
		private bool queryRunning = false;

		public EvolutionMailQueryable () : base ("MailIndex")
		{
			this.local_path = Path.Combine (PathFinder.HomeDir, ".evolution/mail/local");
			this.imap_path = Path.Combine (PathFinder.HomeDir, ".evolution/mail/imap");
		}

		private void Crawl ()
		{
			crawler.Crawl ();
			foreach (FileInfo file in crawler.Summaries)
				IndexSummary (file);
			foreach (FileInfo file in crawler.Mboxes)
				IndexMbox (file);
		}

		private void CrawlHook (Scheduler.Task task)
		{
			Crawl ();
			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
		}

		private void StartWorker ()
		{
			Logger.Log.Info ("Starting Evolution mail backend");

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			// Check that we have data to index
			if ((! Directory.Exists (this.local_path)) && (! Directory.Exists (this.imap_path))) {
				// No mails present, repoll every minute
				log.Warn ("Evolution mail store not found, watching for it.");
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForMailData));
				return;
			}

			// Get notification when an index or summary file changes
			if (Inotify.Enabled) {
				Inotify.Event += OnInotifyEvent;
				Watch (this.local_path);
				Watch (this.imap_path);
			}

			Logger.Log.Debug ("Starting mail crawl");
			crawler = new MailCrawler ();
			Crawl ();
			Logger.Log.Debug ("Mail crawl finished");

			// If we don't have inotify, we have to poll the file system.  Ugh.
			if (! Inotify.Enabled) {
				Scheduler.Task task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlHook));
				task.Tag = "Crawling ~/.evolution to find summary changes";
				ThisScheduler.Add (task);
			}

			stopwatch.Stop ();
			Logger.Log.Info ("Evolution mail driver worker thread done in {0}",
					 stopwatch);
		}

		public override void Start () 
		{
			Logger.Log.Info ("Starting Evolution mail backend");
			base.Start ();
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			if (! root.Exists)
				return;

			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;

				if (! dir.Exists)
					continue;
				
				int wd = Inotify.Watch (dir.FullName,
							Inotify.EventType.CreateSubdir
							| Inotify.EventType.DeleteSubdir
							| Inotify.EventType.MovedTo);
				watched [wd] = dir.FullName;

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}
		}

		private void Ignore (string path)
		{
			Inotify.Ignore (path);
			watched.RemoveAt (watched.IndexOfValue (path));
		}

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (subitem == "" || ! watched.Contains (wd))
				return;

			string fullPath = Path.Combine (path, subitem);

			switch (type) {
				
			case Inotify.EventType.CreateSubdir:
				Watch (fullPath);
				break;

			case Inotify.EventType.DeleteSubdir:
				Ignore (fullPath);
				break;

			case Inotify.EventType.MovedTo:
				if (subitem == "summary") {
					// IMAP summary
					log.Info ("Reindexing updated IMAP summary: {0}", fullPath);
					this.IndexSummary (new FileInfo (fullPath));
				} else if (Path.GetExtension (fullPath) == ".ev-summary") {
					// mbox summary
					string mbox_file = Path.ChangeExtension (fullPath, null);
					log.Info ("Reindexing updated mbox: {0}", mbox_file);
					this.IndexMbox (new FileInfo (mbox_file));
				}

				break;
			}
		}

		private bool CheckForMailData ()
		{
			if ((! Directory.Exists (this.local_path)) && (! Directory.Exists (this.imap_path)))
				return true; // continue polling
			
			// Otherwise stop polling and start indexing
			StartWorker();
			return false;
		}

		public string Name {
			get { return "EvolutionMail"; }
		}

		public void IndexSummary (FileInfo summaryInfo)
		{
			EvolutionMailIndexableGeneratorImap generator = new EvolutionMailIndexableGeneratorImap (this, summaryInfo);
			Scheduler.Task task;
			task = NewAddTask (generator, new Scheduler.Hook (generator.Checkpoint));
			// IndexableGenerator tasks default to having priority Scheduler.Priority Generator
			ThisScheduler.Add (task);
		}

		public void IndexMbox (FileInfo mboxInfo)
		{
			EvolutionMailIndexableGeneratorMbox generator = new EvolutionMailIndexableGeneratorMbox (this, mboxInfo);
			Scheduler.Task task;
			task = NewAddTask (generator, new Scheduler.Hook (generator.Checkpoint));
			task.Tag = mboxInfo.FullName;
			// IndexableGenerator tasks default to having priority Scheduler.Priority Generator
			ThisScheduler.Add (task);
		}

		public static Uri EmailUri (string accountName, string folderName, string uid)
		{
			return new Uri (String.Format ("email://{0}/{1};uid={2}",
						       accountName, folderName, uid));
		}

		override protected double RelevancyMultiplier (Hit hit)
		{
			return HalfLifeMultiplierFromProperty (hit, 0.25,
							       "fixme:received", "fixme:sentdate");
		}
	}

}
