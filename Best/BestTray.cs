//
// Beagle tray icon.
//
// Nat Friedman <nat@novell.com>
//
// Copyright 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;

using Gtk;

using Beagle;
using Beagle.Tile;

namespace Best {
	
	public class BestTray : Gtk.Plug
	{
		BestWindow win;
		Gtk.Button button;
		Beagle.Util.GConfXKeybinder keybinder;
		int PosX;
		int PosY;

		[DllImport ("libtrayiconglue")]
		private static extern IntPtr egg_tray_icon_new (string name);

		public BestTray (BestWindow bw)
		{
			PosX = 0;
			PosY = 0;
			// FIXME: My tray icon is clipped with a 28 pixel tray
			Raw = egg_tray_icon_new ("Search");

			win = bw;
			win.DeleteEvent += new DeleteEventHandler (WindowDeleteEvent);
			button = new Gtk.Button ();			

			Gtk.Widget icon_image = new Gtk.Image (Images.GetPixbuf ("smalldog.png"));
			button.Add (icon_image);
			button.Relief = Gtk.ReliefStyle.None;

			button.Pressed += new EventHandler (ButtonPress);

			Add (button);
			ShowAll ();

			keybinder = new Beagle.Util.GConfXKeybinder ();
			keybinder.Bind ("/apps/Beagle/keybindings/show_beagle",
					"F12",
					new EventHandler (ShowBeaglePressed));
		}

		private void ShowBeaglePressed (object o, EventArgs args)
		{
			if (!win.Visible) {
				button.Press ();
			} else {
				win.Show ();
				win.Present ();
				win.FocusEntry ();
			}
		}

		void ButtonPress (object sender, EventArgs args) 
		{
			if (! win.Visible) {
				win.Show ();
				win.Move (PosX, PosY);
				win.Present ();
				win.FocusEntry ();
			} else {
				win.GetPosition (out PosX, out PosY);
				win.Hide ();
			}
		}

		void WindowDeleteEvent (object sender, DeleteEventArgs args)
		{
			win.Hide ();
			args.RetVal = (object)true;
		}
	}
}
