//
// TileFolder.cs
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
using Mono.Posix;

namespace Beagle.Tile {

	[HitFlavor (Name="Folders", Rank=600, Emblem="emblem-folder.png", Color="#f5f5fe",
		    Uri="file://*", MimeType="inode/directory")]
	public class TileFolder : TileFromHitTemplate {
		public TileFolder (Hit _hit) : base (_hit, 
						     "template-folder.html")
		{
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();
			
			string str;
			int n = Hit.DirectoryInfo.GetFileSystemInfos().Length;
			if (n == 0)
				str = Catalog.GetString ("Empty");
			else if (n == 1)
				str = Catalog.GetString ("Contains 1 Item");
			else
				str = String.Format (Catalog.GetString ("Contains {0} Items"), n);
			Template["Contents"] = str;
		}

		[TileAction]
		public override void Open ()
		{
			OpenFromMime (Hit, "nautilus", "--no-desktop", true);
		} 
	}
}
