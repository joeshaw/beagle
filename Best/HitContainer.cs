//
// HitContainer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

using Gtk;
using GtkSharp;

using Dewey;

namespace Best {

	public class HitContainer : Gtk.VBox {

		bool open = false;
		int count = 0;
		const int MAXCOUNT = 50;
		
		public HitContainer () : base (false, 3)
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

		public void Add (Hit hit)
		{
			if (! open) {
				Console.WriteLine ("Adding Hit to closed HitContainer", hit.Uri);
				return;
			}

			if (count < MAXCOUNT) {
				Widget w = new HitView (hit);
				PackStart (w, false, true, 3);
				w.ShowAll ();
				++count;
			}
		}

		// Call Close when you are done adding hits.
		public void Close ()
		{
			if (count == 0) {
				Widget w = new Label ("No matches!");
				PackStart (w, true, true, 3);
				w.ShowAll ();
			}

			open = false;
		}
	}

}
