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

	public class HitContainer : Gtk.VBox {

		bool open = false;
		int count = 0;
		const int MAXCOUNT = 20;
		
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

			count = 0;
			open = true;
		}

		private bool DoPending ()
		{
			lock (pendingHits) {
				foreach (Hit hit in pendingHits) {
					if (count < MAXCOUNT) {
						Widget w = new HitView (hit);
						PackStart (w, false, true, 2);
						w.ShowAll ();
						++count;
					}
				}
				pendingHits.Clear ();
				pendingIdle = 0;
			}
			return false;
		}

		public void Add (Hit hit)
		{
			if (! open) {
				Console.WriteLine ("Adding Hit to closed HitContainer", hit.Uri);
				return;
			}

			lock (pendingHits) {
				if (count >= MAXCOUNT)
					return;
				pendingHits.Add (hit);
				if (pendingIdle == 0)
					pendingIdle = GLib.Idle.Add (new GLib.IdleHandler (DoPending));
			}
		}

		private bool DoClose ()
		{
			if (count == 0) {
				Widget w = new Label ("No matches!");
				PackStart (w, true, true, 3);
				w.ShowAll ();
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
