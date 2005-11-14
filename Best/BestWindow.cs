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

using Gnome;
using Gtk;
using GD=Gdk;

using Mono.Unix;

using Beagle;
using Beagle.Util;
using Beagle.Tile;

namespace Best {

	public class TypeMenuItem : Gtk.MenuItem {

		public string Type;

		public TypeMenuItem (string label, string type) : base (label)
		{
			this.Type = type;
		}
	}

	public class BestWindow : Gtk.Window {

		public BestWindow (string query) : base (WindowType.Toplevel)
		{
			CreateWindow (query);
		}

		public BestWindow () : base (WindowType.Toplevel)
		{
			CreateWindow (null);
		}
		
		public void FocusEntry () {
			//entry.SelectRegion (0, -1);
			entry.GrabFocus ();
		}

		public void FocusEntryHandler (object o, EventArgs args) {
			FocusEntry ();
		}

		Gtk.AccelGroup accel_group;
		GlobalKeybinder global_keys;

		void CreateWindow (string query)
		{
			Title = Best.DefaultWindowTitle;

			DeleteEvent += new DeleteEventHandler (this.DoDelete);
			MapEvent += new MapEventHandler (MapIt);
			UnmapEvent += new UnmapEventHandler (UnmapIt);

			Icon = Images.GetPixbuf ("best.png");

			Widget content = CreateContents ();

			VBox main = new VBox (false, 3);
			main.PackStart (content, true, true, 3);
			content.Show ();
			Add (main);
			main.Show ();
			main.Realize ();
			canvas.Realize ();

			root = new SimpleRootTile ();
			canvas.Root = root;

			DefaultWidth = 600;
			DefaultHeight = 675;

			accel_group = new Gtk.AccelGroup ();
			this.AddAccelGroup (accel_group);
			global_keys = new GlobalKeybinder (accel_group);

			// Close window (Ctrl-W)
			global_keys.AddAccelerator (new EventHandler (this.HideWindowHandler),
						    (uint) Gdk.Key.w, 
						    Gdk.ModifierType.ControlMask,
						    Gtk.AccelFlags.Visible);

			// Close window (Escape)
			global_keys.AddAccelerator (new EventHandler (this.HideWindowHandler),
						    (uint) Gdk.Key.Escape, 
						    0,
						    Gtk.AccelFlags.Visible);

			// Show source (Ctrl+U)
			global_keys.AddAccelerator (new EventHandler (this.ShowSource),
						    (uint) Gdk.Key.U, 
						    Gdk.ModifierType.ControlMask,
						    Gtk.AccelFlags.Visible);

			// Focus Entry (Ctrl+L)
			global_keys.AddAccelerator (new EventHandler (this.FocusEntryHandler),
						    (uint) Gdk.Key.L, 
						    Gdk.ModifierType.ControlMask,
						    Gtk.AccelFlags.Visible);

			// Previous Page (PageUp)
			global_keys.AddAccelerator (new EventHandler (this.PageBackHandler),
						    (uint) Gdk.Key.Page_Up, 
						    0,
						    Gtk.AccelFlags.Visible);

			// Next Page (PageDown)
			global_keys.AddAccelerator (new EventHandler (this.PageForwardHandler),
						    (uint) Gdk.Key.Page_Down, 
						    0,
						    Gtk.AccelFlags.Visible);

			//DBusisms.BeagleDown += OnBeagleDown;

			UpdateFromConf ();
			
			UpdatePage ();

			if (query != null)
				Search (query);
			
		}

		//////////////////////////

		int posX = 0, posY = 0;

		public new void Present ()
		{
			base.Present ();	
			
			Move (posX, posY);
		}

		public new void Hide ()
		{
			// FIXME: Hack, why does Hide () gets invoked twice, the second time with (0,0) as window position?
			
			int new_posX = 0, new_posY = 0;

			GetPosition (out new_posX, out new_posY);

			if (new_posX != 0 &&  new_posY != 0) {
				posX = new_posX;
				posY = new_posY;
			}

			base.Hide ();
		}

		//////////////////////////

		Query query = null;
		string hit_type = null;
		
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
			Hide ();
		}

		public bool WindowIsVisible;

		private void MapIt (object o, MapEventArgs args)
		{			
			WindowIsVisible = true;
		}

		private void UnmapIt (object o, UnmapEventArgs args)
		{
			WindowIsVisible = false;
		}

