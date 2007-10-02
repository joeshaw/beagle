//
// DownloadsMetadataQueryable.cs
//
// Copyright (C) 2007 Kevin Kubasik <kevin@kubasik.net>
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
using System.Threading;
using System.Xml;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.DownloadsMetadataQueryable {

	[PropertyKeywordMapping (Keyword="origin",  PropertyName="beagle:Origin", IsKeyword=false, Description="The URI/Url where this file was downloaded from.")]
	[QueryableFlavor (Name="DownloadsMetadata", Domain=QueryDomain.Local, RequireInotify=true,
			  DependsOn="Files")]
	public class DownloadsMetadataQueryable : ExternalMetadataQueryable, IIndexableGenerator  {

		private string profile_dir;
		private FileSystemQueryable.FileSystemQueryable target_queryable;
		private Firefox internalff;
		
		private List<Beagle.Util.DownloadedFile> downedfiles; 
		System.Collections.IEnumerator enumer;
		public DownloadsMetadataQueryable ()
		{
			profile_dir = Path.Combine (Path.Combine (PathFinder.HomeDir, ".mozilla"), "firefox");
			string[] dirs = Directory.GetDirectories(profile_dir);
			profile_dir = Path.Combine (profile_dir,dirs[0]);
		}

		public override void Start () 
		{
                        base.Start ();

			// The FSQ
			Queryable queryable = QueryDriver.GetQueryable ("Files");
			this.target_queryable = (FileSystemQueryable.FileSystemQueryable) queryable.IQueryable;

			string fsq_fingerprint = target_queryable.IndexFingerprint;
			InitFileAttributesStore ("DownloadsMetadata", fsq_fingerprint);
			
			if (! Directory.Exists (profile_dir))
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
			else
				ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (Inotify.Enabled) {
			
				Inotify.EventType mask = Inotify.EventType.Modify;
				Inotify.Subscribe (profile_dir, OnInotifyEvent, mask);
			}
			 internalff = new Firefox(profile_dir);
			downedfiles = internalff.GetDownloads();
			enumer   = downedfiles.GetEnumerator();
			
			Scheduler.Task task;
			task = this.target_queryable.NewAddTask (this);
			task.Tag = "Crawling Downloads Metadata";
			task.Source = this;

			ThisScheduler.Add (task);

			Log.Info ("Downloads metadata backend started");
		}

		private bool CheckForExistence ()
		{
			if (!Directory.Exists (profile_dir))
				return true;
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));

			return false;
		}

		/////////////////////////////////////////////////

		public Indexable GetIndexable (Beagle.Util.DownloadedFile df)
		{
			Indexable indexable = new Indexable (new Uri(df.Local));
			indexable.Type = IndexableType.PropertyChange;
			
			Property prop;
			
			prop = Property.New ("beagle:Origin", df.Remote);
			prop.IsMutable = true;
			prop.IsPersistent = true;
			indexable.AddProperty (prop);


			return indexable;

		}

		/////////////////////////////////////////////////

		private void OnInotifyEvent (Inotify.Watch watch,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (subitem == "")
				return;

			if (subitem != "downloads.rdf")
				return;

			
			string file = Path.Combine (path, subitem);

			DateTime last_checked = DateTime.MinValue;

			FileAttributes attr;
			attr = FileAttributesStore.Read (file);
			if (attr != null)
				last_checked = attr.LastWriteTime;
			Firefox f = new Firefox(profile_dir);
			foreach (Beagle.Util.DownloadedFile df in f.GetDownloads()) {
				Indexable indexable = GetIndexable (df);

				Scheduler.Task task;
				task = this.target_queryable.NewAddTask (indexable);
				task.Priority = Scheduler.Priority.Immediate;

				ThisScheduler.Add (task);
			}
		}

		/////////////////////////////////////////////////

		// IIndexableGenerator implementation
		public string StatusName {
			get { return "DownloadsMetadataQueryable"; }
		}

		
		
		public void PostFlushHook () { }

		public bool HasNextIndexable ()
		{
			if (!FileAttributesStore.IsUpToDate (Path.Combine(profile_dir,"downloads.rdf"))){
				internalff = new Firefox(profile_dir);
				downedfiles = internalff.GetDownloads();
				enumer = downedfiles.GetEnumerator();
			}
			
			if(enumer.MoveNext()){
				
				FileAttributesStore.AttachLastWriteTime ((string)Path.Combine(profile_dir,"downloads.rdf"), DateTime.UtcNow);
				return true;
			}			
			
			return false; 
		}

		public Indexable GetNextIndexable ()
		{
			return GetIndexable((Beagle.Util.DownloadedFile)enumer.Current);
		}

	}
}
