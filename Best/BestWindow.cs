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
			main.Realize ();
			canvas.Realize ();

			root = new SimpleRootTile ();
			canvas.Root = root;

			DefaultWidth = 600;
			DefaultHeight = 500;

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

			UpdatePage ();
		}

		private void SetBusy (bool busy)
		{
			if (busy) {
				this.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
			} else {
				this.GdkWindow.Cursor = null;
			}

		}

		private void OnBeagleDown ()
		{
			SetBusy (false);
			DetachQuery ();
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

		private void DoForwardClicked (object o, EventArgs args)
		{
			root.HitCollection.PageForward ();
			UpdatePage ();
		}

		private void DoBackClicked (object o, EventArgs args)
		{
			root.HitCollection.PageBack ();
			UpdatePage ();
		}

		//////////////////////////

		private Gtk.Entry entry;
		
		private Gtk.ScrolledWindow swin;
		private TileCanvas canvas;
		private SimpleRootTile root;
		private Gtk.Label page_label;
		private Gtk.Button back_button;
		private Gtk.Button forward_button;

		private Gtk.Button StockButton (string stockid, string label)
		{
			Gtk.HBox button_contents = new HBox (false, 0);
			button_contents.Show ();
			Gtk.Widget button_image = new Gtk.Image (stockid, Gtk.IconSize.Button);
			button_image.Show ();
			button_contents.PackStart (button_image, false, false, 1);
			Gtk.Label button_label = new Gtk.Label (label);
			button_label.Show ();
			button_contents.PackStart (button_label, false, false, 1);
			
			Gtk.Button button = new Gtk.Button ();
			button.Add (button_contents);

			return button;
		}

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
			canvas.Show ();

			HBox pager = new HBox ();
			page_label = new Label ();
			page_label.Show ();
			pager.PackStart (page_label, false, false, 3);

			forward_button = StockButton ("gtk-go-forward", 
						      "Show More Results");
			forward_button.Show ();
			forward_button.Clicked += new EventHandler (DoForwardClicked);
			pager.PackEnd (forward_button, false, false, 3);

			back_button = StockButton ("gtk-go-back",
						   "Show Previous Results");
			back_button.Show ();

			back_button.Clicked += new EventHandler (DoBackClicked);
			pager.PackEnd (back_button, false, false, 3);

			pager.Show ();

			VBox contents = new VBox (false, 3);
			contents.PackStart (entryLine, false, true, 3);
			contents.PackStart (canvas, true, true, 3);
			contents.PackStart (pager, false, false, 3);

			entryLine.ShowAll ();
			canvas.ShowAll ();

			return contents;
		}

		private void UpdatePage ()
		{
			back_button.Sensitive = root.HitCollection.CanPageBack;
			forward_button.Sensitive = root.HitCollection.CanPageForward;

			string label;
			if (root.HitCollection.NumResults == 0)
				label = "No results.";
			else if (root.HitCollection.FirstDisplayed == 0) 
				label = String.Format ("Best <b>{0} results of {1}</b> are shown.", 
						       root.HitCollection.LastDisplayed + 1,
						       root.HitCollection.NumResults);
			else 
				label = String.Format ("Results <b>{0} through {1} of {2}</b> are shown.",
						       root.HitCollection.FirstDisplayed + 1, 
						       root.HitCollection.LastDisplayed + 1,
						       root.HitCollection.NumResults);						       
			page_label.Markup = label;
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
			UpdatePage ();
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
			UpdatePage ();
		}

		private void OnHitSubtracted (Query source, Uri uri)
		{
			root.Subtract (uri);
			UpdatePage ();
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
			root.Open ();

			SetBusy (true);
			query.Start ();
		}
	}
}
