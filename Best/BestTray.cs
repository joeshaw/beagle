//
// Beagle tray icon.
//
// Nat Friedman <nat@novell.com>
//
// Copyright 2004 Novell, Inc.
//

using System;
using System.Collections;

using Gtk;
using Egg;

using Beagle;
using Beagle.Tile;

namespace Best {
	
	public class BestTray
	{
		TrayIcon tray_icon;

		BestWindow win;

		public BestTray (BestWindow bw)
		{
			win = bw;
			
			Gtk.Button button = new Gtk.Button ();
			Gtk.Widget icon_image = new Gtk.Image (Images.GetPixbuf ("smalldog.png"));
			button.Add (icon_image);
			button.Relief = Gtk.ReliefStyle.None;

			button.Pressed += new EventHandler (ButtonPress);

			// FIXME: My tray icon is clipped with a 28 pixel tray
			tray_icon = new Egg.TrayIcon ("Search");

			tray_icon.Add (button);
			tray_icon.ShowAll ();
		}

		void ButtonPress (object sender, EventArgs args) 
		{
			if (! win.Visible) {
				win.Show ();
				win.Present ();
			} else {
				win.Hide ();
			}
		}
	}
}
