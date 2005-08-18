
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
using Beagle.Daemon;

using Camel = Beagle.Util.Camel;
using Mono.Posix;

namespace Beagle.Daemon.EvolutionMailDriver {

	public abstract class EvolutionMailIndexableGenerator : IIndexableGenerator {

		private static bool gmime_initialized = false;

		private static ArrayList excludes = new ArrayList ();

		static EvolutionMailIndexableGenerator () {
			foreach (ExcludeItem exclude in Conf.Indexing.Excludes)
				if (exclude.Type == ExcludeType.MailFolder)
					excludes.Add (exclude);
		}

		protected EvolutionMailQueryable queryable;

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

		protected bool IgnoreFolder (string path)
		{
			// FIXME: Use System.IO.Path
			foreach (ExcludeItem exclude in excludes) {
				if (exclude.IsMatch (path))
					return true;
			}
			return false;
		}
					
		protected void CrawlFinished ()
		{
			// FIXME: This is a little sketchy
			this.queryable.FileAttributesStore.AttachLastWriteTime (this.CrawlFile.FullName, DateTime.Now);
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

		public override int GetHashCode ()
		{
			return this.CrawlFile.FullName.GetHashCode ();
		}
	}

	public class EvolutionMailIndexableGeneratorMbox : EvolutionMailIndexableGenerator {
		private FileInfo mbox_info;
		private int mbox_fd = -1;
		private GMime.StreamFs mbox_stream;
		private GMime.Parser mbox_parser;
		private long file_size;

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
					folder_name = Path.Combine (Path.GetFileNameWithoutExtension (di.Name), folder_name);
				else
					break;

				di = di.Parent;
			}

			return Path.Combine (folder_name, file_info.Name);
		}

		protected override bool Setup ()
		{
			this.account_name = "local@local";
			this.folder_name = this.GetFolderName (this.mbox_info);

			if (this.IsSpamFolder (this.folder_name))
				return false;

			if (this.IgnoreFolder (this.mbox_info.FullName))
				return false;

			return true;
		}

		private long MboxLastOffset {
			get {
				string offset_str = this.queryable.ReadDataLine ("offset-" + this.folder_name.Replace ('/', '-'));
				long offset = Convert.ToInt64 (offset_str);

				Logger.Log.Debug ("mbox {0} offset is {1}", this.mbox_info.Name, offset);
				return offset;
			}

			set {
				this.queryable.WriteDataLine ("offset-" + this.folder_name.Replace ('/', '-'), value.ToString ());
			}
		}

