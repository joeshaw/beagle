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

namespace Beagle {

	[HitFlavor (Name="Email", Emblem="emblem-mail-message.png", Color="#f5f5f5",
		    Type="MailMessage")]
	public class TileMailMessage : TileFromTemplate {

		Hit hit;

		public TileMailMessage (Hit _hit) : base ("template-mail-message.html")
		{
			hit = _hit;
		}

		override protected string ExpandKey (string key)
		{
			bool sent = (hit ["_IsSent"] != null);
			string str;

			switch (key) {
			case "Subject":
				str = hit ["Subject"];
				if (str == null)
					str = "<i>No Subject</i>";
				if (hit ["_IsDeleted"] != null)
					str = "<strike>" + str + "</strike>";
				return str;
				
			case "Folder":
				return hit ["Folder"];

			case "ToFrom":
				return sent ? "To" : "From";

			case "Who":
				return sent ? hit ["To"] : hit ["From"];

			case "When":
				str = sent ? hit ["SentDate"] : hit ["Received"];
				return BU.StringFu.DateTimeToFuzzy (BU.StringFu.StringToDateTime (str));
			}

			return null;
		}
		
		private void OpenMessage ()
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution-1.5";
			p.StartInfo.Arguments = hit.Uri;
			try {
				p.Start ();
			} catch { }
		}

		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Icon") {
				string icon;

				if (hit ["_IsAnswered"] != null)
					icon = "mail-replied.png";
				else if (hit ["_IsSeen"] != null)
					icon = "mail-read.png";
				else
					icon = "mail.png";

				ctx.Image (icon, new TileActionHandler (OpenMessage));

				if (hit ["_IsFlagged"] != null)
					ctx.Image ("flag-for-followup.png");

				if (hit ["_HasAttachments"] != null)
					ctx.Image ("attachment.png");

				return true;
			}

			return base.RenderKey (key, ctx);
		}

	}
}
