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
using System.Diagnostics;
using System.Text.RegularExpressions;
using BU = Beagle.Util;

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
				str = "<i>No Subject</i>";
			if (Hit ["_IsDeleted"] != null)
				str = "<strike>" + str + "</strike>";
			Template["Subject"] = str;

			Template["ToFrom"] = sent ? "To" : "From";
			Template["Who"] = sent ? Hit ["fixme:to"] : Hit ["fixme:from"];
			Template["SentReceived"] = sent ? "Sent" : "Received";
			Template["When"] = sent ? Hit ["fixme:sentdate"] : Hit ["fixme:received"];

			string icon;
			if (Hit ["fixme:isAnswered"] != null)
				icon = Images.GetHtmlSourceForStock ("stock_mail-replied", 
								     48);
			else if (Hit ["fixme:isSeen"] != null)
				icon = Images.GetHtmlSourceForStock ("stock_mail-open",
								     48);
			else
				icon = Images.GetHtmlSourceForStock ("stock_mail",
								     48);

			Template["Icon"] = icon;
			if (Hit ["fixme:isFlagged"] != null)
				Template["FollowupIcon"] = Images.GetHtmlSource ("flag-for-followup.png", "image/png");
			if (Hit ["fixme:hasAttachments"] != null)
				Template["AttachmentIcon"] = Images.GetHtmlSource ("attachment.png", "image/png");

			GetImNames (Template["Who"]);

			if (aim_name != null)
				Template["CanSendIm"] = "";
		}

		static bool ebook_failed = false;

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

			p.Start ();
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

			p.Start ();			
		}

		[TileAction]
		public void SendIm () 
		{
			if (aim_name != null) 
				SendImAim (aim_name);
			if (groupwise_name != null) 
				SendImGroupwise (aim_name);
			if (icq_name != null) 
				SendImIcq (aim_name);
			if (jabber_name != null) 
				SendImJabber (aim_name);
			if (msn_name != null) 
				SendImMsn (aim_name);
			if (yahoo_name != null) 
				SendImYahoo (aim_name);
		}
	}
}
