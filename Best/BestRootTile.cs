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

using Beagle.Tile;
using Beagle;

namespace Best {

	public class BestRootTile : Tile {

		Hashtable tileTable = new Hashtable ();

		public void Open ()
		{
			tileTable = new Hashtable ();
		}

		public void Add (Hit hit)
		{
			Console.WriteLine ("Added {0}", hit.Uri);

			HitFlavor flavor = HitToHitFlavor.Get (hit);
			if (flavor == null)
				return;

			TileHitCollection hitCollection = (TileHitCollection) tileTable [flavor.Name];

			if (hitCollection == null) {
				hitCollection = new TileHitCollection (flavor.Name,
								       flavor.Emblem,
								       flavor.Color,
								       flavor.Columns);
				
				tileTable [flavor.Name] = hitCollection;
			}

			object[] args = new object [1];
			args[0] = hit;
			Tile tile = (Tile) Activator.CreateInstance (flavor.TileType, args);
			hitCollection.Add (hit, tile);
			Changed ();
		}

		public void Subtract (Uri uri)
		{
			Console.WriteLine ("Subtracting {0}", uri);

			bool changed = false;

			foreach (TileHitCollection hitCollection in tileTable.Values)
				if (hitCollection.Subtract (uri))
					changed = true;

			if (changed) {
				Console.WriteLine ("changed!");
				Changed ();
			}
		}

		override public void Render (TileRenderContext ctx)
		{
			ArrayList array = new ArrayList ();

			foreach (TileHitCollection tile in tileTable.Values)
				array.Add (tile);

			array.Sort ();

			bool first = true;
			foreach (TileHitCollection tile in array) {
				if (! first)
					ctx.Write ("<br>");
				first = false;
				ctx.Tile (tile);
			}
		}
	}
}
