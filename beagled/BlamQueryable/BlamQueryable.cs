//
// BlamQueryable.cs
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
using System.IO;
using System.Collections;
using System.Threading;

using System.Xml;
using System.Xml.Serialization;
	
using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.BlamQueryable {

	[QueryableFlavor (Name="Blam", Domain=QueryDomain.Local, RequireInotify=false)]
	public class BlamQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("BlamQueryable");

		string blam_dir;
		const string blam_file = "collection.xml";
		int blam_wd = -1;

		public BlamQueryable () : base ("BlamIndex")
		{
			blam_dir = Path.Combine (Path.Combine (PathFinder.HomeDir, ".gnome2"), "blam");
		}

		/////////////////////////////////////////////////

		private void StartWorker ()
		{
			if (Inotify.Enabled) {
				Inotify.EventType mask = Inotify.EventType.CloseWrite;
				blam_wd = Inotify.Watch (blam_dir, mask);
				Inotify.Event += OnInotifyEvent;
			} else {
				FileSystemWatcher fsw = new FileSystemWatcher ();
			       	fsw.Path = blam_dir;
				fsw.Filter = "collection.xml";
				
				fsw.Changed += new FileSystemEventHandler (OnChangedEvent);
				fsw.Created += new FileSystemEventHandler (OnChangedEvent);
				
				fsw.EnableRaisingEvents = true;
			}

			Index ();
		}

		public override void Start () 
		{			
			if (! File.Exists (Path.Combine (blam_dir, blam_file)))
				return;

			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		/////////////////////////////////////////////////

		// Modified event using Inotify

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     Inotify.EventType type,
					     uint cookie)
		{
			if (wd != blam_wd)
				return;

			if (subitem == blam_file)
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
			FileInfo file = new FileInfo (Path.Combine (blam_dir, blam_file));

			if (this.FileAttributesStore.IsUpToDate (file.FullName))
				return;

			ChannelCollection collection = null;

			try {
				collection = ChannelCollection.LoadFromFile (file.FullName);
			} catch (Exception e) {
				log.Info ("Could not open Blam! channel list: " + e);
				return;
			}

			if (collection == null)
				return;

			log.Info ("Scanning Weblogs");
			Stopwatch stopwatch = new Stopwatch ();
			int blogCount = 0, itemCount = 0;
			stopwatch.Start ();

			Scheduler.TaskGroup group = NewMarkingTaskGroup (file.FullName, file.LastWriteTime);
			
			foreach (Channel channel in collection.Channels)	{

				if (channel.Items == null)
					continue;

				foreach(Item item in channel.Items) {
					Indexable indexable = new Indexable (
						new Uri (String.Format ("feed:{0};item={1}", channel.Url, item.Id)));
					indexable.MimeType = "text/html";
					indexable.Type = "FeedItem";
					indexable.Timestamp = item.PubDate;
					
					indexable.AddProperty(Property.NewKeyword ("dc:title", item.Title));
					indexable.AddProperty(Property.NewKeyword ("fixme:author", item.Author));
					indexable.AddProperty(Property.NewDate ("fixme:published", item.PubDate));
					indexable.AddProperty(Property.NewKeyword ("fixme:itemuri", item.Link));
					indexable.AddProperty(Property.NewKeyword ("fixme:webloguri", channel.Url));

					int i;
					string img = null;
					i = item.Text.IndexOf ("<img src=\"");
					if (i != -1) {
						i += "<img src=\"".Length;
						int j = item.Text.IndexOf ("\"", i);
						if (j != -1)
							img = item.Text.Substring (i, j-i);
					}

					if (img != null) {
						string path = Path.Combine (Path.Combine (blam_dir, "Cache"),
									    img.GetHashCode ().ToString ());
						indexable.AddProperty (Property.NewKeyword ("fixme:cachedimg", path));
					}

					StringReader reader = new StringReader (item.Text);
					indexable.SetTextReader (reader);

					Scheduler.Task task = NewAddTask (indexable);
					task.Priority = Scheduler.Priority.Delayed;
					task.SubPriority = 0;
					task.AddTaskGroup (group);
					ThisScheduler.Add (task);
					
					++itemCount;
				}
				
				++blogCount;
			}
			
			stopwatch.Stop ();
			log.Info ("Found {0} items in {1} weblogs in {2}", 
				  itemCount, blogCount, stopwatch);
		}
	}

	/////////////////////////////////////////////////

	// Classes from Blam! sources for deserialization

	public class ChannelCollection {

		private ArrayList mChannels;
		
		[XmlElement ("Channel", typeof (Channel))]
		public ArrayList Channels {
			get { return mChannels; }
			set { mChannels = value; }
		}
		
		public static ChannelCollection LoadFromFile (string filename)
		{
			XmlSerializer serializer = new XmlSerializer (typeof (ChannelCollection));
			ChannelCollection collection;

			Stream stream = new FileStream (filename,
							FileMode.Open,
							FileAccess.Read,
							FileShare.ReadWrite);
			XmlTextReader reader = new XmlTextReader (stream);

			collection = (ChannelCollection) serializer.Deserialize (reader);
			reader.Close ();
			stream.Close ();

			return collection;
		}
	}
	
	public class Channel
	{
		[XmlAttribute] public string Name = "";
		[XmlAttribute] public string Url = "";

		[XmlAttribute] public string LastModified = "";
		[XmlAttribute] public string ETag = "";
	     
		ArrayList mItems;
	    
		[XmlElement ("Item", typeof (Item))]
		public ArrayList Items {
			get { return mItems; }
			set { mItems = value; }
		}
	}
	
	public class Item
	{
		[XmlAttribute] public string   Id = "";
		[XmlAttribute] public bool     Unread = true;		
		[XmlAttribute] public string   Title = "";
		[XmlAttribute] public string   Text = "";
		[XmlAttribute] public string   Link = "";
		[XmlAttribute] public DateTime PubDate;
		[XmlAttribute] public string   Author = "";
  	}
}
