//
// TomboyQueryable.cs
//
// Copyright (C) 2004 Christopher Orr
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
using System.IO;

using Beagle.Daemon;
using BU = Beagle.Util;

namespace Beagle.Daemon.TomboyQueryable {

	[QueryableFlavor (Name="TomboyQueryable", Domain=QueryDomain.Local)]
	public class TomboyQueryable : LuceneQueryable {
		FileSystemEventMonitor monitor;

		public TomboyQueryable () : base ("TomboyQueryable", Path.Combine (PathFinder.RootDir, "TomboyIndex"))
		{
			string home = Environment.GetEnvironmentVariable ("HOME");
			DirectoryInfo notesDir = new DirectoryInfo (Path.Combine (home, ".tomboy"));

			// First, do a quick crawl to make sure our notes are all up-to-date
			TomboyNoteCrawler crawler = new TomboyNoteCrawler (this);
			crawler.ScheduleCrawl (notesDir, -1);
			crawler.StopWhenEmpty ();

			monitor = new FileSystemEventMonitor ();
			monitor.FileSystemEvent += OnFileSystemEvent;
			monitor.Subscribe (notesDir, true);
		}

		protected void OnFileSystemEvent (FileSystemEventMonitor monitor,
						  FileSystemEventType eventType,
						  string oldPath,
						  string newPath)
		{
			if (eventType == FileSystemEventType.Created
			    || eventType == FileSystemEventType.Changed) {
				IndexNote (new FileInfo (newPath));
			} else if (eventType == FileSystemEventType.Deleted) {
				RemoveNote (oldPath);
			}
		}

		private static Indexable NoteToIndexable (BU.Note note)
		{
			Indexable indexable = new Indexable (note.Uri);
			indexable.Timestamp = note.timestamp;
			indexable.Type = "Note";

			// Beagle sees the XML as text/plain..
			indexable.MimeType = "text/plain";

			// Using dc:title for a note's subject is debateable?
			indexable.AddProperty (Property.NewKeyword ("dc:title", note.subject));
			indexable.AddProperty (Property.NewDate ("fixme:modified", note.timestamp));

			StringReader reader = new StringReader (note.text);
			indexable.SetTextReader (reader);
			
			return indexable;
		}

		private void IndexNote (FileInfo file)
		{
			// Try and parse a Note from the given path
			BU.Note note = BU.TomboyNote.ParseNote (file);
			if (note == null)
				return;
			
			// A Note was returned; add it to the index
			Indexable indexable = NoteToIndexable (note);
			Driver.ScheduleAddAndMark (indexable, file);
		}
		
		private void RemoveNote (string path)
		{
			Uri uri = BU.UriFu.PathToFileUri(path);
			Driver.ScheduleDelete (uri);
		}
		
		
		private class TomboyNoteCrawler : Crawler {
			TomboyQueryable tbq;
			
			public TomboyNoteCrawler (TomboyQueryable _tbq) : base (_tbq.Driver.Fingerprint)
			{
				tbq = _tbq;
			}
			
			protected override void CrawlFile (FileSystemInfo info)
			{
				FileInfo file = info as FileInfo;
				if (file == null)
					return;
				tbq.IndexNote (file);
			}
		}
	}
}
