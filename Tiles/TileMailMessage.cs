//
// TileMailMessage.cs
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
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using BU = Beagle.Util;
using GMime;
using Mono.Posix;

namespace Beagle.Tile {

	[HitFlavor (Name="Email", Rank=1100, Emblem="emblem-mail-message.png", Color="#f5f5f5",
		    Type="MailMessage")]
	public class TileMailMessage : TileFromHitTemplate {

#if ENABLE_EVO_SHARP
		string aim_name;
		string groupwise_name;
		string icq_name;
		string jabber_name;
		string msn_name;
		string yahoo_name;

		static bool ebook_failed = false;
#endif

		public TileMailMessage (Hit _hit) : base (_hit,
							  "template-mail-message.html")
		{
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

                        bool sent = (Hit ["fixme:isSent"] != null);

			string str;

			str = Hit ["fixme:subject"];
			if (str == null)
				str = Catalog.GetString ("<i>No Subject</i>");
			if (Hit ["_IsDeleted"] != null)
				str = "<strike>" + str + "</strike>";
			Template["Subject"] = str;

			Template["ToFrom"] = sent ? Catalog.GetString ("To") : Catalog.GetString ("From");

			if (sent) {
				// Limit the number of recipients to 3, so the
				// tile doesn't look terrible.
				ICollection list = InternetAddress.ParseString (Hit ["fixme:to"]);

				if (list.Count <= 3)
					Template["Who"] = Hit["fixme:to"];
				else {
					StringBuilder sb = new StringBuilder ();

					int count = 0;
					foreach (InternetAddress ia in list) {
						sb.Append (ia.ToString (false));
						sb.Append (", ");

						++count;
						if (count == 3)
							break;
					}

					sb.Append ("et al");

					Template["Who"] = sb.ToString ();
				}
			} else
				Template["Who"] = Hit ["fixme:from"];

			Template["Folder"] = Hit ["fixme:folder"];
			Template["Account"] = Hit ["fixme:account"];
			Template["SentReceived"] = sent ? Catalog.GetString ("Sent") : Catalog.GetString ("Received");
			Template["When"] = sent ? Hit ["fixme:sentdate"] : Hit ["fixme:received"];

			string icon;
			if (Hit ["fixme:isAnswered"] != null)
				icon = Images.GetHtmlSourceForStock ("stock_mail-replied", 48);
			else if (Hit ["fixme:isSeen"] != null)
				icon = Images.GetHtmlSourceForStock ("stock_mail-open", 48);
			else
				icon = Images.GetHtmlSourceForStock ("stock_mail", 48);

			Template["Icon"] = icon;
			if (Hit ["fixme:isFlagged"] != null)
				Template["FollowupIcon"] = Images.GetHtmlSourceForStock ("stock_mail-priority-high", 16);
			if (Hit ["fixme:hasAttachments"] != null)
				Template["AttachmentIcon"] = Images.GetHtmlSourceForStock ("stock_attach", 16);

			GetImNames (Template["Who"]);

#if ENABLE_EVO_SHARP
			if (aim_name != null)
				Template["CanSendIm"] = "";
#endif
		}

		private string GetEmail (string who)
		{
			Regex re = new Regex (@".*<(?<email>.*)>");
			MatchCollection matches = re.Matches (who);
			foreach (Match match in matches) {
				if (match.Length != 0) {
					return match.Groups["email"].ToString ();
				}
			}
			
			return who;
		}

		private void GetImNames (string who)
		{
#if ENABLE_EVO_SHARP
			Evolution.Book addressbook = null;

			if (ebook_failed)
				return;

			try {
				addressbook = Evolution.Book.NewSystemAddressbook ();
				addressbook.Open (true);
			} catch (Exception e) {
				Console.WriteLine ("\nCould not open Evolution addressbook:\n" + e);
				ebook_failed = true;
				return;
			}

			string email = GetEmail (who);

			System.Console.WriteLine ("Looking for im name for {0}",
						  email);
			System.Console.WriteLine ("FIXME: This query is using the Evolution addressbook instead of querying Beagle directly.  This is slow, dumb, etc.");

			string qstr = 
				String.Format ("(is \"email\" \"{0}\")", email);

			Evolution.BookQuery query = Evolution.BookQuery.FromString (qstr);
			Evolution.Contact[] matches = addressbook.GetContacts (query);
			foreach (Evolution.Contact c in matches) {
				if (c.ImAim.Length > 0)
					aim_name = c.ImAim[0];
				if (c.ImIcq.Length > 0)
					icq_name = c.ImIcq[0];
				if (c.ImJabber.Length > 0)
					jabber_name = c.ImJabber[0];
				if (c.ImMsn.Length > 0)
					msn_name = c.ImMsn[0];
				if (c.ImYahoo.Length > 0)
					yahoo_name = c.ImYahoo[0];
				if (c.ImGroupwise.Length > 0)
					groupwise_name = c.ImGroupwise[0];
			}
#endif
		}

		[TileAction]
		public override void Open () 
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";
			p.StartInfo.Arguments = "'" + Hit.Uri + "'";

			try {
				p.Start ();
			} catch (System.ComponentModel.Win32Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
				
		}

		[TileAction]
		public void Mail ()
		{
                        bool sent = (Hit ["fixme:isSent"] != null);
			string address = sent ? Hit ["fixme:to"] : Hit ["fixme:from"];
			
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";
			p.StartInfo.Arguments = "'mailto:" + address + "'";

			try {
				p.Start ();			
			} catch (System.ComponentModel.Win32Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
		}

#if ENABLE_EVO_SHARP
		[TileAction]
		public void SendIm () 
		{
			if (aim_name != null) 
				SendImAim (aim_name);
			if (groupwise_name != null) 
				SendImGroupwise (groupwise_name);
			if (icq_name != null) 
				SendImIcq (icq_name);
			if (jabber_name != null) 
				SendImJabber (jabber_name);
			if (msn_name != null) 
				SendImMsn (msn_name);
			if (yahoo_name != null) 
				SendImYahoo (yahoo_name);
		}
#endif
	}
}
