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

			Widget w = BuildWidget ();
			PackStart (w, true, true, 3);
			w.ShowAll ();
		}

		private Widget BuildWidget ()
		{
			String name = System.IO.Path.GetFileName (hit.Uri);

			String iconPath = GnomeIconLookup.LookupMimeIcon (hit.MimeType, (Gtk.IconSize) 48);
			Widget icon = new Image (iconPath);

			Box hbox = new HBox (false, 3);
			hbox.PackStart (icon, false, true, 3);

			Label label = new Label (name);
			label.Xalign = 0;
			label.UseUnderline = false;
			hbox.PackStart (label, true, true, 3);

			return hbox;
		}
	}
}
