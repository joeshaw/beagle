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

namespace Beagle.Util {

	public class ImBuddy {
		public string Protocol = String.Empty;
		public string OwnerAccountName = String.Empty;
		public string BuddyAccountName = String.Empty;
		public string Alias = String.Empty;
		public string BuddyIconLocation = String.Empty;
		public string BuddyIconChecksum = String.Empty;

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
			Logger.Log.Debug ("{0}", str);
		}

	}
	
	public class GaimBuddyListReader : ImBuddyListReader {

		string buddyListPath;
		string buddyListDir;
		DateTime buddyListLastWriteTime;
		uint timeoutId;

		public GaimBuddyListReader ()
		{
			string home = Environment.GetEnvironmentVariable ("HOME");
			buddyListDir = Path.Combine (home, ".gaim");
			buddyListPath = Path.Combine (buddyListDir, "blist.xml");
			
			if (File.Exists (buddyListPath))
				Read ();

			// Poll the file once every minute
			timeoutId = GLib.Timeout.Add (60000, new GLib.TimeoutHandler (ReadTimeoutHandler));
		}

		~GaimBuddyListReader ()
		{
			if (timeoutId > 0)
				GLib.Source.Remove (timeoutId);
		}

		private bool ReadTimeoutHandler ()
		{
			if (File.Exists (buddyListPath))
				Read ();

			return true;
		}

		private string Format (string name) {
			return name.ToLower ().Replace (" ", "");
		}

		override public void Read ()
		{
			// If the file hasn't changed, don't do anything.
			DateTime last_write = File.GetLastWriteTime (buddyListPath);
			if (last_write == buddyListLastWriteTime)
				return;

			buddyListLastWriteTime = last_write;

			buddyList = new Hashtable ();

			try {
				XmlDocument accounts = new XmlDocument ();
				accounts.Load (buddyListPath);
				
				XmlNodeList contacts = accounts.SelectNodes ("//contact");
				
				foreach (XmlNode contact in contacts) {
					string groupalias = String.Empty;
					
					foreach (XmlAttribute attr in contact.Attributes) {
						if (attr.Name == "alias") {
							groupalias = attr.Value;
						}
					}
					
					if (groupalias != String.Empty) {
						foreach (XmlNode buddy in contact.ChildNodes) {
							AddBuddy (buddy, groupalias);
						}
					}
				}
				
				foreach (XmlNode buddy in accounts.SelectNodes ("//contact[not(@name)]/buddy")) {
					AddBuddy (buddy);
				}
			} catch (Exception ex) {
				Logger.Log.Error (ex, "Caught exception while trying to parse Gaim contact list:");
			}
		}

		private void AddBuddy (XmlNode buddy, string groupalias) 
		{
			string protocol, owner, other, alias, iconlocation, iconchecksum;
			
			protocol = String.Empty;
			owner = String.Empty;
			other = String.Empty;
			alias = String.Empty;
			iconlocation = String.Empty;
			iconchecksum = String.Empty;

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

			if (groupalias != null)
				alias = groupalias;

			if (buddyList.ContainsKey (Format(other))) {
				old = (ImBuddy)buddyList[Format(other)];
				if (old.Alias == String.Empty && alias != String.Empty)
					old.Alias = alias;
				if (old.BuddyIconLocation == String.Empty && iconlocation == String.Empty) {
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
			AddBuddy (buddy, null);
		}

		public ImBuddy Search (string buddy) {
			return (ImBuddy)buddyList[Format(buddy)];
		}
	}

	/////////////////////////////////////////////////////////////

	public class KopeteBuddyListReader : ImBuddyListReader {

		string buddyListPath;
		string buddyListDir;
		DateTime buddyListLastWriteTime;
		uint timeoutId;

		public KopeteBuddyListReader ()
		{
			buddyListDir = Path.Combine (PathFinder.HomeDir, ".kde/share/apps/kopete");
			buddyListPath = Path.Combine (buddyListDir, "contactlist.xml");
			
			if (File.Exists (buddyListPath))
				Read ();

			// Poll the file once every minute
			timeoutId = GLib.Timeout.Add (60000, new GLib.TimeoutHandler (ReadTimeoutHandler));
		}

		~KopeteBuddyListReader ()
		{
			if (timeoutId > 0)
				GLib.Source.Remove (timeoutId);
		}

		private bool ReadTimeoutHandler ()
		{
			if (File.Exists (buddyListPath))
				Read ();

			return true;
		}

		private string Format (string name) {
			return name.ToLower ().Replace (" ", "");
		}

		override public void Read ()
		{
			// If the file hasn't changed, don't do anything.
			DateTime last_write = File.GetLastWriteTime (buddyListPath);
			if (last_write == buddyListLastWriteTime)
				return;

			buddyListLastWriteTime = last_write;

			buddyList = new Hashtable ();

			try {
				XmlDocument accounts = new XmlDocument ();
				accounts.Load (buddyListPath);
				
				// Find all xml contact nodes in the contact list
				foreach (XmlNode contact in accounts.SelectNodes ("//meta-contact"))
					AddContact (contact);
			} catch (Exception ex) {
				Logger.Log.Error (ex, "Caught exception while trying to parse Kopete contact list:");
			}
		}

		private void AddContact (XmlNode contact) 
		{
			string protocol, owner, other, alias, iconlocation, iconchecksum;
			
			protocol = String.Empty;
			owner = String.Empty;
			other = String.Empty;
			alias = String.Empty;
			iconlocation = String.Empty;
			iconchecksum = String.Empty;

			// For each and every meta-contact, there can be multiple 
			// buddy information entries if we have a contact added on
			// multiple protocols. Loop through them.

 			foreach (XmlNode plugin_node in contact.SelectNodes ("plugin-data")) {
				// Determin the protocol
				XmlAttribute plugin_id_attr = plugin_node.Attributes ["plugin-id"];
				protocol = plugin_id_attr.Value.Substring (0, plugin_id_attr.Value.Length-8).ToLower ();
				DebugPrint ("Protocol=" + protocol);

				// Fetch all the buddy properties
				foreach (XmlNode plugin_data_node in plugin_node.SelectNodes ("plugin-data-field")) { 
					switch (plugin_data_node.Attributes ["key"].Value) {
					case "contactId":
						other = plugin_data_node.InnerText;
						DebugPrint ("Screen=" + other);
						break;
					case "accountId":
						owner = plugin_data_node.InnerText;
						DebugPrint ("Account=" + owner);
						break;
					case "displayName":
						alias = plugin_data_node.InnerText;
						DebugPrint ("Alias=" + alias);
						break;
					}
				}
				
				// Replace any earlier buddies with the same screenname
				// FIXME: Not safe since we can have the same screenname on different accounts.
				if (buddyList.ContainsKey (Format(other)))
					buddyList.Remove (Format(other));
				
				buddyList.Add (Format(other), new ImBuddy (protocol, owner, Format(other), alias, iconlocation, iconchecksum));
			}
		}
		

		public ImBuddy Search (string buddy) {
			return (ImBuddy)buddyList[Format(buddy)];
		}
	}
}
