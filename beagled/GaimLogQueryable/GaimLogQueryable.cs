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

	[QueryableFlavor (Name="IMLog", Domain=QueryDomain.Local, RequireInotify=false)]
	public class GaimLogQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("GaimLogQueryable");

		private string config_dir, log_dir;

		private int polling_interval_in_seconds = 60;
		
		Hashtable watched = new Hashtable ();

		private GaimLogCrawler crawler;

		public GaimLogQueryable () : base ("GaimLogIndex")
		{
			config_dir = Path.Combine (PathFinder.HomeDir, ".gaim");
			log_dir = Path.Combine (config_dir, "logs");
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

			if (Inotify.Enabled) {
				Inotify.Event += OnInotifyEvent;
				Watch (log_dir);
			}

			crawler = new GaimLogCrawler (log_dir);
			Crawl ();

			if (!Inotify.Enabled) {
				Scheduler.Task task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlHook));
                                task.Tag = "Crawling ~/.gaim/logs to find new logfiles";
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

		private void Crawl ()
                {
                        crawler.Crawl ();
                        foreach (FileInfo file in crawler.Logs) {			    
                                IndexLog (file.FullName, Scheduler.Priority.Delayed);
			}
                }

                private void CrawlHook (Scheduler.Task task)
                {
                        Crawl ();
                        task.Reschedule = true;
                        task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
                }

		/////////////////////////////////////////////////

		// Sets up an Inotify watch on all subdirectories withing ~/.gaim/logs
		private void Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			
			if (! root.Exists) {
				log.Warn ("IM: {0} cannot watch path. It doesn't exist.", path);
				return;	
			}
			
			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				// Setup watches on the present directory.
				int wd = Inotify.Watch (dir.FullName,
							Inotify.EventType.Create | Inotify.EventType.Modify);
				
				watched [wd] = true;

				// Add all subdirectories to the queue so their files can be indexed.
				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}
		}
		
		/////////////////////////////////////////////////

		private bool CheckForExistence ()
		{
			if (!Directory.Exists (log_dir))
				return true;

			this.Start ();

			return false;
		}

		/////////////////////////////////////////////////

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (subitem == "" || ! watched.Contains (wd))
				return;

			string full_path = Path.Combine (path, subitem);

			if ((type & Inotify.EventType.Create) != 0 && (type & Inotify.EventType.IsDirectory) != 0) {
				Watch (full_path);
				return;
			}

			if ((type & Inotify.EventType.Modify) != 0) {
				IndexLog (full_path, Scheduler.Priority.Immediate);
				return;
			}
		}

		/////////////////////////////////////////////////
		
		private static Indexable ImLogToIndexable (ImLog log)
		{
			Indexable indexable = new Indexable (log.Uri);
			indexable.Timestamp = log.Timestamp;
			indexable.Type = "IMLog";

			StringBuilder text = new StringBuilder ();
			foreach (ImLog.Utterance utt in log.Utterances) {
				//Console.WriteLine ("[{0}][{1}]", utt.Who, utt.Text);
				text.Append (utt.Text);
				text.Append (" ");
			}

			indexable.AddProperty (Property.NewKeyword ("fixme:file", log.LogFile));
			indexable.AddProperty (Property.NewKeyword ("fixme:offset", log.LogOffset));
			indexable.AddProperty (Property.NewDate ("fixme:starttime", log.StartTime));
			indexable.AddProperty (Property.NewKeyword ("fixme:speakingto", log.SpeakingTo));
			indexable.AddProperty (Property.NewKeyword ("fixme:identity", log.Identity));
			indexable.AddProperty (Property.NewDate ("fixme:endtime", log.EndTime));

			indexable.AddProperty (Property.New ("fixme:client", log.Client));
			indexable.AddProperty (Property.New ("fixme:protocol", log.Protocol));

			StringReader reader = new StringReader (text.ToString ());
			indexable.SetTextReader (reader);
			
			return indexable;
		}

		private void IndexLog (string filename, Scheduler.Priority priority)
		{
			FileInfo info = new FileInfo (filename);
			if (! info.Exists
			    || this.FileAttributesStore.IsUpToDate (filename))
				return;

			Scheduler.TaskGroup group;
			group = NewMarkingTaskGroup (filename, info.LastWriteTime);

			ICollection logs = GaimLog.ScanLog (info);
			foreach (ImLog log in logs) {
				Indexable indexable = ImLogToIndexable (log);
				Scheduler.Task task = NewAddTask (indexable);
				task.Priority = priority;
				task.SubPriority = 0;
				task.AddTaskGroup (group);
				ThisScheduler.Add (task);
			}
		}

		override protected double RelevancyMultiplier (Hit hit)
		{
			return HalfLifeMultiplierFromProperty (hit, 0.25,
							       "fixme:endtime", "fixme:starttime");
		}

		override public string GetSnippet (string[] query_terms, Hit hit)
		{
			// FIXME: This does the wrong thing for old-style logs.
			string file = hit ["fixme:file"];
			ICollection logs = GaimLog.ScanLog (new FileInfo (file));
			IEnumerator iter = logs.GetEnumerator ();
			ImLog log = null;
			if (iter.MoveNext ())
				log = iter.Current as ImLog;
			if (log == null)
				return null;

			string result = "";

			// FIXME: This is very lame, and doesn't do the
			// right thing w/ stemming, word boundaries, etc.
			foreach (ImLog.Utterance utt in log.Utterances) {
				string text = utt.Text;
				string who = utt.Who;
				
				string snippet = SnippetFu.GetSnippet (query_terms, new StringReader (text));

				if (snippet == null || snippet == "")
					continue;

				result += String.Format ("{0}: {1} ", who, snippet);

				if (result.Length > 300)
					break;
			}

			if (result != "")
				return result;
			else
				return log.Snippet;
		}

		override protected bool HitIsValid (Uri uri)
		{
			if (File.Exists (uri.LocalPath))
				return true;
			
			return false;
		}
	}
}
