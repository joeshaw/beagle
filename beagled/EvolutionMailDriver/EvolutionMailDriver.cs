
//
// EvolutionMailDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//
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
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

using System.Threading;

using Mono.Posix;

using Beagle.Util;
using Camel = Beagle.Util.Camel;
using GConf;

namespace Beagle.Daemon {

	internal class SummaryCrawler : Crawler {
		
		EvolutionMailQueryable queryable;

		public SummaryCrawler (EvolutionMailQueryable queryable, string fingerprint) : base (fingerprint)
		{
			this.queryable = queryable;
		}

		protected override bool SkipByName (FileSystemInfo info)
		{
			// Skip directories...
			if (info as DirectoryInfo != null)
				return false;

			if (Path.GetExtension (info.Name) != ".ev-summary" && info.Name != "summary")
				return true;

			return false;
		}

		protected override void CrawlFile (FileSystemInfo info)
		{
			FileInfo file = info as FileInfo;

			if (Path.GetExtension (info.Name) == ".ev-summary" || info.Name == "summary")
				this.queryable.IndexSummary (file);
		}
	}

	internal class EvolutionMailIndexableGenerator : IIndexableGenerator {

		private static GConf.Client gconf_client = null;

		private EvolutionMailQueryable queryable;
		private FileInfo summaryInfo;
		private Camel.Summary summary;
		private IEnumerator summaryEnumerator;
		private string accountName, folderName, appDataName;
		private bool getCachedContent;
		private Hashtable mapping;
		private ArrayList deletedList;
		private int count, indexedCount;
		private ICollection accounts;

		public EvolutionMailIndexableGenerator (EvolutionMailQueryable queryable, FileInfo summaryInfo)
		{
			this.queryable = queryable;
			this.summaryInfo = summaryInfo;
		}

		private bool GConfReady ()
		{
			lock (this) {
				if (gconf_client == null)
					gconf_client = new GConf.Client ();

				this.accounts = (ICollection) gconf_client.Get ("/apps/evolution/mail/accounts");

				Monitor.Pulse (this);
			}

			return false;
		}
					

		private bool Setup ()
		{
			if (summaryInfo.Name == "summary") {
				string dirName = summaryInfo.DirectoryName;

				int imapStartIdx = dirName.IndexOf (".evolution/mail/imap/") + 21;
				string imapStart = dirName.Substring (imapStartIdx);
				string imapName = imapStart.Substring (0, imapStart.IndexOf ('/'));

				lock (this) {
					GLib.Idle.Add (new GLib.IdleHandler (this.GConfReady));
					Monitor.Wait (this);
				}

				foreach (string xml in this.accounts) {
					XmlDocument xmlDoc = new XmlDocument ();

					xmlDoc.LoadXml (xml);

					XmlNode account = xmlDoc.SelectSingleNode ("//account");

					if (account == null)
						continue;

					string uid = null;

					foreach (XmlAttribute attr in account.Attributes) {
						if (attr.Name == "uid") {
							uid = attr.InnerText;
							break;
						}
					}

					if (uid == null)
						continue;

					XmlNode imap_url = xmlDoc.SelectSingleNode ("//source/url");

					if (imap_url == null)
						continue;

					// Escape out additional @s in the name.  I hate the class libs so much.
					int lastIdx = imapName.LastIndexOf ('@');
					if (imapName.IndexOf ('@') != lastIdx) {
						string toEscape = imapName.Substring (0, lastIdx);
						imapName = toEscape.Replace ("@", "%40") + imapName.Substring (lastIdx);
					}

					if (imap_url.InnerText.StartsWith ("imap://" + imapName)) {
						this.accountName = uid;
						break;
					}
				}

				if (accountName == null) {
					EvolutionMailQueryable.log.Info ("Unable to determine account name for {0}", imapName);
					return false;
				}

				// Need to check the directory on disk to see if it's a junk/spam folder,
				// since the folder name will be "foo/spam" and not match the check below.
				DirectoryInfo dirInfo = new DirectoryInfo (dirName);
				if (dirInfo.Name.ToLower () == "spam" || dirInfo.Name.ToLower () == "junk") {
					EvolutionMailQueryable.log.Debug ("Skipping junk/spam folder {0} on {1}", dirName, this.accountName);
					return false;
				}
					
				this.folderName = EvolutionMailQueryable.GetImapFolderName (dirInfo);
				this.getCachedContent = true;
			} else {
				this.accountName = "local@local";
				this.folderName = EvolutionMailQueryable.GetLocalFolderName (this.summaryInfo);
				this.getCachedContent = false;
			}

			this.appDataName = "status-" + this.accountName + "-" + this.folderName.Replace ('/', '-');

			if (this.folderName.ToLower () == "spam" || this.folderName.ToLower () == "junk") {
				EvolutionMailQueryable.log.Debug ("Skipping junk/spam folder {0} on {1}", this.folderName, this.accountName);
				return false;
			}

			return true;
		}

