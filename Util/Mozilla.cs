//
// Mozilla.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Authors:
//    Fredrik Hedberg (fredrik.hedberg@avafan.com)
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

namespace Beagle.Util.Mozilla
{
	/// <summary>
	/// Class representing a Mozilla profile, used to get a user's profiles and accounts
	/// </summary>
	public class Profile
	{
		// FIXME: Move Beagle.Daemon.PathFinder to Beagle.Platform namespace dammit
#if true
		static string datadir = Environment.GetEnvironmentVariable("HOME"); // Unix
#else
		static string datadir = Environment.GetEnvironmentVariable("APPDATA"); // Win32
#endif

		// FIXME: Add more default locations
		static string[] dirs = 
		{
			"Thunderbird",
			".thunderbird",
			"Mozilla" + System.IO.Path.DirectorySeparatorChar + "Firefox",
			".mozilla" + System.IO.Path.DirectorySeparatorChar + "firefox"
		};

		Hashtable accounts = new Hashtable ();

		public ICollection Accounts {
			get { return accounts.Values; }
		}

		string name;
		public string Name {
			get { return name; }
		}

		string path;
		public string Path {
			get { return path; }
		}

		/// <summary>
		/// Creates and parses a profile from a prefs.js file
		/// <summary>
		public Profile (string name, string path)
		{
			this.name = name;
			this.path = path;

			Preferences prefs = new Preferences ( System.IO.Path.Combine (path, "prefs.js"));

			string accountlist = prefs ["mail.accountmanager.accounts"];
			
			if (accountlist != null) {
				
				string[] accounts = accountlist.Split (',');
				
				foreach (string accountname in accounts) {
					
					Account account = new Account ();
					string servername = prefs [String.Format ("mail.account.{0}.server", accountname)];
					
					account.Path = prefs [String.Format ("mail.server.{0}.directory", servername)];
					account.Name = prefs [String.Format ("mail.server.{0}.name", servername)];
					account.Type = prefs [String.Format ("mail.server.{0}.type", servername)];
					
					this.accounts.Add (accountname, account);
				}
			}
		}

		/// <summary>
		/// Fetch a users profiles from default locations
		/// <summary>
		public static ICollection ReadProfiles () 
		{
			ArrayList profiles = new ArrayList ();

			foreach (string subdir in dirs)
			{
				string dir = System.IO.Path.Combine (datadir, subdir);

				if (!Directory.Exists (dir))
					continue;

				profiles.AddRange (ReadProfiles (dir));	
			}

			return profiles;
		}

		/// <summary>
		/// Fetch a users profiles from a specific profiles.ini file
		/// <summary>
		public static ICollection ReadProfiles (string path) 
		{
			ArrayList profiles = new ArrayList ();

			StreamReader reader = new StreamReader (new FileStream (System.IO.Path.Combine (path, "profiles.ini"), FileMode.Open, FileAccess.Read, FileShare.Read));

			string lname = null;
			string lpath = null;

			string data = null;

			while ((data = reader.ReadLine ()) != null) {
			
				if (data.StartsWith ("[") && lname != null && lpath != null)
					profiles.Add (new Profile (lname, System.IO.Path.Combine(path, lpath)));
				
				if (data.IndexOf ("=") == -1)
					continue;

				string[] fields = data.Split ('=');

				switch (fields[0].ToLower ())
				{
					case "name":
						lname = fields[1];
						break;
					case "path":
						lpath = fields[1];
						break;
				}

			}

			if (lname != null && lpath != null)
				profiles.Add (new Profile (lname, System.IO.Path.Combine (path, lpath)));

			return profiles;
		}
	}

	/// <summary>
	/// Class representing a Mozilla account
	/// </summary>
	public class Account
	{
		public string Path;
		public string Name;
		public string Type;
	}

	/// <summary>
	/// Class for parsing Mozilla preferences files - prefs.js
	/// </summary>
	public class Preferences 
	{
		Hashtable properties = new Hashtable ();

		public Preferences (string path)
		{
			StreamReader reader = new StreamReader (new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read));

			string data;

			while ((data = reader.ReadLine ()) != null) {

				if (!data.StartsWith ("user_pref"))
					continue;
			
				// FIXME: Use regexps
				
				int index = data.IndexOf ('"',11);

				string key = data.Substring (11, index-11);
				string value = data.Substring (index, data.IndexOf (')')-index).Substring (3).Trim ('"');

				properties[key] = value;
			}