		private void HideWindowHandler (object o, EventArgs args)
		{
			Hide ();
		}

		private void ShowSource (object o, EventArgs args)
		{
			Gtk.Window win = new Gtk.Window ("Source");
			win.SetDefaultSize (800,500);
			Gtk.ScrolledWindow sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			Gtk.TextView view = new Gtk.TextView ();
			view.CursorVisible = false;
			view.Editable = false;
			Gtk.TextBuffer buffer = view.Buffer;
			buffer.Text = canvas.Source;
			view.Buffer = buffer;
			sw.Add (view);
			win.Add (sw);
			win.ShowAll ();
		}
		
		//////////////////////////

		private void PageForward ()
		{
			if (!root.HitCollection.CanPageForward)
				return;

			root.HitCollection.PageForward ();
			UpdatePage ();
		}

		private void PageBack ()
		{
			if (!root.HitCollection.CanPageBack)
				return;

			root.HitCollection.PageBack ();
			UpdatePage ();
		}

		private void PageForwardHandler (object o, EventArgs args)
		{
			PageForward ();
		}

		private void PageBackHandler (object o, EventArgs args)
		{
			PageBack ();
		}

		//////////////////////////

		private Gtk.Entry entry;
		private Gtk.ListStore history;

		private Gtk.ListStore filter_data;
		
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

		private Gtk.ComboBox FilterComboBox ()
		{
			filter_data = new Gtk.ListStore (new Type[] { typeof (string), typeof (string) });
			
			Gtk.TreeIter iter;

			iter = filter_data.Append ();
			filter_data.SetValue (iter, 0, Catalog.GetString ("Anywhere"));
			filter_data.SetValue (iter, 1, null);

			iter = filter_data.Append ();
			filter_data.SetValue (iter, 0, Catalog.GetString ("in Files"));
			filter_data.SetValue (iter, 1, "File");

			iter = filter_data.Append ();
			filter_data.SetValue (iter, 0, Catalog.GetString ("in Addressbook"));
			filter_data.SetValue (iter, 1, "Contact");

			iter = filter_data.Append ();
			filter_data.SetValue (iter, 0, Catalog.GetString ("in Mail"));
			filter_data.SetValue (iter, 1, "MailMessage");

			iter = filter_data.Append ();
			filter_data.SetValue (iter, 0, Catalog.GetString ("in Web Pages"));
			filter_data.SetValue (iter, 1, "WebHistory");

			iter = filter_data.Append ();
			filter_data.SetValue (iter, 0, Catalog.GetString ("in Chats"));
			filter_data.SetValue (iter, 1, "IMLog");

			Gtk.ComboBox combo = new Gtk.ComboBox (filter_data);
			combo.Active = 0;

			Gtk.CellRendererText renderer = new Gtk.CellRendererText ();
			combo.PackStart (renderer, false);
			combo.SetAttributes (renderer, new object[] { "text", 0 });

			return combo;
		}
		
