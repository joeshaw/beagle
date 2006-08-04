//
// EvolutionDataServerQueryable.cs
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
using System.Globalization;
using System.Text;
using System.Threading;
using System.IO;

using Beagle.Daemon;
using Beagle.Util;

using Evolution;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	[QueryableFlavor (Name="EvolutionDataServer", Domain=QueryDomain.Local, RequireInotify=false)]
	public class EvolutionDataServerQueryable : LuceneQueryable {
		//private Scheduler.Priority priority = Scheduler.Priority.Immediate;
		private Scheduler.Priority priority = Scheduler.Priority.Delayed;

		private string photo_dir;
		private DateTime start_time;

		// Index versions
		// 1: Original version
		// 2: Updated URI scheme for Evolution 2.4/EDS 1.4
		private const int INDEX_VERSION = 2;

		public EvolutionDataServerQueryable () : base ("EvolutionDataServerIndex", INDEX_VERSION)
		{
			photo_dir = Path.Combine (Driver.TopDirectory, "Photos");
			System.IO.Directory.CreateDirectory (photo_dir);
		}

		public string PhotoDir {
			get { return photo_dir; }
		}

		public override void Start ()
		{
			base.Start ();

			// Defer the actual startup till main_loop starts.
			// This will improve kill beagle while starting up.
			// EDS requires StartWorker to run in mainloop,
			// hence it is not started in a separate thread.
			GLib.Idle.Add (new GLib.IdleHandler (delegate () { StartWorker (); return false;}));
		}

		private void StartWorker ()
		{
			Logger.Log.Info ("Scanning addressbooks and calendars");
			Stopwatch timer = new Stopwatch ();
			timer.Start ();

			start_time = DateTime.Now;
			State = QueryableState.Crawling;

			bool success = false;

			// This is the first code which tries to open the
			// evolution-data-server APIs.  Try to catch
			// DllNotFoundException and bail out if things go
			// badly.
			try {
				new SourcesHandler ("/apps/evolution/addressbook/sources", typeof (BookContainer), this, Driver.Fingerprint);
				new SourcesHandler ("/apps/evolution/calendar/sources", typeof (CalContainer), this, Driver.Fingerprint);
				success = true;
			} catch (DllNotFoundException ex) {
				Logger.Log.Error ("Unable to start EvolutionDataServer backend: Unable to find or open {0}", ex.Message);
			} finally {
				State = QueryableState.Idle;
				timer.Stop ();
			}
			
			if (success)
				Logger.Log.Info ("Scanned addressbooks and calendars in {0}", timer);
		}

		public void AddIndexable (Indexable indexable, Scheduler.Priority priority)
		{
			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = priority;
			ThisScheduler.Add (task);
		}

		public void RemoveIndexable (Uri uri)
		{
			Scheduler.Task task;
			task = NewRemoveTask (uri);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}

		public void RemovePropertyIndexable (Property prop)
		{
			Scheduler.Task task;
			task = NewRemoveByPropertyTask (prop);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}
	}
}
