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

using BU = Beagle.Util;

namespace Beagle.Tile {

	[HitFlavor (Name="Email", Rank=1100, Emblem="emblem-mail-message.png", Color="#f5f5f5",
		    Type="MailMessage")]
	public class TileMailMessage : TileFromHitTemplate {
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
	}
}
