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
		static TrayIcon   tray_icon;
		static Gdk.Pixbuf glass_icon;
		static Gtk.ToggleButton button;
		static Gtk.Window       win;
		static Gtk.Entry 	entry;
		static int              w_width = 400;
                static int              w_height = 400;
	
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

		void ButtonPress (object sender, EventArgs args) 
		{

                        if (win != null) {
                                win.Destroy ();
				entry.Destroy();
				entry=null;
                                win = null;
                                return;
                        }

			Console.WriteLine ("*** ButtonPress");

	                win = new Gtk.Window ("Timeline");
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
			entry.MaxLength=100;	
			entry.Activated += new EventHandler (DoSearch);
			vbox.PackStart (entry, false, false, 0);
//Start
			win.Add(vbox);

                        canvas = new TileCanvas ();
                                                                                                                                                             
                        root = new BestRootTile ();
                        canvas.Root = root;
                                                                                                                                                             
                        swin = new Gtk.ScrolledWindow ();
                        swin.Add (canvas);

			canvas.ShowAll();
                                                                                                                                                            
                        VBox contents = new VBox (false, 3);
                        contents.PackStart (swin, true, true, 3);
			contents.ShowAll ();
			vbox.PackStart (contents, true, true, 0);	

//End
			win.ShowAll ();
			//			Gtk.Widget parent = (Gtk.Widget) sender;
			//			Gtk.Menu recent_menu = MakeRecentNotesMenu (parent);
			//			GuiUtils.PopupMenu (recent_menu, args.Event);
		}

                private static void PositionWin ()
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

		private void SDoSearch (object o, EventArgs args)
		{
			Console.WriteLine("Hey");
                        String searchString = entry.Text;
                                                                                                                                                             
                        if (query != null) {
                                query.Cancel ();
                                query.HitAddedEvent -= OnHitAdded;
                                //query.HitSubtractedEvent -= OnHitSubtracted;
                                query.Dispose ();
                        }
                                                                                                                                                             
                        query = Factory.NewQuery ();
                                                                                                                                                             
                        query.AddDomain (QueryDomain.Neighborhood);
                        query.AddDomain (QueryDomain.Global);
                                                                                                                                                             
                        query.AddText (searchString);
                                                                                                                                                             
                        query.HitAddedEvent += OnHitAdded;
                        //query.HitSubtractedEvent += OnHitSubtracted;
                                                                                                                                                             
                                                                                                                                                             
                        query.Start ();
			entry.Text = "";

		}

		public void SOnHitAdded (Query source, Hit hit)
                {
                        //HitFlavor flavor = HitToHitFlavor.Get (hit);
                        //if (flavor == null)
                         //       return;
                                                                                                                                                             
                                                                                                                                                             
                        //if (hitCollection == null) {
                                //hitCollection = new TileHitCollection (flavor.Name,
                                 //                                      flavor.Emblem,
                                  //                                     flavor.Color,
                                   //                                    flavor.Columns);
			//	Console.WriteLine(flavor.Name);
                                                                                                                                                             
                                //tileTable [flavor.Name] = hitCollection;
                        //}
                                                                                                                                                             
                        //object[] args = new object [1];
                        //args[0] = hit;
                        //Tile tile = (Tile) Activator.CreateInstance (flavor.TileType, args);
                        //hitCollection.Add (hit, tile);
                        Console.WriteLine ("+ {0}", hit.Uri);
                }  
                private void DoSearch (object o, EventArgs args)
                {
                        Search (entry.Text);
                }
                                                                                                                                                             
                //////////////////////////
                                                                                                                                                             
                private void Close ()
                {
                        //Best.DecRef ();
                        //Destroy ();
                }
                                                                                                                                                             
                //////////////////////////
                                                                                                                                                             
                private void OnHitAdded (Query source, Hit hit)
                {
                        root.Add (hit);
                }
                                                                                                                                                             
                private void OnHitSubtracted (Query source, Uri uri)
                {
                        root.Subtract (uri);
                }
                                                                                                                                                             
                private void Search (String searchString)
                {
                        entry.Text = searchString;
                                                                                                                                                             
                        if (query != null) {
                                query.Cancel ();
                                query.HitAddedEvent -= OnHitAdded;
                                query.HitSubtractedEvent -= OnHitSubtracted;
                                query.Dispose ();
                        }
                                                                                                                                                             
                        query = Factory.NewQuery ();
                                                                                                                                                             
                        query.AddDomain (QueryDomain.Neighborhood);
                        query.AddDomain (QueryDomain.Global);
                                                                                                                                                             
                        query.AddText (searchString);
                                                                                                                                                             
                        query.HitAddedEvent += OnHitAdded;
                        query.HitSubtractedEvent += OnHitSubtracted;
                                                                                                                                                             
                        root.Open ();
                                                                                                                                                             
                        query.Start ();
                }



	}
}
