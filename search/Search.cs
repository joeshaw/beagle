using System;
using System.Collections;

using Gtk;
using Beagle;
using Beagle.Util;
using Mono.Unix;

using Search.Tiles;
using Search.Tray;

namespace Search {

	public class MainWindow : Window {

		UIManager uim;
		Search.GroupView view;
		Search.Entry entry;
		Gtk.Button button;
		Search.Spinner spinner;
		Gtk.Tooltips tips;
		Gtk.Notebook pages;
		Search.Pages.QuickTips quicktips;
		Search.Pages.RootUser rootuser;
		Search.Pages.StartDaemon startdaemon;
		Search.Pages.NoMatch nomatch;
		Search.Panes panes;
		Search.Tray.TrayIcon tray;

		uint timeout;

		string queryText;
		Beagle.Query currentQuery;
		Search.ScopeType scope = ScopeType.Everything;
		Search.SortType sort = SortType.Modified;
		Search.TypeFilter filter = null;
		bool showDetails = true;

		XKeybinder keybinder = new XKeybinder ();

		public static bool IconEnabled = false;

		public static void Main (string [] args)
		{
			SystemInformation.SetProcessName ("beagle-search");

			Catalog.Init ("beagle", ExternalStringsHack.LocaleDir);

			string query = ParseArgs (args);

			Gnome.Program program = new Gnome.Program ("search", "0.0",
								   Gnome.Modules.UI, args);

			MainWindow window = new MainWindow ();

			if (query != null && query != "" && !IconEnabled) {
				window.entry.Text = query;
				window.Search (true);
			}

			program.Run ();
		}

		private static string ParseArgs (String[] args)
		{
			string query = "";
			int i = 0;

			while (i < args.Length) {
				switch (args [i]) {
				case "--help":
				case "--usage":
					PrintUsageAndExit ();
					return null;

				case "--icon":
					IconEnabled = true;
					break;

				case "--autostarted":
					if (! Conf.Searching.Autostart) {
						Console.WriteLine ("beagle-search: Autostarting is disabled, not starting");
						Environment.Exit (0);
					}
					break;

				// Ignore session management
				case "--sm-config-prefix":
				case "--sm-client-id":
				case "--screen":
					// These all take an argument, so
					// increment i
					i++;
					break;

				default:
					if (args [i].Length < 2 || args [i].Substring (0, 2) != "--") {
						if (query.Length != 0)
							query += " ";
						query += args [i];
					}
					break;
				}

				i++;
			}

			return query;
		}

		public static void PrintUsageAndExit ()
		{
			string usage =
				"beagle-search: GUI interface to the Beagle search system.\n" +
				"Web page: http://www.beagle-project.org/\n" +
				"Copyright (C) 2005-2006 Novell, Inc.\n\n";

			usage +=
				"Usage: beagle-search [OPTIONS] [<query string>]\n\n" +
				"Options:\n" +
				"  --help\t\t\tPrint this usage message.\n" +
				"  --icon\t\t\tAdd an icon to the notification area rather than opening a search window.\n";

			Console.WriteLine (usage);
			System.Environment.Exit (0);
		}

