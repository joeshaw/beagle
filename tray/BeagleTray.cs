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
using Beagle;
using Beagle.Tile;
using Best;
namespace Beagle
{

	public class BeagleTray
	{
		TrayIcon   tray_icon;
		Gdk.Pixbuf glass_icon;
		Gtk.ToggleButton button;
		Gtk.Window       win;
		Gtk.Entry 	entry;
		int              w_width = 400;
                int              w_height = 400;
	
		public BeagleTray ()
		{
			glass_icon = GuiUtils.GetMiniIcon ("trayicon.png");
			button = new Gtk.ToggleButton ();
			button.Add (new Gtk.Image (glass_icon));
			button.Relief = Gtk.ReliefStyle.None;

			button.Toggled += new EventHandler (ButtonPress);

			tray_icon = new Egg.TrayIcon ("Search");

			tray_icon.Add (button);
			tray_icon.ShowAll ();
		}


                private Gtk.ScrolledWindow swin;
                private TileCanvas canvas;
                private BestRootTile root;

		private Gtk.Window CreateQueryWindow ()
		{
	                win = new Gtk.Window ("Beagle");
                        win.Decorated       = false;
                        win.DefaultHeight   = w_height;
                        win.DefaultWidth    = w_width;
                        win.SkipPagerHint   = true;
                        win.SkipTaskbarHint = true;
                        win.CanFocus        = false;
                        win.TypeHint        = Gdk.WindowTypeHint.Dock;
                        win.Stick ();
			PositionWin ();

			Box vbox = new Gtk.VBox (false, 2);
			
			entry = new Gtk.Entry();
			entry.MaxLength = 100;	
			entry.Activated += new EventHandler (DoSearch);
			vbox.PackStart (entry, false, false, 0);

			win.Add (vbox);
			entry.CanFocus = true;
			win.Focus = entry;

                        canvas = new TileCanvas ();
                                                                                                                                                             
                        root = new BestRootTile ();
                        canvas.Root = root;
                                                                                                                                                             
                        swin = new Gtk.ScrolledWindow ();
                        swin.Add (canvas);

			canvas.ShowAll ();
                                                                                                                                                            
                        VBox contents = new VBox (false, 3);
                        contents.PackStart (swin, true, true, 3);
			contents.ShowAll ();
			vbox.PackStart (contents, true, true, 0);

			return win;
		}

		void ButtonPress (object sender, EventArgs args) 
		{
			if (win == null)
				win = CreateQueryWindow ();

			if (win.Visible)
				win.Hide ();
			else {
				win.ShowAll ();
				entry.GrabFocus ();
			}
		}

                private void PositionWin ()
                {
                        int display_width, display_height;
                        Drawable d = (Display.Default.GetScreen (0)).RootWindow;
                        d.GetSize (out display_width, out display_height);
                                                                                                                                                             
                        int x, y, width, height, depth;
                        button.GdkWindow.GetGeometry (out x, out y, out width, out height, out depth);
                        button.GdkWindow.GetPosition (out x, out y);

                        if (((y * 100) / display_height) < 20)
                                y += height;
                        else
                                y -= (height + w_height);
                                                                                                                                                             
                        if (x + w_width > display_width)
                                x = display_width - w_width;
                        if (y + w_height > display_height)
                                y = display_height - w_height;
                        
                        win.Move (x, y);
                }

		Beagle.Query query = null;

                private void DoSearch (object o, EventArgs args)
                {
                        Search (entry.Text);
                }
                                                                                                                                                             
                                                                                                                                                             
                private void OnHitAdded (Query source, Hit hit)
                {
                        root.Add (hit);
                }
                                                                                                                                                             
                private void OnHitSubtracted (Query source, Uri uri)
                {
                        root.Subtract (uri);
                }

		private void CheckQueryError (Exception e)
		{
			if (e.ToString ().IndexOf ("com.novell.Beagle") != -1)
				root.Error ("Could not query.  The Beagle daemon is probably not running, or maybe you\n don't have D-BUS set up properly.");
			else
				root.Error ("The query failed with error:<br><br>" + e);
		}
                                                                                                                                                             
                private void Search (String searchString)
                {
                        entry.Text = searchString;
                                                                                                                                                             
                        if (query != null) {
				try {
					query.Cancel ();
				} catch (Exception e) {
					CheckQueryError (e);
					return;
				}
				
                                query.HitAddedEvent -= OnHitAdded;
                                query.HitSubtractedEvent -= OnHitSubtracted;
                                query.Dispose ();
                        }

                        root.Open ();
                                                                                                                                                             
			try {
				query = Factory.NewQuery ();

				query.AddDomain (QueryDomain.Neighborhood);
				query.AddDomain (QueryDomain.Global);

				query.AddText (searchString);
			} catch (Exception e) {
				CheckQueryError (e);

				return;
			}
                                                                                                                                                             
                        query.HitAddedEvent += OnHitAdded;
                        query.HitSubtractedEvent += OnHitSubtracted;
                                                                                                                                                             
                        query.Start ();
                }



	}
}
