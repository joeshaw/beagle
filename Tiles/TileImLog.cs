//
// TileImLog.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.IO;
using System.Collections;
using BU = Beagle.Util;

namespace Beagle.Tile {

	[HitFlavor (Name="Conversations", Rank=900, Emblem="emblem-im-log.png", Color="#e5f5ef",
		    Type="IMLog")]
	public class TileImLog : TileFromHitTemplate {
		private BU.ImBuddy buddy = null;
		private static BU.GaimBuddyListReader list = null;
		
		public TileImLog (Hit _hit) : base (_hit,
						    "template-im-log.html")
		{
			if (list == null) {
				list = new BU.GaimBuddyListReader ();
			}

			buddy = list.Search (Hit ["fixme:speakingto"]);
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

			Template["nice_duration"] = "(" +
				BU.StringFu.DurationToPrettyString (
					   BU.StringFu.StringToDateTime (Hit ["fixme:endtime"]),
					   BU.StringFu.StringToDateTime (Hit ["fixme:starttime"])) + ")";
			if (Template ["nice_duration"] == "()")
				Template ["nice_duration"] = "";
			
#if false
			Template["snippet"] = getSnippet ();
#endif
			if (buddy != null && buddy.Alias != "")
				Template["speakingalias"] = buddy.Alias;
			else 
				Template["speakingalias"] = Hit["fixme:speakingto"];

			if (buddy != null && buddy.BuddyIconLocation != "") {
				string homedir = Environment.GetEnvironmentVariable ("HOME");
				string fullpath = Path.Combine (homedir, ".gaim");
				fullpath = Path.Combine (fullpath, "icons");
				fullpath = Path.Combine (fullpath, buddy.BuddyIconLocation);

				if (File.Exists (fullpath)) {
					Template["Icon"] = Images.GetHtmlSource (fullpath,
										 BU.GnomeIconLookup.GetFileMimeType (fullpath));
				} else {
					Template["Icon"] = Images.GetHtmlSourceForStock ("gnome-gaim", 48);
				}
			} else {
				Template["Icon"] = Images.GetHtmlSourceForStock ("gnome-gaim", 48);
			}
		}

		private string HighlightOrNull (string haystack, string [] needles)
		{
			string [] highlight_start_list = {"<font color=red>",
							  "<font color=orange>",
							  "<font color=green>",
							  "<font color=blue>"};

			string highlight_end   = "</font>";

			string hili = haystack;
			bool dirty = false;
			int hicolor = -1;

			foreach (string needle in needles) {
				string h_up = hili.ToUpper ();
				string n_up = needle.ToUpper ();

				hicolor = (hicolor + 1) % 4;
				string highlight_start = highlight_start_list [hicolor];
				
				int ni = h_up.IndexOf (n_up);
				if (ni == -1)
					continue;

				while (ni != -1) {
					hili = hili.Insert (ni, highlight_start);
					hili = hili.Insert (ni + highlight_start.Length + needle.Length, highlight_end);

					h_up = hili.ToUpper ();
					dirty = true;

					ni = h_up.IndexOf (n_up, ni + highlight_start.Length + needle.Length + highlight_end.Length);
				}
			}

			if (dirty) {
				return hili;
			}
			else
				return null;
		}

		private string getSnippet ()
		{
			ICollection logs = BU.GaimLog.ScanLog (new FileInfo (Hit ["fixme:file"]));

			string snip = "";
			
			foreach (BU.ImLog log in logs) {
					foreach (BU.ImLog.Utterance utt in log.Utterances) {
						// FIXME: Query.Text is broken for me
#if false
						string s = HighlightOrNull (utt.Text, Query.Text);
#else
						string s = utt.Text;
#endif
						if (s != null) {
							if (snip != "")
								snip += " ... ";							    
							snip += s;
						}
						
					}
			}

			return snip.Substring (0, System.Math.Min (256, snip.Length));

		}

		// FIXME: We really should not even display the "Send
		// mail" action unless we know we have a contact for
		// this person.
		[TileAction]
		public void SendMailForIm ()
		{
			Evolution.Book addressbook = null;

			// Connect to the Evolution addressbook.
			try {
				addressbook = Evolution.Book.NewSystemAddressbook ();
				addressbook.Open (true);
			} catch (Exception e) {
				Console.WriteLine ("\nCould not open Evolution addressbook:\n" + e);
				return;
			}

			// Do a search.
			// FIXME: This should match the contact IM protocol to
			// the protocol of the IM conversation.
			string qstr =
				String.Format ("(or " +
					           "(is \"im_aim\" \"{0}\") " + 
					           "(is \"im_yahoo\" \"{0}\") " +
					           "(is \"im_msn\" \"{0}\") " + 
					           "(is \"im_icq\" \"{0}\") " +
					           "(is \"im_jabber\" \"{0}\") " + 
					           "(is \"im_groupwise\" \"{0}\") " +
					       ")",
					       Hit ["fixme:speakingto"]);
			Evolution.BookQuery query = Evolution.BookQuery.FromString (qstr);
			Evolution.Contact [] matches = addressbook.GetContacts (query);
			foreach (Evolution.Contact c in matches) {
				Console.WriteLine ("Mail Match: {0} <{1}>", c.FullName, c.Email1);
				SendMailToAddress (c.Email1, null);
				return;
			}
		}

		[TileAction]
		public void SendIm ()
		{
			// FIXME: The hit should really have a field
			// for the IM protocol that was used.  This is
			// an ugly hack to check whether the
			// conversation took place over aim.
			if (Hit ["fixme:file"].IndexOf ("logs/aim") == -1)
				return;
			
			SendImAim (Hit ["fixme:speakingto"]);
		}
	}
}
