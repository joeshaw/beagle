//
// SimpleRootTile.cs
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
using System.Diagnostics;

using Beagle.Tile;
using Beagle;

namespace Beagle.Tile {

	public class SimpleRootTile : Tile {

		Hashtable tileTable = new Hashtable ();
		string errorString;
		bool offerDaemonRestart;

		public void Open ()
		{
			tileTable = new Hashtable ();
			Changed ();
		}

		public bool OfferDaemonRestart {
			get { return offerDaemonRestart; }
			set { offerDaemonRestart = value; }
		}

		public void Add (Hit hit)
		{
			HitFlavor flavor = HitToHitFlavor.Get (hit);
			if (flavor == null)
				return;

			TileHitCollection hitCollection = (TileHitCollection) tileTable [flavor.Name];

			if (hitCollection == null) {
				hitCollection = new TileHitCollection (flavor.Name,
								       flavor.Emblem,
								       flavor.Color,
								       flavor.Rank,
								       flavor.Columns);
				
				tileTable [flavor.Name] = hitCollection;
			}

			object[] args = new object [1];
			args[0] = hit;
			Tile tile = (Tile) Activator.CreateInstance (flavor.TileType, args);
			hitCollection.Add (hit, tile);
			Console.WriteLine ("+ {0}", hit.Uri);
			Changed ();
		}

		public void Subtract (Uri uri)
		{
			bool changed = false;

			foreach (TileHitCollection hitCollection in tileTable.Values)
				if (hitCollection.Subtract (uri))
					changed = true;

			if (changed) {
				Console.WriteLine ("- {0}", uri);
				Changed ();
			}
		}

		public void Error (string errorString)
		{
			this.errorString = errorString;
			Changed ();
		}

		public void StartDaemon ()
		{
			// If we're running uninstalled (in the source tree), then run
			// the uninstalled daemon.  Otherwise run the installed daemon.

			string bestpath = System.Environment.GetCommandLineArgs () [0];
			string bestdir = System.IO.Path.GetDirectoryName (bestpath);

			string beagled_filename;
			if (bestdir.EndsWith ("lib/beagle")) {
				Console.WriteLine ("Running uninstalled daemon...");
				beagled_filename = "beagled"; // Running installed
			} else {
				Console.WriteLine ("Running uninstalled daemon...");
				beagled_filename = System.IO.Path.Combine (bestdir, "../beagled/beagled");
			}
				
			
			Process daemon = new Process ();
			daemon.StartInfo.FileName  = beagled_filename;
			daemon.StartInfo.UseShellExecute = false;

			daemon.Start ();
		}

		override public void Render (TileRenderContext ctx)
		{
			Console.WriteLine ("Render!");

			if (errorString != null) {
				ctx.Write (errorString);
				if (offerDaemonRestart) {
					ctx.Write ("<hr noshade>");
					ctx.Link ("Click to start the Beagle daemon...", new TileActionHandler (StartDaemon));
					offerDaemonRestart = false;
				}
				errorString = null;
				return;
			}

			ArrayList array = new ArrayList ();

			foreach (TileHitCollection tile in tileTable.Values)
				if (! tile.IsEmpty)
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
