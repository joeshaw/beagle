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

	[QueryableFlavor (Name="Liferea", Domain=QueryDomain.Local)]
	public class LifereaQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("LifereaQueryable");

		string lifereaDir;

		int wdLiferea = -1;

		public LifereaQueryable () : base ("LifereaIndex")
		{
			lifereaDir = Path.Combine (Environment.GetEnvironmentVariable ("HOME"), ".liferea");
			lifereaDir = Path.Combine (lifereaDir, "cache");
			lifereaDir = Path.Combine (lifereaDir, "feeds");
		}

		private void StartWorker ()
		{
			Inotify.EventType mask;
			mask = Inotify.EventType.CloseWrite;

			wdLiferea = Inotify.Watch (lifereaDir, mask);

			Inotify.Event += OnInotifyEvent;

			Index();
		}

		public override void Start () 
		{			
			// FIXME: We should do something more reasonable if
			// ~/.liferea/cache doesn't exist.
			DirectoryInfo dir = new DirectoryInfo(lifereaDir);
			 
			if(! dir.Exists)
				return;
				
			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     Inotify.EventType type,
					     uint cookie)
		{
			if (wd != wdLiferea)
				return;

			// Ignore operations on the directories themselves
			if (subitem == "")
				return;

			Index (Path.Combine(path, subitem) );
		}
		
		private void Index(){
			Index(null);
		}
		
		private void Index (string filename)
		{
			FileInfo [] files ;
			if( filename == null){
				DirectoryInfo dir = new DirectoryInfo(lifereaDir);
				files = dir.GetFiles();
			}
			else{
				files = new FileInfo[1];
				files[0] = new FileInfo(filename);
				log.Info("indexing " + filename);
			}
			
			
			Feed f;
			log.Info ("Scanning Liferea Weblogs");
			Stopwatch stopwatch = new Stopwatch ();
			int blogCount = 0, itemCount = 0;
			stopwatch.Start ();
			
			foreach(FileInfo file in files){
				blogCount++;
			 	if (this.FileAttributesStore.IsUpToDate (file.FullName))
					continue;
				Scheduler.TaskGroup group = NewMarkingTaskGroup (file.FullName, file.LastWriteTime);
				
				f = Feed.LoadFromFile(file.FullName);
				
				if(f == null)
					continue;
	
				if(f.Items == null)
					continue;
					
				IEnumerator e = f.Items.GetEnumerator();
				
				
				while(e.MoveNext() ){
					itemCount++;
					Item i = (Item) e.Current;
					
					Indexable indexable = new Indexable ( new Uri(i.Source));
					indexable.MimeType = "text/html";
					indexable.Type = "FeedItem";
					
					DateTime date = new DateTime(1970, 1, 1);
					
					date = date.AddSeconds( i.Timestamp );
					indexable.Timestamp = date;				
					indexable.AddProperty(Property.NewKeyword ("dc:title", i.Title) );
					indexable.AddProperty(Property.NewKeyword ("dc:description", i.Description));
					indexable.AddProperty(Property.NewKeyword ("fixme:author", i.Attribs.Author));
					indexable.AddProperty(Property.NewDate ("fixme:published", date));
					indexable.AddProperty(Property.NewKeyword ("fixme:itemuri", i.Source));
					indexable.AddProperty(Property.NewKeyword ("fixme:webloguri", f.Source));

					// FIXME Use FilterHtml to mark "hot" words in content
					StringReader reader = new StringReader (i.Description);
					indexable.SetTextReader (reader);

					Scheduler.Task task = NewAddTask (indexable);
					task.Priority = Scheduler.Priority.Delayed;
					task.SubPriority = 0;
					task.AddTaskGroup (group);
					ThisScheduler.Add (task);

				}
			}
		
			stopwatch.Stop ();
			log.Info ("Found {0} items in {1} Liferea weblogs in {2}", 
			itemCount, blogCount, stopwatch);
		
		}
	}	

	public class Item{
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
		[XmlElement ("feedTitle")] 	public string Title="";
		[XmlElement ("feedSource")] public string Source="";
		[XmlElement ("feedDescription")] public string Description="";
	
		[XmlElement ("feedStatus")] public int	Status;
		[XmlElement ("feedUpdateInterval")] public int UpdateInterval;
		[XmlElement ("feedDiscontinued")] public string Discontinued ="";
		[XmlElement ("feedLastModified")] public string LastModified ="";
		
	
			
		[XmlElement ("item", typeof (Item))]
		public ArrayList Items {
			get { return mItems; }
			set { mItems = value; }
		}
		
		
		private ArrayList mItems = new ArrayList();
	
		
		public static Feed LoadFromFile(string filename){
			
			Feed f;
			XmlRootAttribute xRoot = new XmlRootAttribute();
        	xRoot.ElementName = "feed";
        	
			XmlSerializer serializer = new XmlSerializer (typeof (Feed), xRoot);
			Stream stream = new FileStream (filename,
							FileMode.Open,
							FileAccess.Read,
							FileShare.Read);
			XmlTextReader reader = new XmlTextReader (stream);

			if( !serializer.CanDeserialize(reader) )
				Console.WriteLine("Muopp");
			f = (Feed) serializer.Deserialize (reader);
		
			
			reader.Close ();
			stream.Close ();
			return f;
		}
	}
}
