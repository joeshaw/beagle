//
// Beagle search applet.
//
// Copyright 2004 Novell, Inc.
//
// Nat Friedman <nat@novell.com>
// Srinivasa Ragavan <sragavan@novell.com>
// Lukas Lipka <lukas@pmad.net>
//

using System;
using System.Collections;

using Gtk;
using Egg;

using Beagle;
using Beagle.Tile;

namespace Beagle
{

	public class BeagleTray
	{
		TrayIcon tray_icon;
		Gdk.Pixbuf glass_icon;
		Gtk.ToggleButton button;

		public BeagleTray ()
		{
			glass_icon = GuiUtils.GetMiniIcon ("trayicon.png");
			button = new Gtk.ToggleButton ();
			button.Add (new Gtk.Image (glass_icon));
			button.Relief = Gtk.ReliefStyle.None;

			button.Toggled += new EventHandler (ButtonPress);

			// FIXME: My tray icon is clipped with a 28 pixel tray
			tray_icon = new Egg.TrayIcon ("Search");

			tray_icon.Add (button);
			tray_icon.ShowAll ();
		}

		void ButtonPress (object sender, EventArgs args) 
		{
			if (win == null)
				CreateQueryWindow ();
				
			if (button.Active) {
				win.Show ();
				win.Present ();
				entry.GrabFocus ();
			} else {
				win.Hide ();
			}
		}

		Query query = null;
		Gtk.AccelGroup accel_group;
		GlobalKeybinder global_keys;

		int w_width = 400;
                int w_height = 450;

		private Gtk.Window win;
		private Gtk.Entry entry;
		private TileCanvas canvas;
		private SimpleRootTile root;

		public void CreateQueryWindow ()
		{
			win = new Window (Gtk.WindowType.Toplevel);
			win.Title = "Bleeding-Edge Search Tool";
			win.Decorated = false;
			win.DefaultHeight = w_height;
			win.DefaultWidth = w_width;
			win.SkipPagerHint = true;
			win.SkipTaskbarHint = true;
			win.CanFocus = false;
			win.TypeHint = Gdk.WindowTypeHint.Dock;
			win.Stick ();

			PositionWin ();
			
			entry = new Gtk.Entry ();
			entry.MaxLength = 100;	
			entry.CanFocus = true;
			entry.Activated += new EventHandler (DoSearch);

			canvas = new TileCanvas ();
			canvas.Show ();
		
			VBox contents = new VBox (false, 3);
			contents.PackStart (entry, false, true, 3);
			contents.PackStart (canvas, true, true, 3);
			contents.ShowAll ();
			win.Add (contents);

			win.Focus = entry;
			canvas.Realize ();

			root = new SimpleRootTile ();
			canvas.Root = root;

			accel_group = new Gtk.AccelGroup ();
			win.AddAccelGroup (accel_group);
			global_keys = new GlobalKeybinder (accel_group);

			// Close window (Ctrl-W)
			global_keys.AddAccelerator (new EventHandler (this.CloseWindowHandler),
						    (uint) Gdk.Key.w, 
						    Gdk.ModifierType.ControlMask,
						    Gtk.AccelFlags.Visible);

			// Close window (Escape)
			global_keys.AddAccelerator (new EventHandler (this.CloseWindowHandler),
						    (uint) Gdk.Key.Escape, 
						    0,
						    Gtk.AccelFlags.Visible);

			DBusisms.BeagleDown += OnBeagleDown;
		}

		private void CloseWindowHandler (object o, EventArgs args)
		{
			button.Active = false;
			win.Hide ();
		}

		private void PositionWin ()
                {
                        int display_width, display_height;
                        Gdk.Drawable d = (Gdk.Display.Default.GetScreen (0)).RootWindow;
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

		private void SetBusy (bool busy)
		{
			if (busy) {
				win.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
			} else {
				win.GdkWindow.Cursor = null;
			}

		}

		private void OnBeagleDown ()
		{
			SetBusy (false);
			DetachQuery ();
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
		
		private void DoSearch (object o, EventArgs args)
		{
			try {
				Search (entry.Text);
			}
			catch (Exception e)
			{
				delayedQuery = entry.Text;
				DBusisms.BeagleUpAgain += QueueDelayedQuery;

				if (e.ToString ().IndexOf ("'com.novell.Beagle'") != -1) {
					root.Error ("The query for <i>" + entry.Text + "</i> failed." +
						    "<br>The likely cause is that the beagle daemon isn't running.");
					root.OfferDaemonRestart = true;
				} else if (e.ToString().IndexOf ("Unable to determine the address") != -1) {
					root.Error ("The query for <i>" + entry.Text + "</i> failed.<br>" +
						    "The session bus isn't running.  See http://beaglewiki.org/index.php/Installing%20Beagle for information on setting up a session bus.");
				} else
					root.Error ("The query for <i>" + entry.Text + "</i> failed with error:<br><br>" + e);
			}
		}

		private void OnHitAdded (Query source, Hit hit)
		{
			root.Add (hit);
		}

		private void OnHitSubtracted (Query source, Uri uri)
		{
			root.Subtract (uri);
		}

		private void OnFinished (QueryProxy source)
		{
			SetBusy (false);
		}

		private void OnCancelled (QueryProxy source)
		{
			SetBusy (false);
		}

		private void AttachQuery ()
		{
			query.HitAddedEvent += OnHitAdded;
			query.HitSubtractedEvent += OnHitSubtracted;
			query.FinishedEvent += OnFinished;
			query.CancelledEvent += OnCancelled;
		}

		private void DetachQuery ()
		{
			if (query != null) {
				query.HitAddedEvent -= OnHitAdded;
				query.HitSubtractedEvent -= OnHitSubtracted;
				query.FinishedEvent -= OnFinished;
				query.CancelledEvent -= OnCancelled;
				query.Dispose ();
				query = null;
			}
		}

		private void Search (String searchString)
		{
			entry.Text = searchString;
			if (query != null) {
				query.Cancel ();
				DetachQuery ();
			}

			query = Factory.NewQuery ();
			
			query.AddDomain (QueryDomain.Neighborhood);
			query.AddDomain (QueryDomain.Global);

			query.AddText (searchString);

			AttachQuery ();
			
			root.Query = query;
			root.Start ();

			SetBusy (true);
			query.Start ();
		}
	}
}
