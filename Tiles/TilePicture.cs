//
// TilePicture.cs
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

	[HitFlavor (Name="Pictures", Emblem="emblem-picture.png", Color="#f5f5fe",
		    Type="File", MimeType="image/*")]
	public class TilePicture : TileFromTemplate {

		Hit hit;

		public TilePicture (Hit _hit) : base ("template-picture.html")
		{
			hit = _hit;
		}

		override public bool HandleUrlRequest (string url, Gtk.HTMLStream stream)
		{
			// Try to short-circuit the request for document.png,
			// replacing it w/ the appropriate icon for the hit's mime type.
			if (url == "document.png") {
				Stream icon = new FileStream (hit.Path, FileMode.Open, FileAccess.Read);
				byte[] buffer = new byte [8192];
				int n;
				while ((n = icon.Read (buffer, 0, 8192)) != 0)
					stream.Write (buffer, n);
				return true;
			}

			return false;
		}

		override protected string ExpandKey (string key)
		{
			switch (key) {
			case "FileName":
				return hit.FileName;

			case "LastWriteTime":
				return BU.StringFu.DateTimeToFuzzy (hit.FileInfo.LastWriteTime);

			case "Length":
				return BU.StringFu.FileLengthToString (hit.FileInfo.Length);

			case "ColorType":
				// FIXME: This gets set for pngs, but we have to fake it
				// for jpegs
				if (hit ["_ColorType"] != null)
					return hit ["_ColorType"];
				return null;
			}

			return hit [key];
		}
		
		private void OpenPicture ()
		{
			Console.WriteLine ("Open {0}", hit.Uri);
			BU.GnomeVFSMimeApplication app;
			app = BU.GnomeIconLookup.GetDefaultAction (hit.MimeType);

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = app.command;
			p.StartInfo.Arguments = hit.Path;
			try {
				p.Start ();
			} catch { }
		}

		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Thumbnail") {
				ctx.Image ("document.png", 64, -1, new TileActionHandler (OpenPicture));
				return true;
			}

			return base.RenderKey (key, ctx);
		}

	}
}
