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

		string name;
		string icon;
		string color;
		int    columns;

		public TileHitCollection (string _name, 
					  string _icon,
					  string _color,
					  int    _columns) : base ("template-hit-collection.html")
		{
			name = _name;
			icon = _icon;
			color = _color;
			columns = _columns;

			EnableInlineRendering ();
		}

		private class HitTilePair : IComparable {
			Hit hit;
			Tile tile;

			public HitTilePair (Hit _hit, Tile _tile)
			{
				hit = _hit;
				tile = _tile;
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

		public void Add (Hit hit, Tile tile)
		{
			HitTilePair pair = new HitTilePair (hit, tile);
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
				
			case "Color":
				return color;

			case "NumberOfMatches":
				if (hits.Count > 1)
					return hits.Count + " Matches";
				else
					return hits.Count + " Match";

			case "DisplayedMatches":
				if (hits.Count == 1)
					return "";
				return String.Format ("Displaying Matches {0} to {1}",
						      FirstDisplayed+1, LastDisplayed+1);
			}
			
			return base.ExpandKey (key);
		}

		private void RenderTiles (TileRenderContext ctx)
		{
			int i = FirstDisplayed;
			int i1 = LastDisplayed;
			int counter = 0;

			double widthPerc = 100.0 / columns;
			//string td = String.Format ("<td width=\"{0}%\">", widthPerc);
			string td = "<td>";

			//ctx.Write ("<table width=\"100%\">");
			ctx.Write ("<table>");
			while (i <= i1) {
				HitTilePair pair = (HitTilePair) hits [i];
				if (counter == 0)
					ctx.Write ("<tr>");
				ctx.Write (td);
				ctx.Tile (pair.Tile);
				ctx.Write ("</td>");

				++i;

				++counter;
				if (counter == columns) {
					ctx.Write ("</tr>");
					counter = 0;
				}
			}

			// If necessarry, pad the table w/ empty cells and
			// end the row.
			if (counter > 0) {
				while (counter < columns) {
					ctx.Write (td + "<table width=\"100%\"><tr><td>&nbsp;</td></tr></table></td>");
					++counter;
				}
				ctx.Write ("</tr>");
			}
			ctx.Write ("</table>");
		}

		override protected bool RenderKey (string key, TileRenderContext ctx)
		{
			if (key == "Icon" && icon != null) {
				ctx.Image (icon, 24, 24, null);
				return true;
			}

			if (key == "Tiles") {
				RenderTiles (ctx);
				return true;
			}

			if (key == "BackLink" && CanPageBack) {
				ctx.Link ("&lt;&lt; Previous Matches ",
					  new TileActionHandler (PageBack));
				return true;
			}
			
			if (key == "ForwardLink" && CanPageForward) {
				ctx.Link ("Next Matches &gt;&gt;",
					  new TileActionHandler (PageForward));
				return true;
			}

			if (key == "BothLinks" && (CanPageForward || CanPageBack)) {
				RenderKey ("BackLink", ctx);
				ctx.Write ("&nbsp;&nbsp;");
				RenderKey ("ForwardLink", ctx);
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
