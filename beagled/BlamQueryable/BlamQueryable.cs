//
// BlamQueryable.cs
//
// Copyright (C) 2004 Fredrik Hedberg
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
using System.Collections;

using System.Xml;
using System.Xml.Serialization;
	
using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.BlamQueryable {

	[QueryableFlavor (Name="Blam", Domain=QueryDomain.Local)]
	public class BlamQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("BlamQueryable");

		string blamDir;

		int wdBlam = -1;

		public BlamQueryable () : base (Path.Combine (PathFinder.RootDir, "BlamIndex"))
		{
			blamDir = Path.Combine (Path.Combine (Environment.GetEnvironmentVariable ("HOME"), ".gnome2"), "blam");
			
			// FIXME: We should do something more reasonable if
			// ~/.gnome2/blam doesn't exist.
			if (! Directory.Exists (blamDir))
				return;
			
			InotifyEventType mask;
			mask = InotifyEventType.CreateFile
				| InotifyEventType.DeleteFile
				| InotifyEventType.Modify;

			wdBlam = Inotify.Watch (blamDir, mask);

			Inotify.InotifyEvent += new InotifyHandler (OnInotifyEvent);

			Index();
		}

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     InotifyEventType type,
					     int cookie)
		{
			if (wd != wdBlam)
				return;

			// Ignore operations on the directories themselves
			if (subitem == "")
				return;

			Index ();
		}
		
		private void Index ()
		{
			FileInfo file = new FileInfo (Path.Combine (blamDir, "collection.xml"));
			
			ChannelCollection collection = ChannelCollection.LoadFromFile (file.FullName);
			
			log.Info ("Scanning blogs...");
			Stopwatch stopwatch = new Stopwatch ();
			int blogCount = 0, itemCount = 0;
			stopwatch.Start ();
			
			foreach(Channel channel in collection.Channels)	{

				// FIXME: Only index channels and items that have been modified
				
				foreach(Item item in channel.Items) {
					Indexable indexable = new Indexable(new	Uri(channel.Url.Replace("http://","rss://") + ";item="+item.Id));
					indexable.MimeType = "text/html";
					indexable.Type = "Blog";
					indexable.Timestamp = item.PubDate;
					
					indexable.AddProperty(Property.NewKeyword("dc:title",item.Title));
					indexable.AddProperty(Property.NewKeyword("fixme:author",item.Author));
					indexable.AddProperty(Property.NewKeyword("fixme:published",item.PubDate));
						
					// FIXME Use FilterHtml to mark "hot" words in content
					
					StringReader reader = new StringReader(item.Text);
					indexable.SetTextReader(reader);
					
					Driver.ScheduleAddAndMark (indexable, 0,file);
					
					++itemCount;
				}
				
				++blogCount;
			}
			
			stopwatch.Stop ();
			log.Info ("Scanned {0} items in {1} blogs in {2}", 
				  itemCount, blogCount, stopwatch);
		}
	}
	
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
							FileShare.Read);
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