		private bool LoadCache ()
		{
			Stream cacheStream;
			BinaryFormatter formatter;

			try {
				cacheStream = PathFinder.ReadAppData ("MailIndex", this.appDataName);
				formatter = new BinaryFormatter ();
				this.mapping = formatter.Deserialize (cacheStream) as Hashtable;
				cacheStream.Close ();
				EvolutionMailQueryable.log.Debug ("Successfully loaded previous crawled data from disk: {0}", this.appDataName);

				return true;
			} catch {
				this.mapping = new Hashtable ();

				return false;
			}
		}

		private void SaveCache ()
		{
			Stream cacheStream;
			BinaryFormatter formatter;
			
			cacheStream = PathFinder.WriteAppData ("MailIndex", this.appDataName);
			formatter = new BinaryFormatter ();
			formatter.Serialize (cacheStream, mapping);
			cacheStream.Close ();
		}

		public string GetTarget ()
		{
			return "summary-file:" + summaryInfo.FullName;
		}

		private bool CrawlNeeded ()
		{
			string timeStr;

			try {
				timeStr = ExtendedAttribute.Get (this.summaryInfo.FullName, "LastCrawl");
			} catch {
				EvolutionMailQueryable.log.Debug ("Unable to get last crawl time on {0}",
								  this.summaryInfo.FullName);
				return true;
			}

			DateTime lastCrawl = StringFu.StringToDateTime (timeStr);

			if (this.summaryInfo.LastWriteTime > lastCrawl)
				return true;
			else
				return false;
		}

		public bool HasNextIndexable ()
		{
			if (this.accountName == null) {
				if (!Setup ())
					return false;
			}

			if (this.mapping == null) {
				bool cache_loaded = this.LoadCache ();

				this.deletedList = new ArrayList (this.mapping.Keys);

				// Check to see if we even need to bother walking the summary
				if (cache_loaded && ! CrawlNeeded ()) {
					EvolutionMailQueryable.log.Debug ("{0}: summary has not been updated; crawl unncessary", this.folderName);
					return false;
				}
			}

			if (this.summary == null)
				this.summary = Camel.Summary.load (this.summaryInfo.FullName);

			if (this.summaryEnumerator == null)
				this.summaryEnumerator = this.summary.GetEnumerator ();

			if (this.summaryEnumerator.MoveNext ())
				return true;

			// FIXME: This is kind of a hack, but it's the only way with the IndexableGenerator
			// to handle our removals.
			foreach (string uid in this.deletedList) {
				Uri uri = EvolutionMailQueryable.EmailUri (this.accountName, this.folderName, uid);
				Scheduler.Task task = this.queryable.NewRemoveTask (uri);
				task.Priority = Scheduler.Priority.Immediate;
				this.queryable.ThisScheduler.Add (task);
			}

			EvolutionMailQueryable.log.Debug ("{0}: Finished indexing {1} ({2}/{3} {4:###.0}%)",
							  this.folderName, this.indexedCount, this.count,
							  this.summary.header.count,
							  100.0 * this.count / this.summary.header.count);

			this.SaveCache ();
				
			try {
				ExtendedAttribute.Set (this.summaryInfo.FullName, "LastCrawl",
						       StringFu.DateTimeToString (DateTime.Now));
			} catch {
				EvolutionMailQueryable.log.Debug ("Unable to set last crawl time on {0}",
								  this.summaryInfo.FullName);
			}
			
			return false;
		}

