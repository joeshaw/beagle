//
// SearchWindow.cs
//
// Copyright (c) 2006 Novell, Inc.
// Copyright (C) 2008 Lukas Lipka <lukaslipka@gmail.com>
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Gtk;
using Mono.Unix;

using Beagle;
using Beagle.Util;

using Beagle.Search.Tiles;

namespace Beagle.Search {

	public delegate void QueryEventDelegate (string query);

	public class SearchWindow : Window {

		private ISearch search = null;

		private Gtk.Button button;
		private Gtk.Tooltips tips;
		private Gtk.Notebook pages;
		private Gtk.Statusbar statusbar;

		private Beagle.Search.UIManager uim;
		private Beagle.Search.NotificationArea notification_area;
		private Beagle.Search.GroupView view;
		private Beagle.Search.Entry entry;
		private Beagle.Search.Spinner spinner;
		private Beagle.Search.Panes panes;

		private Beagle.Search.Pages.IndexInfo indexinfo;
		private Beagle.Search.Pages.QuickTips quicktips;
		private Beagle.Search.Pages.RootUser rootuser;
		private Beagle.Search.Pages.StartDaemon startdaemon;
		private Beagle.Search.Pages.NoMatch nomatch;

		private Beagle.Search.ScopeType scope = ScopeType.Everything;
		private Beagle.Search.SortType sort = SortType.Modified;
		private Beagle.Search.TypeFilter filter = null;
		private QueryDomain domain = QueryDomain.Local | QueryDomain.System; // default

		// Whether we should grab focus from the text entry
		private bool grab_focus = false;
		private uint timeout_id = 0;

		private Beagle.Query current_query = null;
		private string query_text = null;
		private bool show_details = true;
		private int total_matches = -1;

		public event QueryEventDelegate QueryEvent;

