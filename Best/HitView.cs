//
// HitView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

using Gtk;
using GtkSharp;

using Dewey;

namespace Best {

	public class HitView : Gtk.VBox {

		Hit hit;

		public HitView (Hit _hit)
		{
			hit = _hit;
			
			Widget w = new Label (hit.Uri);
			PackStart (w, true, true, 3);
			w.ShowAll ();

		}
	}
}
