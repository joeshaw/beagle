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

	public class TileHitCollection : Tile {

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
					tile = new TileFile (hit);
					break;

				case "MailMessage":
					tile = new TileMailMessage (hit);
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
			hits.Add (pair);
			Changed ();
		}

		override public void Render (TileRenderContext ctx)
		{
			foreach (HitTilePair pair in hits)
				ctx.Tile (pair.Tile);
		}

	}

}
