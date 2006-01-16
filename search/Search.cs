using System;
using System.Collections;

using Gtk;
using Beagle;
using Mono.Unix;

using Search.Tiles;
using Search.Tray;

namespace Search {

	public class MainWindow : Window {

		Search.GroupView view;
		Search.Entry entry;
		Search.Spinner spinner;
		Gtk.Tooltips tips;
		Gtk.Notebook pages;
		Search.Pages.QuickTips quicktips;
		Search.Pages.StartDaemon startdaemon;
		Search.Pages.NoMatch nomatch;
		Search.Panes panes;
		Search.Tray.TrayIcon tray;

		uint timeout;

		string queryText;
		Beagle.Query currentQuery;
		Search.ScopeType scope = ScopeType.Everywhere;
		Search.SortType sort = SortType.Relevance;
		Search.TypeFilter filter = null;

		private static bool icon_enabled = false;

		public static void Main (string [] args)
		{
			string query = ParseArgs (args);

			Gnome.Program program = new Gnome.Program ("search", "0.0",
								   Gnome.Modules.UI, args);

			MainWindow window = new MainWindow ();

			if (query != null && query != "" && !icon_enabled) {
				window.entry.Text = query;
				window.Search ();
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
					icon_enabled = true;
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

			BorderWidth = 3;
			DefaultWidth = 700;
			DefaultHeight = 550;
			DeleteEvent += OnWindowDelete;
			
			VBox vbox = new VBox ();
			vbox.Spacing = 3;

			UIManager uim = new UIManager (this);
			uim.ScopeChanged += OnScopeChanged;
			uim.SortChanged += OnSortChanged;
			uim.ShowQuickTips += OnShowQuickTips;
			vbox.PackStart (uim.MenuBar, false, false, 0);
			
			HBox hbox = new HBox (false, 0);

			Label label = new Label ("_Find:");
			hbox.PackStart (label, false, false, 6);
			
			entry = new Entry ();
			label.MnemonicWidget = entry;
			uim.FocusSearchEntry += delegate () { entry.GrabFocus (); };
			entry.Activated += OnEntryActivated;
			entry.Changed += OnEntryChanged;
			hbox.PackStart (entry, true, true, 0);

			Gtk.Button button = new Gtk.Button ();
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

			vbox.PackStart (hbox, false, true, 6);

			pages = new Gtk.Notebook ();
			pages.ShowTabs = false;
			pages.ShowBorder = false;
			vbox.PackStart (pages, true, true, 0);

			quicktips = new Pages.QuickTips ();
			quicktips.Show ();
			pages.Add (quicktips);

			startdaemon = new Pages.StartDaemon ();
			startdaemon.DaemonStarted += Search;
			startdaemon.Show ();
			pages.Add (startdaemon);

			panes = new Search.Panes ();
			panes.Show ();
			pages.Add (panes);

			view = new GroupView ();
			view.TileSelected += ShowInformation;
			panes.MainContents = view;
			
			Add (vbox);

			tips = new Gtk.Tooltips ();
			tips.SetTip (entry, Catalog.GetString ("Type in search terms"), "");
			tips.SetTip (button, Catalog.GetString ("Start searching"), "");
			tips.Enable ();

			pages.CurrentPage = pages.PageNum (quicktips);

			if (icon_enabled) {
				tray = new TrayIcon ();
				tray.Clicked += OnTrayActivated;
				tray.Search += OnTraySearch;
			} else {
				ShowAll ();
			}
		}

		Gtk.Widget oldFocus;

		private void SetWindowTitle (string query)
		{
			Title = String.Format ("Desktop Search: {0}", query);
		}

		private void Search ()
		{
			if (timeout != 0) {
				GLib.Source.Remove (timeout);
				timeout = 0;
			}

			string query = queryText = entry.Text;
			if (query == null || query == "")
				return;

			SetWindowTitle (query);

			if (tray != null) {
				tray.AddSearch (query);
			}

			filter = TypeFilter.MakeFilter (ref query);

			view.Clear ();
			view.Scope = scope;
			view.Sort = sort;
			pages.CurrentPage = pages.PageNum (panes);
			oldFocus = Focus;

			try {
				currentQuery = new Query ();
				currentQuery.AddDomain (QueryDomain.Neighborhood);
				currentQuery.AddText (query);
				currentQuery.HitsAddedEvent += OnHitsAdded;
				currentQuery.HitsSubtractedEvent += OnHitsSubtracted;
				currentQuery.FinishedEvent += OnFinished;

				currentQuery.SendAsync ();
				spinner.Start ();
			} catch (Beagle.ResponseMessageException e){
				pages.CurrentPage = pages.PageNum (startdaemon);
			} catch (Exception e) {
				Console.WriteLine ("Querying the Beagle daemon failed: {0}", e.Message);
			}
		}

		private void OnEntryActivated (object obj, EventArgs args)
		{
			Search ();
		}

		private void OnEntryChanged (object obj, EventArgs args)
		{
			if (timeout != 0)
				GLib.Source.Remove (timeout);
			timeout = GLib.Timeout.Add (1000, OnEntryTimeout);
		}

		private bool OnEntryTimeout ()
		{
			timeout = 0;
			Search ();
			return false;
		}

		private void OnButtonClicked (object obj, EventArgs args)
		{
			Search ();
		}

		private void OnWindowDelete (object o, Gtk.DeleteEventArgs args)
		{
			if (icon_enabled) {
				Hide ();
				args.RetVal = true;
			} else {
				Gtk.Application.Quit ();
			}
		}

		private void OnScopeChanged (Search.ScopeType newScope)
		{
			view.Scope = scope = newScope;
			CheckNoMatch ();
		}

		private void OnSortChanged (Search.SortType newSort)
		{
			view.Sort = sort = newSort;
		}

		private void OnShowQuickTips ()
		{
			pages.CurrentPage = pages.PageNum (quicktips);
		}

		private void ShowInformation (Tiles.Tile tile)
		{
			if (tile != null)
				panes.Details = tile.Details;
			else
				panes.Details = null;
		}

		private void OnFinished (FinishedResponse response)
		{
			spinner.Stop ();
			currentQuery = null;
			view.Finished (oldFocus == Focus);

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
			if (!Visible) {
				ShowAll ();
				entry.GrabFocus ();
			}
		}

		private void OnTraySearch (string query)
		{
			if (!Visible)
				ShowAll ();

			entry.Text = query;
			Search ();
		}
	}
}
