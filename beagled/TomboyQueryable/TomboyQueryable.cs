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
using Beagle.Util;

namespace Beagle.Daemon.TomboyQueryable {

	[QueryableFlavor (Name="Tomboy", Domain=QueryDomain.Local)]
	public class TomboyQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("TomboyQueryable");

		string notesDir;
		string backupDir;

		int wdNotes = -1;
		int wdBackup = -1;

		public TomboyQueryable () : base (Path.Combine (PathFinder.RootDir, "TomboyIndex"))
		{
			notesDir = Path.Combine (Environment.GetEnvironmentVariable ("HOME"), ".tomboy");
			backupDir = Path.Combine (notesDir, "Backup");

			// FIXME: We should do something more reasonable if
			// ~/.tomboy doesn't exist.
			if (! (Directory.Exists (notesDir) && Directory.Exists (backupDir)))
				return;
			
			InotifyEventType mask;
			mask = InotifyEventType.MovedTo
				| InotifyEventType.MovedFrom
				| InotifyEventType.CreateFile
				| InotifyEventType.DeleteFile
				| InotifyEventType.Modify;

			wdNotes = Inotify.Watch (notesDir, mask);
			wdBackup = Inotify.Watch (backupDir, mask);

			Inotify.InotifyEvent += new InotifyHandler (OnInotifyEvent);

			// Crawl all of our existing notes to make sure that
			// everything is up-to-date.
			log.Info ("Scanning Tomboy notes...");
			Stopwatch stopwatch = new Stopwatch ();
			int count = 0;
			stopwatch.Start ();
			DirectoryInfo dir = new DirectoryInfo (notesDir);
			foreach (FileInfo file in dir.GetFiles ()) {
				if (file.Extension == ".note") {
					IndexNote (file, 0);
					++count;
				}
			}
			stopwatch.Stop ();
			log.Info ("Scanned {0} notes in {1}", count, stopwatch);
		}


		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     InotifyEventType type,
					     int cookie)
		{
			if (wd != wdNotes && wd != wdBackup)
				return;

			// Ignore operations on the directories themselves
			if (subitem == "")
				return;

			Console.WriteLine ("*** {0} {1} {2}", path, subitem, type);

			// Ignore backup files, tmp files, etc.
			if (Path.GetExtension (subitem) != ".note")
				return;
			
			if (wd == wdNotes && type == InotifyEventType.MovedTo) {
				IndexNote (new FileInfo (Path.Combine (path, subitem)), 100);
				Console.WriteLine ("Indexed {0}", Path.Combine (path, subitem));
			}

			if (wd == wdBackup && type == InotifyEventType.MovedTo) {
				string oldPath = Path.Combine (notesDir, subitem);
				RemoveNote (oldPath);
				Console.WriteLine ("Removing {0}", oldPath);
			}

		}

		private static Indexable NoteToIndexable (Note note)
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

		private void IndexNote (FileInfo file, int priority)
		{
			if (Driver.IsUpToDate (file))
				return;

			// Try and parse a Note from the given path
			Note note = TomboyNote.ParseNote (file);
			if (note == null)
				return;
			
			// A Note was returned; add it to the index
			Indexable indexable = NoteToIndexable (note);
			Driver.ScheduleAddAndMark (indexable, priority, file);
		}
		
		private void RemoveNote (string path)
		{
			Uri uri = UriFu.PathToFileUri (path);
			Driver.ScheduleDelete (uri, 100);
		}
	}
}
