
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

namespace Beagle.Daemon {

	internal class MailCrawler : Crawler {
		
		EvolutionMailQueryable queryable;

		public MailCrawler (EvolutionMailQueryable queryable, string fingerprint) : base (fingerprint)
		{
			this.queryable = queryable;
		}

		protected override bool SkipByName (FileSystemInfo info)
		{
			// Don't skip directories...
			if (info as DirectoryInfo != null)
				return false;

			if (info.Name != "summary" && !File.Exists (info.FullName + ".ev-summary"))
				return true;

			return false;
		}

		protected override void CrawlFile (FileSystemInfo info)
		{
			FileInfo file = info as FileInfo;

			if (info.Name == "summary")
				this.queryable.IndexSummary (file);
			else if (File.Exists (info.FullName + ".ev-summary"))
				this.queryable.IndexMbox (file);
		}
	}

	[QueryableFlavor (Name="Mail", Domain=QueryDomain.Local)]
	public class EvolutionMailQueryable : LuceneQueryable {

		public static Logger log = Logger.Get ("mail");

		private SortedList watched = new SortedList ();
		private MailCrawler crawler;

		private object lockObj = new object ();
		private ArrayList AddedUris = new ArrayList ();
		private bool queryRunning = false;

		public EvolutionMailQueryable () : base ("MailIndex")
		{
		}

		private void StartWorker ()
		{
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			string home = Environment.GetEnvironmentVariable ("HOME");
			string local_path = Path.Combine (home, ".evolution/mail/local");
			string imap_path = Path.Combine (home, ".evolution/mail/imap");

			// Get notification when an index or summary file changes
			Inotify.Event += OnInotifyEvent;
			Watch (local_path);
			Watch (imap_path);

			this.crawler = new MailCrawler (this, this.Driver.Fingerprint);
			Shutdown.ShutdownEvent += OnShutdown;

			this.crawler.ScheduleCrawl (new DirectoryInfo (local_path), -1);
			this.crawler.ScheduleCrawl (new DirectoryInfo (imap_path), -1);

			stopwatch.Stop ();
			Logger.Log.Info ("Evolution mail driver worker thread done in {0}",
					 stopwatch);
		}

		public override void Start () 
		{
			base.Start ();
			
			Thread th = new Thread (new ThreadStart (StartWorker));
			th.Start ();
		}

		private void OnShutdown ()
		{
			this.crawler.Stop ();
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
	}

}
