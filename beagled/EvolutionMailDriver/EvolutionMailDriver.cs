
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

		public static Logger log = Logger.Get ("mail");
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

		//////////////////////////////////////////////////////////////////////////////////////////////

		// Evil hack: manipulate the lucene index directly to extract
		// statistics about who you sent mail to.
		static private void AnalyzeYourRecipients (LuceneDriver driver)
		{
			LNI.IndexReader reader;
			reader = LNI.IndexReader.Open (driver.Store);

			LNI.TermEnum term_enum;
			term_enum = reader.Terms ();

			const string prop_field = "prop:k:fixme:sentTo";

			LNI.Term skip_to;
			skip_to = new LNI.Term (prop_field, ""); // ACK!

			term_enum.SkipTo (skip_to);
			
			int N = 0;
			while (term_enum.Next ()) {
				LNI.Term t = term_enum.Term ();
				if (t.Field () != prop_field) // Double-ACK!
					break;
				
				AddAsYourRecipient (t.Text (), term_enum.DocFreq ());
				++N;
			}
			term_enum.Close ();

			reader.Close ();
		}

		static private Hashtable your_recipient_weights = new Hashtable ();
		static private int total_recipient_weight = 0;

		static private void AddAsYourRecipient (string address, int weight)
		{
			object obj = your_recipient_weights [address];
			int n = obj != null ? (int) obj : 0;
			your_recipient_weights [address] = n + weight;
		}

		// FIXME: We update the weights when we add indexables, but not
		// when we remove records... the daemon has to be restarted
		// to pick up those changes.
		static public void AddAsYourRecipient (Indexable indexable)
		{
			if (indexable == null)
				return;
			foreach (Property prop in indexable.Properties)
				if (prop.Key == "fixme:sentTo")
					AddAsYourRecipient (prop.Value, 1);
		}

		static private double AnalyzeYourRecipientsMultiplier (Hit hit)
		{
			// The first magic constant: exempt mail that is less than
			// one month old.
			if (hit.DaysSinceTimestamp < 30)
				return 1.0;

			int weight = 0;
			foreach (Property prop in hit.Properties) {

				// This optimization only counts for mails you
				// received: if you sent this mail, bail out immediately.
				if (prop.Key == "fixme:isSent")
					return 1.0;

				if (prop.Key == "fixme:gotFrom") {
					object obj = your_recipient_weights [prop.Value];
					if (obj != null)
						weight = (int) obj;
					break;
				}
			}
			
			const double min_multiplier = 0.6;
			const int weight_threshold = 10;

			if (weight <= 0)
				return min_multiplier;

			if (weight >= weight_threshold)
				return 1.0;

			// t == 0.0 if weight == 0
			// t == 1.0 if weight == weight_threshold
			double t = weight / (double) weight_threshold;

			double multiplier;

			multiplier = min_multiplier + (1 - min_multiplier) * t;

			return multiplier;

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
				log.Warn ("Evolution mail store not found, watching for it.");
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForMailData));
				return;
			}

			// This might be dangerous.
			AnalyzeYourRecipients (Driver);

			// Get notification when an index or summary file changes
			if (Inotify.Enabled) {
				Watch (this.local_path);
				Watch (this.imap_path);
				Watch (this.imap4_path);
			}

			Logger.Log.Debug ("Starting mail crawl");
			crawler = new MailCrawler (this.local_path, this.imap_path, this.imap4_path);
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

				Inotify.Subscribe (dir.FullName, OnInotifyEvent,
							Inotify.EventType.Create
							| Inotify.EventType.Delete
							| Inotify.EventType.MovedTo);

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}
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
				Watch (fullPath);
				return;
			}

			if ((type & Inotify.EventType.Delete) != 0 && (type & Inotify.EventType.IsDirectory) != 0) {
				watch.Unsubscribe ();
				return;
			}

			if ((type & Inotify.EventType.MovedTo) != 0) {
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
			double t = 1.0;

			// FIXME: We should probably be more careful
			// about how we combine these two numbers into one,
			// since the cardinal value of the score is used for
			// comparisons with hits of other types.  It isn't
			// sufficient to just worry about the ordinal relationship
			// between two scores.
			t *= HalfLifeMultiplierFromProperty (hit, 0.25,
							     "fixme:received", "fixme:sentdate");

			t *= AnalyzeYourRecipientsMultiplier (hit);

			return t;
		}
	}

}
