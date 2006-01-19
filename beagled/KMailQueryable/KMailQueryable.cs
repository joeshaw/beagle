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
		 * It is possible to have the path specified in kmailrc. It might not
		 * be present, in which case try to play a guessing game.
		 * Till then, using a guesser to find out which of ~/.Mail and ~/Mail
		 * is valid.
		 * Guesses the kmail local folder path
		 * first try ~/.Mail, then try ~/Mail
		 * then try ~/.kde/share/apps/kmail/mail
		 */
		private string GuessLocalFolderPath ()
		{
			string locationrc = GetLocalFolderPathFromKmailrc ();
			Logger.Log.Debug ("Reading kmail local-mail location from kmailrc: " + 
					    (locationrc == null ? "Unavailable" : locationrc));
			string location1 = Path.Combine (PathFinder.HomeDir, "Mail");
			string location2 = Path.Combine (PathFinder.HomeDir, ".Mail");

			string location3 = Path.Combine (PathFinder.HomeDir, ".kde");
			location3 = Path.Combine (location3, "share");
			location3 = Path.Combine (location3, "apps");
			location3 = Path.Combine (location3, "kmail");
			location3 = Path.Combine (location3, "mail");

			if (locationrc != null && GuessLocalFolder (locationrc))
				return locationrc;
			else if (GuessLocalFolder (location1))
				return location1;
			else if (GuessLocalFolder (location2))
				return location2;
			else if (GuessLocalFolder (location3))
				return location3;
			else 
				return null;
		}

		/**
		 * to check if the path represents a kmail directory:
		 * for all directories and files named "ddd" and not starting with a '.',
		 * there should be matching index file named .ddd.index
		 */
		private bool GuessLocalFolder (string path)
		{
			if (! Directory.Exists (path))
				return false;
			bool flag = true;

			foreach (string subdirname in DirectoryWalker.GetDirectoryNames (path)) {
				if (subdirname.StartsWith ("."))
					continue;
				// index-file name is of pattern .name.index
				string indexfile = Path.Combine (path, "." + subdirname + ".index");
				if (! File.Exists (indexfile)) {
					flag = false;
					Logger.Log.Warn ( "KMail backend: " + 
						path + 
						" contains a maildir directory but no corresponding index file. Probably not a KMail mail directory. Ignoring this location!");
					break;
				}
			}

			if (! flag)
				return false;

			foreach (FileInfo file in DirectoryWalker.GetFileInfos (path)) {
				if (file.Name.StartsWith ("."))
					continue;
				// index-file name is of pattern .name.index
				string indexfile = Path.Combine (path, "." + file.Name + ".index");
				if (! File.Exists (indexfile)) {
					flag = false;
					Logger.Log.Warn ( "KMail backend: " + 
						path + 
						" contains an mbox file but no corresponding index file. Probably not a KMail mail directory. Ignoring this location!");
					break;
				}
			}
			
			return flag;	
		}

		/**
		 * tries to extract folder name from ~/.kde/share/config/kmailrc
		 */
		private string GetLocalFolderPathFromKmailrc ()
		{
			string kmailrc = Path.Combine (PathFinder.HomeDir, ".kde");
			kmailrc = Path.Combine (kmailrc, "share");
			kmailrc = Path.Combine (kmailrc, "config");
			kmailrc = Path.Combine (kmailrc, "kmailrc");

			if (File.Exists (kmailrc)) {
				StreamReader reader = new StreamReader (kmailrc);
				string section = "";
				string line;
				while ((line = reader.ReadLine ()) != null) {
					if (line.StartsWith ("[") && line.EndsWith ("]")) {
						section = line;
					}
					if (section == "[General]") {
						if (line.StartsWith ("folders=") && line.Length > 8) {
							return ExpandEnvVariables (line.Substring(8));
						}
					}
				}
			}

			return null;
		}

		/**
		 * expands environment variables in the location string e.g.
		 * folders=$HOME/.kde/share/...
		 */
		private string ExpandEnvVariables (string path)
		{
			int dollar_pos = path.IndexOf ('$');
			if (dollar_pos == -1)
				return path;
			
			System.Text.StringBuilder sb = 
				new System.Text.StringBuilder ( (dollar_pos == 0 ? "" : path.Substring (0, dollar_pos)));
			
			while (dollar_pos != -1 && dollar_pos + 1 < path.Length) {
				// FIXME: kconfigbase.cpp contains an additional case, $(expression)/.kde/...
				// Ignoring such complicated expressions for now. Volunteers ;) ?
				int end_pos = dollar_pos;
				if (path [dollar_pos + 1] != '$') {
					string var_name;
					end_pos ++;
					if (path [end_pos] == '{') {
						while ((end_pos < path.Length) && 
						       (path [end_pos] != '}'))
							end_pos ++;
						end_pos ++;
						var_name = path.Substring (dollar_pos + 2, end_pos - dollar_pos - 3);
					} else {
						while ((end_pos < path.Length) &&
						       (Char.IsNumber (path [end_pos]) ||
							Char.IsLetter (path [end_pos]) ||
							path [end_pos] == '_'))
							end_pos ++;
						var_name = path.Substring (dollar_pos + 1, end_pos - dollar_pos - 1);
					}
					string value_env = null;
					if (var_name != String.Empty)
						value_env = Environment.GetEnvironmentVariable (var_name);
					if (value_env != null) {
						sb.Append (value_env);
					}
					// else, no environment variable with that name exists. ignore
				}else // else, ignore the first '$', second one will be expanded
					end_pos ++;
				if (end_pos >= path.Length)
					break;
				dollar_pos = path.IndexOf ('$', end_pos);
				if (dollar_pos == -1) {
					sb.Append (path.Substring (end_pos));
				} else {
					sb.Append (path.Substring (end_pos, dollar_pos - end_pos));
				}
			}

			return sb.ToString ();
		}

	}

}
