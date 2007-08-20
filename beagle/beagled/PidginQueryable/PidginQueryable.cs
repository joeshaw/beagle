//
// PidginQueryable.cs
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

namespace Beagle.Daemon.PidginQueryable {

	[QueryableFlavor (Name="Pidgin", Domain=QueryDomain.Local, RequireInotify=false)]
	public class PidginQueryable : LuceneFileQueryable, IIndexableGenerator {

		private string config_dir, log_dir, icons_dir;
		private int polling_interval_in_seconds = 60;
		private PidginBuddyListReader list = new PidginBuddyListReader ();

		public PidginQueryable () : base ("PidginIndex")
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

			Log.Info ("Starting Pidgin IM log backend");

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			if (Inotify.Enabled) {
				Log.Info ("Setting up inotify watches on Pidgin log directories");
				Crawl (false);
			}

			Scheduler.Task task;
			task = NewAddTask (this);
			task.Tag = "Crawling Gaim logs";
			task.Source = this;

			if (!Inotify.Enabled) {
				Scheduler.TaskGroup group = Scheduler.NewTaskGroup ("Repeating Pidgin log crawler", null, AddCrawlTask);
				task.AddTaskGroup (group);
			}

			ThisScheduler.Add (task);

			stopwatch.Stop ();

			Log.Info ("Pidgin log backend worker thread done in {0}", stopwatch); 
		}
		
		public override void Start () 
		{
			base.Start ();
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		/////////////////////////////////////////////////

		private void AddCrawlTask ()
		{
			Scheduler.Task task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlHook));
			task.Tag = "Crawling ~/.gaim/logs to find new logfiles";
			task.Source = this;
			ThisScheduler.Add (task);
		}

		private void CrawlHook (Scheduler.Task task)
		{
			Crawl (true);
			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
		}

		private void Crawl (bool index)
		{
			this.IsIndexing = true;

			if (Inotify.Enabled)
				Inotify.Subscribe (log_dir, OnInotifyNewProtocol, Inotify.EventType.Create);

			// Walk through protocol subdirs
			foreach (string proto_dir in DirectoryWalker.GetDirectories (log_dir))
				CrawlProtocolDirectory (proto_dir, index);
		}

		private void CrawlProtocolDirectory (string proto_dir, bool index)
		{
			if (Inotify.Enabled)
				Inotify.Subscribe (proto_dir, OnInotifyNewAccount, Inotify.EventType.Create);

			// Walk through accounts
			foreach (string account_dir in DirectoryWalker.GetDirectories (proto_dir))
				CrawlAccountDirectory (account_dir, index);
		}

		private void CrawlAccountDirectory (string account_dir, bool index)
		{
			if (Inotify.Enabled)
				Inotify.Subscribe (account_dir, OnInotifyNewRemote, Inotify.EventType.Create);

			// Walk through remote user conversations
			foreach (string remote_dir in DirectoryWalker.GetDirectories (account_dir)) {
				if (remote_dir.IndexOf (".system") < 0)
					CrawlRemoteDirectory (remote_dir, index);
			}
		}

		private void CrawlRemoteDirectory (string remote_dir, bool index)
		{
			if (Inotify.Enabled)
				Inotify.Subscribe (remote_dir, OnInotifyNewConversation, Inotify.EventType.CloseWrite | Inotify.EventType.Modify);

			if (index) {
				foreach (FileInfo file in DirectoryWalker.GetFileInfos (remote_dir))
					if (FileIsInteresting (file.Name))
						IndexLog (file.FullName, Scheduler.Priority.Delayed);

				IsIndexing = false;
			}
		}

		/////////////////////////////////////////////////

		public string StatusName {
			get { return "PidginQueryable"; }
		}

		private IEnumerator log_files = null;

		public void PostFlushHook () { }

		public bool HasNextIndexable ()
		{
			if (log_files == null)
				log_files = DirectoryWalker.GetFileInfosRecursive (log_dir).GetEnumerator ();

			if (log_files.MoveNext ())
				return true;
			else {
				IsIndexing = false;
				return false;
			}
		}

		public Indexable GetNextIndexable ()
		{
			FileInfo file = (FileInfo) log_files.Current;

			if (! file.Exists)
				return null;

			if (IsUpToDate (file.FullName))
				return null;

			Indexable indexable = PidginLogToIndexable (file.FullName);
			
			return indexable;
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

			CrawlProtocolDirectory (Path.Combine (path, subitem), true);
		}

		private void OnInotifyNewAccount (Inotify.Watch watch,
						string path, string subitem, string srcpath,
						Inotify.EventType type)
		{
			if (subitem.Length == 0 || (type & Inotify.EventType.IsDirectory) == 0)
				return;

			CrawlAccountDirectory (Path.Combine (path, subitem), true);
		}

		private void OnInotifyNewRemote (Inotify.Watch watch,
						string path, string subitem, string srcpath,
						Inotify.EventType type)
		{
			if (subitem.Length == 0 || (type & Inotify.EventType.IsDirectory) == 0)
				return;

			CrawlRemoteDirectory (Path.Combine (path, subitem), true);
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
		
		private static Indexable PidginLogToIndexable (string filename)
		{
			Uri uri = UriFu.PathToFileUri (filename);
			Indexable indexable = new Indexable (uri);
			indexable.ContentUri = uri;
			indexable.Timestamp = File.GetLastWriteTimeUtc (filename);
			indexable.MimeType = "beagle/x-pidgin-log";
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

			Indexable indexable = PidginLogToIndexable (filename);
			Scheduler.Task task = NewAddTask (indexable);
			task.Priority = priority;
			task.SubPriority = 0;
			ThisScheduler.Add (task);
		}

		protected override double RelevancyMultiplier (Hit hit)
		{
			return HalfLifeMultiplierFromProperty (hit, 0.25, "fixme:endtime", "fixme:starttime");
		}

		protected override bool HitFilter (Hit hit) 
		{
			// If the protocol isn't set (because maybe we got an
			// exception while we were indexing), this isn't a
			// valid hit.
			if (hit ["fixme:protocol"] == null) {
				Log.Warn ("Discarding IM log hit with missing protocol info: {0}", hit.Uri);
				return false;
			}

			string speakingto = hit ["fixme:speakingto"];

			// We have no idea who we're speaking to.  Bad, but we
			// still want to present it.
			if (speakingto == null || speakingto == String.Empty)
				return true;

			ImBuddy buddy = list.Search (speakingto);
			
			// We might still want to see a chat even if someone's
			// not on our buddy list.
			if (buddy == null) 
				return true;
			
			if (buddy.Alias != "")
 				hit.AddProperty (Beagle.Property.NewKeyword ("fixme:speakingto_alias", buddy.Alias));
 				
 			if (buddy.BuddyIconLocation != "")
 				hit.AddProperty (Beagle.Property.NewUnsearched ("fixme:speakingto_icon", Path.Combine (icons_dir, buddy.BuddyIconLocation)));
			
			return true;
		}

		public override ISnippetReader GetSnippet (string [] query_terms, Hit hit, bool full_text)
		{
			TextReader reader = TextCache.UserCache.GetReader (hit.Uri);

			if (reader == null)
				return null;

			string line = reader.ReadLine ();

			if (line[0] == '<')
				reader = new HtmlRemovingReader (reader);

			return SnippetFu.GetSnippet (query_terms, reader, full_text);
		}
	}
}

