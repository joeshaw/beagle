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

using LNI = Lucene.Net.Index;

namespace Beagle.Daemon.EvolutionMailDriver {

	[QueryableFlavor (Name="Mail", Domain=QueryDomain.Local, RequireInotify=false)]
	public class EvolutionMailQueryable : LuceneQueryable {

		public int polling_interval_in_seconds = 60;

		private string local_path, imap_path, imap4_path;

		private MailCrawler crawler;

		// Index versions
		// 1: Original version, stored all recipient addresses as a
		//    RFC822 string
		// 2: Stores recipients in separate properties,
		//    filters/indexes all attachments
		private const int INDEX_VERSION = 2;

		public EvolutionMailQueryable () : base ("MailIndex", INDEX_VERSION)
		{
			this.local_path = Path.Combine (PathFinder.HomeDir, ".evolution/mail/local");
			this.imap_path = Path.Combine (PathFinder.HomeDir, ".evolution/mail/imap");
			this.imap4_path = Path.Combine (PathFinder.HomeDir, ".evolution/mail/imap4");
		}

		private void CrawlHook (Scheduler.Task task)
		{
			crawler.Crawl ();
			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
		}

		//////////////////////////////////////////////////////////////////////////////////////////////

		private void StartWorker ()
		{
			Logger.Log.Info ("Starting Evolution mail backend");

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			// Check that we have data to index
			if ((! Directory.Exists (this.local_path)) && (! Directory.Exists (this.imap_path))) {
				// No mails present, repoll every minute
				Logger.Log.Warn ("Evolution mail store not found, watching for it.");
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForMailData));
				return;
			}

			Logger.Log.Debug ("Starting mail crawl");
			crawler = new MailCrawler (this.local_path, this.imap_path, this.imap4_path);
			crawler.MboxAddedEvent += IndexMbox;
			crawler.SummaryAddedEvent += IndexSummary;
			crawler.Crawl ();
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
			// If there's already a task running for this folder,
			// don't interrupt it.
			if (ThisScheduler.ContainsByTag (summaryInfo.FullName)) {
				Logger.Log.Debug ("Not adding task for already running task: {0}", summaryInfo.FullName);
				return;
			}

			Logger.Log.Debug ("Will index summary {0}", summaryInfo.FullName);
			EvolutionMailIndexableGeneratorImap generator = new EvolutionMailIndexableGeneratorImap (this, summaryInfo);
			Scheduler.Task task;
			task = NewAddTask (generator, new Scheduler.Hook (generator.Checkpoint));
			task.Tag = summaryInfo.FullName;
			// IndexableGenerator tasks default to having priority Scheduler.Priority Generator
			ThisScheduler.Add (task);
		}

		public void IndexMbox (FileInfo mboxInfo)
		{
			// If there's already a task running for this mbox,
			// don't interrupt it.
			if (ThisScheduler.ContainsByTag (mboxInfo.FullName)) {
				Logger.Log.Debug ("Not adding task for already running task: {0}", mboxInfo.FullName);
				return;
			}

			Logger.Log.Debug ("Will index mbox {0}", mboxInfo.FullName);
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
			double t = 1.0;

			// FIXME: We should probably be more careful
			// about how we combine these two numbers into one,
			// since the cardinal value of the score is used for
			// comparisons with hits of other types.  It isn't
			// sufficient to just worry about the ordinal relationship
			// between two scores.
			t *= HalfLifeMultiplierFromProperty (hit, 0.25,
							     "fixme:received", "fixme:sentdate");


			return t;
		}
	}

}