		public MainWindow () : base (WindowType.Toplevel)
		{
			Title = "Desktop Search";
			Icon = Beagle.Images.GetPixbuf ("system-search.png");

			DefaultWidth = 700;
			DefaultHeight = 550;
			DeleteEvent += OnWindowDelete;
			
			VBox vbox = new VBox ();
			vbox.Spacing = 3;

			uim = new UIManager (this);
			uim.ScopeChanged += OnScopeChanged;
			uim.SortChanged += OnSortChanged;
			uim.ToggleDetails += OnToggleDetails;
			uim.ShowQuickTips += OnShowQuickTips;
			vbox.PackStart (uim.MenuBar, false, false, 0);

			HBox padding_hbox = new HBox ();

			HBox hbox = new HBox (false, 6);
			
			Label label = new Label (Catalog.GetString ("_Find:"));
			hbox.PackStart (label, false, false, 0);
			
			entry = new Entry ();
			label.MnemonicWidget = entry;
			uim.FocusSearchEntry += delegate () { entry.GrabFocus (); };
			entry.Activated += OnEntryActivated;
			entry.Changed += OnEntryChanged;
			entry.MoveCursor += OnEntryMoveCursor;
			hbox.PackStart (entry, true, true, 0);

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

			padding_hbox.PackStart (hbox, true, true, 9);

			vbox.PackStart (padding_hbox, false, true, 6);

			pages = new Gtk.Notebook ();
			pages.ShowTabs = false;
			pages.ShowBorder = false;
			pages.BorderWidth = 3;
			vbox.PackStart (pages, true, true, 0);

			quicktips = new Pages.QuickTips ();
			quicktips.Show ();
			pages.Add (quicktips);

			rootuser = new Pages.RootUser ();
			rootuser.Show ();
			pages.Add (rootuser);

			startdaemon = new Pages.StartDaemon ();
			startdaemon.DaemonStarted += OnDaemonStarted;
			startdaemon.Show ();
			pages.Add (startdaemon);

			panes = new Search.Panes ();
			panes.Show ();
			pages.Add (panes);

			view = new GroupView ();
			view.TileSelected += ShowInformation;
			view.CategoryToggled += OnCategoryToggled;
			panes.MainContents = view;
			
			Add (vbox);

			tips = new Gtk.Tooltips ();
			tips.SetTip (entry, Catalog.GetString ("Type in search terms"), "");
			tips.SetTip (button, Catalog.GetString ("Start searching"), "");
			tips.Enable ();

			if (Environment.UserName == "root" && ! Conf.Daemon.AllowRoot) {
				pages.CurrentPage = pages.PageNum (rootuser);
				entry.Sensitive = button.Sensitive = uim.Sensitive = false;
			} else {
				pages.CurrentPage = pages.PageNum (quicktips);
			}

			if (IconEnabled) {
				tray = new Search.Tray.TrayIcon ();
				tray.Clicked += OnTrayActivated;
				tray.Search += OnTraySearch;

				// Attach the hide/show keybinding
				keybinder.Bind (Conf.Searching.ShowSearchWindowBinding.ToString (), OnTrayActivated);
			} else {
				ShowAll ();
			}
		}

		private void SetWindowTitle (string query)
		{
			Title = String.Format ("Desktop Search: {0}", query);
		}

		// Whether we should grab focus from the text entry
		private bool grab_focus;

		private void Search (bool grab_focus)
		{
			if (timeout != 0) {
				GLib.Source.Remove (timeout);
				timeout = 0;
			}

			string query = queryText = entry.Text;
			if (query == null || query == "")
				return;

			SetWindowTitle (query);
			ShowInformation (null);

			if (tray != null) {
				tray.AddSearch (query);
			}

			filter = TypeFilter.MakeFilter (ref query);

			view.Clear ();
			view.Scope = scope;
			view.Sort = sort;
			pages.CurrentPage = pages.PageNum (panes);

			this.grab_focus = grab_focus;

			try {
				if (currentQuery != null) {
					currentQuery.HitsAddedEvent -= OnHitsAdded;
					currentQuery.HitsSubtractedEvent -= OnHitsSubtracted;
					currentQuery.Close ();
				}

				currentQuery = new Query ();
				currentQuery.AddDomain (QueryDomain.Neighborhood);

				// Don't search documentation by default
				QueryPart_Property part = new QueryPart_Property ();
				part.Logic = QueryPartLogic.Prohibited;
				part.Type = PropertyType.Keyword;
				part.Key = "beagle:Source";
				part.Value = "documentation";
				currentQuery.AddPart (part);

				currentQuery.AddText (query);
				currentQuery.HitsAddedEvent += OnHitsAdded;
				currentQuery.HitsSubtractedEvent += OnHitsSubtracted;
				currentQuery.FinishedEvent += OnFinished;

				currentQuery.SendAsync ();
				spinner.Start ();
			} catch (Beagle.ResponseMessageException){
				pages.CurrentPage = pages.PageNum (startdaemon);
			} catch (Exception e) {
				Console.WriteLine ("Querying the Beagle daemon failed: {0}", e.Message);
			}
		}

