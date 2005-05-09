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
using System.Threading;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.TomboyQueryable {

	[QueryableFlavor (Name="Tomboy", Domain=QueryDomain.Local, RequireInotify=false)]
	public class TomboyQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("TomboyQueryable");

		string tomboy_dir;
		int tomboy_wd = -1;
		FileSystemWatcher tomboy_fsw = null;

		public TomboyQueryable () : base ("TomboyIndex")
		{
			tomboy_dir = Path.Combine (PathFinder.HomeDir, ".tomboy");
		}

		public override void Start () 
		{
                        base.Start ();

                        ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (!Directory.Exists (tomboy_dir) ) {
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
				return;
			}

			if (Inotify.Enabled) {			
				Inotify.EventType mask = Inotify.EventType.Delete | 
					Inotify.EventType.MovedTo |
					Inotify.EventType.MovedFrom;

				tomboy_wd = Inotify.Watch (tomboy_dir, mask);
				Inotify.Event += OnInotifyEvent;
			} else {
				FileSystemWatcher fsw = new FileSystemWatcher ();
				fsw.Path = tomboy_dir;
				fsw.Filter = "*.note";

				fsw.Changed += new FileSystemEventHandler (OnChanged);
				fsw.Created += new FileSystemEventHandler (OnChanged);
				fsw.Deleted += new FileSystemEventHandler (OnDeleted);

				fsw.EnableRaisingEvents = true;
			}

			// Crawl all of our existing notes to make sure that
			// everything is up-to-date.
			log.Info ("Scanning Tomboy notes...");

			Stopwatch stopwatch = new Stopwatch ();
			int count = 0;
			stopwatch.Start ();
			DirectoryInfo dir = new DirectoryInfo (tomboy_dir);

			foreach (FileInfo file in dir.GetFiles ()) {
				if (file.Extension == ".note") {
					IndexNote (file, Scheduler.Priority.Delayed);
					++count;
				}
			}

			stopwatch.Stop ();
			log.Info ("Scanned {0} notes in {1}", count, stopwatch);
		}

		private bool CheckForExistence ()
		{
			if (!Directory.Exists (tomboy_dir))
				return true;
			
			this.Start ();

			return false;
		}

		/////////////////////////////////////////////////

		// Modified/Created/Deleted event using Inotify
		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (wd != tomboy_wd)
				return;

			if (subitem == "")
				return;

			if (Path.GetExtension (subitem) != ".note")
				return;

			if ((type & Inotify.EventType.MovedTo) != 0) {
				IndexNote (new FileInfo (Path.Combine (path, subitem)), Scheduler.Priority.Immediate);
			}

			if ((type & Inotify.EventType.MovedFrom) != 0 ||
					((type & Inotify.EventType.Delete) != 0 &&
					 (type & Inotify.EventType.IsDirectory) == 0))
				RemoveNote (subitem);
		}

		// Modified/Created event using FSW
		private void OnChanged (object o, FileSystemEventArgs args)
		{
			IndexNote (new FileInfo (args.FullPath), Scheduler.Priority.Immediate);
		}

		// Deleted event using FSW
		private void OnDeleted (object o, FileSystemEventArgs args)
		{
			RemoveNote (args.FullPath);
		}

		/////////////////////////////////////////////////

		private static Indexable NoteToIndexable (FileInfo file, Note note)
		{
			Indexable indexable = new Indexable (note.Uri);

			indexable.ContentUri = UriFu.PathToFileUri (file.FullName);

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

		private void IndexNote (FileInfo file, Scheduler.Priority priority)
		{
			if (this.FileAttributesStore.IsUpToDate (file.FullName))
				return;

			// Try and parse a Note from the given path
			Note note = TomboyNote.ParseNote (file);
			if (note == null)
				return;
			
			// A Note was returned; add it to the index
			Indexable indexable = NoteToIndexable (file, note);
			
			Scheduler.Task task = NewAddTask (indexable);
			task.Priority = priority;
			task.SubPriority = 0;
			ThisScheduler.Add (task);

			// Write a plain-text version of our note out into the
			// text cache
			try {
				TextWriter writer = TextCache.GetWriter (note.Uri);
				writer.Write (note.text);
				writer.Close ();
			} catch (Exception ex) {
				Console.WriteLine (">>>>> Caught exception writing {0} to text cache", note.Uri);
			}
		}
		
		private void RemoveNote (string file)
		{
			Uri uri = Note.BuildNoteUri (file, "tomboy");
			Scheduler.Task task = NewRemoveTask (uri);
			task.Priority = Scheduler.Priority.Immediate;
			task.SubPriority = 0;
			ThisScheduler.Add (task);
		}

		override protected bool HitIsValid (Uri uri)
		{
			string note = Path.Combine (tomboy_dir, uri.Segments [1] + ".note");

			if (File.Exists (note))
				return true;

			return false;
		}
	}
}
