//
// LifereaQueryable.cs
//
// Copyright (C) 2005 Carl-Emil Lagerstedt
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
using System.Collections;
using System.Threading;

using System.Xml;
using System.Xml.Serialization;
	
using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.LifereaQueryable {

	[QueryableFlavor (Name="Liferea", Domain=QueryDomain.Local, RequireInotify=false)]
	public class LifereaQueryable : LuceneQueryable, IIndexableGenerator {

		private static Logger log = Logger.Get ("LifereaQueryable");

		string liferea_dir;

		public LifereaQueryable () : base ("LifereaIndex")
		{
			liferea_dir = Path.Combine (PathFinder.HomeDir, ".liferea");
			liferea_dir = Path.Combine (liferea_dir, "cache");
			liferea_dir = Path.Combine (liferea_dir, "feeds");
		}

		/////////////////////////////////////////////////

		public override void Start () 
		{			
			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (!Directory.Exists (liferea_dir)) {
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
                                return;
			}
				
			if (Inotify.Enabled) {
				Inotify.EventType mask = Inotify.EventType.CloseWrite;

				Inotify.Subscribe (liferea_dir, OnInotifyEvent, mask);
			} else {
                                FileSystemWatcher fsw = new FileSystemWatcher ();
                                fsw.Path = liferea_dir;

                                fsw.Changed += new FileSystemEventHandler (OnChanged);
                                fsw.Created += new FileSystemEventHandler (OnChanged);

                                fsw.EnableRaisingEvents = true;
			}

                        log.Info ("Scanning Liferea feeds...");

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

                        DirectoryInfo dir = new DirectoryInfo (liferea_dir);
			this.files_to_parse = dir.GetFiles ();

			Scheduler.Task task = NewAddTask (this);
			task.Tag = "Liferea";
			ThisScheduler.Add (task);

			stopwatch.Stop ();
                        log.Info ("{0} files will be parsed (scanned in {1})", this.files_to_parse.Count, stopwatch);
		}

		private bool CheckForExistence ()
                {
                        if (!Directory.Exists (liferea_dir))
                                return true;

                        this.Start ();

                        return false;
                }

		/////////////////////////////////////////////////

                // Modified/Created event using Inotify

		private void OnInotifyEvent (Inotify.Watch watch,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (subitem == "")
				return;

			IndexSingleFeed (Path.Combine (path, subitem), Scheduler.Priority.Immediate);
		}

		// Modified/Created event using FSW
		
		private void OnChanged (object o, FileSystemEventArgs args)
		{
			IndexSingleFeed (args.FullPath, Scheduler.Priority.Immediate);
		}
		
		/////////////////////////////////////////////////

		private Indexable FeedItemToIndexable (Feed feed, Item item)
		{
			Indexable indexable = new Indexable (new Uri (String.Format ("feed:{0};item={1}", feed.Source, item.Source)));
			indexable.MimeType = "text/html";
			indexable.Type = "FeedItem";

			DateTime date = new DateTime (1970, 1, 1);
			date = date.AddSeconds (item.Timestamp);
			date = TimeZone.CurrentTimeZone.ToLocalTime (date);

			indexable.Timestamp = date;				

			indexable.AddProperty (Property.NewKeyword ("dc:title", item.Title));
			indexable.AddProperty (Property.NewKeyword ("fixme:author", item.Attribs.Author));
			indexable.AddProperty (Property.NewDate ("fixme:published", date));
			indexable.AddProperty (Property.NewKeyword ("fixme:itemuri", item.Source));
			indexable.AddProperty (Property.NewKeyword ("fixme:webloguri", feed.Source));
				
			StringReader reader = new StringReader (item.Description);
			indexable.SetTextReader (reader);

			return indexable;
		}

		// Parse and index a single feed

		private int IndexSingleFeed (string filename, Scheduler.Priority priority)
		{
			FileInfo file = new FileInfo(filename);
			
			Feed feed;
			int item_count = 0;

			if (this.FileAttributesStore.IsUpToDate (file.FullName))
			        return 0;

			Scheduler.TaskGroup group = NewMarkingTaskGroup (file.FullName, file.LastWriteTime);
			
			feed = Feed.LoadFromFile (file.FullName);
			
			if (feed == null || feed.Items == null)
				return 0;
			
			foreach (Item item in feed.Items) {
				item_count++;
				
				Indexable indexable = FeedItemToIndexable (feed, item);
				
				Scheduler.Task task = NewAddTask (indexable);
				task.Priority = priority;
				task.SubPriority = 0;
				task.AddTaskGroup (group);
				ThisScheduler.Add (task);
				
			}
		     
			return item_count;
		}

		////////////////////////////////////////////////

		// IIndexableGenerator implementation

		private ICollection files_to_parse;
		private IEnumerator file_enumerator = null;
		private IEnumerator item_enumerator = null;
		private Feed current_feed;

		public Indexable GetNextIndexable ()
		{
			Item item = (Item) this.item_enumerator.Current;

			return FeedItemToIndexable (this.current_feed, item);
		}

		public bool HasNextIndexable ()
		{
			if (this.files_to_parse.Count == 0)
				return false;

			while (this.item_enumerator == null || !this.item_enumerator.MoveNext ()) {
				if (this.file_enumerator == null)
					this.file_enumerator = this.files_to_parse.GetEnumerator ();

				do {
					if (!this.file_enumerator.MoveNext ())
						return false;

					FileInfo file = (FileInfo) this.file_enumerator.Current;

					if (this.FileAttributesStore.IsUpToDate (file.FullName))
						continue;

					Feed feed = Feed.LoadFromFile (file.FullName);

					this.FileAttributesStore.AttachTimestamp (file.FullName, file.LastWriteTime);
				
					if (feed == null || feed.Items == null)
						continue;

					this.current_feed = feed;

				} while (this.current_feed == null);

				this.item_enumerator = this.current_feed.Items.GetEnumerator ();
			}

			return true;
		}

		public string StatusName {
			get { return null; }
		}

	}	

	////////////////////////////////////////////////

	// De-serialization classes
	// FIXME: Change to standard stream parsing for performance? 

	public class Item {
		[XmlElement ("title")] public string Title = "";
		[XmlElement ("description")] public string Description ="";
		[XmlElement ("source")] public string Source="";
		[XmlElement ("attributes")] public Attributes Attribs;
		[XmlElement ("time")] public ulong Timestamp; 
	}
	
	public class Attributes{
		[XmlAttribute ("author")] public string Author = "";
	}
	
	public class Feed{
		[XmlElement ("feedTitle")] public string Title="";
		[XmlElement ("feedSource")] public string Source="";
		[XmlElement ("feedDescription")] public string Description="";
		
		[XmlElement ("feedStatus")] public int Status;
		[XmlElement ("feedUpdateInterval")] public int UpdateInterval;
		[XmlElement ("feedDiscontinued")] public string Discontinued ="";
		[XmlElement ("feedLastModified")] public string LastModified ="";

		[XmlElement ("item", typeof (Item))]
		public ArrayList Items {
			get { return mItems; }
			set { mItems = value; }
		}
		
		private ArrayList mItems = new ArrayList ();
		
		public static Feed LoadFromFile (string filename) {
			Feed f;
			XmlRootAttribute xRoot = new XmlRootAttribute();
			xRoot.ElementName = "feed";
			
			XmlSerializer serializer = new XmlSerializer (typeof (Feed), xRoot);
			Stream stream = new FileStream (filename,
							FileMode.Open,
							FileAccess.Read,
							FileShare.Read);
			XmlTextReader reader = new XmlTextReader (stream);
			
			if (!serializer.CanDeserialize(reader) )
				Console.WriteLine ("Muopp");
			f = (Feed) serializer.Deserialize (reader);

			reader.Close ();
			stream.Close ();
			return f;
		}
	}
}
