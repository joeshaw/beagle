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
using BU = Beagle.Util;

namespace Beagle.Daemon.GaimLogQueryable {

	[QueryableFlavor (Name="GaimLogQueryable", Domain=QueryDomain.Local)]
	public class GaimLogQueryable : LuceneQueryable {

		private class GaimLogCrawler : Crawler {

			GaimLogQueryable glq;

			public GaimLogCrawler (GaimLogQueryable _glq) : base (_glq.Driver.Fingerprint)
			{
				glq = _glq;
			}

			protected override void CrawlFile (FileSystemInfo info)
			{
				FileInfo file = info as FileInfo;
				// We don't need to do anything with directories.
				if (file == null)
					return;

				glq.IndexLog (file);
			}

		}

		FileSystemEventMonitor monitor;

		public GaimLogQueryable () : base ("IMLog",
						   Path.Combine (PathFinder.RootDir, "GaimLogQueryable"))
		{
			string home = Environment.GetEnvironmentVariable ("HOME");
			DirectoryInfo logDir = new DirectoryInfo (Path.Combine (Path.Combine (home, ".gaim"), "logs"));

			// First, do a quick crawl to make sure our logs are all up-to-date
			GaimLogCrawler crawler = new GaimLogCrawler (this);
			crawler.ScheduleCrawl (logDir, -1);
			crawler.StopWhenEmpty ();

			monitor = new FileSystemEventMonitor ();
			monitor.FileSystemEvent += OnFileSystemEvent;
			monitor.Subscribe (logDir, true);
		}

		protected void OnFileSystemEvent (FileSystemEventMonitor monitor,
						  FileSystemEventType eventType,
						  string oldPath,
						  string newPath)
		{
			if (eventType == FileSystemEventType.Created || eventType == FileSystemEventType.Changed)
				IndexLog (new FileInfo (newPath));
		}

		private static Indexable ImLogToIndexable (BU.ImLog log)
		{
			Indexable indexable = new Indexable (log.Uri);
			indexable.Timestamp = log.Timestamp;
			indexable.Type = "IMLog";

			// We don't have a specific mime type for this
			// blob, but a mime type must be specified for
			// indexables that provide a stream
			indexable.MimeType = "text/plain";
			
			StringBuilder text = new StringBuilder ();
			foreach (BU.ImLog.Utterance utt in log.Utterances) {
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

		private void IndexLog (FileInfo file)
		{
			ICollection logs = BU.GaimLog.ScanLog (file);
			int n = 0;
			foreach (BU.ImLog log in logs) {
				Indexable indexable = ImLogToIndexable (log);
				++n;
				if (n < logs.Count)
					Driver.ScheduleAdd (indexable);
				else
					Driver.ScheduleAddAndMark (indexable, file);
			}
		}
	}
}
