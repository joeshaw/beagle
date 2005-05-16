//
// MozillaQueryable.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Authors:
//   Fredrik Hedberg (fredrik.hedberg@avafan.com)
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
using System.Threading;

using Beagle.Util;
using Beagle.Util.Mozilla;

namespace Beagle.Daemon.Mozilla {

	internal class MozillaCrawler : Crawler {
		
		MozillaQueryable queryable;

		public MozillaCrawler (MozillaQueryable queryable, string fingerprint) : base (fingerprint)
		{
			this.queryable = queryable;
		}

		protected override bool SkipByName (FileSystemInfo info)
		{
			if (info as DirectoryInfo != null)
				return false;

			if (File.Exists (info + ".msf"))
				return false;

			return true;
		}

		protected override void CrawlFile (FileSystemInfo info)
		{
			FileInfo file = info as FileInfo;

			if (file != null)
				this.queryable.Index (file);
		}
	}

	[QueryableFlavor (Name="Mail", Domain=QueryDomain.Local)]
	public class MozillaMailQueryable : MozillaQueryable
	{
		public MozillaMailQueryable () : base ("MozillaMailIndex")
		{
			AddAccountType ("none");
			AddAccountType ("imap");
		}

		protected override Indexable MessageToIndexable (Message message)
		{
			Uri uri = new Uri (String.Format ("email:///{0};id={1}", message.Path, message.Id));

			Indexable indexable = new Indexable (uri);
			indexable.Type = "MailMessage";

			indexable.AddProperty (Property.New ("fixme:client", "mozilla"));

			indexable.AddProperty (Property.NewKeyword ("dc:title", message.Subject));

			indexable.AddProperty (Property.NewKeyword ("fixme:subject", message.Subject));
			indexable.AddProperty (Property.NewKeyword ("fixme:to", message.To));
			indexable.AddProperty (Property.NewKeyword ("fixme:from", message.From));

			indexable.AddProperty (Property.New ("fixme:offset", message.Offset));

			StringReader reader = new StringReader (message.Body);
			indexable.SetTextReader (reader);

			return indexable;
		}
	}

	[QueryableFlavor (Name="Feed", Domain=QueryDomain.Local)]
	public class MozillaFeedQueryable : MozillaQueryable
	{
		public MozillaFeedQueryable () : base ("MozillaFeedIndex")
		{
			AddAccountType ("rss");
		}

		protected override Indexable MessageToIndexable (Message message)
		{
			Uri uri = new Uri (String.Format ("feed:///{0};id={1}", message.Path, message.Id));

			Indexable indexable = new Indexable (uri);
			indexable.MimeType = "text/html";
			indexable.Type = "FeedItem";

			indexable.AddProperty (Property.New ("fixme:client", "mozilla"));

			indexable.AddProperty(Property.NewKeyword ("dc:title", message.Subject));
			indexable.AddProperty(Property.NewKeyword ("fixme:author", message.From));
			//indexable.AddProperty(Property.NewDate ("fixme:published", item.PubDate));
			indexable.AddProperty(Property.NewKeyword ("fixme:itemuri", message.Headers ["Content-Base"]));

			indexable.AddProperty (Property.New ("fixme:offset", message.Offset));

			StringReader reader = new StringReader (message.Body);
			indexable.SetTextReader (reader);
			
			return indexable;
		}
	}

	public abstract class MozillaQueryable : LuceneQueryable {

		public static Logger log = Logger.Get ("mozilla");

		private MozillaCrawler crawler;

		private ArrayList accountTypes = new ArrayList ();

		public MozillaQueryable (string indexname) : base (indexname)
		{
		}


		protected void AddAccountType (string str)
		{
			accountTypes.Add (str);
		}

		private void StartWorker ()
		{
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			Shutdown.ShutdownEvent += OnShutdown;

			this.crawler = new MozillaCrawler (this, this.Driver.Fingerprint);

			foreach (Profile profile in Profile.ReadProfiles ()) {
				foreach (Account account in profile.Accounts) {
					if (!accountTypes.Contains (account.Type))
						continue;

					this.crawler.ScheduleCrawl (new DirectoryInfo (account.Path), -1);

					FileSystemWatcher fsw = new FileSystemWatcher ();
					fsw.Path = account.Path;
					fsw.IncludeSubdirectories = true;

					fsw.Changed += new FileSystemEventHandler (OnChanged);
					fsw.Created += new FileSystemEventHandler (OnChanged);
					fsw.Deleted += new FileSystemEventHandler (OnChanged);

					fsw.EnableRaisingEvents = true;
				}
			}

			Shutdown.ShutdownEvent += OnShutdown;

			this.crawler.StopWhenEmpty ();

			stopwatch.Stop ();
			Logger.Log.Info ("MozillaQueryable worker thread done in {0}",
					 stopwatch);
		}

		private void OnChanged (object source, FileSystemEventArgs args)
		{
			switch (args.ChangeType) {
			case WatcherChangeTypes.Changed:
			case WatcherChangeTypes.Created:
				if (File.Exists (args.FullPath + ".msf"))
					Index (new FileInfo (args.FullPath));
				break;
			case WatcherChangeTypes.Deleted:
				// FIXME: Do
				break;
			}
		}

		public override void Start () 
		{
			base.Start ();
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void OnShutdown ()
		{
			this.crawler.Stop ();
		}

		public string Name {
			get { return "EvolutionMail"; }
		}

		public void Index (FileInfo file)
		{
			Scheduler.TaskGroup group = NewMarkingTaskGroup (file.FullName, file.LastWriteTime);

			MessageReader reader = new MessageReader (file.FullName);

			while (reader.HasMoreMessages) {
				Message message = reader.NextMessage;
				Indexable indexable = MessageToIndexable (message);

				Scheduler.Task task = NewAddTask (indexable);
				task.Priority = Scheduler.Priority.Delayed;
				task.SubPriority = 0;
				task.AddTaskGroup (group);
				ThisScheduler.Add (task);
			}
			
		}

		protected abstract Indexable MessageToIndexable (Message message);
	}
}
