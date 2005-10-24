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
using System.Diagnostics;
using Beagle.Util;

using Mono.Unix;

namespace Beagle.Tile {

	[HitFlavor (Name="Conversations", Rank=900, Emblem="emblem-im-log.png", Color="#e5f5ef",
		    Type="IMLog")]
	public class TileImLog : TileFromHitTemplate {

#if ENABLE_EVO_SHARP
		private static Hashtable buddy_emails = new Hashtable ();
#endif
		
		private string email = null;
		private string speaking_alias = null;

		public TileImLog (Hit _hit) : base (_hit,
						    "template-im-log.html")
		{
			if (Hit ["fixme:speakingto_alias"] != null)
				email = GetEmailForName (Hit ["fixme:speakingto_alias"]);
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

			Template["nice_duration"] = "(" +
				StringFu.DurationToPrettyString (
					StringFu.StringToDateTime (Hit ["fixme:endtime"]),
					StringFu.StringToDateTime (Hit ["fixme:starttime"])) + ")";
			if (Template ["nice_duration"] == "()")
				Template ["nice_duration"] = "";

			if (email != null)
				Template ["SendMailAction"] = Catalog.GetString ("Send Mail");

			// FIXME: This is a temporary hack until gaim supports other protocols than AIM via gaim-remote
			if (Hit ["fixme:protocol"] == "aim")
				Template ["SendIMAction"] = Catalog.GetString ("Send IM");
			
			speaking_alias = (Hit ["fixme:speakingto_alias"] != null) ? Hit ["fixme:speakingto_alias"] : Hit ["fixme:speakingto"];

			// FIXME: Hack to figure out if the conversation is taken place in a chat room
			if (Hit["fixme:speakingto"].EndsWith (".chat"))
				Template["title"] = String.Format (Catalog.GetString ("Conversation in {0}"), speaking_alias.Replace(".chat",""));
			else
				Template["title"] = String.Format (Catalog.GetString ("Conversation with {0}"), speaking_alias);

			if (Hit ["fixme:speakingto_icon"] != null && File.Exists (Hit ["fixme:speakingto_icon"]))
				Template["Icon"] = StringFu.PathToQuotedFileUri (Hit ["fixme:speakingto_icon"]);
		        else
				Template["Icon"] = Images.GetHtmlSource ("gnome-gaim.png", "image/png");
			
#if ENABLE_GALAGO
			if (Hit ["fixme:protocol"] == "aim") {
				string status = GalagoTools.GetPresence (Hit ["fixme:protocol"], Hit["fixme:speakingto"]);
				if (status != null && status != "")
					Template ["Presence"] = status;
			}
#endif
		}

#if ENABLE_EVO_SHARP
		static bool ebook_failed = false;
#endif

		private string GetEmailForName (string name)
		{
#if ENABLE_EVO_SHARP
			if (name == null || name == "")
				return null;

			Evolution.Book addressbook = null;


			// If we've previously failed to open the
			// addressbook, don't keep trying.
			if (ebook_failed)
				return null;

			// We keep a little cache so we don't have to query
			// the addressbook too often.
			if (buddy_emails.Contains (name)) {
				string str = (string)buddy_emails[name];
				return str != "" ? str : null;
			}

			// Connect to the Evolution addressbook.
			try {
				addressbook = Evolution.Book.NewSystemAddressbook ();
				addressbook.Open (true);
			} catch (Exception e) {
				Console.WriteLine ("\nCould not open Evolution addressbook:\n" + e);
				ebook_failed = true;
				return null;
			}

			// Do a search.
			string qstr =
				String.Format ("(is \"full_name\" \"{0}\")", name);

			Evolution.BookQuery query = Evolution.BookQuery.FromString (qstr);
			Evolution.Contact [] matches = addressbook.GetContacts (query);
			foreach (Evolution.Contact c in matches) {
				Console.WriteLine ("FIXME: querying the evolution addressbook instead of using Lucene, this is slow and dumb");
				Console.WriteLine ("Got match: {0} {1}", c.FullName, c.Email1);
				if (c.Email1 != null) {
					buddy_emails[name] = c.Email1;
					return c.Email1;
				}
			}
			buddy_emails[name] = "";
#endif

			return null;
		}

		[TileAction]
		public override void Open ()
		{
			//FIXME: At least for now
			Process p = new Process ();
			p.StartInfo.UseShellExecute = true;
			p.StartInfo.FileName = "beagle-imlogviewer";
			p.StartInfo.Arguments = String.Format ("--client \"{0}\" --highlight-search \"{1}\" {2}",
					Hit ["fixme:client"], Query.QuotedText, Hit.Uri.LocalPath);

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
		}

		[TileAction]
		public void SendMailForIm ()
		{
			if (email != null)
				SendMailToAddress (email, null);
		}

		[TileAction]
		public void SendIm ()
		{
			SendImAim (Hit ["fixme:speakingto"]);
		}
	}
}
