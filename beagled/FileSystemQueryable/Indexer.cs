//
// Indexer.cs
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

using DBus;
using System;
using System.IO;
using System.Collections;
using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Daemon.FileSystemQueryable
{
	public class Indexer : Beagle.Indexer 
	{
		IndexerQueue indexerQueue;
		public IndexDriver driver = new MainIndexDriver (); // FIXME: Ugly!

		FileSystemEventMonitor monitor = new FileSystemEventMonitor ();
		FileMatcher doNotIndex = new FileMatcher ();

		public Indexer (IndexerQueue _indexerQueue) {
			indexerQueue = _indexerQueue;

			string home = Environment.GetEnvironmentVariable ("HOME");
			monitor.FileSystemEvent += OnFileSystemEvent;
			monitor.Subscribe (home);
			monitor.Subscribe (Path.Combine (home, "Desktop"));
			monitor.Subscribe (Path.Combine (home, "Documents"));
		}

		public override void Index (string xml)
		{
			FilteredIndexable indexable = FilteredIndexable.NewFromXml (xml);
			indexerQueue.ScheduleAdd (indexable);
		}

		private void OnFileSystemEvent (object source, FileSystemEventType eventType,
						string oldPath, string newPath)
		{
			Console.WriteLine ("Got event {0} {1} {2}", eventType, oldPath, newPath);

			if (eventType == FileSystemEventType.Changed
			    || eventType == FileSystemEventType.Created) {

				if (doNotIndex.IsMatch (newPath)) {
					Console.WriteLine ("Ignoring {0}", newPath);
					return;
				}

				string uri = StringFu.PathToQuotedFileUri (newPath);
				FilteredIndexable indexable = new FilteredIndexable (uri);
				indexerQueue.ScheduleAdd (indexable);

			} else if (eventType == FileSystemEventType.Deleted) {

				string uri = StringFu.PathToQuotedFileUri (oldPath);
				Hit hit = driver.FindByUri (uri);
				if (hit != null)
					indexerQueue.ScheduleRemove (hit);
				
			} else {
				Console.WriteLine ("Unhandled!");
			}
		}
	}
}