		public SearchWindow (ISearch search) : base (WindowType.Toplevel)
		{
			this.search = search;

			base.Title = Catalog.GetString ("Desktop Search");
			base.Icon = WidgetFu.LoadThemeIcon ("system-search", 16);
			base.DefaultWidth = 700;
			base.DefaultHeight = 550;
			base.DeleteEvent += OnWindowDelete;
			
			VBox vbox = new VBox ();
			vbox.Spacing = 3;

			uim = new UIManager (this);
			uim.DomainChanged += OnDomainChanged;
			uim.ScopeChanged += OnScopeChanged;
			uim.SortChanged += OnSortChanged;
			uim.ToggleDetails += OnToggleDetails;
			uim.ShowQuickTips += OnShowQuickTips;
			uim.ShowIndexInfo += OnShowIndexInfo;
			vbox.PackStart (uim.MenuBar, false, false, 0);

			HBox hbox = new HBox (false, 6);
			
			Label label = new Label (Catalog.GetString ("_Find:"));
			hbox.PackStart (label, false, false, 0);
			
			entry = new Entry ();
			entry.Activated += OnEntryActivated;
			hbox.PackStart (entry, true, true, 0);

			label.MnemonicWidget = entry;
			uim.FocusSearchEntry += delegate () { entry.GrabFocus (); };

			// The auto search after timeout feauture is now optional
			// and can be disabled.

			if (Conf.BeagleSearch.GetOption (Conf.Names.BeagleSearchAutoSearch, true)) {
				entry.Changed += OnEntryResetTimeout;
				entry.MoveCursor += OnEntryResetTimeout;
			}

			button = new Gtk.Button ();
			Gtk.HBox button_hbox = new Gtk.HBox (false, 2);
			Gtk.Image icon = new Gtk.Image (Gtk.Stock.Find, Gtk.IconSize.Button);
			button_hbox.PackStart (icon, false, false, 0);
			label = new Gtk.Label (Catalog.GetString ("Find Now"));
			button_hbox.PackStart (label, false, false, 0);
			button.Add (button_hbox);
			button.Clicked += OnButtonClicked;

			Gtk.VBox buttonVBox = new Gtk.VBox (false, 0);
			buttonVBox.PackStart (button, true, false, 0);
			hbox.PackStart (buttonVBox, false, false, 0);

			spinner = new Spinner ();
			hbox.PackStart (spinner, false, false, 0);

			HBox padding_hbox = new HBox ();
			padding_hbox.PackStart (hbox, true, true, 9);
			vbox.PackStart (padding_hbox, false, true, 6);

			VBox view_box = new VBox (false, 3);
			vbox.PackStart (view_box, true, true, 0);

			HBox na_padding = new HBox ();
			view_box.PackStart (na_padding, false, true, 0);

			notification_area = new NotificationArea ();
			na_padding.PackStart (notification_area, true, true, 3);

			pages = new Gtk.Notebook ();
			pages.ShowTabs = false;
			pages.ShowBorder = false;
			pages.BorderWidth = 3;
			view_box.PackStart (pages, true, true, 0);

			quicktips = new Pages.QuickTips ();
			quicktips.Show ();
			pages.Add (quicktips);

			indexinfo = new Pages.IndexInfo ();
			indexinfo.Show ();
			pages.Add (indexinfo);

			rootuser = new Pages.RootUser ();
			rootuser.Show ();
			pages.Add (rootuser);

			startdaemon = new Pages.StartDaemon ();
			startdaemon.DaemonStarted += OnDaemonStarted;
			startdaemon.Show ();
			pages.Add (startdaemon);

			panes = new Beagle.Search.Panes ();
			panes.Show ();
			pages.Add (panes);

			view = new GroupView ();
			view.TileSelected += ShowInformation;
			view.CategoryToggled += OnCategoryToggled;
			panes.MainContents = view;

			this.statusbar = new Gtk.Statusbar ();
			vbox.PackEnd (this.statusbar, false, false, 0);
			
			Add (vbox);

			tips = new Gtk.Tooltips ();
			tips.SetTip (entry, Catalog.GetString ("Type in search terms"), "");
			tips.SetTip (button, Catalog.GetString ("Start searching"), "");
			tips.Enable ();

			if (Environment.UserName == "root" && !Conf.Daemon.GetOption (Conf.Names.AllowRoot, false)) {
				pages.CurrentPage = pages.PageNum (rootuser);
				entry.Sensitive = button.Sensitive = uim.Sensitive = false;
			} else {
				pages.CurrentPage = pages.PageNum (quicktips);
			}

			StartCheckingIndexingStatus ();
		}

		private void SetWindowTitle (string query)
		{
			Title = String.Format (Catalog.GetString ("Desktop Search: {0}"), query);
		}

		public void GrabEntryFocus ()
		{
			entry.GrabFocus ();
		}

		public void Search (string query)
		{
			entry.Text = query;
			Query (true);
		}

		private void DetachQuery ()
		{
			if (current_query == null)
				return;

			current_query.HitsAddedEvent -= OnHitsAdded;
			current_query.HitsSubtractedEvent -= OnHitsSubtracted;
			current_query.Close ();
			TotalMatches = -1;
		}