		public Indexable GetNextIndexable ()
		{
			Indexable indexable = null;

			Stream statusStream;
			BinaryFormatter formatter;
			
			Camel.MessageInfo mi = this.summaryEnumerator.Current as Camel.MessageInfo;

			// Checkpoint our progress to disk every 500 messages
			if (this.count % 500 == 0) {
				EvolutionMailQueryable.log.Debug ("{0}: indexed {1} messages ({2}/{3} {4:###.0}%)",
								  this.folderName, this.indexedCount, this.count,
								  this.summary.header.count,
								  100.0 * this.count / this.summary.header.count);

				if (this.count > 0)
					this.SaveCache ();
			}
			++this.count;

			if (this.mapping[mi.uid] == null || (uint) mapping[mi.uid] != mi.flags) {
				FileStream msgStream = null;
				TextReader msgReader = null;

				// FIXME: We need to handle MIME parts
				if (this.getCachedContent && this.mapping[mi.uid] == null) {
					string path = Path.Combine (summaryInfo.DirectoryName, mi.uid + ".");

					try {
						msgStream = new FileStream (path, System.IO.FileMode.Open,
									    FileAccess.Read);
						msgReader = new StreamReader (msgStream, new ASCIIEncoding ());
					} catch { }
				}

				indexable = EvolutionMailQueryable.MailToIndexable (this.accountName, this.folderName,
										    mi, msgReader);

				if (msgStream != null)
					msgStream.Close ();

				this.mapping[mi.uid] = mi.flags;
				++indexedCount;
			} 

			this.deletedList.Remove (mi.uid);

			return indexable;
		}

		public string StatusName {
			get {
				if (this.folderName != null)
					return this.folderName + " (" + this.accountName + ")";
				else
					return this.summaryInfo.FullName;
			}
		}

		public override bool Equals (object o)
		{
			EvolutionMailIndexableGenerator generator = o as EvolutionMailIndexableGenerator;

			if (generator == null)
				return false;

			if (Object.ReferenceEquals (this, generator))
				return true;

			if (this.summaryInfo.FullName == generator.summaryInfo.FullName)
				return true;
			else
				return false;
		}
	}

	[QueryableFlavor (Name="Mail", Domain=QueryDomain.Local)]
	public class EvolutionMailQueryable : LuceneQueryable {

		public static Logger log = Logger.Get ("mail");

		private SortedList watched = new SortedList ();
		private SummaryCrawler crawler;

		private object lockObj = new object ();
		private ArrayList AddedUris = new ArrayList ();
		private bool queryRunning = false;

		public EvolutionMailQueryable () : base (Path.Combine (PathFinder.RootDir, "MailIndex"))
		{
		}

		private void StartWorker ()
		{
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			string home = Environment.GetEnvironmentVariable ("HOME");
			string local_path = Path.Combine (home, ".evolution/mail/local");
			string imap_path = Path.Combine (home, ".evolution/mail/imap");

			// Get notification when an index or summary file changes
			Inotify.Event += OnInotifyEvent;
			Watch (local_path);
			Watch (imap_path);

			this.crawler = new SummaryCrawler (this, this.Driver.Fingerprint);
			Shutdown.ShutdownEvent += OnShutdown;

			this.crawler.ScheduleCrawl (new DirectoryInfo (local_path), -1);
			this.crawler.ScheduleCrawl (new DirectoryInfo (imap_path), -1);

			stopwatch.Stop ();
			Logger.Log.Info ("Evolution mail driver worker thread done in {0}",
					 stopwatch);
		}

		public override void Start () 
		{
			base.Start ();
			
			Thread th = new Thread (new ThreadStart (StartWorker));
			th.Start ();
		}

		private void OnShutdown ()
		{
			this.crawler.Stop ();
		}

		private void Watch (string path)
		{
			DirectoryInfo root = new DirectoryInfo (path);
			if (! root.Exists)
				return;

			Queue queue = new Queue ();
			queue.Enqueue (root);

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				int wd = Inotify.Watch (dir.FullName,
							Inotify.EventType.CreateSubdir
							| Inotify.EventType.DeleteSubdir
							| Inotify.EventType.MovedTo);
				watched [wd] = dir.FullName;

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}
		}

