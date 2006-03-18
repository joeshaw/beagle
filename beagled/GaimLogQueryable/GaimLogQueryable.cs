//
// GaimLogQueryable.cs
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
using System.Text;
using System.Threading;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.GaimLogQueryable {

	[QueryableFlavor (Name="GaimLog", Domain=QueryDomain.Local, RequireInotify=false)]
	public class GaimLogQueryable : LuceneFileQueryable {

		private static Logger log = Logger.Get ("GaimLogQueryable");

		private string config_dir, log_dir, icons_dir;

		private int polling_interval_in_seconds = 60;
		
		private GaimBuddyListReader list = new GaimBuddyListReader ();

		public GaimLogQueryable () : base ("GaimLogIndex")
		{
			config_dir = Path.Combine (PathFinder.HomeDir, ".gaim");
			log_dir = Path.Combine (config_dir, "logs");
			icons_dir = Path.Combine (config_dir, "icons");
		}

		/////////////////////////////////////////////////
					
		private void StartWorker() 
		{	
			if (! Directory.Exists (log_dir)) {
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
				return;
			}

			log.Info ("Starting Gaim log backend");

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			State = QueryableState.Crawling;
			Crawl ();
			State = QueryableState.Idle;

			if (!Inotify.Enabled) {
				Scheduler.Task task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlHook));
				task.Tag = "Crawling ~/.gaim/logs to find new logfiles";
				task.Source = this;
				ThisScheduler.Add (task);
			}

			stopwatch.Stop ();

			log.Info ("Gaim log backend worker thread done in {0}", stopwatch); 
		}
		
		public override void Start () 
		{
			base.Start ();
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		/////////////////////////////////////////////////

		private void CrawlHook (Scheduler.Task task)
		{
			Crawl ();
			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
		}

		private void Crawl ()
		{
			Inotify.Subscribe (log_dir, OnInotifyNewProtocol, Inotify.EventType.Create);

			// Walk through protocol subdirs
			foreach (string proto_dir in DirectoryWalker.GetDirectories (log_dir))
				CrawlProtocolDirectory (proto_dir);
		}

		private void CrawlProtocolDirectory (string proto_dir)
		{
			Inotify.Subscribe (proto_dir, OnInotifyNewAccount, Inotify.EventType.Create);

			// Walk through accounts
			foreach (string account_dir in DirectoryWalker.GetDirectories (proto_dir))
				CrawlAccountDirectory (account_dir);
		}

		private void CrawlAccountDirectory (string account_dir)
		{
			Inotify.Subscribe (account_dir, OnInotifyNewRemote, Inotify.EventType.Create);

			// Walk through remote user conversations
			foreach (string remote_dir in DirectoryWalker.GetDirectories (account_dir))
				CrawlRemoteDirectory (remote_dir);
		}

		private void CrawlRemoteDirectory (string remote_dir)
		{
			Inotify.Subscribe (remote_dir, OnInotifyNewConversation, Inotify.EventType.CloseWrite | Inotify.EventType.Modify);

			foreach (FileInfo file in DirectoryWalker.GetFileInfos (remote_dir))
				if (FileIsInteresting (file.Name))
					IndexLog (file.FullName, Scheduler.Priority.Delayed);
		}

		/////////////////////////////////////////////////

		private bool CheckForExistence ()
		{
			if (!Directory.Exists (log_dir))
				return true;

			this.Start ();

			return false;
		}

		private bool FileIsInteresting (string filename)
		{
			if (filename.Length < 21)
				return false;

			string ext = Path.GetExtension (filename);
			if (ext != ".txt" && ext != ".html")
				return false;

			// Pre-gaim 2.0.0 logs are in the format "2005-07-22.161521.txt".  Afterward a
			// timezone field as added, ie. "2005-07-22.161521-0500EST.txt".
			//
			// This is a lot uglier than a regexp, but they are so damn expensive.

			return Char.IsDigit (filename [0]) && Char.IsDigit (filename [1])
				&& Char.IsDigit (filename [2]) && Char.IsDigit (filename [3])
				&& filename [4] == '-'
				&& Char.IsDigit (filename [5]) && Char.IsDigit (filename [6])
				&& filename [7] == '-'
				&& Char.IsDigit (filename [8]) && Char.IsDigit (filename [9])
				&& filename [10] == '.'
				&& Char.IsDigit (filename [11]) && Char.IsDigit (filename [12])
				&& Char.IsDigit (filename [13]) && Char.IsDigit (filename [14])
				&& Char.IsDigit (filename [15]) && Char.IsDigit (filename [16])
				&& (filename [17] == '+' || filename [17] == '-' || filename [17] == '.');
		}

		/////////////////////////////////////////////////

		private void OnInotifyNewProtocol (Inotify.Watch watch,
						string path, string subitem, string srcpath,
						Inotify.EventType type)
		{
			if (subitem.Length == 0 || (type & Inotify.EventType.IsDirectory) == 0)
				return;

			CrawlProtocolDirectory (Path.Combine (path, subitem));
		}

		private void OnInotifyNewAccount (Inotify.Watch watch,
						string path, string subitem, string srcpath,
						Inotify.EventType type)
		{
			if (subitem.Length == 0 || (type & Inotify.EventType.IsDirectory) == 0)
				return;

			CrawlAccountDirectory (Path.Combine (path, subitem));
		}

		private void OnInotifyNewRemote (Inotify.Watch watch,
						string path, string subitem, string srcpath,
						Inotify.EventType type)
		{
			if (subitem.Length == 0 || (type & Inotify.EventType.IsDirectory) == 0)
				return;

			CrawlRemoteDirectory (Path.Combine (path, subitem));
		}

		private void OnInotifyNewConversation (Inotify.Watch watch,
						string path, string subitem, string srcpath,
						Inotify.EventType type)
		{
			if (subitem.Length == 0 || (type & Inotify.EventType.IsDirectory) != 0)
				return;

			if (FileIsInteresting (subitem))
				IndexLog (Path.Combine (path, subitem), Scheduler.Priority.Immediate);			
		}

		/////////////////////////////////////////////////
		
		private static Indexable ImLogToIndexable (string filename)
		{
			Uri uri = UriFu.PathToFileUri (filename);
			Indexable indexable = new Indexable (uri);
			indexable.ContentUri = uri;
			indexable.Timestamp = File.GetLastWriteTimeUtc (filename);
			indexable.MimeType = GaimLog.MimeType;
			indexable.HitType = "IMLog";
			indexable.CacheContent = false;

			return indexable;
		}

		private void IndexLog (string filename, Scheduler.Priority priority)
		{
			if (! File.Exists (filename))
				return;

			if (IsUpToDate (filename))
				return;

			Indexable indexable = ImLogToIndexable (filename);
			Scheduler.Task task = NewAddTask (indexable);
			task.Priority = priority;
			task.SubPriority = 0;
			ThisScheduler.Add (task);
		}

		override protected double RelevancyMultiplier (Hit hit)
		{
			return HalfLifeMultiplierFromProperty (hit, 0.25, "fixme:endtime", "fixme:starttime");
		}

		override protected bool HitFilter (Hit hit) 
		{
			ImBuddy buddy = list.Search (hit ["fixme:speakingto"]);
			
			if (buddy != null) {
				if (buddy.Alias != "")
					hit ["fixme:speakingto_alias"] = buddy.Alias;
				
				if (buddy.BuddyIconLocation != "")
					hit ["fixme:speakingto_icon"] = Path.Combine (icons_dir, buddy.BuddyIconLocation);
			}
			
			return true;
		}

	}
}

