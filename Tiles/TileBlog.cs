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

	[HitFlavor (Name="Feeds", Rank=800, Emblem="emblem-blog.png", Color="#f5f5fe",
		    Type="FeedItem")]
	public class TileBlog : TileFromHitTemplate 
	{
		public TileBlog (Hit _hit) : base (_hit, "template-blog.html")
		{
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

			string path = Hit ["fixme:cachedimg"];

			if (path != null && File.Exists (path))
				Template["Icon"] = Images.GetHtmlSource (path, null);
			else
				Template["Icon"] = Images.GetHtmlSource ("icon-blog", "text/html");
		}

		[TileAction]
		public override void Open ()
                {
			Gnome.Url.Show(Hit["fixme:itemuri"]);
		}
	}
}
