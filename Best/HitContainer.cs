//
// HitContainer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

using Gtk;
using GtkSharp;

using Beagle;

namespace Best {

	public class HitContainer : Gtk.HBox {

		bool open = false;
		int count = 0;
		int maxHitsPerType = 10;

		Hashtable hitsByType;
		
		ArrayList pendingHits = new ArrayList ();
		uint pendingIdle;
		
		public HitContainer () : base (false, 1)
		{
			
		}

		// Call Open before adding any hits.
		public void Open ()
		{
			foreach (Widget w in Children)
				Remove (w);

			open = true;
			count = 0;
			hitsByType = new Hashtable ();
		}

		public void Add (Hit hit)
		{
			if (! open) {
				Console.WriteLine ("Adding Hit to closed HitContainer", hit.Uri);
				return;
			}

			lock (hitsByType) {
				++count;
				ArrayList hits = (ArrayList) hitsByType [hit.Type];
				if (hits == null) {
					hits = new ArrayList ();
					hitsByType [hit.Type] = hits;
				}
				if (hits.Count < maxHitsPerType)
					hits.Add (hit);
			}
		}

		private bool DoClose ()
		{
			if (count == 0) {
				Widget w = new Label ("No matches!");
				PackStart (w, true, true, 3);
				w.ShowAll ();
			} else {
				// This isn't optimal, but at least it presents
				// the renderers in a semi-consistent order that
				// doesn't depend on Hashtable internals...
				ArrayList keys = new ArrayList ();
				foreach (String key in hitsByType.Keys)
					keys.Add (key);
				keys.Sort ();

				foreach (String key in keys) {
					HitRenderer renderer = HitRenderer.FindRendererByType (key);
					ScrolledWindow sw = new ScrolledWindow ();
					Widget w = renderer.Widget;
					sw.Add (w);
					PackStart (sw, true, true, 3);
					renderer.RenderHits ((ArrayList) hitsByType [key]);
					sw.ShowAll ();
				}
			}

			return false;
		}

		// Call Close when you are done adding hits.
		public void Close ()
		{
			GLib.Idle.Add (new GLib.IdleHandler (DoClose));
			open = false;
		}
	}

}
