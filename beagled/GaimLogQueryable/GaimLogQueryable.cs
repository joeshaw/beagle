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

	[QueryableFlavor (Name="IMLog", Domain=QueryDomain.Local)]
	public class GaimLogQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("GaimLogQueryable");
		private string config_dir, log_dir;
		
		Hashtable watched = new Hashtable ();

		public GaimLogQueryable () : base ("GaimLogIndex")
		{
			config_dir = Path.Combine (PathFinder.HomeDir, ".gaim");
			log_dir = Path.Combine (config_dir, "logs");
		}
					
		private void StartWorker() 
		{
			// Check to see if ~/.gaim exists.
			if (! Directory.Exists (config_dir)) {
				log.Warn ("IM: {0} not found, watching for it.", config_dir);
				Inotify.Event += WatchForGaim;
				Inotify.Watch (PathFinder.HomeDir,
					       Inotify.EventType.CreateSubdir
					       | Inotify.EventType.MovedFrom
					       | Inotify.EventType.MovedTo);
				return;
			}
		
			// Check to see if ~/.gaim/logs exists.
			if (! Directory.Exists (log_dir)) {
				log.Warn ("IM: {0} not found, watching for it.", log_dir);
				Inotify.Event += WatchForGaim;
				Inotify.Watch (config_dir,
					       Inotify.EventType.CreateSubdir
					       | Inotify.EventType.MovedFrom
					       | Inotify.EventType.MovedTo);
				return;
			}
			
			Inotify.Event += OnInotifyEvent;
			ScanLogs ();
		}
		
		public override void Start () 
		{
			base.Start ();
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		// Scan the logs directory for logs.
		private void ScanLogs ()
		{
			int file_count;
			
			log.Info ("IM: Scanning IM Logs in {0}", log_dir);
			Stopwatch timer = new Stopwatch ();
			timer.Start ();
			file_count = Watch (log_dir);
			timer.Stop ();
			log.Info ("IM: Found {0} logs in {1}", file_count, timer);		
		}

		// Sets up an Inotify watch on all subdirectories withing ~/.gaim/logs
		// Also indexes the existing log files.
		private int Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			
			if (! root.Exists) {
				log.Warn ("IM: {0} cannot watch path. It doesn't exist.", path);
				return 0;	
			}
			
			int file_count = 0;
			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				// Setup watches on the present directory.
				int wd = Inotify.Watch (dir.FullName,
							Inotify.EventType.CreateSubdir
							| Inotify.EventType.Modify);
				
				watched [wd] = true;

				// Index the existing files.
				foreach (FileInfo file in dir.GetFiles ()) {
 					IndexLog (file.FullName, Scheduler.Priority.Delayed);
					++file_count;
				}

				// Add all subdirectories to the queue so their files can be indexed.
				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}
			
			return file_count;
		}
		
		// Watches to see if the ~/.gaim or ~/.gaim/logs directory was created.
		private void WatchForGaim (int wd,
					     string path,
 					     string subitem,
					     Inotify.EventType type,
					     uint cookie)
		{
			
			// Checking to see if the Gaim config directory was created.
			if (subitem == ".gaim" && path == PathFinder.HomeDir) {
				log.Info ("IM: Found the Gaim config directory.");
				Inotify.Event -= WatchForGaim;
				StartWorker ();
				return;
			}
			
			// Checking to see if the Gaim Log directory was created.
			if (subitem == "logs" && path == config_dir) {
				log.Info ("IM: Found the Gaim logs directory.");
				Inotify.Event -= WatchForGaim;
				Inotify.Ignore (path);
				StartWorker ();
				return;
			}
		}
		
		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     Inotify.EventType type,
					     uint cookie)
		{
			if (subitem == "" || ! watched.Contains (wd))
				return;

			string full_path = Path.Combine (path, subitem);

			switch (type) {
				
				case Inotify.EventType.CreateSubdir:
					Watch (full_path);
					break;
	
				case Inotify.EventType.Modify:
					IndexLog (full_path, Scheduler.Priority.Immediate);
					break;
			}
		}
		
		
		private static Indexable ImLogToIndexable (ImLog log)
		{
			Indexable indexable = new Indexable (log.Uri);
			indexable.Timestamp = log.Timestamp;
			indexable.Type = "IMLog";

			// We don't have a specific mime type for this
			// blob, but a mime type must be specified for
			// indexables that provide a stream
			indexable.MimeType = "text/plain";
			
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

		override public string GetSnippet (QueryBody body, Hit hit)
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

			// FIXME: This is very lame, and doesn't do the
			// right thing w/ stemming, word boundaries, etc.
			foreach (ImLog.Utterance utt in log.Utterances) {
				string lower_text = utt.Text.ToLower ();
				string text = utt.Text;
				int i = -1;
				foreach (string query_text in body.Text) {
					i = lower_text.IndexOf (query_text.ToLower ());
					if (i >= 0) {
						text = String.Concat (text.Substring (0, i), "<b>", text.Substring (i, query_text.Length), "</b>", text.Substring (i + query_text.Length));
						lower_text = text.ToLower ();
					}
				}
				if (i >= 0)
					return text;
			}

			// If all else fails, return the log's generic snippet
			return log.Snippet;
		}
	}
}
