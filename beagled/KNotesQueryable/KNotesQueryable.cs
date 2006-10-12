//
// KNotesQueryable.cs
//
// Copyright (C) 2006 Debajyoti Bera <dbera.web@gmail.com>
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
using System.Text;
using System.Collections;
using System.Threading;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.KNotesQueryable {

	[QueryableFlavor (Name="KNotes", Domain=QueryDomain.Local, RequireInotify=false)]
	public class KNotesQueryable : LuceneFileQueryable {

		private static Logger log = Logger.Get ("KNotesQueryable");

		string knotes_dir;
		string knotes_file;

		public KNotesQueryable () : base ("KNotesIndex")
		{
			knotes_dir = Path.Combine (PathFinder.HomeDir, ".kde");
			knotes_dir = Path.Combine (knotes_dir, "share");
			knotes_dir = Path.Combine (knotes_dir, "apps");
			knotes_dir = Path.Combine (knotes_dir, "knotes");

			knotes_file = Path.Combine (knotes_dir, "notes.ics");
		}

		/////////////////////////////////////////////////

		public override void Start () 
		{			
			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (!Directory.Exists (knotes_dir)) {
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
				return;
			}

			if (Inotify.Enabled) {
				Inotify.EventType mask =  Inotify.EventType.CloseWrite
							| Inotify.EventType.MovedTo;
				Inotify.Subscribe (knotes_dir, OnInotifyEvent, mask);
			} else {
				FileSystemWatcher fsw = new FileSystemWatcher ();
			       	fsw.Path = knotes_dir;
				fsw.Filter = knotes_file;

				fsw.Changed += new FileSystemEventHandler (OnChangedEvent);
				fsw.Created += new FileSystemEventHandler (OnChangedEvent);
				fsw.Renamed += new RenamedEventHandler (OnChangedEvent);
				
				fsw.EnableRaisingEvents = true;
			}

			if (File.Exists (knotes_file) && ! FileAttributesStore.IsUpToDate (knotes_file))
				Index ();
		}

		private bool CheckForExistence ()
                {
                        if (!Directory.Exists (knotes_dir))
                                return true;

                        this.Start ();

                        return false;
                }

		/////////////////////////////////////////////////

		// Modified event using Inotify
		private void OnInotifyEvent (Inotify.Watch watch,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (Path.Combine (path, subitem) != knotes_file)
				return;

			Index ();
		}

		// Modified/Created event using FSW
		private void OnChangedEvent (object o, FileSystemEventArgs args)
		{
			Index ();		
		}

		/////////////////////////////////////////////////

		private void Index ()
		{
			if (ThisScheduler.ContainsByTag ("KNotes")) {
				Logger.Log.Debug ("Not adding task for already running KNotes task");
				return;
			}

			// First remove all old notes, so that actually deleted notes will be deleted
			Uri uri = UriFu.PathToFileUri (knotes_file);
			Scheduler.Task del_task = NewRemoveTask (uri);
			del_task.Priority = Scheduler.Priority.Immediate;
			del_task.SubPriority = 1;
			ThisScheduler.Add (del_task);

			// Then add the notes from the notes file
			NotesIndexableGenerator generator = new NotesIndexableGenerator (this, knotes_file);
			Scheduler.Task task;
			task = NewAddTask (generator);
			task.Tag = "KNotes";
			// Make sure add task gets scheduled after delete task
			task.Priority = Scheduler.Priority.Delayed;
			task.SubPriority = 0;
			ThisScheduler.Add (task);
		}

	}

	/**
	 * Indexable generator for KNotes Feeds
	 */
	internal class NotesIndexableGenerator : IIndexableGenerator {
		private string knotes_file;
		private StreamReader reader;
		private KNotesQueryable queryable;
		private int indexed_count;
		private bool is_valid_file = true;
		
		public NotesIndexableGenerator (KNotesQueryable queryable, string knotes_file)
		{
			this.queryable = queryable;
			this.knotes_file = knotes_file;
			CheckNoteHeader ();
			if (is_valid_file)
				string_builder = new StringBuilder ();
			indexed_count = 0;
		}

		public void PostFlushHook ()
		{
			//queryable.FileAttributesStore.AttachLastWriteTime (knotes_file, DateTime.UtcNow);
		}

		public string StatusName {
			get { return knotes_file; }
		}

		private bool IsUpToDate (string path)
		{
			return queryable.FileAttributesStore.IsUpToDate (path);
		}

		private void CheckNoteHeader () {
			
			if (IsUpToDate (knotes_file)) {
				is_valid_file = false;
				return;
			}
			try {
				Logger.Log.Debug ("Checking if {0} is a valid KNotes file.", knotes_file);
				/** KNotes file notes.ics should start with
BEGIN:VCALENDAR
PRODID:-//K Desktop Environment//NONSGML libkcal 3.5//EN
VERSION:2.0
				*/
				// FIXME: Encoding of notes.ics
				reader = new StreamReader (knotes_file);
				if (reader.ReadLine () != "BEGIN:VCALENDAR" ||
				    reader.ReadLine () != "PRODID:-//K Desktop Environment//NONSGML libkcal 3.5//EN" ||
				    reader.ReadLine () != "VERSION:2.0") {
					is_valid_file = false;
					return;
				}
			} catch (Exception ex) {
				Logger.Log.Warn (ex, "Caught exception parsing knotes file:");
				is_valid_file = false;
				reader.Close ();
			}
		}

		private StringBuilder string_builder;
		public bool HasNextIndexable ()
		{	
			if (!is_valid_file || reader == null)
				return false;

			string line;
			while ((line = reader.ReadLine ()) != null) {
				if (line == "BEGIN:VJOURNAL")
					break;
			}
			if (line == null) {
				reader.Close ();
				return false;
			}
			return true;
		}

		public Indexable GetNextIndexable ()
		{
			string line;
			string_builder.Length = 0;
			indexed_count ++;

			Uri uri = new Uri (String.Format ("knotes://{0}", indexed_count));
			Indexable indexable = new Indexable (uri);
			indexable.ParentUri = UriFu.PathToFileUri (knotes_file);
			indexable.MimeType = ICalParser.KnotesMimeType;
			indexable.HitType = "Note";

			// Keep reading till "END:VJOURNAL"
			while ((line = reader.ReadLine ()) != null) {
				if (line == "END:VJOURNAL")
					break;
				string_builder.Append (line);
				string_builder.Append ('\n');
			}
			if (line == null) {
				reader.Close ();
				return null;
			}

			if (string_builder.Length == 0)
				return null;
			
			Log.Debug ("Creating knotes://{0} from:[{1}]", indexed_count, string_builder.ToString ());
			StringReader string_reader = new StringReader (string_builder.ToString());
			indexable.SetTextReader (string_reader);

			return indexable;
		}

	}

}
