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
//using Beagle;

namespace Beagle.Daemon
{
	public class Indexer : Beagle.Indexer 
	{
		IndexerQueue indexerQueue;
		FileSystemEventMonitor monitor = new FileSystemEventMonitor ();
		IndexDriver driver = new MainIndexDriver ();

		struct DirectoryHitEntry {
			public string directoryName;
			public Hit[] hits;
		};

		ArrayList directoryHits = new ArrayList ();
		const int maxDirectoryHits = 5;
		
		public Indexer (IndexerQueue _indexerQueue) {
			indexerQueue = _indexerQueue;

			string home = Environment.GetEnvironmentVariable ("HOME");
			monitor.FileSystemEvent += OnFileSystemEvent;
			monitor.Subscribe (home);
			monitor.Subscribe (Path.Combine (home, "Desktop"));
			monitor.Subscribe (Path.Combine (home, "Documents"));
		}

		Hit[] GetDirectoryHits (DirectoryInfo dir) 
		{
			foreach (DirectoryHitEntry entry in directoryHits) {
				if (entry.directoryName == dir.FullName) {
					return entry.hits;
				}
			}

			if (directoryHits.Count >= maxDirectoryHits) {
				directoryHits.RemoveAt (directoryHits.Count - 1);
			}
			
			DirectoryHitEntry newEntry = new DirectoryHitEntry ();
			newEntry.directoryName = dir.FullName;
			newEntry.hits = driver.FindByProperty ("fixme:directory",
							       dir.FullName);
			directoryHits.Insert (0, newEntry);

			return newEntry.hits;
		}

		Hit GetExistingHit (FileInfo file) 
		{
			// For right now the primary user of the indexer
			// is the Crawler, which will request a lot of 
			// files in the same directory.  To speed that
			// up a bit, cache hits for directories
			// to reduce the number of searches
			string uri;
			uri = "file://" + file.FullName;
			
			Hit[] hits = GetDirectoryHits (file.Directory);

			foreach (Hit hit in hits) {
				if (hit.Uri == uri) {
					return hit;
				}
			}
			return null;
		}

		public void IndexIndexable (FilteredIndexable indexable)
		{
			Hit hit;

			FileInfo file = indexable.GetFileInfo ();
			
			if (file != null) {
				hit = GetExistingHit (file);

				if (!file.Exists) {
					indexerQueue.ScheduleRemove (hit);
					return;
				}
				if (hit != null) 
					System.Console.WriteLine ("checking {0} against {1}", hit.Timestamp, indexable.Timestamp);
			       
				if (hit != null && !hit.IsObsoletedBy (indexable.Timestamp))
					return;
			} else {
				hit = driver.FindByUri (indexable.Uri);
			}

			indexerQueue.ScheduleAdd (indexable);
			indexerQueue.ScheduleRemove (hit);
		}

		public override void Index (string xml)
		{
			FilteredIndexable indexable = FilteredIndexable.NewFromXml (xml);
			IndexIndexable (indexable);
		}

		private void OnFileSystemEvent (object source, FileSystemEventType eventType,
						string oldPath, string newPath)
		{
			Console.WriteLine ("Got event {0} {1} {2}", eventType, oldPath, newPath);

			if (eventType == FileSystemEventType.Changed
			    || eventType == FileSystemEventType.Created) {

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
