//
// BestWindow.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.Collections;

using Gtk;
using GtkSharp;

using Beagle;
using Beagle.Tile;

namespace Best {

	public class BestWindow : Gtk.Window {

		static public void Create (string queryStr)
		{
			BestWindow best = new BestWindow ();
			if (queryStr != null)
				best.Search (queryStr);
			best.Show ();
 		}

		static public void Create ()
		{
			Create (null);
		}

		//////////////////////////

		Query query = null;
		Gtk.AccelGroup accel_group;
		GlobalKeybinder global_keys;
		
		private BestWindow () : base (WindowType.Toplevel)
		{
			Title = "Bleeding-Edge Search Tool";

			DeleteEvent += new DeleteEventHandler (this.DoDelete);

			//Widget menus = CreateMenuBar ();
			Widget content = CreateContents ();

			VBox main = new VBox (false, 3);
			//main.PackStart (menus, false, true, 3);
			main.PackStart (content, true, true, 3);
			//menus.Show ();
			content.Show ();
			Add (main);
			main.Show ();

			const double GOLDEN = 1.61803399;
			DefaultHeight = 500;
			DefaultWidth = (int) (DefaultHeight * GOLDEN);

			accel_group = new Gtk.AccelGroup ();
			this.AddAccelGroup (accel_group);
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

			Best.IncRef ();

			DBusisms.BeagleDown += OnBeagleDown;
		}

		private void OnBeagleDown ()
		{
			query.HitAddedEvent -= OnHitAdded;
			query.HitSubtractedEvent -= OnHitSubtracted;
			query.Dispose ();
			query = null;
		}

		private void DoDelete (object o, DeleteEventArgs args)
		{
			Close ();
		}

		private void CloseWindowHandler (object o, EventArgs args)
		{
			Close ();
		}
		
		//////////////////////////

		private Widget CreateMenuBar ()
		{
			AccelGroup group = new AccelGroup ();
			AddAccelGroup (group);

			MenuBar bar = new MenuBar ();

			Menu fileMenu = new Menu ();
			MenuItem file = new MenuItem ("_File");
			file.Submenu = fileMenu;

			ImageMenuItem fileNew = new ImageMenuItem (Gtk.Stock.New, group);
			fileNew.Activated += new EventHandler (this.DoNew);
			fileMenu.Append (fileNew);
			
			ImageMenuItem fileClose = new ImageMenuItem (Gtk.Stock.Close, group);
			fileClose.Activated += new EventHandler (this.DoClose);
			fileMenu.Append (fileClose);

			bar.Append (file);

			bar.ShowAll ();
			return bar;
		}
		
		private void DoNew (object o, EventArgs args)
		{
			Create ();
		}

		private void DoClose (object o, EventArgs args)
		{
			Close ();
		}

		//////////////////////////

		private Gtk.Entry entry;
		
		private Gtk.ScrolledWindow swin;
		private TileCanvas canvas;
		private BestRootTile root;

		private Gtk.Widget CreateContents ()
		{
			Gtk.HBox entryLine = new HBox (false, 3);

			Gtk.Label words = new Gtk.Label ("Enter search terms:");
			entryLine.PackStart (words, false, false, 3);
			
			entry = new Gtk.Entry ();
			entry.Activated += new EventHandler (this.DoSearch);
			entryLine.PackStart (entry, true, true, 3);

			
			Gtk.HBox buttonContents = new HBox (false, 0);
			Gtk.Widget buttonImg = Beagle.Images.GetWidget ("icon-search.png");
			buttonContents.PackStart (buttonImg, false, false, 1);
			Gtk.Label buttonLabel = new Gtk.Label ("Find");
			buttonContents.PackStart (buttonLabel, false, false, 1);
			
			Gtk.Button button = new Gtk.Button ();
			button.Add (buttonContents);
			button.Clicked += new EventHandler (this.DoSearch);
			entryLine.PackStart (button, false, false, 3);

			canvas = new TileCanvas ();

			root = new BestRootTile ();
			canvas.Root = root;

			swin = new Gtk.ScrolledWindow ();
			swin.Add (canvas);

			VBox contents = new VBox (false, 3);
			contents.PackStart (entryLine, false, true, 3);
			contents.PackStart (swin, true, true, 3);

			entryLine.ShowAll ();
			swin.ShowAll ();

			return contents;
		}

		private string DelayedQuery = null;

		private void RunQuery ()
		{
			if (DelayedQuery != null) {
				string tmp = DelayedQuery;
				DelayedQuery = null;
				System.Console.WriteLine ("Query fired");
				Search (tmp);
			}
		}

		private bool RunDelayedQuery ()
		{
			RunQuery ();
			return false;
		}

		private void QueueDelayedQuery ()
		{
			GLib.Timeout.Add (500, new GLib.TimeoutHandler (RunDelayedQuery));
		}
		
		private void DoSearch (object o, EventArgs args)
		{
			root.Open ();
			try {
				Search (entry.Text);
			}
			catch (Exception e)
			{
				DelayedQuery = entry.Text;
				DBusisms.BeagleUpAgain += QueueDelayedQuery;

				if (e.ToString ().IndexOf ("'com.novell.Beagle'") != -1) {
					root.Error ("The query for <i>" + entry.Text + "</i> failed." +
						    "<br>The likely cause is that the beagle daemon isn't running.");
					root.OfferDaemonRestart = true;
				} else
					root.Error ("The query for <i>" + entry.Text + "</i> failed with error:<br><br>" + e);
			}
		}

		//////////////////////////

		private void Close ()
		{
			Best.DecRef ();
			Destroy ();
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
