//
// TileFile.cs
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

	[HitFlavor (Name="Files", Rank=300, Emblem="emblem-file.png", Color="#f5f5fe",
		    Type="File"),
	 HitFlavor (Name="Files", Rank=300, Emblem="emblem-file.png", Color="#f5f5fe",
		    Uri="file://*")]
	public class TileFile : TileFromHitTemplate {
		public TileFile (Hit _hit) : base (_hit,
						   "template-file.html")
		{
		}

		public TileFile (Hit _hit, string template) : base (_hit,
								    template)
		{
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

			if (Hit.FileInfo == null) {
				Console.WriteLine ("FileInfo is null");
				return;
			}

			Gtk.IconSize size = (Gtk.IconSize) 48;
			string path = BU.GnomeIconLookup.LookupMimeIcon (Hit.MimeType,
									 size);
			string icon = Images.GetHtmlSource (path,
								 BU.GnomeIconLookup.GetFileMimeType (path));

			if (icon != null)
				Template["Icon"] = icon;
			else
				Template ["Icon"] = Images.GetHtmlSource ("document", "image/png");
		}

		[TileAction]
		public override void Open ()
		{
			OpenFromMime (Hit);
		}

		[TileAction]
		public void SendTo ()
		{
			SendFile (String.Format ("{0}", Hit.Uri.LocalPath));
		}

		[TileAction]
		public void Reveal ()
		{
			OpenFolder (Path.GetDirectoryName (Hit.Uri.LocalPath));
		}
	}
}
	