		private Gtk.Widget CreateContents ()
		{
			Gtk.HBox entryLine = new HBox (false, 4);

			Gtk.Label words = new Gtk.Label (Catalog.GetString ("Search terms:"));
			entryLine.PackStart (words, false, false, 3);

			history = new Gtk.ListStore (new Type[] { typeof (string) });

			Gtk.EntryCompletion comp = new Gtk.EntryCompletion ();
			comp.Model = history;
			comp.TextColumn = 0;
			
			entry = new Gtk.Entry ("");
			entry.Activated += new EventHandler (this.DoSearch);
			entry.Completion = comp;
			entryLine.PackStart (entry, true, true, 3);

			words = new Gtk.Label ("");
			entryLine.PackStart (words, false, false, 3);

			Gtk.ComboBox combo = FilterComboBox ();
			combo.Changed += new EventHandler (this.ChangeType);
			entryLine.PackStart (combo, false, false, 3);

			Gtk.HBox buttonContents = new HBox (false, 0);
			Gtk.Widget buttonImg = Beagle.Images.GetWidget ("icon-search.png");
			buttonContents.PackStart (buttonImg, false, false, 1);
			Gtk.Label buttonLabel = new Gtk.Label (Catalog.GetString ("Find"));
			buttonContents.PackStart (buttonLabel, false, false, 1);
			
			Gtk.Button button = new Gtk.Button ();
			button.Add (buttonContents);
			button.Clicked += new EventHandler (this.DoSearch);
			entryLine.PackStart (button, false, false, 3);
			
			Gtk.Button clearButton = new Gtk.Button ();
			clearButton.Label = "Clear";
			clearButton.Clicked += new EventHandler (this.ClearSearch);
			entryLine.PackStart (clearButton, false, false, 4);

			canvas = new TileCanvas ();
			canvas.Show ();

			HBox pager = new HBox ();
			page_label = new Label ();
			page_label.Show ();
			pager.PackStart (page_label, false, false, 3);

			forward_button = StockButton ("gtk-go-forward", 
						      Catalog.GetString ("Show More Results"));
			forward_button.Show ();
			forward_button.Clicked += new EventHandler (PageForwardHandler);
			pager.PackEnd (forward_button, false, false, 3);

			back_button = StockButton ("gtk-go-back",
						   Catalog.GetString ("Show Previous Results"));
			back_button.Show ();

			back_button.Clicked += new EventHandler (PageBackHandler);
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

		private void UpdateFromConf ()
		{
			Console.WriteLine ("Reading settings from Config");
			// FIXME: there might be weird cases with multiple screens,
			// multiple monitors, resolution related problems that might
			// cause problem is remapping the stored values to current
			// screen coordinates
			int res_x = GD.Screen.Default.Width;
			int res_y = GD.Screen.Default.Height;
			int pos_x = (int)(Conf.Searching.BestPosX * res_x / 100);
			int pos_y = (int)(Conf.Searching.BestPosY * res_y / 100);
			int width = (int)(Conf.Searching.BestWidth * res_x / 100 );
			int height = (int)(Conf.Searching.BestHeight * res_y / 100);
			
			if (pos_x != 0 || pos_y != 0) {
				posX = pos_x;
				posY = pos_y;
				Move (pos_x, pos_y);
			}
			if (width != 0)
				DefaultWidth = width;
			if (height != 0)
				DefaultHeight = height;
			Gtk.TreeIter iter;
			foreach (string search in Conf.Searching.SearchHistory) {
				iter = history.Append ();
				history.SetValue (iter, 0, search);
			}
		}

		public void StoreSettingsInConf (bool with_tray)
		{
			Console.WriteLine ("Storing setting in Config");
			int pos_x = posX, pos_y = posY;
			if (with_tray && WindowIsVisible)
				GetPosition (out pos_x, out pos_y);
			int width = 0, height = 0;
			GetSize (out width, out height);
			
			int res_x = GD.Screen.Default.Width;
			int res_y = GD.Screen.Default.Height;
			Conf.Searching.BestPosX = ((float) pos_x / res_x) * 100;
			Conf.Searching.BestPosY = ((float) pos_y / res_y) * 100;
			Conf.Searching.BestWidth = ((float) width / res_x) * 100;
			Conf.Searching.BestHeight = ((float) height / res_y) * 100;
			Conf.Searching.SearchHistory = RetriveSearches ();
			Conf.Searching.SaveNeeded = true;
			Conf.Save ();
		}

		private void UpdatePage ()
		{
			//Console.WriteLine ("In UpdatePage");
			back_button.Sensitive = root.HitCollection.CanPageBack;
			forward_button.Sensitive = root.HitCollection.CanPageForward;

			string label;

			int results;
			if (this.hit_type == null)
				results = root.HitCollection.NumResults;
			else
				results = root.HitCollection.NumDisplayableResults;

			if (results == 0) {
				label = Catalog.GetString ("No results.");
			} else if (root.HitCollection.FirstDisplayed == 0) {
				/* To translators: {0} is the current count of results shown of {1} in total, this is the message that is initially shown */
				/* when results are returned to the user. */
				label = String.Format (Catalog.GetString ("Best <b>{0} results of {1}</b> are shown."), 
						       root.HitCollection.LastDisplayed + 1,
						       results);
			} else {
				/* To translators: {0} to {1} is the interval of results currently shown of {2} results in total*/
				label = String.Format (Catalog.GetString ("Results <b>{0} through {1} of {2}</b> are shown."),
						       root.HitCollection.FirstDisplayed + 1, 
						       root.HitCollection.LastDisplayed + 1,
						       results);
			}
			page_label.Markup = label;
		}

		private string delayedQuery = null;

		private bool RunDelayedQuery ()
		{
			if (delayedQuery != null) {
				string tmp = delayedQuery;
				delayedQuery = null;
				System.Console.WriteLine ("Delayed query fired");
				StartQuery ();
			}

			return false;
		}

		private void QueueDelayedQuery (string query)
		{
			delayedQuery = query;
			GLib.Timeout.Add (10000, new GLib.TimeoutHandler (RunDelayedQuery));
		}
		
		private void DoSearch (object o, EventArgs args)
		{
			if (entry.Text != null && entry.Text != "")
				Search (entry.Text);
		}

		//private string lastType = null;

		private void ChangeType (object o, EventArgs args)
		{
			Gtk.ComboBox combo = (Gtk.ComboBox) o;
			string hit_type = null;
			Gtk.TreeIter iter;

			if (combo.GetActiveIter (out iter))
				hit_type = (string) filter_data.GetValue (iter, 1);

			if (this.hit_type == hit_type)
				return;

			this.hit_type = hit_type;

			root.SetSource (this.hit_type);
			root.HitCollection.PageFirst ();
			UpdatePage ();
		}

		//////////////////////////

		private void OnHitsAdded (HitsAddedResponse response)
		{
			root.Add (response.Hits);
			UpdatePage ();
		}

		private void OnHitsSubtracted (HitsSubtractedResponse response)
		{
			root.Subtract (response.Uris);
			UpdatePage ();
		}

		private void OnFinished (FinishedResponse repsonse)
		{
			SetBusy (false);
		}

		private void OnCancelled (CancelledResponse response)
		{
			SetBusy (false);
		}

		private void AttachQuery ()
		{
			query.HitsAddedEvent += OnHitsAdded;
			query.HitsSubtractedEvent += OnHitsSubtracted;
			query.FinishedEvent += OnFinished;
			query.CancelledEvent += OnCancelled;
		}

		private void DetachQuery ()
		{
			if (query != null) {
				query.HitsAddedEvent -= OnHitsAdded;
				query.HitsSubtractedEvent -= OnHitsSubtracted;
				query.FinishedEvent -= OnFinished;
				query.CancelledEvent -= OnCancelled;
				query.Close ();

				query = null;
			}
		}

		private void StartQuery ()
		{
			try {
				query.SendAsync ();
				SetBusy (true);
			} catch (Beagle.ResponseMessageException e) {
				QueueDelayedQuery (entry.Text);
				
				/* To translators: {0} represents the current query keywords */
				root.Error (String.Format (Catalog.GetString ("The query for <i>{0}</i> failed." +
						    "<br>The likely cause is that the beagle daemon isn't running."), entry.Text));
				root.OfferDaemonRestart = true;
			} catch (Exception e) {
				/* To translators: {0} represents the current query keywords, {1} contains the errormessage */
				root.Error (String.Format (Catalog.GetString ("The query for <i>{0}</i> failed with error:<br>{1}<br>"), 
							   entry.Text, e));
			}
		}

		private void Search (String searchString)
		{
			entry.Text = searchString;
			StoreSearch (searchString);

			if (query != null) {
				try { DetachQuery (); } catch (ObjectDisposedException e) {}
			}
			
			query = new Query ();
			query.AddDomain (QueryDomain.Neighborhood);
			
			// FIXME: Disable non-local searching for now.
			//query.AddDomain (QueryDomain.Global);
			
			query.AddText (searchString);
			root.SetSource (hit_type);
			
			AttachQuery ();
			
			root.Query = query;
			root.Start ();
			
			StartQuery ();
			
			UpdatePage ();
		}
		
		private void ClearSearch (object o, EventArgs args)
		{
			root.Clear ();
			UpdatePage ();
			entry.Text = "";
		}

		private void StoreSearch (string query)
		{
			Gtk.TreeIter iter;

			if (history.GetIterFirst (out iter)) {
				string val;

				do {
					val = (string) history.GetValue (iter, 0);

					if (val == query)
						history.Remove (ref iter);
				} while (val != query && history.IterNext (ref iter));
			}

			iter = history.Insert (0);
			history.SetValue (iter, 0, query);
		}
		
		public ArrayList RetriveSearches () 
		{
			ArrayList searches = new ArrayList ();

			int i = 0;
			foreach (object[] o in history) {
				searches.Add (o[0]);
				i++;

				if (i == 10)
					break;
			}

			return searches;
		}

		public void ClearHistory ()
		{
			history.Clear ();
		}
		
		public void QuickSearch (string query) 
		{
			Search (query);
		}
	}

	
}
