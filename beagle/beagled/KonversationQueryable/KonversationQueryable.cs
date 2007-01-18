//
// KonversationQueryable.cs
//
// Copyright (C) 2007 Debajyoti Bera <dbera.web@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Globalization;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.KonversationQueryable {

	// FIXME: Absolutely requires Inotify currently
	[QueryableFlavor (Name="Konversation", Domain=QueryDomain.Local, RequireInotify=true)]
	public class KonversationQueryable : LuceneFileQueryable {
		private string log_dir;
		private Dictionary<string, long> session_offset_table;

		public KonversationQueryable () : base ("KonversationIndex")
		{
			log_dir = Path.Combine (PathFinder.HomeDir, ".kde");
			log_dir = Path.Combine (log_dir, "share");
			log_dir = Path.Combine (log_dir, "apps");
			log_dir = Path.Combine (log_dir, "konversation");
			log_dir = Path.Combine (log_dir, "logs");
		}

		public override void Start () 
		{
			base.Start ();
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (! Directory.Exists (log_dir)) {
				GLib.Timeout.Add (300000, new GLib.TimeoutHandler (CheckForExistence));
				return;
			}

			Log.Info ("Starting konversation backend; using log files from {0}", log_dir);

			session_offset_table = new Dictionary<string, long> ();

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			if (Inotify.Enabled)
				Inotify.Subscribe (log_dir, OnInotify,
						    Inotify.EventType.Create |
						    Inotify.EventType.Modify);

			int log_count = 0, index_count = 0;
			foreach (FileInfo fi in DirectoryWalker.GetFileInfos (log_dir)) {
				if (fi.Name == "konversation.log")
					continue;

				// FIXME: Handle "Excludes" from Conf

				log_count ++;
				if (IsUpToDate (fi.FullName)) {
					PostFlushHook (fi.FullName, fi.Length);
					continue;
				}

				index_count ++;

				LogIndexableGenerator generator = new LogIndexableGenerator (this, fi.FullName, 0);
				Scheduler.Task task = NewAddTask (generator);
				task.Tag = fi.FullName;
				task.Source = this;
				ThisScheduler.Add (task);
			}

			stopwatch.Stop ();
			Log.Info ("Konversation backend: Scanned {0} log files in {2}, will index {1}", log_count, index_count, stopwatch);
		}

		protected override bool IsIndexing {
			// FIXME: Set proper value
			get { return true; }
		}

		// FIXME: Improve this by storing the data on disk. Then scan the data on startup
		// and compare with the last modified time or file length to determine if a complete
		// rescan is needed.
		private void PostFlushHook (string log_file)
		{
			// FIXME!!!
			PostFlushHook (log_file, -1);
		}

		internal void PostFlushHook (string log_file, long session_start_position)
		{
			//Log.Debug ("Asked to store offset for {0} = {1}", log_file, session_start_position);

			if (! session_offset_table.ContainsKey (log_file))
				session_offset_table [log_file] = session_start_position;

			long stored_pos = session_offset_table [log_file];
			if (stored_pos < session_start_position)
				session_offset_table [log_file] = session_start_position;
		}

		private void OnInotify (Inotify.Watch watch,
					string path, string subitem, string srcpath,
					Inotify.EventType type)
		{
			long offset = 0;
			path = Path.Combine (path, subitem);
			if (ThisScheduler.ContainsByTag (path)) {
				Log.Debug ("Not adding task for already running task: {0}", path);
				return;
			}

			if (session_offset_table.ContainsKey (path))
				offset = session_offset_table [path];

			LogIndexableGenerator generator = new LogIndexableGenerator (this, path, offset);
			Scheduler.Task task = NewAddTask (generator);
			task.Tag = path;
			task.Source = this;
			ThisScheduler.Add (task);
		}

		private class LogIndexableGenerator : IIndexableGenerator {
			private KonversationQueryable queryable;
			private string log_file;
			private LineReader reader;
			private StringBuilder log_line_as_sb;
			private StringBuilder data_sb;
			private Dictionary<string, bool> speakers; // list of speakers in the session
			private string channel_name, speaking_to;

			// Split log into 6 hour sessions or 50 lines, which ever is larger
			private DateTime session_begin_time;
			private DateTime session_end_time;
			private long session_begin_offset;
			private long prev_line_offset; // stores the offset of the previous line read by reader
			private long session_num_lines;

			public LogIndexableGenerator (KonversationQueryable queryable, string log_file, long offset)
			{
				this.queryable = queryable;
				this.log_file = log_file;
				this.session_begin_offset = offset;
				this.prev_line_offset = offset;

				this.data_sb = new StringBuilder ();
				this.log_line_as_sb = null;
				this.session_begin_time = DateTime.MinValue;
				this.speakers = new Dictionary<string, bool> (10); // rough default value

				//Log.Debug ("Reading from file " + log_file);
			}

			public void PostFlushHook ()
			{
				//Log.Debug ("Storing reader position {0}", session_begin_offset);
				queryable.PostFlushHook (log_file, session_begin_offset);
			}

			public string StatusName {
				get { return log_file; }
			}

			public bool HasNextIndexable ()
			{
				data_sb.Length = 0;
				session_num_lines = 0;
				speakers.Clear ();

				if (reader == null) {
					// Log files are in system encoding
					reader = new ReencodingLineReader (log_file, Encoding.Default);
					reader.Position = session_begin_offset;
					log_line_as_sb = reader.ReadLineAsStringBuilder ();
					//Log.Debug ("Read line from {0}:[{1}]", log_file, log_line_as_sb);
				}

				if (log_line_as_sb == null) {
					reader.Close ();
					return false;
				} else {
					// Update session_begin_offset
					session_begin_offset = prev_line_offset;
				}

				return true;
			}

			public Indexable GetNextIndexable ()
			{
				DateTime line_dt = DateTime.MinValue;

				while (log_line_as_sb != null) {
					bool in_session = AppendLogText (log_line_as_sb, out line_dt);
					if (! in_session)
						break;

					prev_line_offset = reader.Position;
					log_line_as_sb = reader.ReadLineAsStringBuilder ();
					//Log.Debug ("Reading more line from {0}:[{1}]", log_file, log_line);
				}

				// Check if there is new data to index
				if (data_sb.Length == 0)
					return null;

				Uri uri = new Uri (String.Format ("konversation://{0}@dumb/{1}", session_begin_offset, log_file));
				//Log.Debug ("Creating indexable {0}", uri);
				Indexable indexable = new Indexable (uri);
				indexable.ParentUri = UriFu.PathToFileUri (log_file);
				indexable.Timestamp = session_begin_time;
				indexable.HitType = "IMLog";
				indexable.CacheContent = false;
				indexable.Filtering = IndexableFiltering.AlreadyFiltered;

				indexable.AddProperty (Beagle.Property.NewUnsearched ("fixme:session_begin_offset", session_begin_offset));
				indexable.AddProperty (Beagle.Property.NewUnsearched ("fixme:session_end_offset", prev_line_offset));
				indexable.AddProperty (Beagle.Property.NewDate ("fixme:starttime", session_begin_time));
				indexable.AddProperty (Beagle.Property.NewUnsearched ("fixme:client", "Konversation"));
				indexable.AddProperty (Beagle.Property.NewUnsearched ("fixme:protocol", "IRC"));

				AddChannelInformation (indexable);

				foreach (string speaker in speakers.Keys)
					indexable.AddProperty (Beagle.Property.NewUnstored ("fixme:speaker", speaker));

				StringReader data_reader = new StringReader (data_sb.ToString ());
				indexable.SetTextReader (data_reader);

				return indexable;
			}

			// Returns false if log_line belonged to next session and was not appended
			// line_dt is set to the timestamp of the log_line
			private bool AppendLogText (StringBuilder log_line_as_sb, out DateTime line_dt)
			{
				line_dt = DateTime.MinValue;

				string dt_string, text;
				string speaker = String.Empty;
				bool is_good_line = KonversationLog.ProcessLine (log_line_as_sb, out text, out dt_string, ref speaker);

				if (! is_good_line)
					return false;

				line_dt = DateTime.ParseExact (
					dt_string,
					KonversationLog.LogTimeFormatString,
					CultureInfo.InvariantCulture,
					DateTimeStyles.AssumeLocal);

				// On first scan, set the session_begin_time
				if (session_begin_time == DateTime.MinValue) {
					session_begin_time = new DateTime (
						line_dt.Year,
						line_dt.Month,
						line_dt.Day,
						line_dt.Hour,
						0,
						0);
					session_end_time = line_dt;
					//Log.Debug ("Adding session begin time {0}", DateTimeUtil.ToString (session_begin_time));
					AddLine (text, speaker);
					return true;
				}

				// If we are on a different day then make a new session
				if (session_begin_time.Day != line_dt.Day)
					return false;
				// or if more than 50 useful lines were seen in this session
				else if (session_num_lines > 50) {
					// Split session in 6 hour = 360 min interval
					TimeSpan ts = line_dt - session_begin_time;
					if (ts.TotalMinutes > 360)
						return false;
				}

				AddLine (text, speaker);
				session_end_time = line_dt;

				return true;
			}

			private void AddLine (string text, string speaker)
			{
				//Log.Debug ("Adding [{0}]", text);
				data_sb.Append (text);
				data_sb.Append (' ');
				session_num_lines ++;

				speakers [speaker] = true;
			}

			private void AddChannelInformation (Indexable indexable)
			{
				// Parse identity information from konversation .config file
				//AddProperty (Beagle.Property.NewUnsearched ("fixme:identity", log.Identity));

				// Get server name, channel name from the filename and add it here
				//indexable.AddProperty (Beagle.Property.NewKeyword ("fixme:server", server_name));

				if (channel_name != null)
					indexable.AddProperty (Beagle.Property.NewKeyword ("fixme:channel", channel_name));
				if (speaking_to != null)
					indexable.AddProperty (Beagle.Property.NewKeyword ("fixme:speakingto", speaking_to));
			}
		}

		private bool CheckForExistence ()
		{
			if (!Directory.Exists (log_dir))
				return true;

			this.Start ();

			return false;
		}

	}
}

