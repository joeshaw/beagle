//
// TileMailMessage.cs
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

namespace Beagle {

	public class TileHitCollection : TileFromTemplate, IComparable {

		string name = null;
		string icon = null;

		public TileHitCollection (string _name, string _icon) : base ("template-hit-collection.html")
		{
			name = _name;
			icon = _icon;

			EnableInlineRendering ();
		}

		private class HitTilePair : IComparable {
			Hit hit;
			Tile tile;

			public HitTilePair (Hit _hit)
			{
				hit = _hit;
				tile = null;

				// FIXME: This shouldn't be hard-wired
				switch (hit.Type) {
					
				case "Contact":
					tile = new TileContact (hit);
					break;

				case "File":
					if (hit.MimeType.StartsWith ("image/"))
						tile = new TilePicture (hit);
					else if (hit.MimeType == "audio/x-mp3")
						tile = new TileMusic (hit);
					else
						tile = new TileFile (hit);
					break;

				case "Google":
					tile = new TileGoogle (hit);
					break;

				case "IMLog":
					tile = new TileImLog (hit);
					break;

				case "MailMessage":
					tile = new TileMailMessage (hit);
					break;

				case "WebHistory":
					tile = new TileWebHistory (hit);
					break;
				}
			}

			public Hit Hit {
				get { return hit; }
			}

			public Tile Tile {
				get { return tile; }
			}

			public int CompareTo (object obj)
			{
				HitTilePair other = (HitTilePair) obj;
				return hit.CompareTo (other.hit);
			}
		}

		private ArrayList hits = new ArrayList ();
		int firstDisplayed = 0;
		int maxDisplayed = 10;

		public float MaxScore {
			get {
				if (hits.Count == 0)
					return 0;
				HitTilePair pair = (HitTilePair) hits [0];
				return pair.Hit.Score;
			}
		}

		public int FirstDisplayed {
			get { return firstDisplayed; }
		}

		public int LastDisplayed {
			get { return Math.Min (firstDisplayed + maxDisplayed, hits.Count) - 1; }
		}

		public bool CanPageForward {
			get { return LastDisplayed != (hits.Count - 1); }
		}

		public void PageForward ()
		{
			firstDisplayed += maxDisplayed;
			if (firstDisplayed + maxDisplayed > hits.Count)
				firstDisplayed = hits.Count - maxDisplayed;
			if (firstDisplayed < 0)
				firstDisplayed = 0;
			Changed ();
		}

		public bool CanPageBack {
			get { return FirstDisplayed > 0; }
		}

		public void PageBack ()
		{
			firstDisplayed -= maxDisplayed;
			if (firstDisplayed < 0)
				firstDisplayed = 0;
			Changed ();
		}

		public void Clear ()
		{
			bool changed = false;
			lock (this) {
				if (hits.Count > 0) {
					hits.Clear ();
					changed = true;
				}
			}
			if (changed)
				Changed ();
		}

		public void Add (Hit hit)
		{
			HitTilePair pair = new HitTilePair (hit);
			if (pair.Tile == null) {
				Console.WriteLine ("Dropping Hit w/ type '{0}'", hit.Type);
				return;
			}

			int i = hits.BinarySearch (pair);
			hits.Insert (i < 0 ? ~i : i, pair);
			if (i == 0 || i < LastDisplayed)
				Changed ();
		}

		override protected string ExpandKey (string key)
		{
			switch (key) {

			case "Name":
				return name;

			case "DisplayedInfo":
				if (hits.Count == 0)
					return null;
				return String.Format ("{0} to {1} of {2}",
						      FirstDisplayed+1, LastDisplayed+1,
						      hits.Count);
			}
			
			return base.ExpandKey (key);
		}

		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Icon" && icon != null) {
				ctx.Image (icon, 32, 32, null);
				return true;
			}

			if (key == "Tiles") {
				int i = FirstDisplayed;
				int i1 = LastDisplayed;
				while (i <= i1) {
					HitTilePair pair = (HitTilePair) hits [i];
					ctx.Tile (pair.Tile);
					++i;
				}
				return true;
			}

			if (key == "BackLink" && CanPageBack) {
				ctx.Link ("Back", new TileActionHandler (PageBack));
				return true;
			}
			
			if (key == "ForwardLink" && CanPageForward) {
				ctx.Link ("Forward", new TileActionHandler (PageForward));
				return true;
			}

			return base.RenderKey (key, ctx);
		}

		public int CompareTo (object obj)
		{
			TileHitCollection other = (TileHitCollection) obj;
			return other.MaxScore.CompareTo (MaxScore);
		}

	}
}
