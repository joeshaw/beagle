
//
// EvolutionMailIndexableGenerator.cs
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
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml;

using Beagle.Util;
using Camel = Beagle.Util.Camel;
using GConf;
using Mono.Posix;

namespace Beagle.Daemon {

	public abstract class EvolutionMailIndexableGenerator : IIndexableGenerator {

		private static bool gmime_initialized = false;

		private EvolutionMailQueryable queryable;

		protected string account_name, folder_name;
		protected int count, indexed_count;

		protected EvolutionMailIndexableGenerator (EvolutionMailQueryable queryable)
		{
			this.queryable = queryable;
		}

		protected abstract string GetFolderName (FileSystemInfo info);
		protected abstract bool Setup ();
		protected abstract FileInfo CrawlFile { get; }
		public abstract string GetTarget ();
		public abstract bool HasNextIndexable ();
		public abstract Indexable GetNextIndexable ();
		public abstract void Checkpoint ();

		protected EvolutionMailQueryable Queryable {
			get { return this.queryable; }
		}

		protected static void InitializeGMime ()
		{
			if (!gmime_initialized) {
				GMime.Global.Init ();
				gmime_initialized = true;
			}
		}

		protected bool IsSpamFolder (string name)
		{
			if (name.ToLower () == "spam" || name.ToLower () == "junk")
				return true;
			else
				return false;
		}
					
		protected bool CrawlNeeded ()
		{
			string timeStr;

			try {
				timeStr = ExtendedAttribute.Get (this.CrawlFile.FullName, "LastCrawl");
			} catch {
				EvolutionMailQueryable.log.Debug ("Unable to get last crawl time on {0}",
								  this.CrawlFile.FullName);
				return true;
			}

			DateTime lastCrawl = StringFu.StringToDateTime (timeStr);

			if (this.CrawlFile.LastWriteTime > lastCrawl)
				return true;
			else
				return false;
		}

		protected void CrawlFinished ()
		{
			try {
				ExtendedAttribute.Set (this.CrawlFile.FullName, "LastCrawl",
						       StringFu.DateTimeToString (DateTime.Now));
			} catch {
				EvolutionMailQueryable.log.Debug ("Unable to set last crawl time on {0}",
								  this.CrawlFile.FullName);
			}
		}

		protected class PartHandler {
			private MultiReader reader = null;
			private int depth = 0;

			public void OnEachPart (GMime.Object part)
			{
				if (this.reader == null)
					this.reader = new MultiReader ();

				for (int i = 0; i < depth; i++)
					Console.Write ("  ");
				Console.WriteLine ("Content-Type: {0}", part.ContentType);

				++depth;

				if (part is GMime.MessagePart) {
					GMime.MessagePart msg_part = (GMime.MessagePart) part;

					msg_part.Message.ForeachPart (new GMime.PartFunc (this.OnEachPart));
				} else if (part is GMime.Multipart) {
					GMime.Multipart multipart = (GMime.Multipart) part;

					multipart.ForeachPart (new GMime.PartFunc (this.OnEachPart));
				} else {
					MemoryStream stream = new MemoryStream (part.GetData ());
					StreamReader reader = new StreamReader (stream);
					this.reader.Add (reader);
				}

				--depth;
			}

			public static MultiReader GetReader (GMime.Message message)
			{
				// FIXME: This will work for now, but really we need
				// to make it so that we can index multiple things
				// from a single indexable.
				PartHandler handler = new PartHandler ();
				//message.ForeachPart (new GMime.PartFunc (handler.OnEachPart));
				handler.OnEachPart (message.MimePart);
				return handler.reader;
			}

		}