			reader.Close ();
		}

		public IDictionary Properties {
			get { return properties; }
		}

		public ICollection Keys {
			get { return properties.Keys; }
		}

		virtual public string this [string key] {
			get { return (string) properties [key]; }
			set { 
				if (value == null || value == "") {
					if (properties.Contains (key))
						properties.Remove (key);
					return;
				}
				properties [key] = value as string;
			}
		}

	}

	/// <summary>
	/// Message (mail, rss whatever) in Mozilla
	/// </summary>
	public class Message
	{
		public string Id;
		public string Date;
		public string Subject;
		public string From;
		public string To;
		public string Path;
		public int Offset;

		StringBuilder body = new StringBuilder ();

		public string Body {
			get { return body.ToString (); }
		}

		public void AppendBody (string str)
		{
			body.Append (str);
		}

		public Hashtable Headers = new Hashtable ();
	}

	public class Address
	{
		public string Name;
		public string Email;

		public override string ToString ()
		{
			if (Name != null && Name != "")
				return String.Format ("{0} <{1}>", Name, Email);
			else
				return Email;
		}

		public static ICollection Parse (string str)
		{
			return null;
		}
	}

	/// <summary>
	/// FIXME: This is a hack and does not comply with any RFC, nor does it support attachments, encodings and other fancy shit
	/// FIXME: Use a lib like gmime to parse messages, must be available on Linux, Win32 & MacOSX.
	/// </summary>
	public class MessageReader
	{
		FileStream stream;
		StreamReader reader;
		bool hasMore = true;
		Message message;
		string path;

		public MessageReader (string path) : this (path, -1)
		{
			Console.WriteLine ("Doing: " + path);
		}

		public MessageReader (string path, int offset)
		{
			this.path = path;

			FileStream stream;

			try {
				stream = new FileStream (path,
						     FileMode.Open,
						     FileAccess.Read,
						     FileShare.Read);
				
				if (offset > 0)
					stream.Seek (offset, SeekOrigin.Begin);

				reader = new StreamReader (stream);

			} catch (Exception e) {
				Console.WriteLine ("Could not open '{0}' (offset={1})", path, offset);
				Console.WriteLine (e);
			}

			reader.ReadLine ();

			
		}

		public bool HasMoreMessages
		{
			get { return hasMore; }
		}

		public Message NextMessage
		{
			get {
				Read ();
				return message;
			}
		}

		private void Read ()
		{
		        message = new Message ();
			message.Path = path;
			string lastdata = "";
			string data;
			bool isBody = false;
			
			try {
				
				while ((data = reader.ReadLine ()) != null) {

					// Break for new message
					
					if (data.StartsWith ("From - ")) {
						return;
					}

					// Add body to message
					
					if (isBody) {
						message.AppendBody (data);
						continue;
					}

					// Break for message content

					if (data.Length == 0) {
						isBody = true;
						continue;
					}

					// It's a header 

					int index = data.IndexOf (":");

					if (index != -1 && !data.StartsWith (" ")) {

						if (data.Length < index +2)
							continue;
						string key = data.Substring (0, index);
						string value = data.Substring (index + 2);

						message.Headers [key] = value;

						switch (key.ToLower ()) {
						case "subject":
							message.Subject = value;
							break;
						case "from":
							message.From = value;
							break;
						case "to":
							message.To = value;
							break;
						case "date":
							message.Date = value;
							break;
						case "message-id":
							char[] shit = {'<', '>'};
							message.Id = value.Trim (shit);
							break;
						}
					}
						
					lastdata = data;
				}
				
				hasMore = false;
			} catch (Exception e) {
				Console.WriteLine (e);
				return;
			}
		}
	}

#if false
	public class Test 
	{
		public static void Main (string[] args)
		{
			foreach (Profile profile in Profile.ReadProfiles ()) {
				Console.WriteLine("Profile: {0} - {1}", profile.Name, profile.Path);
				foreach (Account account in profile.Accounts) {
					Console.WriteLine ("\t{0} ({1}) - {2}", account.Name, account.Type, account.Path);
				}
			}
		}
	}
#endif
}
