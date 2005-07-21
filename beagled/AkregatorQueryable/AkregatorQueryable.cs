//
// AkregatorQueryable.cs
//
// Copyright (C) 2005 Debajyoti Bera
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
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.AkregatorQueryable {

	[QueryableFlavor (Name="Akregator", Domain=QueryDomain.Local, RequireInotify=false)]
	public class AkregatorQueryable : LuceneQueryable, IIndexableGenerator  {

		private static Logger log = Logger.Get ("AkregatorQueryable");

		string akregator_dir;

		public AkregatorQueryable () : base ("AkregatorIndex")
		{
			akregator_dir = Path.Combine (PathFinder.HomeDir, ".kde");
			akregator_dir = Path.Combine (akregator_dir, "share");
			akregator_dir = Path.Combine (akregator_dir, "apps");
			akregator_dir = Path.Combine (akregator_dir, "akregator");
			akregator_dir = Path.Combine (akregator_dir, "Archive");
		}

		/////////////////////////////////////////////////

		public override void Start () 
		{			
			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (!Directory.Exists (akregator_dir)) {
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
                                return;
			}
				
			if (Inotify.Enabled) {
				Inotify.EventType mask = Inotify.EventType.CloseWrite;

				Inotify.Subscribe (akregator_dir, OnInotifyEvent, mask);
			} else {
                                FileSystemWatcher fsw = new FileSystemWatcher ();
                                fsw.Path = akregator_dir;

                                fsw.Changed += new FileSystemEventHandler (OnChanged);
                                fsw.Created += new FileSystemEventHandler (OnChanged);

                                fsw.EnableRaisingEvents = true;
			}

                        log.Info ("Scanning Akregator feeds...");

			Stopwatch stopwatch = new Stopwatch ();
                        int feed_count = 0, item_count = 0;

			stopwatch.Start ();

                        DirectoryInfo dir = new DirectoryInfo (akregator_dir);
			this.files_to_parse = dir.GetFiles ();
			Scheduler.Task task = NewAddTask (this);
			task.Tag = "Akregator";
			ThisScheduler.Add (task);

			stopwatch.Stop ();
                        log.Info ("{0} files will be parsed (scanned in {1})", this.files_to_parse.Count, stopwatch);
		}

		private bool CheckForExistence ()
                {
                        if (!Directory.Exists (akregator_dir))
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
		
		private Indexable FeedItemToIndexable (Channel channel, Item item)
		{
			Indexable indexable = new Indexable (new Uri (String.Format ("feed:{0};item={1}", channel.Link, item.Link)));
			indexable.MimeType = "text/html";
			indexable.Type = "FeedItem";

			int offset; //will be ignored - only store the time at current machine
			DateTime date = GMime.Utils.HeaderDecodeDate (item.PubDate, out offset);

			indexable.Timestamp = date;				

			indexable.AddProperty (Property.New ("dc:title", item.Title));
			indexable.AddProperty (Property.NewDate ("fixme:published", date));
			indexable.AddProperty (Property.NewKeyword ("fixme:itemuri", item.Link));
			indexable.AddProperty (Property.NewKeyword ("fixme:webloguri", channel.Link));
				
			StringReader reader = new StringReader (item.Description);
			indexable.SetTextReader (reader);

			return indexable;
		}
		// Parse and index a single feed

		private int IndexSingleFeed (string filename, Scheduler.Priority priority)
		{
			FileInfo file = new FileInfo(filename);
			
			RSS feed;
			int item_count = 0;

			if (this.FileAttributesStore.IsUpToDate (file.FullName))
			        return 0;

			Scheduler.TaskGroup group = NewMarkingTaskGroup (file.FullName, file.LastWriteTime);
			
			feed = RSS.LoadFromFile(file.FullName);
			
			if(feed == null || feed.channel == null || feed.channel.Items == null)
				return 0;
			
			foreach (Item item in feed.channel.Items) {
				item_count++;
				
				Indexable indexable = FeedItemToIndexable (feed.channel, item);
				
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
		private RSS current_feed;

		public Indexable GetNextIndexable ()
		{
			Item item = (Item) this.item_enumerator.Current;

			return FeedItemToIndexable (this.current_feed.channel, item);
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

					RSS feed = RSS.LoadFromFile (file.FullName);

					this.FileAttributesStore.AttachTimestamp (file.FullName, file.LastWriteTime);
				
					if (feed == null || feed.channel == null || feed.channel.Items == null)
						continue;

					this.current_feed = feed;

				} while (this.current_feed == null);

				this.item_enumerator = this.current_feed.channel.Items.GetEnumerator ();
			}

			return true;
		}

		public string StatusName {
			get { return null; }
		}

	}	

	////////////////////////////////////////////////

	// De-serialization classes
	// Changing to standard stream parsing will increse performance no doubt
	// but not sure if it will be noticable

	public class Item {
		[XmlElement ("title")] public string Title = "";
		[XmlElement ("description")] public string Description ="";
		[XmlElement ("link")] public string Link="";
		[XmlElement ("pubDate")] public string PubDate; 
	}
	
	public class Channel{
		[XmlElement ("title")] public string Title="";
		[XmlElement ("link")] public string Link="";
		[XmlElement ("description")] public string Description="";
		

		[XmlElement ("item", typeof (Item))]
		public ArrayList Items {
			get { return mItems; }
			set { mItems = value; }
		}
		private ArrayList mItems = new ArrayList ();
	}	
	
	public class RSS{
		[XmlElement ("channel", typeof (Channel))]
		public Channel channel;
		
		public static RSS LoadFromFile (string filename) {
			RSS f;
			XmlRootAttribute xRoot = new XmlRootAttribute();
			xRoot.ElementName = "rss";
			
			XmlSerializer serializer = new XmlSerializer (typeof (RSS), xRoot);
			Stream stream = new FileStream (filename,
							FileMode.Open,
							FileAccess.Read,
							FileShare.Read);
			XmlTextReader reader = new XmlTextReader (stream);
			
			if (!serializer.CanDeserialize(reader) )
				Console.WriteLine ("Muopp");
			f = (RSS) serializer.Deserialize (reader);

			reader.Close ();
			stream.Close ();
			return f;
		}

	}
}
