//
// TileContact.cs
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
using System.IO;

using Beagle.Util;

namespace Beagle.Tile {

	[HitFlavor (Name="People", Rank=1200, Emblem="emblem-contact.png", Color="#eeeebd",
		    Type="Contact")]
	public class TileContact : TileFromHitTemplate {

		static string default_contact_icon_data;

		static TileContact ()
		{
			default_contact_icon_data = Images.GetHtmlSource ("contact-icon.png", "image/png");
		}

		public TileContact (Hit _hit) : base (_hit,
						      "template-contact.html")
		{
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

			string photo_filename = Hit["Photo"];

			if (photo_filename != null) {
				System.Console.WriteLine ("photo: {0}", photo_filename);
				string height = "";
				
				try {
					// bad hack to scale the image 
					Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (photo_filename);
					if (pixbuf.Width > pixbuf.Height) {
						if (pixbuf.Width > 80)
							height = "width=\"80\"";
					} else {
						if (pixbuf.Height > 80)
							height = "height=\"80\"";
					}
				} catch {
				}

				Template["size_adjustment"] = height;
				Template["Icon"] = StringFu.PathToQuotedFileUri (photo_filename);
			} else {
				Template["size_adjustment"] = "";
				Template["Icon"] = default_contact_icon_data;
			}

			if (Hit["fixme:ImAim"] != null)
				Template["CanSendIm"] = "";

#if ENABLE_GALAGO
			if (Hit ["fixme:ImAim"] != null) {
				string status = GalagoTools.GetPresence ("aim", Hit ["fixme:ImAim"]);
				if (status != null && status != "")
					Template ["Presence"] = status;
			}
#endif
		}

		[TileAction]
		public void SendMailEmail1 ()
		{
			SendMailToAddress (Hit ["fixme:Email1"], null);
		}

		[TileAction]
		public void SendImAim ()
		{
			SendImAim (Hit["fixme:ImAim"]);
		}
	}
}
	
