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

namespace Beagle.Tile {

	[TileStyle (Resource="template-page.css")]
	public class TileHitCollection : Tile {

		public TileHitCollection () {
		}

		private class HitTilePair : IComparable {
			Hit hit;
			Tile tile;

			public HitTilePair (Hit _hit, Tile _tile)
			{
				hit = _hit;
				tile = _tile;
			}

			public Beagle.Hit Hit {
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

		public int NumResults {
			get { return hits.Count; }
		} 

		public bool CanPageForward {
			get { return LastDisplayed != (hits.Count - 1); }
		}

		[TileAction]
		public void PageForward ()
		{
			firstDisplayed += maxDisplayed;
			if (firstDisplayed < 0)
				firstDisplayed = 0;
			Changed ();
		}

		public bool CanPageBack {
			get { return FirstDisplayed > 0; }
		}

		[TileAction]
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
					firstDisplayed = 0;
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

		public bool Subtract (Uri uri)
		{
			for (int i = 0; i < hits.Count; ++i) {
				HitTilePair pair = (HitTilePair) hits [i];
				if (pair.Hit.Uri.Equals (uri) && pair.Hit.Uri.Fragment == uri.Fragment) {
					hits.RemoveAt (i);
					return true;
				}
			}

			return false;
		}

		public bool IsEmpty {
			get { return hits.Count == 0; }
		}

		protected void PopulateTemplate (Template t)
		{

			t["TileId"] = UniqueKey;
			t["action:"] = "action:" + UniqueKey + "!"
;
			if (hits.Count > 1)
				t["NumberOfMatches"] = hits.Count + " Matches";
			else
				t["NumberOfMatches"] = hits.Count + " Match";

			if (hits.Count == 1 || ! (CanPageForward ||CanPageBack))
				t["DisplayedMatches"] = "";
			else 
				t["DisplayedMatches"] = String.Format ("Displaying Matches {0} to {1}",
								       FirstDisplayed+1, LastDisplayed+1);				

			

			if (CanPageForward) 
				t["CanPageForward"] = " ";
			if (CanPageBack) 
				t["CanPageBack"] = "";
			if (CanPageBack && CanPageForward) 
				t["CanPageBoth"] = "";
		}

		private void RenderTiles (TileRenderContext ctx)
		{
			int i = FirstDisplayed;
			int i1 = LastDisplayed;

			while (i <= i1 && i < NumResults) {
				HitTilePair pair = (HitTilePair) hits [i];
				ctx.Tile (pair.Tile);
				++i;
			}

		}

		public override void Render (TileRenderContext ctx)
		{
			Template t = new Template ("template-head.html");
			PopulateTemplate (t);
			ctx.Write (t.ToString ());

			RenderTiles (ctx);
			
			t = new Template ("template-foot.html");
			PopulateTemplate (t);
			ctx.Write (t.ToString ());
		}
	}
}
