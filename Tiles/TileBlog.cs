//
// TileBlog.cs
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

	[HitFlavor (Name="Blogs", Rank=800, Emblem="emblem-blog.png", Color="#f5f5fe",
		    Type="Blog")]
	public class TileBlog : TileFromHitTemplate {
		public TileBlog (Hit _hit) : base (_hit, "template-blog.html")
		{
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

			// Hacky: if an image exists for this blog entry,
			// load it into a pixbuf to find the size and
			// scale the image if it is too big.
			Stream fs = ImageStream ();
			if (fs != null) {
				string height = "";
				Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (fs);
				if (pixbuf.Width > pixbuf.Height) {
					if (pixbuf.Width > 48)
						height = "width=\"48\"";
				} else {
					if (pixbuf.Height > 48)
						height = "height=\"48\"";
				}
				
				Template["height"] = height;
				fs.Close ();
				
				
				Template["Icon"] = Images.GetHtmlSource (Hit ["fixme:cachedimg"],
									 null);
			}
		}

		private Stream ImageStream ()
		{
			if (Hit == null)
				return null;

			string path = Hit ["fixme:cachedimg"];

			if (path == null || ! File.Exists (path))
				return null;

			return new FileStream (path,
					       FileMode.Open,
					       FileAccess.Read,
					       FileShare.Read);
		}
	}
}