		public override bool HasNextIndexable ()
		{
			if (this.account_name == null) {
				if (!Setup ())
					return false;
			}

			if (this.mbox_fd < 0) {
				Logger.Log.Debug ("opening mbox {0}", this.mbox_info.Name);

				try {
					InitializeGMime ();
				} catch (Exception e) {
					Logger.Log.Warn ("Caught exception trying to initalize gmime:");
					Logger.Log.Warn (e);
					return false;
				}

				this.mbox_fd = Syscall.open (this.mbox_info.FullName, OpenFlags.O_RDONLY);
				this.mbox_stream = new GMime.StreamFs (this.mbox_fd);
				this.mbox_stream.Seek ((int) this.MboxLastOffset);
				this.mbox_parser = new GMime.Parser (this.mbox_stream);
				this.mbox_parser.ScanFrom = true;

				FileInfo info = new FileInfo (this.mbox_info.FullName);
				this.file_size = info.Length;
			}

			if (this.mbox_parser.Eos ()) {
				long offset = this.mbox_parser.FromOffset;

				this.mbox_stream.Close ();

				this.mbox_fd = -1;
				this.mbox_stream.Dispose ();
				this.mbox_stream = null;
				this.mbox_parser.Dispose ();
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
			using (GMime.Message message = this.mbox_parser.ConstructMessage ()) {
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
				
				int separator_idx = x_evolution.IndexOf ('-');

				string uid_str = x_evolution.Substring (0, separator_idx);
				string uid = Convert.ToUInt32 (uid_str, 16).ToString (); // ugh.
				uint flags = Convert.ToUInt32 (x_evolution.Substring (separator_idx + 1), 16);

				Indexable indexable = this.GMimeMessageToIndexable (uid, message, flags);
				
				if (indexable == null)
					return null;

				++this.indexed_count;

				return indexable;
			}
		}

		private static bool CheckFlags (uint flags, Camel.CamelFlags test)
		{
			return (flags & (uint) test) == (uint) test;
		}

		private Indexable GMimeMessageToIndexable (string uid, GMime.Message message, uint flags)
		{
			// Don't index messages flagged as junk
			if (CheckFlags (flags, Camel.CamelFlags.Junk))
				return null;

			System.Uri uri = EvolutionMailQueryable.EmailUri (this.account_name, this.folder_name, uid);
			Indexable indexable = new Indexable (uri);

			indexable.Timestamp = message.Date;
			indexable.Type = "MailMessage";
			indexable.MimeType = "message/rfc822";
			indexable.CacheContent = false;

			indexable.AddProperty (Property.NewKeyword ("fixme:client", "evolution"));
			indexable.AddProperty (Property.NewKeyword ("fixme:account", "Local"));
                        indexable.AddProperty (Property.NewKeyword ("fixme:folder", this.folder_name));

			GMime.InternetAddressList addrs;

			addrs = message.GetRecipients (GMime.Message.RecipientType.To);
			foreach (GMime.InternetAddress ia in addrs) {
				if (this.folder_name == "Sent")
					indexable.AddProperty (Property.NewKeyword ("fixme:sentTo", ia.Addr));
			}
			addrs.Dispose ();

			addrs = message.GetRecipients (GMime.Message.RecipientType.Cc);
			foreach (GMime.InternetAddress ia in addrs) {
				if (this.folder_name == "Sent")
					indexable.AddProperty (Property.NewKeyword ("fixme:sentTo", ia.Addr));
			}
			addrs.Dispose ();

			addrs = GMime.InternetAddressList.ParseString (GMime.Utils.HeaderDecodePhrase (message.Sender));
			foreach (GMime.InternetAddress ia in addrs) {
				if (this.folder_name != "Sent")
					indexable.AddProperty (Property.NewKeyword ("fixme:gotFrom", ia.Addr));
			}
			addrs.Dispose ();

			if (this.folder_name == "Sent")
				indexable.AddProperty (Property.NewFlag ("fixme:isSent"));

			if (this.folder_name == "Sent")
				indexable.AddProperty (Property.NewDate ("fixme:sentdate", message.Date));
			else
				indexable.AddProperty (Property.NewDate ("fixme:received", message.Date));

			indexable.AddProperty (Property.NewKeyword ("fixme:flags", flags));

			if (CheckFlags (flags, Camel.CamelFlags.Answered))
				indexable.AddProperty (Property.NewFlag ("fixme:isAnswered"));

			if (CheckFlags (flags, Camel.CamelFlags.Deleted))
				indexable.AddProperty (Property.NewFlag ("fixme:isDeleted"));

			if (CheckFlags (flags, Camel.CamelFlags.Draft))
				indexable.AddProperty (Property.NewFlag ("fixme:isDraft"));

			if (CheckFlags (flags, Camel.CamelFlags.Flagged))
				indexable.AddProperty (Property.NewFlag ("fixme:isFlagged"));

			if (CheckFlags (flags, Camel.CamelFlags.Seen))
				indexable.AddProperty (Property.NewFlag ("fixme:isSeen"));

			if (CheckFlags (flags, Camel.CamelFlags.AnsweredAll))
				indexable.AddProperty (Property.NewFlag ("fixme:isAnsweredAll"));

			MemoryStream stream = new MemoryStream ();
			message.WriteToStream (stream);
			stream.Seek (0, SeekOrigin.Begin);
			indexable.SetBinaryStream (stream);

			return indexable;
		}

		public override void Checkpoint ()
		{
			long offset = -1;

			if (this.mbox_parser != null)
				this.MboxLastOffset = offset = this.mbox_parser.FromOffset;

			EvolutionMailQueryable.log.Debug ("{0}: indexed {1} messages ({2}/{3} bytes {4:###.0}%)",
							  this.folder_name, this.indexed_count,
							  offset, this.file_size,
							  100.0 * offset / this.file_size);
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
		private enum ImapBackendType {
			Imap,
			Imap4
		};

		private FileInfo summary_info;
		private string imap_name;
		private ImapBackendType backend_type;
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

		protected override bool Setup ()
		{
			string dir_name = summary_info.DirectoryName;
			int imap_start_idx;

			int idx = dir_name.IndexOf (".evolution/mail/imap4/");

			if (idx >= 0) {
				this.backend_type = ImapBackendType.Imap4;
				imap_start_idx = idx + 22;
			} else {
				this.backend_type = ImapBackendType.Imap;
				imap_start_idx = dir_name.IndexOf (".evolution/mail/imap/") + 21;
			}

			string imap_start = dir_name.Substring (imap_start_idx);
			this.imap_name = imap_start.Substring (0, imap_start.IndexOf ('/'));

			try {
				this.accounts = (ICollection) GConfThreadHelper.Get ("/apps/evolution/mail/accounts");
			} catch (Exception ex) {
				EvolutionMailQueryable.log.Warn ("Caught exception in Setup(): " + ex.Message);
				EvolutionMailQueryable.log.Warn ("There are no configured evolution accounts, ignoring {0}", this.imap_name);
				return false;
			}

			// This should only happen if we shut down while waiting for the GConf results to come back.
			if (this.accounts == null)
				return false;

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

				XmlNode imap_url_node = xmlDoc.SelectSingleNode ("//source/url");

				if (imap_url_node == null)
					continue;

				string imap_url = imap_url_node.InnerText;
				// If there is a semicolon in the username part of the URL, it
				// indicates that there's an auth scheme there.  We don't care
				// about that, so remove it.
				int user_end = imap_url.IndexOf ('@');
				int semicolon = imap_url.IndexOf (';', 0, user_end + 1);

				if (semicolon != -1)
					imap_url = imap_url.Substring (0, semicolon) + imap_url.Substring (user_end);

				// Escape out additional @s in the name.  I hate the class libs so much.
				int lastIdx = this.imap_name.LastIndexOf ('@');
				if (this.imap_name.IndexOf ('@') != lastIdx) {
					string toEscape = this.imap_name.Substring (0, lastIdx);
					this.imap_name = toEscape.Replace ("@", "%40") + this.imap_name.Substring (lastIdx);
				}

				string backend_url_prefix;
				if (this.backend_type == ImapBackendType.Imap)
					backend_url_prefix = "imap";
				else
					backend_url_prefix = "imap4";

				if (imap_url.StartsWith (backend_url_prefix + "://" + this.imap_name + "/")) {
					this.account_name = uid;
					break;
				}
			}

			if (account_name == null) {
				EvolutionMailQueryable.log.Info ("Unable to determine account name for {0}", this.imap_name);
				return false;
			}

			// Need to check the directory on disk to see if it's a junk/spam folder,
			// since the folder name will be "foo/spam" and not match the check below.
			DirectoryInfo dir_info = new DirectoryInfo (dir_name);
			if (this.IsSpamFolder (dir_info.Name))
				return false;

			// Check if the folder is listed in the configuration as to be excluded from indexing
			if (this.IgnoreFolder (dir_info.FullName))
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
				cacheStream = this.queryable.ReadDataStream (this.FolderCacheName);
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
			
			cacheStream = this.queryable.WriteDataStream (this.FolderCacheName);
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
				this.deleted_list.Sort ();
				Logger.Log.Debug ("Deleted list starting at {0} for {1}", this.deleted_list.Count, this.folder_name);

				// Check to see if we even need to bother walking the summary
				if (cache_loaded && this.queryable.FileAttributesStore.IsUpToDate (this.CrawlFile.FullName)) {
					EvolutionMailQueryable.log.Debug ("{0}: summary has not been updated; crawl unncessary", this.folder_name);
					return false;
				}
			}

			if (this.summary == null) {
				try {
					if (this.backend_type == ImapBackendType.Imap)
						this.summary = Camel.Summary.LoadImapSummary (this.summary_info.FullName);
					else
						this.summary = Camel.Summary.LoadImap4Summary (this.summary_info.FullName);
				} catch (Exception e) {
					EvolutionMailQueryable.log.Warn ("Unable to index {0}: {1}", this.folder_name,
									 e.Message);
					return false;
				}
			}

			if (this.summary_enumerator == null)
				this.summary_enumerator = this.summary.GetEnumerator ();

			if (this.summary_enumerator.MoveNext ())
				return true;

			Logger.Log.Debug ("Queuing up {0} removals for deleted messages in ", this.deleted_list.Count, this.folder_name);
			foreach (string uid in this.deleted_list) {
				Uri uri = EvolutionMailQueryable.EmailUri (this.account_name, this.folder_name, uid);

				// FIXME: This is kind of a hack, but it's the only way
				// with the IndexableGenerator to handle our removals.
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

		// Kind of nasty, but we need the function.
		[System.Runtime.InteropServices.DllImport("libglib-2.0.so.0")]
		static extern int g_str_hash (string str);

		// Stolen from deep within e-d-s's camel-data-cache.c  Very evil.
		private const int CAMEL_DATA_CACHE_MASK = ((1 << 6) - 1);

		public override Indexable GetNextIndexable ()
		{
			Indexable indexable = null;

			Camel.MessageInfo mi = (Camel.MessageInfo) this.summary_enumerator.Current;

			++this.count;

			// Try to load the cached message data off disk
			object flags = this.mapping[mi.uid];

			if (flags == null || (uint) flags != mi.flags) {
				string msg_file;

				if (this.backend_type == ImapBackendType.Imap)
					msg_file = Path.Combine (summary_info.DirectoryName, mi.uid + ".");
				else {
					// This is taken from e-d-s's camel-data-cache.c.  No doubt
					// NotZed would scream bloody murder if he saw this here.
					int hash = (g_str_hash (mi.uid) >> 5) & CAMEL_DATA_CACHE_MASK;
					string cache_path = String.Format ("cache/{0:x}/{1}", hash, mi.uid);
					msg_file = Path.Combine (summary_info.DirectoryName, cache_path);
				}

				indexable = this.CamelMessageToIndexable (mi, msg_file);

				this.mapping[mi.uid] = mi.flags;

				if (indexable != null) {
					++this.indexed_count;
					this.deleted_list.Remove (mi.uid);
				}
			} else if (flags != null)
				this.deleted_list.Remove (mi.uid);

			return indexable;
		}

		private Uri CamelMessageUri (Camel.MessageInfo message_info)
		{
			return EvolutionMailQueryable.EmailUri (this.account_name, this.folder_name, message_info.uid);
		}

		private Indexable CamelMessageToIndexable (Camel.MessageInfo messageInfo, string msg_file)
		{
			// Don't index messages flagged as junk
			if (messageInfo.IsJunk)
				return null;

			// Many properties will be set by the filter when
			// processing the cached data, if it's there.  So
			// don't set a number of properties in that case.
			bool have_content = File.Exists (msg_file);

			Uri uri = CamelMessageUri (messageInfo);
			Indexable indexable = new Indexable (uri);

			indexable.Timestamp = messageInfo.Date;
			indexable.MimeType = "message/rfc822";
			indexable.Type = "MailMessage";

			indexable.AddProperty (Property.NewKeyword ("fixme:account",  this.imap_name));
                        indexable.AddProperty (Property.NewKeyword ("fixme:folder",   this.folder_name));
			indexable.AddProperty (Property.NewKeyword ("fixme:client", "evolution"));
			
			if (!have_content)
				indexable.AddProperty (Property.New ("dc:title", messageInfo.subject));

			GMime.InternetAddressList addrs;
			addrs = GMime.InternetAddressList.ParseString (messageInfo.to);
			foreach (GMime.InternetAddress ia in addrs) {
				if (!have_content) {
					indexable.AddProperty (Property.NewKeyword ("fixme:to", ia.ToString (false)));
					indexable.AddProperty (Property.NewKeyword ("fixme:to_address", ia.Addr));
					indexable.AddProperty (Property.New ("fixme:to_name", ia.Name));
				}

				if (this.folder_name == "Sent")
					indexable.AddProperty (Property.NewKeyword ("fixme:sentTo", ia.Addr));
			}
			addrs.Dispose ();

			addrs = GMime.InternetAddressList.ParseString (messageInfo.cc);
			foreach (GMime.InternetAddress ia in addrs) {
				if (!have_content) {
					indexable.AddProperty (Property.NewKeyword ("fixme:cc", ia.ToString (false)));
					indexable.AddProperty (Property.NewKeyword ("fixme:cc_address", ia.Addr));
					indexable.AddProperty (Property.New ("fixme:cc_name", ia.Name));
				}

				if (this.folder_name == "Sent")
					indexable.AddProperty (Property.NewKeyword ("fixme:sentTo", ia.Addr));
			}
			addrs.Dispose ();

			addrs = GMime.InternetAddressList.ParseString (messageInfo.from);
			foreach (GMime.InternetAddress ia in addrs) {
				if (!have_content) {
					indexable.AddProperty (Property.NewKeyword ("fixme:from", ia.ToString (false)));
					indexable.AddProperty (Property.NewKeyword ("fixme:from_address", ia.Addr));
					indexable.AddProperty (Property.New ("fixme:from_name", ia.Name));
				}

				if (this.folder_name != "Sent")
					indexable.AddProperty (Property.NewKeyword ("fixme:gotFrom", ia.Addr));
			}
			addrs.Dispose ();

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

			if (messageInfo.HasAttachments && !have_content)
				indexable.AddProperty (Property.NewFlag ("fixme:hasAttachments"));

			if (messageInfo.IsAnsweredAll)
				indexable.AddProperty (Property.NewFlag ("fixme:isAnsweredAll"));

			if (have_content)
				indexable.ContentUri = UriFu.PathToFileUri (msg_file);
			else
				indexable.NoContent = true;

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