		protected static Stream ReadAppData (string name)
		{
			string path = Path.Combine (Path.Combine (PathFinder.RootDir, "MailIndex"), name);
			return new FileStream (path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		protected static string ReadAppDataLine (string name)
		{
			Stream stream;

			try {
				stream = ReadAppData (name);
			} catch (FileNotFoundException) {
				return null;
			}

			StreamReader sr = new StreamReader (stream);
			string line = sr.ReadLine ();
			sr.Close ();
			return line;
		}

		protected static Stream WriteAppData (string name)
		{
			string path = Path.Combine (Path.Combine (PathFinder.RootDir, "MailIndex"), name);
			return new FileStream (path, System.IO.FileMode.Create, FileAccess.Write, FileShare.None);
		}

		protected static void WriteAppDataLine (string name, string line)
		{
			if (line == null) {
				string path = Path.Combine (Path.Combine (PathFinder.RootDir, "MailIndex"), name);

				if (File.Exists (path))
					File.Delete (path);

				return;
			}

			StreamWriter sw = new StreamWriter (WriteAppData (name));
			sw.WriteLine (line);
			sw.Close ();
		}

		public string StatusName {
			get { return this.CrawlFile.FullName; }
		}

		public override bool Equals (object o)
		{
			EvolutionMailIndexableGenerator generator = o as EvolutionMailIndexableGenerator;

			if (generator == null)
				return false;

			if (Object.ReferenceEquals (this, generator))
				return true;

			if (this.CrawlFile.FullName == generator.CrawlFile.FullName)
				return true;
			else
				return false;
		}
	}

	public class EvolutionMailIndexableGeneratorMbox : EvolutionMailIndexableGenerator {
		private FileInfo mbox_info;
		private int mbox_fd = -1;
		private GMime.StreamFs mbox_stream;
		private GMime.Parser mbox_parser;

		public EvolutionMailIndexableGeneratorMbox (EvolutionMailQueryable queryable, FileInfo mbox_info) : base (queryable)
		{
			this.mbox_info = mbox_info;
		}

		protected override string GetFolderName (FileSystemInfo info)
		{
			FileInfo file_info = (FileInfo) info;
			DirectoryInfo di;
			string folder_name = "";

			di = file_info.Directory;
			while (di != null) {
				// Evo uses ".sbd" as the extension on a folder
				if (di.Extension == ".sbd")
					folder_name = Path.Combine (folder_name, Path.GetFileNameWithoutExtension (di.Name));
				else
					break;

				di = di.Parent;
			}

			return Path.Combine (folder_name, Path.GetFileNameWithoutExtension (file_info.Name));
		}

		protected override bool Setup ()
		{
			this.account_name = "local@local";
			this.folder_name = this.GetFolderName (this.mbox_info);

			if (this.IsSpamFolder (this.folder_name))
				return false;
			
			return true;
		}

		private long MboxLastOffset {
			get {
				string offset_str = ReadAppDataLine ("offset-" + this.folder_name.Replace ('/', '-'));
				long offset = Convert.ToInt64 (offset_str);

				Console.WriteLine ("offset is {0}", offset);
				return offset;
			}

			set {
				WriteAppDataLine ("offset-" + this.folder_name.Replace ('/', '-'), value.ToString ());
			}
		}

		public override bool HasNextIndexable ()
		{
			if (this.account_name == null) {
				if (!Setup ())
					return false;
			}

			if (this.mbox_fd < 0) {
				Console.WriteLine ("opening mbox {0}", this.mbox_info.Name);
				this.mbox_fd = Syscall.open (this.mbox_info.FullName, OpenFlags.O_RDONLY);
				InitializeGMime ();
				this.mbox_stream = new GMime.StreamFs (this.mbox_fd);
				this.mbox_stream.Seek ((int) this.MboxLastOffset);
				this.mbox_parser = new GMime.Parser (this.mbox_stream);
				this.mbox_parser.ScanFrom = true;
			}

			if (this.mbox_parser.Eos ()) {
				long offset = this.mbox_parser.FromOffset;

				this.mbox_stream.Close ();

				this.mbox_fd = -1;
				this.mbox_stream = null;
				this.mbox_parser = null;
				
				EvolutionMailQueryable.log.Debug ("{0}: Finished indexing {1} messages",
								  this.folder_name, this.indexed_count);

				this.MboxLastOffset = offset;
				this.CrawlFinished ();

				return false;
			} else
				return true;
		}

		public override Indexable GetNextIndexable ()
		{
			GMime.Message message = this.mbox_parser.ConstructMessage ();

			++this.count;

			// Work around what I think is a bug in GMime: If you
			// have a zero-byte file or seek to the end of a
			// file, parser.Eos () will return true until it
			// actually tries to read something off the wire.
			// Since parser.ConstructMessage() always returns a
			// message (which may also be a bug), we'll often get
			// one empty message which we need to deal with here.
			//
			// Check if its empty by seeing if the Headers
			// property is null or empty.
			if (message.Headers == null || message.Headers == "")
				return null;

			string x_evolution = message.GetHeader ("X-Evolution");
			if (x_evolution == null || x_evolution == "") {
				EvolutionMailQueryable.log.Info ("{0}: Message at offset {1} has no X-Evolution header!",
								 this.folder_name, this.mbox_parser.FromOffset);
				return null;
			}

			string uid_str = x_evolution.Substring (0, x_evolution.IndexOf ('-'));
			string uid = Convert.ToUInt32 (uid_str, 16).ToString (); // ugh.

			Indexable indexable = this.GMimeMessageToIndexable (uid, message);
			++this.indexed_count;

			return indexable;
		}

		private Indexable GMimeMessageToIndexable (string uid, GMime.Message message)
		{
			System.Uri uri = EvolutionMailQueryable.EmailUri (this.account_name, this.folder_name, uid);
			Indexable indexable = new Indexable (uri);

			indexable.Timestamp = message.Date;
			indexable.Type = "MailMessage";
			indexable.MimeType = "text/plain"; // FIXME

			indexable.AddProperty (Property.NewKeyword ("dc:title",       message.Subject));

                        indexable.AddProperty (Property.NewKeyword ("fixme:folder",   this.folder_name));
			indexable.AddProperty (Property.NewKeyword ("fixme:subject",  message.Subject));
                        indexable.AddProperty (Property.NewKeyword ("fixme:to",       message.GetRecipientsAsString (GMime.Message.RecipientType.To)));
			indexable.AddProperty (Property.NewKeyword ("fixme:from",     message.Sender));
			indexable.AddProperty (Property.NewKeyword ("fixme:cc",       message.GetRecipientsAsString (GMime.Message.RecipientType.Cc)));

			if (this.folder_name == "Sent")
				indexable.AddProperty (Property.NewFlag ("fixme:isSent"));

			if (message.MimePart is GMime.Multipart || message.MimePart is GMime.MessagePart)
				indexable.AddProperty (Property.NewFlag ("fixme:hasAttachments"));

			string list_id = message.GetHeader ("List-Id");

			if (list_id != null) {
				// FIXME: Might need some additional parsing.
				indexable.AddProperty (Property.NewKeyword ("fixme:mlist", list_id));
			}

#if false
			// FIXME - XXX
                        indexable.AddProperty (Property.NewKeyword ("fixme:flags",    messageInfo.flags));

			if (messageInfo.received != DateTime.MinValue)
				indexable.AddProperty (Property.NewDate ("fixme:received", messageInfo.received));

			if (messageInfo.sent != DateTime.MinValue)
				indexable.AddProperty (Property.NewDate ("fixme:sentdate", messageInfo.sent));

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

			if (messageInfo.IsAnsweredAll)
				indexable.AddProperty (Property.NewFlag ("fixme:isAnsweredAll"));
#endif
	                indexable.SetTextReader (PartHandler.GetReader (message));

			return indexable;
		}

		public override void Checkpoint ()
		{
			EvolutionMailQueryable.log.Debug ("{0}: indexed {1} messages",
							  this.folder_name, this.indexed_count);

			this.MboxLastOffset = this.mbox_parser.FromOffset;
		}

		public override string GetTarget ()
		{
			return "mbox-file:" + mbox_info.FullName;
		}

		protected override FileInfo CrawlFile {
			get { return this.mbox_info; }
		}
	}

	public class EvolutionMailIndexableGeneratorImap : EvolutionMailIndexableGenerator {
		private static GConf.Client gconf_client = null;

		private FileInfo summary_info;
		private Camel.Summary summary;
		private IEnumerator summary_enumerator;
		private ICollection accounts;
		private string folder_cache_name;
		private Hashtable mapping;
		private ArrayList deleted_list;

		public EvolutionMailIndexableGeneratorImap (EvolutionMailQueryable queryable, FileInfo summary_info) : base (queryable)
		{
			this.summary_info = summary_info;
		}

		protected override string GetFolderName (FileSystemInfo info)
		{
			DirectoryInfo dir_info = (DirectoryInfo) info;
			string folder_name = "";

			while (dir_info != null) {
				folder_name = Path.Combine (dir_info.Name, folder_name);

				dir_info = dir_info.Parent;

				if (dir_info.Name != "subfolders")
					break;
				else
					dir_info = dir_info.Parent;
			}

			return folder_name;
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

		protected override bool Setup ()
		{
			string dir_name = summary_info.DirectoryName;

			int imap_start_idx = dir_name.IndexOf (".evolution/mail/imap/") + 21;
			string imap_start = dir_name.Substring (imap_start_idx);
			string imap_name = imap_start.Substring (0, imap_start.IndexOf ('/'));

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
				int lastIdx = imap_name.LastIndexOf ('@');
				if (imap_name.IndexOf ('@') != lastIdx) {
					string toEscape = imap_name.Substring (0, lastIdx);
					imap_name = toEscape.Replace ("@", "%40") + imap_name.Substring (lastIdx);
				}

				if (imap_url.InnerText.StartsWith ("imap://" + imap_name)) {
					this.account_name = uid;
					break;
				}
			}

			if (account_name == null) {
				EvolutionMailQueryable.log.Info ("Unable to determine account name for {0}", imap_name);
				return false;
			}

			// Need to check the directory on disk to see if it's a junk/spam folder,
			// since the folder name will be "foo/spam" and not match the check below.
			DirectoryInfo dir_info = new DirectoryInfo (dir_name);
			if (this.IsSpamFolder (dir_info.Name))
				return false;
					
			this.folder_name = GetFolderName (new DirectoryInfo (dir_name));

			return true;
		}

		private string FolderCacheName {
			get {
				if (this.folder_cache_name == null)
					this.folder_cache_name = "status-" + this.account_name + "-" + this.folder_name.Replace ('/', '-');

				return this.folder_cache_name;
			}
		}

		private bool LoadCache ()
		{
			Stream cacheStream;
			BinaryFormatter formatter;

			try {
				cacheStream = ReadAppData (this.FolderCacheName);
				formatter = new BinaryFormatter ();
				this.mapping = formatter.Deserialize (cacheStream) as Hashtable;
				cacheStream.Close ();
				EvolutionMailQueryable.log.Debug ("Successfully loaded previous crawled data from disk: {0}", this.FolderCacheName);

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
			
			cacheStream = WriteAppData (this.FolderCacheName);
			formatter = new BinaryFormatter ();
			formatter.Serialize (cacheStream, mapping);
			cacheStream.Close ();
		}

		public override bool HasNextIndexable ()
		{
			if (this.account_name == null) {
				if (!Setup ())
					return false;
			}

			if (this.mapping == null) {
				bool cache_loaded = this.LoadCache ();

				this.deleted_list = new ArrayList (this.mapping.Keys);

				// Check to see if we even need to bother walking the summary
				if (cache_loaded && ! CrawlNeeded ()) {
					EvolutionMailQueryable.log.Debug ("{0}: summary has not been updated; crawl unncessary", this.folder_name);
					return false;
				}
			}

			if (this.summary == null)
				this.summary = Camel.Summary.load (this.summary_info.FullName);

			if (this.summary_enumerator == null)
				this.summary_enumerator = this.summary.GetEnumerator ();

			if (this.summary_enumerator.MoveNext ())
				return true;

			// FIXME: This is kind of a hack, but it's the only way with the IndexableGenerator
			// to handle our removals.
			foreach (string uid in this.deleted_list) {
				Uri uri = EvolutionMailQueryable.EmailUri (this.account_name, this.folder_name, uid);
				Scheduler.Task task = this.Queryable.NewRemoveTask (uri);
				task.Priority = Scheduler.Priority.Immediate;
				this.Queryable.ThisScheduler.Add (task);
			}

			EvolutionMailQueryable.log.Debug ("{0}: Finished indexing {1} ({2}/{3} {4:###.0}%)",
							  this.folder_name, this.indexed_count, this.count,
							  this.summary.header.count,
							  100.0 * this.count / this.summary.header.count);

			this.SaveCache ();
			this.CrawlFinished ();

			return false;
		}

		private MultiReader GetMessageData (string file)
		{
			int fd;

			fd = Syscall.open (file, OpenFlags.O_RDONLY);

			InitializeGMime ();

			GMime.StreamFs stream = new GMime.StreamFs (fd);
			GMime.Parser parser = new GMime.Parser (stream);
			GMime.Message message = parser.ConstructMessage ();

			MultiReader reader = PartHandler.GetReader (message);

			stream.Close ();

			return reader;
		}

		public override Indexable GetNextIndexable ()
		{
			Indexable indexable = null;

			Camel.MessageInfo mi = (Camel.MessageInfo) this.summary_enumerator.Current;

			++this.count;

			if (this.mapping[mi.uid] == null || (uint) mapping[mi.uid] != mi.flags) {
				TextReader msgReader = null;
				string msg_file = Path.Combine (summary_info.DirectoryName, mi.uid + ".");

				// FIXME - XXX
				if (File.Exists (msg_file))
					msgReader = GetMessageData (msg_file);

				indexable = this.CamelMessageToIndexable (mi, msgReader);

				this.mapping[mi.uid] = mi.flags;
				++this.indexed_count;
			} 

			this.deleted_list.Remove (mi.uid);

			return indexable;
		}

		private Indexable CamelMessageToIndexable (Camel.MessageInfo messageInfo, TextReader msgReader)
		{
			System.Uri uri = EvolutionMailQueryable.EmailUri (this.account_name, this.folder_name, messageInfo.uid);
			Indexable indexable = new Indexable (uri);

			indexable.Timestamp = messageInfo.Date;
			indexable.Type = "MailMessage";
			indexable.MimeType = "text/plain"; // FIXME

			indexable.AddProperty (Property.NewKeyword ("dc:title", messageInfo.subject));

                        indexable.AddProperty (Property.NewKeyword ("fixme:folder",   this.folder_name));
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

			if (this.folder_name == "Sent")
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

		public override void Checkpoint ()
		{
			EvolutionMailQueryable.log.Debug ("{0}: indexed {1} messages ({2}/{3} {4:###.0}%)",
							  this.folder_name, this.indexed_count, this.count,
							  this.summary.header.count,
							  100.0 * this.count / this.summary.header.count);

			this.SaveCache ();
		}

		public override string GetTarget ()
		{
			return "summary-file:" + summary_info.FullName;
		}

		protected override FileInfo CrawlFile {
			get { return this.summary_info; }
		}
	}
}