		private void Ignore (string path)
		{
			Inotify.Ignore (path);
			watched.RemoveAt (watched.IndexOfValue (path));
		}

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     Inotify.EventType type,
					     uint cookie)
		{
			if (subitem == "" || ! watched.Contains (wd))
				return;

			string fullPath = Path.Combine (path, subitem);

			switch (type) {
				
			case Inotify.EventType.CreateSubdir:
				Watch (fullPath);
				break;

			case Inotify.EventType.DeleteSubdir:
				Ignore (fullPath);
				break;

			case Inotify.EventType.MovedTo:
				if (Path.GetExtension (fullPath) == ".ev-summary" || subitem == "summary") {
					log.Info ("reindexing updated summary: {0}", fullPath);
					this.IndexSummary (new FileInfo (fullPath));
				}

				break;
			}
		}

		public string Name {
			get { return "EvolutionMail"; }
		}

		public static string GetLocalFolderName (FileInfo fileInfo)
		{
			DirectoryInfo di;
			string folderName = "";

			di = fileInfo.Directory;
			while (di != null) {
				// Evo uses ".sbd" as the extension on a folder
				if (di.Extension == ".sbd")
					folderName = Path.Combine (folderName, Path.GetFileNameWithoutExtension (di.Name));
				else
					break;

				di = di.Parent;
			}

			return Path.Combine (folderName, Path.GetFileNameWithoutExtension (fileInfo.Name));
		}

		public static string GetImapFolderName (DirectoryInfo dirInfo)
		{
			string folderName = "";

			while (dirInfo != null) {
				folderName = Path.Combine (dirInfo.Name, folderName);

				dirInfo = dirInfo.Parent;

				if (dirInfo.Name != "subfolders")
					break;
				else
					dirInfo = dirInfo.Parent;
			}

			return folderName;
		}

		public override void DoQuery (QueryBody body,
					      IQueryResult result,
					      IQueryableChangeData changeData)
		{
			// First, chain up to the Lucene index
			base.DoQuery (body, result, changeData);

			if (changeData != null)
				return;

			// Now create a CamelIndexDriver and pass it off there
			CamelIndexDriver driver = new CamelIndexDriver (this, this.Driver, body, result);

			if (Shutdown.ShutdownRequested)
				return;

			driver.Start ();
                }

		public void IndexSummary (FileInfo summaryInfo)
		{
			EvolutionMailIndexableGenerator generator = new EvolutionMailIndexableGenerator (this, summaryInfo);
			Scheduler.Task task;
			task = NewAddTask (generator);
			// IndexableGenerator tasks default to having priority Scheduler.Priority Generator
			ThisScheduler.Add (task);
		}

		public static Uri EmailUri (string accountName, string folderName, string uid)
		{
			return new Uri (String.Format ("email://{0}/{1};uid={2}",
						       accountName, folderName, uid));
		}

		public static Indexable MailToIndexable (string accountName, string folderName,
							 Camel.MessageInfo messageInfo, TextReader msgReader)
		{
			System.Uri uri = EmailUri (accountName, folderName, messageInfo.uid);
			Indexable indexable = new Indexable (uri);

			indexable.Timestamp = messageInfo.Date;
			indexable.Type = "MailMessage";
			indexable.MimeType = "text/plain";

			indexable.AddProperty (Property.NewKeyword ("dc:title", messageInfo.subject));

                        indexable.AddProperty (Property.NewKeyword ("fixme:folder",   folderName));
			indexable.AddProperty (Property.NewKeyword ("fixme:subject",  messageInfo.subject));
                        indexable.AddProperty (Property.NewKeyword ("fixme:to",       messageInfo.to));
			indexable.AddProperty (Property.NewKeyword ("fixme:from",     messageInfo.from));
                        indexable.AddProperty (Property.NewKeyword ("fixme:cc",       messageInfo.cc));
                        indexable.AddProperty (Property.NewKeyword ("fixme:mlist",    messageInfo.mlist));
                        indexable.AddProperty (Property.NewKeyword ("fixme:flags",    messageInfo.flags));

			if (messageInfo.received != DateTime.MinValue)
				indexable.AddProperty (Property.NewDate ("fixme:received", messageInfo.received));

			if (messageInfo.sent != DateTime.MinValue)
				indexable.AddProperty (Property.NewDate ("fixme:sentdate", messageInfo.sent));

			if (folderName == "Sent")
				indexable.AddProperty (Property.NewFlag ("fixme:isSent"));

			if (messageInfo.IsAnswered)
				indexable.AddProperty (Property.NewFlag ("fixme:isAnswered"));

			if (messageInfo.IsDeleted)
				indexable.AddProperty (Property.NewFlag ("fixme:isDeleted"));

			if (messageInfo.IsDraft)
				indexable.AddProperty (Property.NewFlag ("fixme:isDraft"));

			if (messageInfo.IsFlagged)
				indexable.AddProperty (Property.NewFlag ("fixme:isFlagged"));

			if (messageInfo.IsSeen)
				indexable.AddProperty (Property.NewFlag ("fixme:isSeen"));

			if (messageInfo.HasAttachments)
				indexable.AddProperty (Property.NewFlag ("fixme:hasAttachments"));

			if (messageInfo.IsAnsweredAll)
				indexable.AddProperty (Property.NewFlag ("fixme:isAnsweredAll"));

			if (msgReader != null)
				indexable.SetTextReader (msgReader);

			return indexable;
		}

		
	}

}