		private void Query (bool grab_focus)
		{
			if (timeout_id != 0) {
				GLib.Source.Remove (timeout_id);
				timeout_id = 0;
			}

			string query = query_text = entry.Text;

			if (String.IsNullOrEmpty (query))
				return;

			SetWindowTitle (query);
			ShowInformation (null);

			if (QueryEvent != null)
				QueryEvent (query);

			filter = TypeFilter.MakeFilter (ref query);

			view.Clear ();
			view.Scope = scope;
			view.SortType = sort;
			pages.CurrentPage = pages.PageNum (panes);

			this.grab_focus = grab_focus;

			try {
				// Clean up our previous query, if any exists.
				DetachQuery ();

				TotalMatches = 0;

				current_query = new Query ();
				current_query.QueryDomain = domain;

				current_query.AddText (query);
				current_query.HitsAddedEvent += OnHitsAdded;
				current_query.HitsSubtractedEvent += OnHitsSubtracted;
				current_query.FinishedEvent += OnFinished;

				// Don't search documentation by default
				if (!search.DocsEnabled) {
					QueryPart_Property part = new QueryPart_Property ();
					part.Logic = QueryPartLogic.Prohibited;
					part.Type = PropertyType.Keyword;
					part.Key = "beagle:Source";
					part.Value = "documentation";
					current_query.AddPart (part);
				}

				current_query.SendAsync ();

				spinner.Start ();
			} catch (Beagle.ResponseMessageException) {
				pages.CurrentPage = pages.PageNum (startdaemon);
			} catch (Exception e) {
				Console.WriteLine ("Querying the Beagle daemon failed: {0}", e.Message);
			}
		}

		private void OnEntryActivated (object obj, EventArgs args)
		{
			Query (true);
		}

		private void OnDaemonStarted ()
		{
			Query (true);
		}

		private void OnEntryResetTimeout (object o, EventArgs args)
		{
			if (timeout_id != 0)
				GLib.Source.Remove (timeout_id);

			timeout_id = GLib.Timeout.Add (1000, OnEntryTimeout);
		}

		private bool OnEntryTimeout ()
		{
			timeout_id = 0;
			Query (false);

			return false;
		}

		private void OnButtonClicked (object obj, EventArgs args)
		{
			Query (true);
		}

		private void OnWindowDelete (object o, Gtk.DeleteEventArgs args)
		{
			// FIXME: Destroy window
			Hide ();
			args.RetVal = true;
		}

		private void OnScopeChanged (ScopeType toggled, bool active)
		{
			if (active) {
				view.Scope = scope = scope | toggled;
			} else {
				view.Scope = scope = scope ^ toggled;
			}
			
			CheckNoMatch ();
		}
		
		private void OnCategoryToggled (ScopeType toggled)
		{
			string name =  ScopeType.GetName (typeof (ScopeType), toggled);

			try {
				ToggleAction act = (ToggleAction) uim.GetAction ("/ui/MenuBar/Search/Scope/" +  name);
				act.Active = !act.Active;
			} catch (Exception e) {
				Console.WriteLine ("Exception caught when trying to deactivate menu entry {0}:",name);
				Console.WriteLine (e);
			}
		}
		
		private void OnSortChanged (SortType value)
		{
			view.SortType = sort = value;
		}

		private void OnToggleDetails (bool active)
		{
			show_details = active;
			if (panes.Details != null)
				panes.ToggleDetails (show_details);
			else
				panes.ToggleDetails (false);
		}

		private void OnShowQuickTips ()
		{
			DetachQuery ();
			pages.CurrentPage = pages.PageNum (quicktips);
		}
		
		private void OnShowIndexInfo ()
		{
			DetachQuery ();
			
			if (! indexinfo.Refresh ()) {
				pages.CurrentPage = pages.PageNum (startdaemon);
			} else {
				pages.CurrentPage = pages.PageNum (indexinfo);
			}
		}
		
		private void OnDomainChanged (QueryDomain domain, bool active)
		{
			if (active)
				this.domain |= domain;
			else
				this.domain &= ~domain;

			// FIXME: Most likely refire the query.
		}
		
		private void ShowInformation (Tiles.Tile tile)
		{
			if (tile != null) {
				panes.Details = tile.Details;
				if (tile.Details != null)
					panes.ToggleDetails (show_details);
				else
					panes.ToggleDetails (false);
			} else {
				panes.Details = null;
				panes.ToggleDetails (false);
			}
		}

		private void OnFinished (FinishedResponse response)
		{
			spinner.Stop ();
			view.Finished (grab_focus);
			grab_focus = false;

			CheckNoMatch ();
		}

