//
// ImBuddy.cs
//
// Copyright (C) 2004 Matthew Jones <mattharrison sbcglobal net>
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
using System.Collections;
using System.Xml;
using System.IO;

using Beagle.Daemon;

namespace Beagle.Util {

	public class ImBuddy {
		public string Protocol = "";
		public string OwnerAccountName = "";
		public string BuddyAccountName = "";
		public string Alias = "";
		public string BuddyIconLocation = "";
		public string BuddyIconChecksum = "";

		public ImBuddy (string protocol, string owneraccount, string buddyaccount, string alias, string iconlocation, string iconchecksum) {
			Protocol = protocol;
			OwnerAccountName = owneraccount;
			BuddyAccountName = buddyaccount;
			Alias = alias;
			BuddyIconLocation = iconlocation;
			BuddyIconChecksum = iconchecksum;
		}
	}
	
	///////////////////////////////////////////////////////////////////////////////

	public abstract class ImBuddyListReader {

		public bool verbose = false;

		public Hashtable buddyList = new Hashtable ();

		abstract public void Read ();

		public void DebugPrint (string str) {
			if (!verbose)
				return;
			System.Console.WriteLine (str);
		}

		public class ImBuddyComparer : IComparer {
			
			int IComparer.Compare (Object x, Object y) {
				string accountx, accounty;
	
				try {
					accountx = ((ImBuddy) x).BuddyAccountName;
				} catch {
					accountx = "";
				}
				
				try {
					accounty = ((ImBuddy) y).BuddyAccountName;
				} catch {
					accounty = "";
				}
	
				return System.String.Compare (accountx, accounty);
			}
		}

		public class ImBuddyAliasComparer : IComparer {
			
			int IComparer.Compare (Object x, Object y) {
				string account;
				
				try {
					account = (string) x;
					return System.String.Compare ((string)x, ((ImBuddy) y).BuddyAccountName);
				} catch {
					return System.String.Compare (((ImBuddy) x).BuddyAccountName, (string)y);
				}
			}
		}
	}
	
	public class GaimBuddyListReader : ImBuddyListReader {

		FileSystemEventMonitor monitor = null;
		string buddyListPath;
		string buddyListDir;

		public GaimBuddyListReader ()
		{
			string home = Environment.GetEnvironmentVariable ("HOME");
			buddyListDir = Path.Combine (home, ".gaim");
			DirectoryInfo gaimDir = new DirectoryInfo (buddyListDir);

			buddyListPath = Path.Combine (buddyListDir, "blist.xml");
			Read ();

			monitor = new FileSystemEventMonitor ();
			monitor.FileSystemEvent += OnFileSystemEvent;
			monitor.Subscribe (gaimDir, false);
			
		}

		~GaimBuddyListReader ()
		{
			DirectoryInfo gaimDir = new DirectoryInfo (buddyListDir);
			monitor.Unsubscribe (gaimDir);
		}
	
		protected void OnFileSystemEvent (FileSystemEventMonitor monitor,
						  FileSystemEventType eventType,
						  string oldPath,
						  string newPath)
		{
			if (oldPath == buddyListPath || newPath == buddyListPath) {
				Read ();
			}
				
		}

		private string Format (string name) {
			return name.ToLower ().Replace (" ", "");
		}

		override public void Read ()
		{
			buddyList = new Hashtable ();

			XmlDocument accounts = new XmlDocument ();
			accounts.Load (buddyListPath);

			XmlNodeList contacts = accounts.SelectNodes ("//contact");

			foreach (XmlNode contact in contacts) {
				string groupalias = "";
				
				foreach (XmlAttribute attr in contact.Attributes) {
					if (attr.Name == "alias") {
						groupalias = attr.Value;
					}
				}

				if (groupalias != "") {
					string xpath = "//contact[ name='" + groupalias + "']/buddy";
					
					foreach (XmlNode buddy in contact.ChildNodes) {
						AddBuddy (buddy, groupalias);
					}

				}
			}
			
			foreach (XmlNode buddy in accounts.SelectNodes ("//contact[not(@name)]/buddy")) {
				AddBuddy (buddy);
			}
		}

