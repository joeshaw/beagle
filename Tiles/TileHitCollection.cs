//
// TileHitCollection.cs
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

		private ArrayList all_hits = new ArrayList ();
		private ArrayList hits = new ArrayList ();
		int firstDisplayed = 0;
		int maxDisplayed = 5;

		Template head_template;
		Template foot_template;

		public float MaxScore {
			get {
				if (all_hits.Count == 0)
					return 0;
				HitTilePair pair = (HitTilePair) all_hits [0];
				return pair.Hit.Score;
			}
		}

		public int FirstDisplayed {
			get { return firstDisplayed; }
		}

		public int LastDisplayed {
			get { return Math.Min (firstDisplayed + maxDisplayed, hits.Count) - 1; }
		}

		public int NumDisplayableResults {
			get { return hits.Count; }
		} 

		public int NumResults {
			get { return all_hits.Count; }
		} 

		public bool CanPageForward {
			get { return LastDisplayed != (hits.Count - 1); }
		}

		[TileAction]
		public void PageFirst ()
		{
			firstDisplayed = 0;
			Changed ();
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
				if (all_hits.Count > 0) {
					all_hits.Clear ();
					firstDisplayed = 0;
					changed = true;
				}
				if (hits.Count > 0) {
					hits.Clear ();
					firstDisplayed = 0;
					changed = true;
				}
			}
			if (changed)
				Changed ();
		}

		private bool InsertDisplayable (HitTilePair pair)
		{
			int i = hits.BinarySearch (pair);
				
			hits.Insert (i < 0 ? ~i : i, pair);
			if (i == 0 || i < LastDisplayed) {
				Changed ();
				return true;
			}

			return false;
		}

		public bool Add (Hit hit, Tile tile)
		{
			bool changed = false;
			
			HitTilePair pair = new HitTilePair (hit, tile);
			int i = all_hits.BinarySearch (pair);
			all_hits.Insert (i < 0 ? ~i : i, pair);
			if (i == 0 || i < LastDisplayed) {
				Changed ();
				changed = true;
			}

			if (SourceIsDisplayable (hit)) {
				if (InsertDisplayable (pair))
					changed = true;
			}

			return changed;
		}

		public bool Subtract (Uri uri)
		{
			bool removed = false;

			for (int i = 0; i < all_hits.Count; ++i) {
				HitTilePair pair = (HitTilePair) all_hits [i];
				if (pair.Hit.Uri.Equals (uri) && pair.Hit.Uri.Fragment == uri.Fragment) {
					all_hits.Remove (pair);
					removed = true;
					break;
				}
			}

			for (int i = 0; i < hits.Count; ++i) {
				HitTilePair pair = (HitTilePair) hits [i];
				if (pair.Hit.Uri.Equals (uri) && pair.Hit.Uri.Fragment == uri.Fragment) {
					hits.Remove (pair);
					break;
				}
			}

			return removed;
		}

		public bool IsEmpty {
			get { return all_hits.Count == 0; }
		}

		private ArrayList hitSources = new ArrayList ();
		
		public void SetSource (string source)
		{
			Console.WriteLine ("SetSource: {0}", source);
			
			hitSources = new ArrayList ();

			if (source != null)
				hitSources.Add (source);

			hits = new ArrayList ();
			for (int i = 0; i < NumResults; i ++) {
				HitTilePair pair = (HitTilePair) all_hits [i];

				if (SourceIsDisplayable (pair.Hit)) {
					InsertDisplayable (pair);
				} else
					Console.WriteLine ("{0} -- {1}", pair.Hit.Type, pair.Hit.Uri);
			}

			Changed ();
		}

		public void AddSource (string source)
		{
			hitSources.Add (source);

			for (int i = 0; i < NumDisplayableResults; i ++) {
				HitTilePair pair = (HitTilePair) hits [i];

				if (! SourceIsDisplayable (pair.Hit))
					hits.RemoveAt (i);
			}
		}

		public void SubtractSource (string source)
		{
			hitSources.Remove (source);

			for (int i = 0; i < NumResults; i ++) {
				HitTilePair pair = (HitTilePair) hits [i];

				if (pair.Hit.Source != source)
					continue;

				int j = hits.BinarySearch (pair);
			
				hits.Insert (j < 0 ? ~j : j, pair);
				if (j == 0 || j < LastDisplayed)
					Changed ();
			}
		}

		public void ClearSources (string source)
		{
			if (hitSources.Count == 0)
				return;
			
			hitSources.Clear ();

			hits = hits;

			Changed ();
		}

		public bool SourceIsDisplayable (Hit hit)
		{
			if (hitSources.Count == 0)
				return true;
			
			if (hitSources.IndexOf (hit.Type) >= 0)
				return true;

			return false;
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

		private void PopulateTemplate (Template t)
		{
		}

		public override void Render (TileRenderContext ctx)
		{
			if (head_template == null) {
				head_template = new Template ("template-head.html");
				PopulateTemplate (head_template);
			}
			
			ctx.Write (head_template.ToString ());

			RenderTiles (ctx);
			
			if (foot_template == null) {
				foot_template = new Template ("template-foot.html");
				PopulateTemplate (foot_template);
			}
			ctx.Write (foot_template.ToString ());
		}
	}
}
