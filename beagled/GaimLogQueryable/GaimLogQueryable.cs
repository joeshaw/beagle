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

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.GaimLogQueryable {

	[QueryableFlavor (Name="IMLog", Domain=QueryDomain.Local)]
	public class GaimLogQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("GaimLogQueryable");

		Hashtable watched = new Hashtable ();

		public GaimLogQueryable () : base (Path.Combine (PathFinder.RootDir, "GaimLogIndex"))
		{
			string home = Environment.GetEnvironmentVariable ("HOME");
			string logDir = Path.Combine (Path.Combine (home, ".gaim"), "logs");

			// FIXME: If ~/.gaim/logs doesn't exist we should set up watches
			// and wait for it to appear instead of just giving up.
			if (! Directory.Exists (logDir))
				return;

			Inotify.InotifyEvent += new InotifyHandler (OnInotifyEvent);

			log.Info ("Scanning IM Logs");
			Stopwatch timer = new Stopwatch ();
			timer.Start ();
			int foundCount = Watch (logDir);
			timer.Stop ();
			log.Info ("Found {0} logs in {1}", foundCount, timer);
		}

		private int Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			if (! root.Exists)
				return 0;

			int fileCount = 0;

			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				int wd = Inotify.Watch (dir.FullName,
							InotifyEventType.CreateSubdir
							| InotifyEventType.Modify);
				watched [wd] = true;

				foreach (FileInfo file in dir.GetFiles ()) {
 					IndexLog (file, -100);
					++fileCount;
				}

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}

			return fileCount;
		}

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     InotifyEventType type,
					     int cookie)
		{
			if (subitem == "" || ! watched.Contains (wd))
				return;

			string fullPath = Path.Combine (path, subitem);

			switch (type) {
				
			case InotifyEventType.CreateSubdir:
				Watch (fullPath);
				break;

			case InotifyEventType.Modify:
				IndexLog (new FileInfo (fullPath), 100);
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

		private void IndexLog (FileInfo file, int priority)
		{
			if (! file.Exists || Driver.IsUpToDate (file))
				return;

			ICollection logs = GaimLog.ScanLog (file);
			foreach (ImLog log in logs) {
				Indexable indexable = ImLogToIndexable (log);
				Driver.ScheduleAddAndMark (indexable, priority, file);
			}
		}
	}
}
