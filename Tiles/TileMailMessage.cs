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

		private static string GetHitProperty (Hit hit, string name)
		{
			// FIXME: We should handle this case better, but
			// for now, if we match an attachment, we just want
			// to display the properties for the parent message.
			if (hit.ParentUri == null)
				return hit [name];
			else
				return hit ["parent:" + name];
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

                        bool sent = (GetHitProperty (Hit, "fixme:isSent") != null);

			string str;

			str = GetHitProperty (Hit, "dc:title");

			if (str == null)
				str = String.Format ("<i>{0}</i>", Catalog.GetString ("No Subject"));

			if (Hit.ParentUri != null)
				str += " [" + Catalog.GetString ("email attachment") + "]";

			if (GetHitProperty (Hit, "_IsDeleted") != null)
				str = "<strike>" + str + "</strike>";
			Template["Subject"] = str;

			Template["ToFrom"] = sent ? Catalog.GetString ("To") : Catalog.GetString ("From");

			// Limit the number of recipients to 3, so the
			// tile doesn't look terrible.
			if (sent) {
				string[] values = Hit.GetProperties ("fixme:to");

				if (values != null) {
					StringBuilder sb = new StringBuilder ();
					int i;

					for (i = 0; i < 3 && i < values.Length; i++) {
						if (i != 0)
							sb.Append (", ");

						sb.Append (values [i]);
					}

					if (i < values.Length)
						sb.Append (", et al");

					Template["Who"] = sb.ToString ();
				}
			} else
				Template["Who"] = GetHitProperty (Hit, "fixme:from");

			Template["Folder"] = GetHitProperty (Hit, "fixme:folder");
			Template["Account"] = GetHitProperty (Hit, "fixme:account");
			Template["SentReceived"] = sent ? Catalog.GetString ("Sent") : Catalog.GetString ("Received");
			Template["When"] = sent ? GetHitProperty (Hit, "fixme:sentdate") : GetHitProperty (Hit, "fixme:received");

			string icon;
			if (GetHitProperty (Hit, "fixme:isAnswered") != null)
				icon = Images.GetHtmlSourceForStock ("stock_mail-replied", 48);
			else if (GetHitProperty (Hit, "fixme:isSeen") != null)
				icon = Images.GetHtmlSourceForStock ("stock_mail-open", 48);
			else
				icon = Images.GetHtmlSourceForStock ("stock_mail", 48);

			Template["Icon"] = icon;
			
			if (GetHitProperty (Hit, "fixme:isFlagged") != null)
				Template["FollowupIcon"] = Images.GetHtmlSourceForStock ("stock_mail-priority-high", 16);
			if (GetHitProperty (Hit, "fixme:hasAttachments") != null)
				Template["AttachmentIcon"] = Images.GetHtmlSourceForStock ("stock_attach", 16);

#if ENABLE_EVO_SHARP
			GetImNames (Template["Who"]);

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


#if ENABLE_EVO_SHARP

		private void GetImNames (string who)
		{
			if (who == null || who == "")
				return;

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

			if (query == null)
				return;

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
		}
#endif

		[TileAction]
		public override void Open () 
		{
			string uri_str;

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";

			if (Hit.ParentUriAsString != null)
				uri_str = Hit.ParentUriAsString;
			else
				uri_str = Hit.UriAsString;

			p.StartInfo.Arguments  = "'" + uri_str + "'";

			try {
				p.Start ();
			} catch (System.ComponentModel.Win32Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
		}

		[TileAction]
		public void Mail ()
		{
                        bool sent = (GetHitProperty (Hit, "fixme:isSent") != null);
			string address = sent ? GetHitProperty (Hit, "fixme:to") : GetHitProperty (Hit, "fixme:from");
			
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

		[TileAction]
		public void Reply ()
		{
			string uri_str;

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";

			if (Hit.ParentUriAsString != null)
				uri_str = Hit.ParentUriAsString;
			else
				uri_str = Hit.UriAsString;
			
			p.StartInfo.Arguments = String.Format ("'{0};reply=sender'", uri_str);

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
