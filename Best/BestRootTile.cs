//
// BestRootTile.cs
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
using System.Collections;

using Beagle;

namespace Best {

	public class BestRootTile : Tile {

		Hashtable tileTable = new Hashtable ();
		TileHitCollection hitTile = new TileHitCollection ("Foo", null);

		public void Open ()
		{
			tileTable = new Hashtable ();
		}

		public void Add (Hit hit)
		{
			TileHitCollection tile;

			tile = (TileHitCollection) tileTable [hit.Type];

			if (tile == null) {
				string name = null;
				string icon = null;
				switch (hit.Type) {
				case "Contact":
					name = "People";
					icon = "person.png";
					break;
				case "File":
					if (hit.MimeType.StartsWith ("image/")) {
						name = "Pictures";
						icon = "document.png";
					} else if (hit.MimeType == "audio/x-mp3") {
						name = "Music";
						icon = "music.png";
					} else {
						name = "Files";
						icon = "document.png";
					}
					break;
				case "Google":
					name = "Google";
					icon = "icon-web.png";
					break;
				case "IMLog":
					name = "Conversations";
					icon = "gnome-gaim.png";
					break;
				case "MailMessage":
					name = "Email";
					icon = "mail-message-icon.png";
					break;
				case "WebHistory":
					name = "Web";
					icon = "icon-web.png";
					break;
				}

				tile = new TileHitCollection (name, icon);
				tileTable [hit.Type] = tile;
			}

			if (tile != null) {
				tile.Add (hit);
				Changed ();
			}
		}

		public void Close ()
		{

		}

		override public void Render (TileRenderContext ctx)
		{
			ArrayList array = new ArrayList ();

			foreach (TileHitCollection tile in tileTable.Values)
				array.Add (tile);

			array.Sort ();

			foreach (TileHitCollection tile in array) {
				ctx.WriteLine ("MaxScore={0}", tile.MaxScore);
				ctx.Tile (tile);
			}
		}
	}
}
