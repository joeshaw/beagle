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

namespace Beagle.Tile {

	[HitFlavor (Name="Folders", Emblem="emblem-folder.png", Color="#f5f5fe",
		    Uri="file://*", MimeType="inode/directory")]
	public class TileFolder : TileFromTemplate {

		Hit hit;

		public TileFolder (Hit _hit) : base ("template-folder.html")
		{
			hit = _hit;
		}

		override protected string ExpandKey (string key)
		{
			switch (key) {
			case "FileName":
				return hit.FileName;

			case "LastWriteTime":
				return BU.StringFu.DateTimeToFuzzy (hit.FileSystemInfo.LastWriteTime);

			case "Contents":
				int n = hit.DirectoryInfo.GetFileSystemInfos().Length;
				if (n == 0)
					return "Empty";
				else if (n == 1)
					return "Contains 1 Item";
				else
					return "Contains " + n + " Items";
			}

			return null;
		}
		
		private void OpenFolder ()
		{
			hit.OpenWithDefaultAction ();
		}

		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Icon") {
				ctx.Image ("icon-folder.png",
					   new TileActionHandler (OpenFolder));
				return true;
			}

			return base.RenderKey (key, ctx);
		}

	}
}
