//
// Beagle search applet.
//
// Copyright 2004 Novell, Inc.
//
// Nat Friedman <nat@novell.com>
//

using System;
using System.Collections;

using Gtk;
using Gdk;
using Gnome;
using GtkSharp;
using GLib;
using Egg;

namespace Beagle
{

	public class BeagleTray
	{
		static TrayIcon   tray_icon;
		static Gdk.Pixbuf glass_icon;
		
		public BeagleTray ()
		{
			glass_icon = GuiUtils.GetMiniIcon ("trayicon.png");

			Gtk.EventBox ev = new Gtk.EventBox ();
			ev.CanFocus = true;
			ev.ButtonPressEvent += new Gtk.ButtonPressEventHandler (ButtonPress);
			ev.Add (new Gtk.Image (glass_icon));

			tray_icon = new Egg.TrayIcon ("Search");

			tray_icon.Add (ev);
			tray_icon.ShowAll ();
		}

		void ButtonPress (object sender, Gtk.ButtonPressEventArgs args) 
		{
			//			Gtk.Widget parent = (Gtk.Widget) sender;
			//			Gtk.Menu recent_menu = MakeRecentNotesMenu (parent);
			//			GuiUtils.PopupMenu (recent_menu, args.Event);
		}
	}
}
