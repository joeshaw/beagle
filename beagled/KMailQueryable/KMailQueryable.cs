//
// KMailQueryable.cs
//
// Copyright (C) 2005 Novell, Inc.
// Copyright (C) 2005 Debajyoti Bera
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

namespace Beagle.Daemon.KMailQueryable {

	[QueryableFlavor (Name="KMail", Domain=QueryDomain.Local, RequireInotify=false)]
	public class KMailQueryable : LuceneFileQueryable {

		// for non-inotify case, poll after this number of seconds
		public const int polling_interval_in_seconds = 300;
		// mail folder paths
		private string local_path, dimap_path;
		// indexers - one for each mailfolder path
		private KMailIndexer local_indexer, dimap_indexer;
		// global variable
		public static bool gmime_initialized = false;
		public static void InitializeGMime ()
		{
			if (!gmime_initialized) {
				GMime.Global.Init ();
				gmime_initialized = true;
			}
		}

		// name of the sentmail folder - should be parsed from kmailrc
		private string sentmail_foldername;
		public string SentMailFolderName {
			get { return sentmail_foldername; }
		}
		
		public KMailQueryable () : base ("KMailIndex")
		{
			// the local mail path is different for different distributions
			local_path = GuessLocalFolderPath ();
			if (local_path == null) {
				Logger.Log.Info ("KMail folders not found. Will keep trying ");
			} else
				Logger.Log.Info ("Guessing for location of KMail folders ... found at " + local_path);
			// I hope there is no ambiguity over imap path :P
			dimap_path = Path.Combine (PathFinder.HomeDir, ".kde");
			dimap_path = Path.Combine (dimap_path, "share");
			dimap_path = Path.Combine (dimap_path, "apps");
			dimap_path = Path.Combine (dimap_path, "kmail");
			dimap_path = Path.Combine (dimap_path, "dimap");

			local_indexer = null;
			dimap_indexer = null;
			sentmail_foldername = "sent-mail";
		}

		//////////////////////////////////////////////////////////////////////////////////////////////

		/**
		 * initial method called by the daemon
		 */
		public override void Start () 
		{
			base.Start ();
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		/**
		 * for non-inotify case, this method is invoked repeatedly
		 */
		private void CrawlHook (Scheduler.Task task)
		{
			if (local_indexer != null)
				local_indexer.Crawl ();
			if (dimap_indexer != null)
				dimap_indexer.Crawl ();
			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
		}

		/**
		 * called by Start(), starts actual work
		 * create indexers
		 * ask indexers to crawl the mails
		 * for non-inotify case, ask to poll
		 */
		private void StartWorker ()
		{
			Logger.Log.Info ("Starting KMail backend");

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			// check if there is at all anything to crawl
                        if ( local_path == null && (!Directory.Exists (dimap_path))) {
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
				Logger.Log.Debug ("KMail directories (local mail) " + dimap_path + " not found, will repoll.");
                                return;
			}

			Logger.Log.Debug ("Starting mail crawl");
			State = QueryableState.Crawling;
			if (local_path != null) {
				local_indexer = new KMailIndexer (this, "local", local_path);
				local_indexer.Crawl ();
			}
			// FIXME: parse kmailrc to get dimap account name
			if (Directory.Exists (dimap_path)) {
				dimap_indexer = new KMailIndexer (this, "dimap", dimap_path);
				dimap_indexer.Crawl ();
			}
			State = QueryableState.Idle;
			Logger.Log.Debug ("Mail crawl done");

			if (! Inotify.Enabled) {
				Scheduler.Task task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlHook));
				task.Tag = "Crawling Maildir directories";
				task.Source = this;
				task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
				ThisScheduler.Add (task);
			}

			stopwatch.Stop ();
			Logger.Log.Info ("KMail driver worker thread done in {0}", stopwatch);
		}

		/** 
		 * use this method to determine if we have anything to crawl and index
		 */
		private bool CheckForExistence ()
                {
			local_path = GuessLocalFolderPath ();
                        if (local_path == null && (!Directory.Exists (dimap_path)))
                                return true;

			StartWorker();
                        return false;
                }

		/////////////////////////////////////////////////////////////////////////////

		// FIXME: How to determine if an mbox hit is valid without scanning the whole file

		public string Name {
			get { return "KMail"; }
		}

		/** 
		 * path of local maildir - mine is in ~/.Mail
		 * This is distribution specific. Mandrake puts kmail mails in
		 * ~/.Mail whereas default kmail folder location is ~/Mail
		 * I guess each distribution can fix this path as they know what is
		 * the path.
		 * Till then, using a guesser to find out which of ~/.Mail and ~/Mail
		 * is valid.
		 * Guesses the kmail local folder path
		 * first try ~/.Mail, then try ~/Mail
		 * then try ~/.kde/share/apps/kmail/mail
		 */
		private string GuessLocalFolderPath ()
		{
			string location1 = Path.Combine (PathFinder.HomeDir, "Mail");
			string location2 = Path.Combine (PathFinder.HomeDir, ".Mail");
			string location3 = Path.Combine (PathFinder.HomeDir, ".kde");
			location3 = Path.Combine (location3, "share");
			location3 = Path.Combine (location3, "apps");
			location3 = Path.Combine (location3, "kmail");
			location3 = Path.Combine (location3, "mail");
			if (GuessLocalFolder (location1))
				return location1;
			else if (GuessLocalFolder (location2))
				return location2;
			else if (GuessLocalFolder (location3))
				return location3;
			else 
				return null;
		}

		/**
		 * to check if the path represents a kmail directory
		 * find all file named .name.index
		 * there should be matching directories/files with name "name"
		 */
		private bool GuessLocalFolder (string path)
		{
			if (! Directory.Exists (path))
				return false;
			bool flag = true;
			foreach (FileInfo file in DirectoryWalker.GetFileInfos (path)) {
				if (!file.Name.EndsWith (".index"))
					continue;
				// the filename is of pattern .name.index
				// get name from filename
				string filename = file.Name.Substring (1, file.Name.LastIndexOf (".index")-1);
				if (!Directory.Exists (Path.Combine (path, filename)) &&
				    !Directory.Exists (Path.Combine (path, "." + filename + ".directory")) &&
				    !File.Exists (Path.Combine (path, filename))) {
					flag = false;
					Logger.Log.Warn ("KMail backend: " + path + " contains a KMail index file but no corresponding mail file or directory. Ignoring directory!");
				}
			}
			return flag;	
		}

	}

}