		private void AddBuddy (XmlNode buddy, string groupalias) 
		{
			string protocol, owner, other, alias, iconlocation, iconchecksum;
			
			protocol = "";
			owner = "";
			other = "";
			alias = "";
			iconlocation = "";
			iconchecksum = "";

			foreach (XmlAttribute attr in buddy.Attributes) {
				switch (attr.Name) {
					case "account":
						owner = attr.Value;
						DebugPrint ("owner: " + owner);
						break;
					case "proto":
					protocol = attr.Value;
					DebugPrint ("protocol: " + protocol);
					break;
				}
			}
		
			foreach (XmlNode attr in buddy.ChildNodes) {
				switch (attr.LocalName) {
					case "name":
						other = attr.InnerText;
						DebugPrint ("other: " + other);
						break;
					case "alias":
						alias = attr.InnerText;
						DebugPrint ("alias: " + alias);
						break;
					case "setting":
						foreach (XmlAttribute subattr in attr.Attributes) {
							if (subattr.Name == "name" && subattr.Value == "buddy_icon")
							{
								iconlocation = attr.InnerText;
								DebugPrint ("iconlocation: " + iconlocation);
							}
							else if ( subattr.Name == "name" && subattr.Value == "icon_checksum")
							{
								iconchecksum = attr.InnerText;
								DebugPrint ("iconchecksum: " + iconchecksum);
							}
						}
						break;
				}
			}

			ImBuddy old;

			alias = groupalias;

			if (buddyList.ContainsKey (Format(other))) {
				old = (ImBuddy)buddyList[Format(other)];
				if (old.Alias == "" && alias != "")
					old.Alias = alias;
				if (old.BuddyIconLocation == "" && iconlocation == "") {
					old.BuddyIconLocation = iconlocation;
					old.BuddyIconChecksum = iconchecksum;
				}
				buddyList.Remove (Format(other));
				buddyList.Add (Format(other), old);
			} 
			else
				buddyList.Add (Format(other), new ImBuddy (protocol, owner, Format(other), alias, iconlocation, iconchecksum));
		}

		private void AddBuddy (XmlNode buddy) 
		{
			string protocol, owner, other, alias, iconlocation, iconchecksum;
			
			protocol = "";
			owner = "";
			other = "";
			alias = "";
			iconlocation = "";
			iconchecksum = "";

			foreach (XmlAttribute attr in buddy.Attributes) {
				switch (attr.Name) {
					case "account":
						owner = attr.Value;
						DebugPrint ("owner: " + owner);
						break;
					case "proto":
					protocol = attr.Value;
					DebugPrint ("protocol: " + protocol);
					break;
				}
			}
		
			foreach (XmlNode attr in buddy.ChildNodes) {
				switch (attr.LocalName) {
					case "name":
						other = attr.InnerText;
						DebugPrint ("other: " + other);
						break;
					case "alias":
						alias = attr.InnerText;
						DebugPrint ("alias: " + alias);
						break;
					case "setting":
						foreach (XmlAttribute subattr in attr.Attributes) {
							if (subattr.Name == "name" && subattr.Value == "buddy_icon")
							{
								iconlocation = attr.InnerText;
								DebugPrint ("iconlocation: " + iconlocation);
							}
							else if ( subattr.Name == "name" && subattr.Value == "icon_checksum")
							{
								iconchecksum = attr.InnerText;
								DebugPrint ("iconchecksum: " + iconchecksum);
							}
						}
						break;
				}
			}

			ImBuddy old;

			if (buddyList.ContainsKey (Format(other))) {
				old = (ImBuddy)buddyList[Format(other)];
				if (old.Alias == "" && alias != "")
					old.Alias = alias;
				if (old.BuddyIconLocation == "" && iconlocation == "") {
					old.BuddyIconLocation = iconlocation;
					old.BuddyIconChecksum = iconchecksum;
				}
				buddyList.Remove (Format(other));
				buddyList.Add (Format(other), old);
			} 
			else
				buddyList.Add (Format(other), new ImBuddy (protocol, owner, Format(other), alias, iconlocation, iconchecksum));
		}
		

		public ImBuddy Search (string buddy) {
			return (ImBuddy)buddyList[Format(buddy)];
		}

	}

}
