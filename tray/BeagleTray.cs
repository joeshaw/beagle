//
// Beagle search applet.
//
// Copyright 2004 Novell, Inc.
//
// Nat Friedman <nat@novell.com>
// Srinivasa Ragavan <sragavan@novell.com>
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

namespace Beagle
{

	public class BeagleTray
	{
		TrayIcon         tray_icon;
		Gdk.Pixbuf       glass_icon;
		Gtk.ToggleButton button;
		Gtk.Window       win;
		Gtk.Entry 	 entry;
		int              w_width = 400;
                int              w_height = 400;
	
		Gtk.TreeView     tree;
		Gtk.ListStore    store;

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
                private SimpleRootTile root;

		private GlobalKeybinder globalKeys;

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

			Gtk.AccelGroup accelGroup = new Gtk.AccelGroup ();
			win.AddAccelGroup (accelGroup);
			globalKeys = new GlobalKeybinder (accelGroup);

			// Close window (Ctrl-W)
			globalKeys.AddAccelerator (new EventHandler (this.CloseWindowHandler),
						   (uint) Gdk.Key.w, 
						   Gdk.ModifierType.ControlMask,
						   Gtk.AccelFlags.Visible);

			// Close window (Escape)
			globalKeys.AddAccelerator (new EventHandler (this.CloseWindowHandler),
						   (uint) Gdk.Key.Escape, 
						   0,
						   Gtk.AccelFlags.Visible);

			Box vbox = new Gtk.VBox (false, 2);
			
			entry = new Gtk.Entry();
			entry.MaxLength = 100;	
			entry.Activated += new EventHandler (DoSearch);
			vbox.PackStart (entry, false, false, 0);

			win.Add (vbox);
			entry.CanFocus = true;
			win.Focus = entry;

			//                        canvas = new TileCanvas ();
                                                                                                                                                             
			//                        root = new SimpleRootTile ();
			//                        canvas.Root = root;

			MakeHitTree ();

                        swin = new Gtk.ScrolledWindow ();
                        swin.Add (tree);

			tree.ShowAll ();
                                                                                                                                                            
                        VBox contents = new VBox (false, 3);
                        contents.PackStart (swin, true, true, 3);
			contents.ShowAll ();
			vbox.PackStart (contents, true, true, 0);

			return win;
		}

		void CloseWindowHandler (object sender, EventArgs args)
		{
			button.Active = false; 
		}

		void ButtonPress (object sender, EventArgs args) 
		{
			if (win == null)
				win = CreateQueryWindow ();

			if (button.Active) {
				win.ShowAll ();
				entry.GrabFocus ();
				win.Present ();
			} else {
				win.Hide ();
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
                        AddHitToTreeView (hit);
                }
                                                                                                                                                             
                private void OnHitSubtracted (Query source, Uri uri)
                {
                        root.Subtract (uri);
                }

	        private void MakeHitTree ()
	        {
			Type [] types = new Type [] {
				typeof (string),     // type
				typeof (string),     // name
				typeof (string),     // change date
			};

			store = new Gtk.ListStore (types);
			store.SetSortFunc (2 /* change date */,
					   new Gtk.TreeIterCompareFunc (CompareHits),
					   IntPtr.Zero, 
					   null);

			tree = new Gtk.TreeView (store);
			tree.HeadersVisible = true;
			tree.RulesHint = true;

			Gtk.CellRenderer renderer;

			Gtk.TreeViewColumn title = new Gtk.TreeViewColumn ();
			renderer = new Gtk.CellRendererText ();
			title.Title = "Match";
			title.Sizing = Gtk.TreeViewColumnSizing.Autosize;
			title.Resizable = true;
			
			title.PackStart (renderer, true);
			title.AddAttribute (renderer, "text", 1 /* title */);

			title.SortColumnId = 1; /* title */
			tree.AppendColumn (title);

			Gtk.TreeViewColumn change = new Gtk.TreeViewColumn ();
			change.Title = "Last Changed";
			change.Sizing = Gtk.TreeViewColumnSizing.Autosize;
			change.Resizable = true;

			renderer = new Gtk.CellRendererText ();
			renderer.Data ["xalign"] = 1.0;
			change.PackStart (renderer, false);
			change.AddAttribute (renderer, "text", 2 /* change date */);

			change.SortColumnId = 2; /* change date */
			tree.AppendColumn (change);

			Gtk.TreeViewColumn typecol = new Gtk.TreeViewColumn ();
			typecol.Title = "Type";
			typecol.PackStart (renderer, false);
			typecol.AddAttribute (renderer, "text", 0 /* type */);

			typecol.SortColumnId = 3; /* title */
			tree.AppendColumn (typecol);

			//			tree.RowActivated += new Gtk.RowActivatedHandler (OnRowActivated);
		}

		int CompareHits (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			Console.WriteLine ("CompareDates Called!");

			Hit hit_a = (Hit) model.GetValue (a, 3 /* hit */);
			Hit hit_b = (Hit) model.GetValue (b, 3 /* hit */);

			if (hit_a == null || hit_b == null)
				return -1;
			else
				return String.Compare (hit_a.Type, hit_b.Type);
		}

		string PrettyPrintDate (DateTime date)
		{
			DateTime now = DateTime.Now;
			string short_time = date.ToShortTimeString ();

			if (date.Year == now.Year) {
				if (date.DayOfYear == now.DayOfYear)
					return String.Format ("Today, {0}", short_time);
				else if (date.DayOfYear == now.DayOfYear - 1)
					return String.Format ("Yesterday, {0}", short_time);
				else if (date.DayOfYear > now.DayOfYear - 6)
					return String.Format ("{0} days ago, {1}", 
							      now.DayOfYear - date.DayOfYear,
							      short_time);
				else
					return date.ToString ("MMMM d, h:mm tt");
			} else
				return date.ToString ("MMMM d yyyy, h:mm tt");
		}
		    
		void AddHitToTreeView (Hit hit)
		{
			string nice_date = PrettyPrintDate (hit.Timestamp);

			Gtk.TreeIter iter = store.Append ();
			store.SetValue (iter, 0 /* icon */, hit.Type);
			store.SetValue (iter, 1 /* title */, hit.FileName);
			store.SetValue (iter, 2 /* change date */, nice_date);
		}

		private string delayedQuery = null;
		
		private bool RunDelayedQuery ()
		{
			if (delayedQuery != null) {
				string tmp = delayedQuery;
				delayedQuery = null;
				System.Console.WriteLine ("Delayed query fired");
				Search (tmp);
			}
			
			return false;
		}

		private void QueueDelayedQuery ()
		{
			GLib.Timeout.Add (1000, new GLib.TimeoutHandler (RunDelayedQuery));
		}
		
		private void CheckQueryError (Exception e)
		{
			delayedQuery = entry.Text;
			DBusisms.BeagleUpAgain += QueueDelayedQuery;

			if (e.ToString ().IndexOf ("com.novell.Beagle") != -1) {
		                root.Error ("Could not query.  The Beagle daemon is probably not running, or maybe you\n don't have D-BUS set up properly.");
			        root.OfferDaemonRestart = true;
			} else
			        root.Error ("The query failed with error:<br><br>" + e);
		}
                                                                                                                                                             
                private void Search (String searchString)
                {
                        entry.Text = searchString;

			store.Clear ();
                                                                                                                                                             
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
