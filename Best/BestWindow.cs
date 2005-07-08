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

using Mono.Posix;

using Beagle;
using BU = Beagle.Util;
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
			entry.SelectRegion (0, -1);
			entry.GtkEntry.GrabFocus ();
		}

		public void FocusEntryHandler (object o, EventArgs args) {
			FocusEntry ();
		}

		Gtk.AccelGroup accel_group;
		BU.GlobalKeybinder global_keys;

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
			global_keys = new BU.GlobalKeybinder (accel_group);

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

		private Gnome.Entry entry;
		
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

		private Gtk.OptionMenu TypesOptionMenu ()
		{
			Gtk.OptionMenu opt = new Gtk.OptionMenu ();

			ArrayList items = new ArrayList ();
			Gtk.MenuItem mi;
			mi = new TypeMenuItem (Catalog.GetString ("Anywhere"), null);
			items.Add (mi);
			mi = new TypeMenuItem (Catalog.GetString ("in Files"), "File");
			items.Add (mi);
			mi = new TypeMenuItem (Catalog.GetString ("in Addressbook"), "Contact");
			items.Add (mi);
			mi = new TypeMenuItem (Catalog.GetString ("in Mail"), "MailMessage");
			items.Add (mi);
			mi = new TypeMenuItem (Catalog.GetString ("in Web Pages"), "WebHistory");
			items.Add (mi);
			mi = new TypeMenuItem (Catalog.GetString ("in Chats"), "IMLog");
			items.Add (mi);

			Gtk.Menu menu = new Gtk.Menu ();
			foreach (Gtk.MenuItem item in items)
				menu.Append (item);
			opt.Menu = menu;
			opt.Data["items"] = items;
		
			return opt;
		}

		private Gtk.Widget CreateContents ()
		{
			Gtk.HBox entryLine = new HBox (false, 3);

			Gtk.Label words = new Gtk.Label (Catalog.GetString ("Search terms:"));
			entryLine.PackStart (words, false, false, 3);
			
			entry = new Gnome.Entry ("");
			entry.Activated += new EventHandler (this.DoSearch);
			entryLine.PackStart (entry, true, true, 3);

			words = new Gtk.Label ("");
			entryLine.PackStart (words, false, false, 3);

			Gtk.OptionMenu types = TypesOptionMenu ();
			types.Changed += new EventHandler (this.ChangeType);
			entryLine.PackStart (types, false, false, 3);

			Gtk.HBox buttonContents = new HBox (false, 0);
			Gtk.Widget buttonImg = Beagle.Images.GetWidget ("icon-search.png");
			buttonContents.PackStart (buttonImg, false, false, 1);
			Gtk.Label buttonLabel = new Gtk.Label (Catalog.GetString ("Find"));
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
			if (entry.GtkEntry.Text != null && entry.GtkEntry.Text != "")
				Search (entry.GtkEntry.Text);
		}

		//private string lastType = null;

		private void ChangeType (object o, EventArgs args)
		{
			Gtk.OptionMenu opt = (Gtk.OptionMenu) o;
			TypeMenuItem mi = (TypeMenuItem) ((ArrayList) opt.Data["items"])[opt.History];

			if (this.hit_type == mi.Type)
				return;

			this.hit_type = mi.Type;

			root.SetSource (this.hit_type);
			root.HitCollection.PageFirst ();
			UpdatePage ();

			//if (this.query != null && lastType != this.hit_type) {
			//	Search (entry.GtkEntry.Text);
			//	lastType = this.hit_type;
			//}
			//UpdatePage ();
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
				QueueDelayedQuery (entry.GtkEntry.Text);
				
				/* To translators: {0} represents the current query keywords */
				root.Error (String.Format (Catalog.GetString ("The query for <i>{0}</i> failed." +
						    "<br>The likely cause is that the beagle daemon isn't running."), entry.GtkEntry.Text));
				root.OfferDaemonRestart = true;
			} catch (Exception e) {
				/* To translators: {0} represents the current query keywords, {1} contains the errormessage */
				root.Error (String.Format (Catalog.GetString ("The query for <i>{0}</i> failed with error:<br>{1}<br>"), 
							   entry.GtkEntry.Text, e));
			}
		}

		private void Search (String searchString)
		{
			StoreSearch (searchString);
			entry.GtkEntry.Text = searchString;

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

		ArrayList recentSearches;
		
		private void StoreSearch (string newQuery)
		{
			if (recentSearches == null) {
				recentSearches = new ArrayList ();
			}

			if (newQuery != null && newQuery != "") {
				int duplicate = recentSearches.IndexOf (newQuery);
				if (duplicate >= 0) {
					recentSearches.RemoveAt (duplicate);
				}
				recentSearches.Insert (0, newQuery);				
			}

			if (recentSearches.Count == 11) {
				recentSearches.RemoveAt (10);
			}
		}
		
		public ArrayList RetriveSearches () 
		{
			return recentSearches;
		}

		public void ClearHistory ()
		{
			entry.ClearHistory ();
			recentSearches.Clear ();
		}
		
		public void QuickSearch (string query) 
		{
			Search (query);
		}
	}

	
}