		private void OnHitsAdded (HitsAddedResponse response)
		{
			foreach (Hit hit in response.Hits) {
				Tile tile = TileActivatorOrg.MakeTile (hit, current_query);

				if (tile == null) {
					Console.WriteLine ("No tile found for: {0} ({1})", hit.Uri, hit.Type);
					continue;
				}

				if (filter != null && !filter.Filter (tile))
					continue;

				view.AddHit (tile);

				if (pages.CurrentPageWidget != panes)
					pages.CurrentPage = pages.PageNum (panes);
			}

			if (response.NumMatches != -1)
				TotalMatches += response.NumMatches;
		}

		private void OnHitsSubtracted (HitsSubtractedResponse response)
		{
			foreach (Uri uri in response.Uris)
				view.SubtractHit (uri);

			TotalMatches -= response.Uris.Count;

			CheckNoMatch ();
		}

#if ENABLE_AVAHI
                private void OnUnknownHostFound (object sender, AvahiEventArgs args)
                {
			NotificationMessage m = new NotificationMessage ();
			m.Pixbuf = WidgetFu.LoadThemeIcon ("network-workgroup", 48);
			m.Title = Catalog.GetString ("There are computers near you running Beagle");
			m.Message = Catalog.GetString ("You can select to search other computers from the \"Search\" menu.");
			m.AddAction ("Configure", OnNetworkConfigure);

			notification_area.Display (m);
		}

		private void OnNetworkConfigure (object o, EventArgs args)
		{
			SafeProcess p = new SafeProcess ();
			p.Arguments = new string[] { "beagle-settings", "--networking" };

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Could not start beagle-settings:\n{0}", e);
			}
                }
#endif

		private void CheckNoMatch ()
		{
			MatchType matches = view.MatchState;

			if (matches == MatchType.Matched) {
				pages.CurrentPage = pages.PageNum (panes);
				return;
			}

			if (nomatch != null)
				nomatch.Destroy ();

			nomatch = new Pages.NoMatch (query_text, matches == MatchType.NoneInScope);
			nomatch.Show ();

			pages.Add (nomatch);
			pages.CurrentPage = pages.PageNum (nomatch);

			// Since there is no match, possibly the user wants to modify query; focus the search entry field.
			GrabEntryFocus ();
		}

		/////////////////////////////////////

		private void StartCheckingIndexingStatus ()
		{
			InformationalMessagesRequest msg_request = new InformationalMessagesRequest ();
			msg_request.IndexingStatusEvent += OnIndexingStatusEvent;
			msg_request.SendAsync ();
		}

		private void OnIndexingStatusEvent (IndexingStatus status)
		{
			if (status == IndexingStatus.Running) {
				NotificationMessage m = new NotificationMessage ();
				m.Icon = Gtk.Stock.DialogInfo;
				m.Title = Catalog.GetString ("Your data is being indexed");
				m.Message = Catalog.GetString ("The search service is in the process of indexing your data.  Search results may be incomplete until indexing has finished.");
				notification_area.Display (m);
			} else {
				notification_area.Hide ();
			}
		}

		/////////////////////////////////////

		public bool IconEnabled {
			get { return search.IconEnabled; }
		}

		private int TotalMatches {
			get { return this.total_matches; }
			set {
				if (this.total_matches != -1)
					this.statusbar.Pop (0);

				this.total_matches = value;
				
				if (this.total_matches > -1) {
					string message;
					int tile_count = view.TileCount;

					if (tile_count == this.total_matches)
						message = String.Format (Catalog.GetPluralString ("Showing {0} match", "Showing all {0} matches", this.total_matches), this.total_matches);
					else
						message = String.Format (Catalog.GetPluralString ("Showing the top {0} of {1} total matches", "Showing the top {0} of {1} total matches", this.total_matches), view.TileCount, this.total_matches);

					this.statusbar.Push (0, message);
				}
			}
		}
	}
}
