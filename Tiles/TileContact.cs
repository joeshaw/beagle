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

using BU = Beagle.Util;

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

			bool use_generic_icon = true;

			byte[] data = (byte[]) Hit.GetData ("Photo");

			if (data != null) {
				Console.WriteLine ("Got photo for {0}", Hit.Uri);
				string size_adjustment = "";

				// bad hack to scale the image 
				MemoryStream stream = new MemoryStream (data);
				Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (stream);
				Console.WriteLine ("Build pixbuf {0} {1}x{2}", pixbuf, pixbuf.Width, pixbuf.Height);
				if (pixbuf.Width > pixbuf.Height) {
					if (pixbuf.Width > 80)
						size_adjustment = "width=\"80\"";
				} else {
					if (pixbuf.Height > 80)
						size_adjustment = "height=\"80\"";
				}
				stream.Close ();

				string data_chunk;
				data_chunk = Images.GetHtmlSource (data, "image/jpeg");
				if (data_chunk.Length  < 65000) {
					Template["size_adjustment"] = size_adjustment;
					Template["Icon"] = data_chunk;
					use_generic_icon = false;
				}
			}

			if (use_generic_icon) {
				Template["size_adjustment"] = "";
				Template["Icon"] = default_contact_icon_data;
			}

			if (Hit["fixme:ImAim"] != null)
				Template["CanSendIm"] = "";
		}

		[TileAction]
		public void SendMailEmail1 ()
		{
			SendMailToAddress (Hit ["fixme:Email1"], null);
		}

		[TileAction]
		public void SendIm ()
		{
			SendImAim (Hit["fixme:ImAim"]);
		}
	}
}
	
