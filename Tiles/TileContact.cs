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

namespace Beagle {

	[HitFlavor (Name="People", Emblem="person.png",
		    Type="Contact")]
	public class TileContact : TileFromTemplate {

		Hit hit;

		public TileContact (Hit _hit) : base ("template-contact.html")
		{
			hit = _hit;
		}

		override public bool HandleUrlRequest (string url, Gtk.HTMLStream stream)
		{
			// Try to short-circuit the request for contact-icon.png,
			// replacing it w/ a photo of the contact.
			if (url == "contact-icon.png") {
				byte[] data = (byte[]) hit.GetData ("Photo");
				if (data != null) {
					stream.Write (data, data.Length);
					return true;
				}
			}

			return false;
		}

		override protected string ExpandKey (string key)
		{
			return hit [key];
		}
		
		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Icon") {
				ctx.Image ("contact-icon.png");
				return true;
			}

			return base.RenderKey (key, ctx);
		}
	}
}
