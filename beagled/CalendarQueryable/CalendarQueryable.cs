//
// CalendarQueryable.cs
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

using ICalParser = Semaview.Shared.ICalParser;

namespace Beagle.Daemon.CalendarQueryable {

	[QueryableFlavor (Name="Calendar", Domain=QueryDomain.Local)]
	public class CalendarQueryable : LuceneQueryable {

		public static Logger Log = Logger.Get ("calendar");
		private string cal_dir;

		Hashtable watched = new Hashtable ();

		public CalendarQueryable () : base ("CalendarIndex")
		{
			cal_dir = Path.Combine (PathFinder.HomeDir, ".evolution/calendar/local");
		}

		private void StartWorker () 
		{
			Inotify.Event += OnInotifyEvent;

			Stopwatch timer = new Stopwatch ();
			timer.Start ();
			int foundCount = Watch (cal_dir);
			timer.Stop ();
			Log.Info ("Found {0} calendars in {1}", foundCount, timer);
		}

		public override void Start () 
		{
			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private int Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			if (! root.Exists)
				return 0;

			int file_count = 0;

			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				int wd = Inotify.Watch (dir.FullName,
							Inotify.EventType.CreateSubdir
							| Inotify.EventType.Modify);
				watched [wd] = true;

				foreach (FileInfo file in dir.GetFiles ()) {
 					IndexCalendar (file.FullName, Scheduler.Priority.Generator);
					++file_count;
				}

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}

			return file_count;
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

			Console.WriteLine ("{0}: {1}", type, full_path);

			switch (type) {
				
			case Inotify.EventType.CreateSubdir:
				Watch (full_path);
				break;

			case Inotify.EventType.Modify:
				IndexCalendar (full_path, Scheduler.Priority.Immediate);
				break;
			}
		}

