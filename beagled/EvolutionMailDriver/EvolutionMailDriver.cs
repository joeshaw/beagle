
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

	internal class CamelIndex : IDisposable {
		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_text_index_new (string path, int mode);

		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_index_words (IntPtr index);

		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_index_find (IntPtr index, string word);

		[DllImport ("libcamel.so.0")]
		extern static IntPtr camel_index_cursor_next (IntPtr cursor);

		[DllImport ("libcamel.so.0")]
		extern static void camel_object_unref (IntPtr obj);

		private IntPtr index = IntPtr.Zero;

		public CamelIndex (string path)
		{
			this.index = camel_text_index_new (path, (int) OpenFlags.O_RDONLY);

			if (this.index == IntPtr.Zero)
				throw new ArgumentException ();
		}

		~CamelIndex ()
		{
			if (this.index != IntPtr.Zero)
				camel_object_unref (this.index);
		}

		public void Dispose ()
		{
			if (this.index != IntPtr.Zero)
				camel_object_unref (this.index);
			GC.SuppressFinalize (this);
		}

		private static string GetUid (IntPtr cursor)
		{
			IntPtr uid_ptr = camel_index_cursor_next (cursor);

			if (uid_ptr == IntPtr.Zero)
				return null;
			else
				return Marshal.PtrToStringAnsi (uid_ptr);
		}

		public Hashtable Match (IList words)
		{
			Hashtable matches = null;

			foreach (string word in words) {
				Hashtable word_matches = new Hashtable ();

				IntPtr cursor = camel_index_find (this.index, word);

				string uid;
				while ((uid = GetUid (cursor)) != null)
					word_matches [uid] = null;

				if (matches == null)
					matches = word_matches;
				else {
					foreach (string m in matches.Keys) {
						if (!word_matches.ContainsKey (m))
							matches.Remove (m);
					}
				}

				camel_object_unref (cursor);
			}

			return matches;
		}
	}

	internal class SummaryCrawler : Crawler {
		
		EvolutionMailDriver queryable;

		public SummaryCrawler (EvolutionMailDriver queryable, string fingerprint) : base (fingerprint)
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

		private static Logger log = Logger.Get ("mail");

		private EvolutionMailDriver driver;
		private FileInfo summaryInfo;
		private Camel.Summary summary;
		private IEnumerator summaryEnumerator;
		private string accountName, folderName;
		private bool getCachedContent;
		private Hashtable mapping;
		private string appDataName;
		private ArrayList deletedList;
		private int count, indexedCount;

		public EvolutionMailIndexableGenerator (EvolutionMailDriver driver, FileInfo summaryInfo)
		{
			this.driver = driver;
			this.summaryInfo = summaryInfo;
		}

		private bool Setup ()
		{
			if (summaryInfo.Name == "summary") {
				string dirName = summaryInfo.DirectoryName;

				int imapStartIdx = dirName.IndexOf (".evolution/mail/imap/") + 21;
				string imapStart = dirName.Substring (imapStartIdx);
				string imapName = imapStart.Substring (0, imapStart.IndexOf ('/'));

				GConf.Client gc = new GConf.Client ();
				ICollection accounts = (ICollection) gc.Get ("/apps/evolution/mail/accounts");

				foreach (string xml in accounts) {
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
					log.Info ("Unable to determine account name for {0}", imapName);
					return false;
				}
					
				this.folderName = driver.GetImapFolderName (new DirectoryInfo (dirName));
				this.getCachedContent = true;
			} else {
				this.accountName = "local@local";
				this.folderName = driver.GetLocalFolderName (this.summaryInfo);
				this.getCachedContent = false;
			}

			return true;
		}


		public Indexable GetNextIndexable ()
		{
			Indexable indexable = null;

			if (this.accountName == null) {
				if (!Setup ())
					return null;
			}

			if (this.summary == null)
				this.summary = Camel.Summary.load (this.summaryInfo.FullName);

			if (this.summaryEnumerator == null)
				this.summaryEnumerator = this.summary.GetEnumerator ();

			string appDataName;
			Stream statusStream;
			BinaryFormatter formatter;
				
			appDataName = "status-" + this.accountName + "-" + this.folderName.Replace ('/', '-');

			if (this.mapping == null) {
				try {
					statusStream = PathFinder.ReadAppData ("MailIndex", appDataName);
					formatter = new BinaryFormatter ();
					this.mapping = formatter.Deserialize (statusStream) as Hashtable;
					statusStream.Close ();
					log.Debug ("Successfully loaded previous crawled data from disk: {0}", appDataName);
				} catch {
					this.mapping = new Hashtable ();
				}

				this.deletedList = new ArrayList (this.mapping.Keys);
			}

			if (this.summaryEnumerator.MoveNext ()) {
				Camel.MessageInfo mi = this.summaryEnumerator.Current as Camel.MessageInfo;

				if ((this.count & 1500) == 0) {
					log.Debug ("{0}: indexed {1} messages ({2}/{3} {4:###.0}%)",
						   this.folderName, this.indexedCount, this.count,
						   this.summary.header.count,
						   100.0 * this.count / this.summary.header.count);
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

					indexable = EvolutionMailDriver.MailToIndexable (this.accountName, this.folderName,
											 mi, msgReader);

					if (msgStream != null)
						msgStream.Close ();

					this.mapping[mi.uid] = mi.flags;
					++indexedCount;
				} 

				this.deletedList.Remove (mi.uid);
			} else {
				statusStream = PathFinder.WriteAppData ("MailIndex", appDataName);
				formatter = new BinaryFormatter ();
				formatter.Serialize (statusStream, mapping);
				statusStream.Close ();
				log.Debug ("Wrote previously crawled data to disk: {0}", appDataName);
			}

			return indexable;
		}

	}

	// Kind of weird that Uri doesn't implement IComparable
	internal class UriComparer : IComparer {
		
		public int Compare (object x, object y)
		{
			Uri xUri = (Uri) x;
			Uri yUri = (Uri) y;

			if (xUri.Equals (yUri))
				return 0;

			return String.Compare (xUri.ToString (), yUri.ToString ());
		}
	}
				

	[QueryableFlavor (Name="Mail", Domain=QueryDomain.Local)]
	public class EvolutionMailDriver : LuceneQueryable {

		private static Logger log = Logger.Get ("mail");

		private SortedList watched = new SortedList ();
		private ArrayList indexes = new ArrayList ();
		private SummaryCrawler crawler;

		private object lockObj = new object ();
		private ArrayList AddedUris = new ArrayList ();
		private bool queryRunning = false;

		public EvolutionMailDriver () : base (Path.Combine (PathFinder.RootDir, "MailIndex"))
		{
		}

		public override void Start () 
		{
			base.Start ();

			string home = Environment.GetEnvironmentVariable ("HOME");
			string local_path = Path.Combine (home, ".evolution/mail/local");
			string imap_path = Path.Combine (home, ".evolution/mail/imap");

			// Get notification when an index or summary file changes
			Inotify.InotifyEvent += new InotifyHandler (OnInotifyEvent);
			Watch (local_path);

			this.crawler = new SummaryCrawler (this, this.Driver.Fingerprint);
			Shutdown.ShutdownEvent += OnShutdown;

			this.crawler.ScheduleCrawl (new DirectoryInfo (local_path), -1);
			this.crawler.ScheduleCrawl (new DirectoryInfo (imap_path), -1);
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
			this.indexes.Clear ();

			while (queue.Count > 0) {
				DirectoryInfo dir = queue.Dequeue () as DirectoryInfo;
				
				int wd = Inotify.Watch (dir.FullName,
							InotifyEventType.CreateSubdir
							| InotifyEventType.Modify
							| InotifyEventType.MovedTo);
				watched [wd] = dir.FullName;
				this.indexes.AddRange (Directory.GetFiles (dir.FullName, "*.ibex.index"));

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					queue.Enqueue (subdir);
			}
		}

		private void Ignore (string path)
		{
			Inotify.Ignore (path);
			watched.RemoveAt (watched.IndexOfValue (path));

			this.indexes.Clear ();
			foreach (string p in this.watched.Values)
				this.indexes.AddRange (Directory.GetFiles (p, "*.ibex.index"));
		}

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     InotifyEventType type,
					     int cookie)
		{
			if (subitem == "" || ! watched.Contains (wd))
				return;

			string fullPath = Path.Combine (path, subitem);

			switch (type) {
				
			case InotifyEventType.CreateSubdir:
				Watch (fullPath);
				break;

			case InotifyEventType.DeleteSubdir:
				Ignore (fullPath);
				break;

			case InotifyEventType.Modify:
			case InotifyEventType.MovedTo:
				if (Path.GetExtension (fullPath) == ".ev-summary" || subitem == "summary") {
					log.Info ("reindexing updated summary: {0}", fullPath);
					this.IndexSummary (new FileInfo (fullPath));
				} else if (Path.GetExtension (fullPath) == ".ibex.index") {
					// FIXME: If it's an index that's been updated, alter the query somehow.
				}

				break;
			}
		}

		public string Name {
			get { return "EvolutionMail"; }
		}

		public string GetLocalFolderName (FileInfo fileInfo)
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

		public string GetImapFolderName (DirectoryInfo dirInfo)
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

		private ArrayList CamelIndexQuery (QueryBody body)
		{
			ArrayList hits = new ArrayList ();

			log.Debug ("### indexes: {0}", this.indexes);

			foreach (string idx_path in this.indexes) {
				// gets rid of the ".index" from the file.  camel expects it to just end in ".ibex"
				string path = Path.ChangeExtension (idx_path, null);

				CamelIndex index;

				try {
					index = new CamelIndex (path);
				} catch (DllNotFoundException e) {
					log.Info ("Couldn't load libcamel.so.  You probably need to set $LD_LIBRARY_PATH");
					return null;
				} catch {
					// If an index is invalid, just skip it.
					continue;
				}

				log.Debug ("### Trying to match in {0}: {1}", path, body.Text);
				Hashtable matches = index.Match (body.Text);
				log.Debug ("### Returned {0} matches", matches.Count);
				index.Dispose ();
				log.Debug ("### Disposed of the index");

				if (matches == null || matches.Count == 0)
					continue;

				string summaryPath = Path.ChangeExtension (path, "ev-summary");

				if (!File.Exists (summaryPath))
					continue;

				Camel.Summary summary = Camel.Summary.load (summaryPath);

				foreach (Camel.MessageInfo mi in summary) {
					if (!matches.ContainsKey (mi.uid))
						continue;

					string folderName = GetLocalFolderName (new FileInfo (path));

					Hit hit = new Hit ();
					hit.Uri = EmailUri ("local@local", folderName, mi.uid);
					hit.Type = "MailMessage"; // Maybe MailMessage?
					hit.MimeType = "text/plain";
					hit.Source = "EvolutionMail";
					hit.ScoreRaw = 1.0F;

					// These should map to the same properties in MailToIndexable ()
					hit ["dc:title"] = mi.subject;

					hit ["fixme:folder"]   = folderName;
					hit ["fixme:subject"]  = mi.subject;
					hit ["fixme:to"]       = mi.to;
					hit ["fixme:from"]     = mi.from;
					hit ["fixme:cc"]       = mi.cc;
					hit ["fixme:received"] = StringFu.DateTimeToString (mi.received);
					hit ["fixme:sentdate"] = StringFu.DateTimeToString (mi.sent);
					hit ["fixme:mlist"]    = mi.mlist;
					hit ["fixme:flags"]    = mi.flags.ToString ();

					if (folderName == "Sent")
						hit ["fixme:isSent"] = "true";

					if (mi.IsAnswered)
						hit ["fixme:isAnswered"] = "true";

					if (mi.IsDeleted)
						hit ["fixme:isDeleted"] = "true";

					if (mi.IsDraft)
						hit ["fixme:isDraft"] = "true";

					if (mi.IsFlagged)
						hit ["fixme:isFlagged"] = "true";

					if (mi.IsSeen)
						hit ["fixme:isSeen"] = "true";

					if (mi.HasAttachments)
						hit ["fixme:hasAttachments"] = "true";

					if (mi.IsAnsweredAll)
						hit ["fixme:isAnsweredAll"] = "true";

					hits.Add (hit);
				}
			}

			log.Debug ("### Returning {0} hits", hits.Count);

			return hits;
		}

		public override void DoQuery (QueryBody body,
					      IQueryResult result,
					      IQueryableChangeData changeData)
		{
			// First, chain up to the Lucene index
			base.DoQuery (body, result, changeData);

			LuceneQueryableChangeData lqcd = (LuceneQueryableChangeData) changeData;
			ArrayList hits = new ArrayList ();
			ArrayList addedUris = null;

			if (lqcd == null) {
				hits = CamelIndexQuery (body);
			} else if (lqcd.UriDeleted != null) {
				Uri[] subtracted = new Uri[1];
				subtracted[0] = lqcd.UriDeleted;
				result.Subtract (subtracted);
			} else if (lqcd.UriAdded != null) {
				bool runQuery = false;
 
				log.Debug ("*** {0}: Acquring lockObj lock", Thread.CurrentThread.Name);
				lock (this.lockObj) {
					if (! this.AddedUris.Contains (lqcd.UriAdded))
						this.AddedUris.Add (lqcd.UriAdded);

					if (this.queryRunning) {
						log.Debug ("*** {0}: Query is already running",
								   Thread.CurrentThread.Name);
					} else {
						log.Debug ("*** {0}: We're going to run the query",
								   Thread.CurrentThread.Name);
						this.queryRunning = true;
						runQuery = true;
					}
				}

				if (runQuery) {
					lock (this.lockObj) {
						addedUris = (ArrayList) this.AddedUris.Clone ();
						this.AddedUris.Clear ();

						log.Debug ("*** {0}: this.AddedUris has {1} elements, addedUris has {2}",
								   Thread.CurrentThread.Name,
								   this.AddedUris.Count, addedUris.Count);
					}

					log.Debug ("*** {0}: Starting query", Thread.CurrentThread.Name);
					try {
						hits = CamelIndexQuery (body);
					} catch (Exception e) {
						log.Debug ("*** Caught an exception!");
					} finally {
						log.Debug ("*** {0}: Query finished",
								   Thread.CurrentThread.Name);

						lock (this.lockObj) {
							log.Debug("*** {0}: Finished running query",
									  Thread.CurrentThread.Name);
							this.queryRunning = false;
						}
					}
				}
			}

			if (hits != null && addedUris != null) {
				ArrayList filteredHits = new ArrayList ();
				UriComparer uriComparer = new UriComparer ();

				addedUris.Sort (uriComparer);

				foreach (Hit hit in hits) {
					if (addedUris.BinarySearch (hit.Uri, uriComparer) >= 0) {
						filteredHits.Add (hit);
						break;
					}
				}

				hits = filteredHits;
			}

			if (hits != null)
				result.Add (hits);
                }

		public void IndexSummary (FileInfo summaryInfo)
		{
			EvolutionMailIndexableGenerator generator = new EvolutionMailIndexableGenerator (this, summaryInfo);
			Driver.ScheduleAdd (generator);
		}

		private static Uri EmailUri (string accountName, string folderName, string uid)
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
			indexable.AddProperty (Property.NewDate    ("fixme:received", messageInfo.received));
                        indexable.AddProperty (Property.NewDate    ("fixme:sentdate", messageInfo.sent));
                        indexable.AddProperty (Property.NewKeyword ("fixme:mlist",    messageInfo.mlist));
                        indexable.AddProperty (Property.NewKeyword ("fixme:flags",    messageInfo.flags));

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