		private void OnEntryActivated (object obj, EventArgs args)
		{
			Search (true);
		}

		private void OnDaemonStarted ()
		{
			Search (true);
		}

		private void OnEntryChanged (object obj, EventArgs args)
		{
			if (timeout != 0)
				GLib.Source.Remove (timeout);
			timeout = GLib.Timeout.Add (1000, OnEntryTimeout);
		}

		private void OnEntryMoveCursor (object obj, EventArgs args)
		{
			if (timeout != 0)
				GLib.Source.Remove (timeout);
			timeout = GLib.Timeout.Add (1000, OnEntryTimeout);
		}

		private bool OnEntryTimeout ()
		{
			timeout = 0;
			Search (false);
			return false;
		}

		private void OnButtonClicked (object obj, EventArgs args)
		{
			Search (true);
		}

		private void OnWindowDelete (object o, Gtk.DeleteEventArgs args)
		{
			if (IconEnabled) {
				Hide ();
				args.RetVal = true;
			} else {
				Gtk.Application.Quit ();
			}
		}

		private void OnScopeChanged (Search.ScopeType toggled, bool active)
		{
			if (active)
				view.Scope = scope = scope | toggled;
			else
				view.Scope = scope = scope ^ toggled;
			
			CheckNoMatch ();
		}
		
		private void OnCategoryToggled (ScopeType toggled)
		{
			string name =  ScopeType.GetName (typeof (ScopeType), toggled);
			try {
				ToggleAction act = (ToggleAction) uim.GetAction ("/ui/MenuBar/Search/Scope/" +  name);
				act.Active = ! act.Active;
			}
			catch (Exception ex) {
				Console.WriteLine("Exception caught when trying to deactivate menu entry {0}:",name);
				Console.WriteLine(ex);
				return;
			}
		}
		

		private void OnSortChanged (Search.SortType newSort)
		{
			view.Sort = sort = newSort;
		}

		private void OnToggleDetails (bool active)
		{
			showDetails = active;
			if (panes.Details != null)
				panes.ToggleDetails (showDetails);
			else
				panes.ToggleDetails (false);
		}

		private void OnShowQuickTips ()
		{
			if (currentQuery != null) {
				currentQuery.HitsAddedEvent -= OnHitsAdded;
				currentQuery.HitsSubtractedEvent -= OnHitsSubtracted;
				currentQuery.Close ();
				currentQuery = null;
			}

			pages.CurrentPage = pages.PageNum (quicktips);
		}

		private void ShowInformation (Tiles.Tile tile)
		{
			if (tile != null) {
				panes.Details = tile.Details;
				if (tile.Details != null)
					panes.ToggleDetails (showDetails);
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
				Tile tile = TileActivatorOrg.MakeTile (hit, currentQuery);
				if (tile == null)
					continue;

				if (filter != null && !filter.Filter (tile))
					continue;

				view.AddHit (tile);
				if (pages.CurrentPageWidget != panes)
					pages.CurrentPage = pages.PageNum (panes);
			}
		}

		private void OnHitsSubtracted (HitsSubtractedResponse response)
		{
			foreach (Uri uri in response.Uris)
				view.SubtractHit (uri);

			CheckNoMatch ();
		}

		private void CheckNoMatch ()
		{
			MatchType matches = view.MatchState;
			if (matches == MatchType.Matched) {
				pages.CurrentPage = pages.PageNum (panes);
				return;
			}

			if (nomatch != null)
				nomatch.Destroy ();
			nomatch = new Pages.NoMatch (queryText, matches == MatchType.NoneInScope);
			nomatch.Show ();
			pages.Add (nomatch);
			pages.CurrentPage = pages.PageNum (nomatch);
		}

		/////////////////////////////////////

		private void OnTrayActivated (object o, EventArgs args)
		{
			if (! Visible) {
				base.ShowAll ();
				base.Present ();
				entry.GrabFocus ();
			} else {
				base.Hide ();
			}
		}

		private void OnTraySearch (string query)
		{
			if (!Visible)
				ShowAll ();

			entry.Text = query;
			Search (true);
		}
	}
}