		private void IndexCalendar (string filename, Scheduler.Priority priority)
		{
			FileInfo info = new FileInfo (filename);
			if (! info.Exists || Driver.IsUpToDate (filename))
				return;

			Scheduler.TaskGroup group;
			group = NewMarkingTaskGroup (filename, info.LastWriteTime);

			IndexableEmitter emitter = new IndexableEmitter ();
			ICalParser.Parser parser = new ICalParser.Parser (new StreamReader (info.FullName), emitter);
			parser.Parse ();

			if (!parser.HasErrors) {
				foreach (Indexable indexable in emitter.Indexables) {
					Scheduler.Task task = NewAddTask (indexable);
					task.Priority = priority;
					task.SubPriority = 0;
					task.AddTaskGroup (group);
					ThisScheduler.Add (task);
				}
			}
		}
	}

	class IndexableEmitter : ICalParser.IEmitter {
		private ArrayList indexables = new ArrayList ();
		private ICalParser.Parser parser;

		// Our current state
		private Indexable cur = null;
		private string cur_id = null;

		public ICollection Indexables {
			get { return this.indexables; }
		}

		private static DateTime ParseICalDate (string icaldate, bool utc)
		{
			// There is no error checking at all.
			string year_str = icaldate.Substring (0, 4);
			string month_str = icaldate.Substring (4, 2);
			string day_str = icaldate.Substring (6, 2);

			DateTime date;

			if (icaldate.Length >= 15) {
				string hour_str = icaldate.Substring (9, 2);
				string minute_str = icaldate.Substring (11, 2);
				string second_str = icaldate.Substring (13, 2);
				
				date = new DateTime (Convert.ToInt32 (year_str),
						     Convert.ToInt32 (month_str),
						     Convert.ToInt32 (day_str),
						     Convert.ToInt32 (hour_str),
						     Convert.ToInt32 (minute_str),
						     Convert.ToInt32 (second_str));

				if (utc) {
					TimeSpan utc_offset = DateTime.Now - DateTime.UtcNow;
					
					date += utc_offset;
				}
			} else {
				date = new DateTime (Convert.ToInt32 (year_str),
						     Convert.ToInt32 (month_str),
						     Convert.ToInt32 (day_str));
			}

			return date;
		}

		private static DateTime ParseICalDate (string icaldate)
		{
			bool utc = icaldate.EndsWith ("Z");

			return ParseICalDate (icaldate, utc);
		}

		// Implement IEmitter
		public void doIntro ()
		{
			CalendarQueryable.Log.Debug ("-------");
		}

		public void doOutro ()
		{
			CalendarQueryable.Log.Debug ("-------");
		}
		
		public void doEnd (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doEnd: {0}", t.TokenText);

			this.cur_id = null;

			if (t.TokenText.ToLower () == "vevent") {
				this.indexables.Add (this.cur);
				this.cur = null;
			}
		}

		public void doResourceBegin (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doResourceBegin: {0}", t.TokenText);
		}

		public void doBegin (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doBegin: {0}", t.TokenText);
		}

		public void doComponentBegin (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doComponentBegin: {0}", t.TokenText);

			// FIXME: Need more types to index.
			if (t.TokenText.ToLower () != "vevent")
				return;

			this.cur = new Indexable ();
			this.cur.Type = "Calendar";
		}

		public void doComponent ()
		{
		}

		public void doEndComponent ()
		{
		}

		public void doID (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doID: {0}", t.TokenText);

			if (this.cur == null)
				return;

			this.cur_id = t.TokenText;
		}

		public void doSymbolic (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doSymbolic: {0}", t.TokenText);
		}

		public void doResource (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doResource: {0}", t.TokenText);
		}

		public void doURIResource (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doURIResource: {0}", t.TokenText);
		}

		public void doMailto (ICalParser.Token t)
		{
			CalendarQueryable.Log.Debug ("doMailto: {0}", t.TokenText);
		}

		public void doValueProperty (ICalParser.Token t, ICalParser.Token iprop)
		{
			CalendarQueryable.Log.Debug ("doValueProperty: {0} {1}", t.TokenText, iprop == null ? "(null)" : iprop.TokenText);

			if (this.cur == null || this.cur_id == null)
				return;

			switch (this.cur_id.ToLower ()) {
			case "dtstart":
				// When the event starts; in local timezone.
				this.cur.AddProperty (Property.NewDate ("fixme:starttime", ParseICalDate (t.TokenText)));
				break;

			case "dtend":
				// When the event starts; in local timezone.
				this.cur.AddProperty (Property.NewDate ("fixme:endtime", ParseICalDate (t.TokenText)));
				break;
			}
		}

		public void doIprop (ICalParser.Token t, ICalParser.Token iprop)
		{
			CalendarQueryable.Log.Debug ("doIprop: {0} {1}", t.TokenText, iprop.TokenText);
		}

		public void doRest (ICalParser.Token t, ICalParser.Token id)
		{
			CalendarQueryable.Log.Debug ("doRest: {0} {1}", t.TokenText, id.TokenText);

			if (this.cur == null || this.cur_id == null)
				return;

			switch (this.cur_id.ToLower ()) {
			case "uid":
				this.cur.Uri = new Uri ("calendar:///" + t.TokenText);
				break;

			case "dtstart":
				// When the event starts; in local timezone.
				// Usually this won't be processed here, it'll
				// more likely be in doValueProperty w/ a
				// timezone.
				this.cur.AddProperty (Property.NewDate ("fixme:starttime", ParseICalDate (t.TokenText)));
				break;

			case "dtend":
				// When the event ends; in local timezone.
				// Same deal as dtstart above.
				this.cur.AddProperty (Property.NewDate ("fixme:endtime", ParseICalDate (t.TokenText)));
				break;

			case "lastmodified":
				// Always in GMT
				this.cur.Timestamp = ParseICalDate (t.TokenText, true);
				break;

			case "summary":
				// Short summary of the event
				this.cur.AddProperty (Property.NewKeyword ("fixme:summary", t.TokenText));
				break;

			case "description":
				// Longer description of the event
				StringReader reader = new StringReader (t.TokenText);
				this.cur.SetTextReader (reader);
				break;

			case "location":
				// Where the event takes place
				this.cur.AddProperty (Property.NewKeyword ("fixme:location", t.TokenText));
				break;

			case "categories":
				// Categories associated with this event
				this.cur.AddProperty (Property.NewKeyword ("fixme:categories", t.TokenText));
				break;

			case "class":
				// private, public, or confidential
				this.cur.AddProperty (Property.NewKeyword ("fixme:class", t.TokenText));
				break;
			}

			this.cur_id = null;
		}

		public void doAttribute (ICalParser.Token t1, ICalParser.Token t2)
		{
			CalendarQueryable.Log.Debug ("doAttribute: {0} {1}", t1.TokenText, t2.TokenText);
		}

		public ICalParser.Parser VParser {
			get { return this.parser; }
			set { this.parser = value; }
		}

		public void emit (string val)
		{
			CalendarQueryable.Log.Debug ("emit: {0}", val);
		}
	}
}
