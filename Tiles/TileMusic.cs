//
// TileMusic.cs
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

	[HitFlavor (Name="Music", Emblem="emblem-music.png", Color="#f5f5fe",
		    Type="File", MimeType="audio/x-mp3")]
	public class TileMusic : TileFromTemplate {

		Hit hit;

		public TileMusic (Hit _hit) : base ("template-music.html")
		{
			hit = _hit;
		}

#if false
		override public bool HandleUrlRequest (string url, Gtk.HTMLStream stream)
		{
			// Try to short-circuit the request for document.png,
			// replacing it w/ the appropriate icon for the hit's mime type.
			if (url == "document.png") {
				Gtk.IconSize size = (Gtk.IconSize) 48;
				string path = BU.GnomeIconLookup.LookupMimeIcon (hit.MimeType,
										 size);
				if (path == null)
					return false;
				Stream icon = new FileStream (path, FileMode.Open, FileAccess.Read);
				byte[] buffer = new byte [8192];
				int n;
				while ((n = icon.Read (buffer, 0, 8192)) != 0)
					stream.Write (buffer, n);
				return true;
			}

			return false;
		}
#endif

		override protected string ExpandKey (string key)
		{
			switch (key) {
			case "FileName":
				return hit.FileName;

			case "LastWriteTime":
				return BU.StringFu.DateTimeToFuzzy (hit.FileInfo.LastWriteTime);

			case "Length":
				return BU.StringFu.FileLengthToString (hit.FileInfo.Length);
			}

			return hit [key];
		}
		
		private void OpenMusic ()
		{
			OpenHitWithDefaultAction (hit);
		}

		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Icon") {
				ctx.Image ("music.png", new TileActionHandler (OpenMusic));
				return true;
			}

			return base.RenderKey (key, ctx);
		}

	}
}
