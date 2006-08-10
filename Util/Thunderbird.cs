//
// Thunderbird.cs: A utility class with methods and classes that might be needed to parse Thunderbird data
//
// Copyright (C) 2006 Pierre Östlund
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

using Beagle;
using Beagle.Util;

using GMime;

namespace Beagle.Util {

	public class Thunderbird {
	
		public static bool Debug = false;
		
		/////////////////////////////////////////////////////////////////////////////////////
	
		public enum AccountType {
			Pop3,
			Imap,
			Rss,
			Nntp,
			AddressBook,
			MoveMail,
			Invalid
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		public class Account {
			private string server_string = null;
			private string path = null;
			private int server_port = -1;
			private AccountType account_type;
			private char delimiter;
			
			public Account (string server, string path, int port, AccountType type, char delim)
			{
				this.server_string = server;
				this.path = path;
				this.server_port = port;
				this.account_type = type;
				this.delimiter = delim;
			}

			public string Server {
				get { return server_string; }
			}
			
			public string Path {
				get { return path; }
			}
			
			public int Port {
				get { return (server_port > 0 ? server_port : Thunderbird.ParsePort (Type)); }
			}
			
			public AccountType Type {
				get { return account_type; }
			}
			
			public char Delimiter {
				get { return (delimiter ==  char.MinValue ? '/' : delimiter); }
			}
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		public abstract class StorageBase {
			protected Hashtable data;
			protected System.Uri uri;
			protected Account account;
			
			public StorageBase ()
			{
				data = new Hashtable ();
			}
			
			public string GetString (string key)
			{
				return Convert.ToString (data [key]);
			}
			
			public int GetInt (string key)
			{
				try {
					if (!data.ContainsKey (key))
						return -1;
					
					return Convert.ToInt32 (data [key]);
				} catch {
					return -1;
				}
			}
			
			public bool GetBool (string key)
			{
				try {
					return Convert.ToBoolean (data [key]);
				} catch {
					return false;
				}
			}
			
			public object GetObject (string key)
			{
				return data [key];
			}
			
			public void SetObject (string key, object value)
			{
				if (key != null)
					data [key] = value;
			}
			
			public System.Uri Uri {
				get { return uri; }
				set { uri = value; }
			}
			
			public Account Account {
				get { return account; }
			}
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		// String types:
		// id, sender, subject, recipients, date, mailbox
		// Integer types:
		// size, msgOffset, offlineMsgSize
		// Bool types:
		// FullIndex
		public class Mail : StorageBase {
			private string workfile;
		
			public Mail (Account account, Hashtable data, string workfile)
			{
				foreach (string key in data.Keys) {
					if (key == "id")
						SetObject (key, data [key]);
					else if (key == "sender")
						SetObject (key, Utils.HeaderDecodePhrase ((string) data [key]));
					else if (key == "subject")
						SetObject (key, Utils.HeaderDecodeText ((string) data [key]));
					else if (key == "recipients")
						SetObject (key, Utils.HeaderDecodePhrase ((string) data [key]));
					else if (key == "date")
						SetObject (key, Thunderbird.HexDateToString ((string) data [key]));
					else if (key == "size")
						SetObject (key, Thunderbird.Hex2Dec ((string) data [key]));
					else if (key == "msgOffset")
						SetObject (key, Thunderbird.Hex2Dec ((string) data[key]));
					else if (key == "offlineMsgSize")
						SetObject (key, Thunderbird.Hex2Dec ((string) data [key]));
					else if (key == "message-id")
						SetObject (key, (string) data [key]);
					else if (key == "references")
						SetObject (key, (data [key] as string).Replace ("\\", ""));
				}
				
				this.account = account;
				this.workfile = workfile;
				SetObject ("mailbox", Thunderbird.ConstructMailboxString (workfile, account));
				this.uri = Thunderbird.NewUri (Account, GetString ("mailbox"), GetString ("id"));
			}
			
			private GMime.Message ConstructMessage ()
			{
				GMime.Message message = null;
				
				// Try to fully index this mail by loading the entire mail into memory
				if (GetBool ("FullIndex"))
					message = FullMessage ();
				
				// Make sure we have the correct status set on this message, in case something went wrong
				if (message == null || (message != null && message.Stream.Length <= 1)) {
					SetObject ("FullIndex", (object) false);
					return PartialMessage ();
				} else 
					return message;
			}
			
			private GMime.Message PartialMessage ()
			{
				string date = GetString ("date");
				GMime.Message message = new GMime.Message (true);

				message.Subject = GetString ("subject");
				message.Sender = GetString ("sender");
				message.MessageId = GetString ("message-id");
				message.SetDate ((date != string.Empty ? DateTime.Parse (date) : new DateTime (1970, 1, 1, 0, 0, 0)), 0);
				
				// Add references
				if (data.ContainsKey ("references")) {
					foreach (Match m in Regex.Matches ((data ["references"] as string), @"\<(?>[^\<\>]+)\>"))
						message.AddHeader ("References", m.Value);
				}
				
				return message;
			}

			private GMime.Message FullMessage ()
			{
				int fd;
				string file = Thunderbird.GetFullyIndexableFile (workfile);
				GMime.Message message = null;

				// gmime will go nuts and make the daemon "segmentation fault" in case the file doesn't exist!
				if (!File.Exists (file))
					return message;

				try {
					fd = Mono.Unix.Native.Syscall.open (file, Mono.Unix.Native.OpenFlags.O_RDONLY);
					StreamFs stream = new StreamFs (fd, Offset, Offset + Size);
					Parser parser = new Parser (stream);
					message = parser.ConstructMessage ();
				
					stream.Dispose ();
					parser.Dispose ();
				} catch {}

				return message;
			}
			
			public int Offset {
				get {
					int msg_offset = GetInt ("msgOffset"); 
					return (msg_offset >= 0 ? msg_offset : Thunderbird.Hex2Dec (GetString ("id"))); 
				}
			}
			
			public int Size {
				get {
					int msg_offline_size = GetInt ("offlineMsgSize");
					return (msg_offline_size >= 0 ? msg_offline_size : GetInt ("size")); 
				}
			}

			public GMime.Message Message {
				get { return ConstructMessage (); }
			}
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		// String types:
		// id, FirstName, LastName, DisplayName, NickName, PrimaryEmail, SecondEmail,
		// WorkPhone, FaxNumber, HomePhone, PagerNumber, CellularNumber, HomeAddress,
		// HomeAddress2, HomeCity, HomeState, HomeZipCode,  HomeCountry, WorkAddress,
		// WorkAddress2, WorkCity, WorkState, WorkZipCode, WorkCountry, JobTitle, Department,
		// Company, _AimScreenName, FamilyName, WebPage1, WebPage2, BirthYear, BirthMonth
		// , BirthDay, Custom1, Custom2, Custom3, Custom4, Notes, PreferMailFormat
		// Integer types:
		// None
		public class Contact : StorageBase {
			private string workfile;
		
			public Contact (Account account, Hashtable data, string workfile)
			{
				this.account = account;
				this.data = data;
				this.workfile = workfile;
				this.uri = NewUri (account, Thunderbird.ConstructMailboxString (workfile, account), GetString ("id"));
			}
			
			public string Workfile {
				get { return workfile; }
			}
		
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		// String types:
		// id, subject, sender, date, message-id
		// Integer types:
		// size
		public class RssFeed : StorageBase {
			private string workfile;
		
			public RssFeed (Account account, Hashtable data, string workfile)
			{
				foreach (string key in data.Keys) {
					if (key == "id")
						SetObject (key, data [key]);
					else if (key == "subject") // title
						SetObject (key, Utils.HeaderDecodePhrase ((string) data [key]));
					else if (key == "sender") // publisher
						SetObject (key, Utils.HeaderDecodePhrase ((string) data [key]));
					else if (key == "date") // date
						SetObject (key, HexDateToString ((string) data [key]));
					else if (key == "size") // size
						SetObject (key, Hex2Dec ((string) data [key]));
					else if (key == "message-id") { // links
						string tmp = (string) data [key];
						SetObject (key, Utils.HeaderDecodePhrase (tmp.Substring (0, tmp.LastIndexOf ("@"))));
					}
				}
				
				this.account = account;
				this.workfile = workfile;
				this.uri = NewUri (account, ConstructMailboxString (workfile, account), GetString ("id"));
			}
			
			// FIXME: Make this a lot faster!
			private StringReader ConstructContent ()
			{
				string content = null;
				string file = GetFullyIndexableFile (workfile);
				
				if (!File.Exists (file))
					return null;
				
				try {
					StreamReader reader = new StreamReader (file);
					
					char[] tmp = new char [GetInt ("size")];
					reader.BaseStream.Seek (Hex2Dec (GetString ("id")), SeekOrigin.Begin);
					reader.Read (tmp, 0, tmp.Length);
					
					// We don't want to index all HTTP headers, so we cut 'em off
					content = new string (tmp);
					content = content.Substring (content.IndexOf ("<html>"));
					
					reader.Close ();
				} catch { }
				
				return (content != null ? new StringReader (content) : null);
			}
			
			public string Workfile {
				get { return workfile; }
			}
			
			public StringReader Content {
				get { return ConstructContent (); }
			}
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		// String types:
		// id, subject, sender, date
		// Integer types:
		// size
		// An NNTP message resambles a mail so very much...
		public class NntpMessage : Mail {
		
			public NntpMessage (Account account, Hashtable data, string workfile)
				: base (account, data, workfile)
			{
				foreach (string key in data.Keys) {
					if (key == "id")
						SetObject (key,  data [key]);
					else if (key == "subject")
						SetObject (key, Utils.HeaderDecodeText ((string) data [key]));
					else if (key == "sender")
						SetObject (key, Utils.HeaderDecodePhrase ((string) data [key]));
					else if (key == "date")
						SetObject (key, Thunderbird.HexDateToString ((string) data [key]));
					else if (key == "size")
						SetObject (key, Thunderbird.Hex2Dec ((string) data [key]));
				}
				
				Uri = NewUri (account, ConstructMailboxString (workfile, account), GetString ("id"));
			}
		
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		// Still just a stub, will be fixed later on
		public class MoveMail : StorageBase {
		
			public MoveMail (Account account, Hashtable data, string workfile)
			{
				this.account = account;
				this.data = data;
				//this.workfile = workfile;
				this.uri = NewUri (account, GetString ("tmp"), GetString ("id"));
			}
		
		}

		/////////////////////////////////////////////////////////////////////////////////////
		
		public class Database : IEnumerable {
			private static MorkDatabase db;
			private Account account;
			private string file;
			
			private IEnumerator current = null;

			public Database (Account account, string file)
			{
				this.account = account;
				this.file = file;
			}
			
			public void Load ()
			{
				db = new MorkDatabase (file);
				db.Read();
				
				switch (account.Type) {
				case AccountType.Pop3:
				case AccountType.Imap:
				case AccountType.Rss:
				case AccountType.Nntp:
				case AccountType.MoveMail:
					db.EnumNamespace = "ns:msg:db:row:scope:msgs:all";
					break;
				case AccountType.AddressBook:
					db.EnumNamespace = "ns:addrbk:db:row:scope:card:all";
					break;
				}
				
				current = db.GetEnumerator ();
			}
			
			public Account Account {
				get { return account; }
			}
			
			public int Count {
				get {
					if (db == null)
						return 0;
						
					return (account.Type == AccountType.AddressBook ? 
						db.GetRowCount ("ns:addrbk:db:row:scope:card:all", "BF") : 
						db.GetRowCount ("ns:msg:db:row:scope:msgs:all"));
				}
			}
			
			public string Filename {
				get { return (db != null ? db.Filename : string.Empty); }
			}
			
			public MorkDatabase Db {
				get { return db; }
			}
			
			public IEnumerator GetEnumerator ()
			{
				return new DatabaseEnumerator (db, account, current);
			}
			
			public class DatabaseEnumerator : IEnumerator {
				private MorkDatabase db;
				private Account account;
				private IEnumerator enumerator;

				public DatabaseEnumerator (MorkDatabase db, Account account, IEnumerator enumerator)
				{
					this.db = db;
					this.enumerator = enumerator;
					this.account = account;
				}
				
				public bool MoveNext ()
				{
					return (enumerator != null ? enumerator.MoveNext () : false);
				}
				
				public void Reset ()
				{
					enumerator.Reset ();
				}
				
				public object Current {
					get { 
						switch (account.Type) {
						case AccountType.Pop3:
						case AccountType.Imap:
							return new Mail (account, db.Compile ((string) enumerator.Current, 
								"ns:msg:db:row:scope:msgs:all"), db.Filename); 
						case AccountType.AddressBook:
							return new Contact (account, db.Compile ((string) enumerator.Current,
								"ns:addrbk:db:row:scope:card:all"), db.Filename);
						case AccountType.Rss:
							return new RssFeed (account, db.Compile ((string) enumerator.Current, 
								"ns:msg:db:row:scope:msgs:all"), db.Filename);
						case AccountType.Nntp:
							return new NntpMessage (account, db.Compile ((string) enumerator.Current, 
								"ns:msg:db:row:scope:msgs:all"), db.Filename);
						case AccountType.MoveMail:
							return new MoveMail (account, db.Compile ((string) enumerator.Current,
								"ns:msg:db:row:scope:msgs:all"), db.Filename);
						}
						
						return null;
					}
				}
			}
		}
		
		/////////////////////////////////////////////////////////////////////////////////////

		public static string HexDateToString (string hex)
		{
			DateTime time = new DateTime (1970,1,1,0,0,0);
			
			try {
				time = time.AddSeconds (
					Int32.Parse (hex, NumberStyles.HexNumber));
			} catch {}
			
			return time.ToString ();
		}
		
		public static int Hex2Dec (string hex)
		{
			int dec = -1;
			
			try {
				dec = Convert.ToInt32 (hex, 16);
			} catch { }
			
			return dec;
		}
		
		public static int ParsePort (AccountType type)
		{
			int port = 0;
			
			switch (type) {
			case AccountType.Pop3:
				port = 110;
				break;
			case AccountType.Imap:
				port = 143;
				break;
			}
			
			return port;
		}
		
		public static AccountType ParseAccountType (string type_str)
		{
			AccountType type;
			
			try {
				type = (AccountType) Enum.Parse (typeof (AccountType), type_str, true);
			} catch {
				type = AccountType.Invalid;
			}
			
			return type;
		}
		
		// A hack to extract a potential delimiter from a namespace-string
		public static char GetDelimiter (params string[] namespace_str)
		{
			MatchCollection matches = null;
			Regex reg = new Regex (@"\\\""(.*)(?<delimiter>[^,])\\\""", RegexOptions.Compiled);
			
			if (namespace_str == null)
				return char.MinValue;
			
			foreach (string str in namespace_str) {
				try {
					matches = reg.Matches (str);
				} catch {
					continue;
				}

				foreach (Match m in matches) {
					char delim = Convert.ToChar (m.Result ("${delimiter}"));
					if (delim != ' ')
						return delim;
				}
			}
			
			return char.MinValue;
		}
		
		public static Uri NewUri (Account account, string mailbox, string id)
		{
			Uri uri = null;

			switch (account.Type) {
				case AccountType.Pop3:
				case AccountType.MoveMail:
				case AccountType.Rss: // rss, movemail and pop3 share the same uri scheme
					uri = new Uri (String.Format ("mailbox://{0}/{1}?number={2}", 
						account.Path, mailbox, Convert.ToInt32 (id, 16))); 
					break;
				case AccountType.Imap:
					uri = new Uri (String.Format ("imap://{0}:{1}/fetch%3EUID%3E{2}%3E{3}",
						account.Server, account.Port, mailbox, Convert.ToInt32 (id, 16)));
					break;
				case AccountType.AddressBook:
					uri = new Uri (String.Format ("abook://{0}?id={1}", mailbox, id));
					break;
				case AccountType.Nntp:
					uri = new Uri (String.Format ("news://{0}:{1}/{2}?number={3}" , 
						account.Server, account.Port.ToString(), mailbox, id));
					break;
				case AccountType.Invalid:
					break;
			}
			
			return uri;
		}
		
		public static long GetFileSize (string filename)
		{
			long filesize = -1;
			
			try {
				FileInfo file = new FileInfo (filename);
				filesize = file.Length;
			} catch { }
				
			return filesize;
		}
		
		public static  string GetFullyIndexableFile (string mork_file)
		{
			string mailbox_file = Path.Combine (
				Path.GetDirectoryName (mork_file), 
				Path.GetFileNameWithoutExtension (mork_file));
			
			return mailbox_file;
		}
		
		// a generic way to determine where thunderbird is storing it's files
		public static string GetRootPath ()
		{
			foreach (string dir in Directory.GetDirectories (PathFinder.HomeDir, ".*thunderbird*")) {
				if (File.Exists (Path.Combine (dir, "profiles.ini")))
					return dir;
			}
			
			return null;
		}
		
		public static string[] GetProfilePaths (string root)
		{
			string line;
			StreamReader reader;
			ArrayList profiles = new ArrayList ();
			
			try {
				reader = new StreamReader (Path.Combine (root, "profiles.ini"));
			} catch { 
				return (string[]) profiles.ToArray ();
			}
			
			// Read the profile path
			while ((line = reader.ReadLine ()) != null) {
				if (line.StartsWith ("Path=")) {
					profiles.Add (String.Format ("{0}/{1}", root, line.Substring (5)));
					continue;
				}
			}

			return (string[]) profiles.ToArray (typeof (string));
		}
		
		public static string GetRelativePath (string mork_file)
		{
			string path = null;
			foreach (string root in Thunderbird.GetProfilePaths (Thunderbird.GetRootPath ())) {
				if (!mork_file.StartsWith (root))
					continue;

				path = mork_file.Substring (root.Length+1);
				break;
			}
			
			return path;
		}
		
		public static ArrayList ReadAccounts (string profile_dir)
		{
			string line = null;
			Queue accounts = new Queue();
			Hashtable tbl = new Hashtable ();
			ArrayList account_list = new ArrayList ();
			StreamReader reader;
			Regex id_reg = new Regex (@"account.account(?<id>\d).server");
			Regex reg = new Regex (@"user_pref\(""mail\.(?<key>.*)""\s*,\s*(""(?<value>.*)"" | (?<value>.*))\);",
				RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

			try {
				reader = new StreamReader (Path.Combine (profile_dir, "prefs.js"));
			} catch (Exception e) {
				if (Debug)
					Logger.Log.Debug (e, "Failed to open file {0}:", Path.Combine (profile_dir , "prefs.js"));

				return account_list;
			}

			while ((line = reader.ReadLine()) != null) {
				if (!line.StartsWith ("user_pref(\"mail."))
					continue;

				try {
					string key = reg.Match (line).Result ("${key}");

					if (key.StartsWith ("account.account")) {
						if (Debug)
							Logger.Log.Debug ("account.account: {0}", id_reg.Match (key).Result ("${id}"));

						accounts.Enqueue (id_reg.Match (key).Result ("${id}"));
					}

					tbl [key] = reg.Match (line).Result ("${value}");
				} catch (Exception e) { 
					if (Debug)
						Logger.Log.Debug (e, "ReadAccounts 1:");
				}
			}
			
			if (Debug)
				Logger.Log.Info ("ReadAccounts: {0} accounts", accounts.Count);

			while (accounts.Count > 0) {
				string id = "server.server" + (accounts.Dequeue() as string);
				AccountType type = ParseAccountType ((string) tbl [id + ".type"]);
				char delimiter = GetDelimiter ((string) tbl [id + ".namespace.personal"], 
					(string) tbl [id + ".namespace.public"], (string) tbl [id + ".namespace.other_users"]);
				
				if (type == AccountType.Invalid)
					continue;
				
				if (Debug)
					Logger.Log.Debug ("ReadAccounts 2: {0}", id);

				try {
					account_list.Add (new Account (
						String.Format ("{0}@{1}", (string) tbl [id + ".userName"], (string) tbl [id + ".hostname"]), 
						(string) tbl [id + ".directory"], Convert.ToInt32 ((string) tbl [id + ".port"]), type, delimiter));
				} catch (Exception e) {
					if (Debug)
						Logger.Log.Debug (e, "ReadAccounts 3:");
					continue;
				}
			}
			
			// In case the address book file exists, add it as well
			if (File.Exists (Path.Combine (profile_dir, "abook.mab"))) {
				account_list.Add (new Account (Path.GetFileName (profile_dir), 
					Path.Combine (profile_dir, "abook.mab"), 0, AccountType.AddressBook, ' '));
			}

			return account_list;
		}
		
		public static bool IsMorkFile (string path, string filename)
		{
			string full_path = Path.Combine (path, filename);
			
			if (Path.GetExtension (filename) == ".msf" && File.Exists (full_path))
				return true;
		
			return false;
		}
		
		public static bool IsFullyIndexable (string mork_file)
		{
			try {
				FileInfo file_info = new FileInfo (GetFullyIndexableFile (mork_file));
				if (file_info.Length > 0)
					return true;
			} catch {}
			
			return false;
		}
		
		public static string ConstructMailboxString (string mork_file, Account account)
		{
			string mailbox = null;

			switch (account.Type) {
			case AccountType.Pop3:
			case AccountType.Rss:
			case AccountType.MoveMail:
				mailbox = GetFullyIndexableFile (mork_file.Substring (account.Path.Length+1));
				break;
			case AccountType.Imap:
				mailbox = String.Format ("{0}{1}", 
						account.Delimiter, 
						GetFullyIndexableFile (mork_file.Substring (account.Path.Length+1).Replace (".sbd/", Convert.ToString (account.Delimiter))));
				break;
			case AccountType.AddressBook:
				mailbox = mork_file;
				break;
			case AccountType.Nntp:
				// Doesn't really matter what this is as long as it's unique (at least until I've figure the uri schemes)
				mailbox = account.Server;
				break;
			case AccountType.Invalid:
				mailbox = String.Format ("InvalidMailbox-{0}", mork_file);
				break;
			}

			return mailbox;
		}
	}

}
