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
	public class TileMailMessage : TileFromTemplate {

		Hit hit;

		public TileMailMessage (Hit _hit) : base ("template-mail-message.html")
		{
			hit = _hit;
		}

		override protected string ExpandKey (string key)
		{
			bool sent = (hit ["fixme:isSent"] != null);
			string str;

			switch (key) {
			case "Subject":
				str = hit ["fixme:subject"];
				if (str == null)
					str = "<i>No Subject</i>";
				if (hit ["_IsDeleted"] != null)
					str = "<strike>" + str + "</strike>";
				return str;
				
			case "Folder":
				return hit ["fixme:folder"];

			case "ToFrom":
				return sent ? "To" : "From";

			case "Who":
				return sent ? hit ["fixme:to"] : hit ["fixme:from"];

			case "When":
				str = sent ? hit ["fixme:sentdate"] : hit ["fixme:received"];
				return BU.StringFu.DateTimeToFuzzy (BU.StringFu.StringToDateTime (str));
			}

			return null;
		}
		
		private void OpenMessage ()
		{
			OpenHitWithDefaultAction (hit);
		}

		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Icon") {
				string icon;

				if (hit ["fixme:isAnswered"] != null)
					icon = "mail-replied.png";
				else if (hit ["fixme:isSeen"] != null)
					icon = "mail-read.png";
				else
					icon = "mail.png";

				ctx.Image (icon, new TileActionHandler (OpenMessage));

				if (hit ["fixme:isFlagged"] != null)
					ctx.Image ("flag-for-followup.png");

				if (hit ["fixme:hasAttachments"] != null)
					ctx.Image ("attachment.png");

				return true;
			}

			return base.RenderKey (key, ctx);
		}

	}
}
