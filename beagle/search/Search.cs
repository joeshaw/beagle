//
// Search.cs
//
// Copyright (c) 2006 Novell, Inc.
//

using System;
using System.Reflection;
using System.Collections;
using System.Diagnostics;

using Gtk;
using Mono.Unix;

using Beagle;
using Beagle.Util;

using Search.Tiles;
using Search.Tray;

[assembly: AssemblyTitle ("beagle-search")]
[assembly: AssemblyDescription ("GUI interface to the Beagle search system")]

namespace Search {

	public class MainWindow : Window {

		private Gtk.Button button;
		private Gtk.Tooltips tips;
		private Gtk.Notebook pages;
		private Gtk.Statusbar statusbar;

		private Search.UIManager uim;
		private Search.NotificationArea notification_area;
		private Search.GroupView view;
		private Search.Entry entry;
		private Search.Spinner spinner;
		private Search.Panes panes;
		private Search.Tray.TrayIcon tray;

		private Search.Pages.QuickTips quicktips;
		private Search.Pages.RootUser rootuser;
		private Search.Pages.StartDaemon startdaemon;
		private Search.Pages.NoMatch nomatch;

		private uint timeout;

		private string query_text;
		private Beagle.Query current_query;
		private Search.ScopeType scope = ScopeType.Everything;
		private Search.SortType sort = SortType.Modified;
		private Search.TypeFilter filter = null;
		private bool show_details = true;
		private int total_matches = -1;

		private XKeybinder keybinder = new XKeybinder ();

		public static bool IconEnabled = false;

		public static void Main (string [] args)
		{
			SystemInformation.SetProcessName ("beagle-search");
			Catalog.Init ("beagle", ExternalStringsHack.LocaleDir);

			string query = ParseArgs (args);

			Gnome.Program program = new Gnome.Program ("search", "0.0", Gnome.Modules.UI, args);

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

				case "--version":
					VersionFu.PrintVersion ();
					Environment.Exit (0);
					break;

				case "--icon":
					IconEnabled = true;
					break;

				case "--autostarted":
					// FIXME: This option is deprecated and will be removed in a future release.
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
			VersionFu.PrintHeader ();

			string usage =
				"Usage: beagle-search [OPTIONS] [<query string>]\n\n" +
				"Options:\n" +
				"  --icon\t\t\tAdd an icon to the notification area rather than opening a search window.\n" +
				"  --help\t\t\tPrint this usage message.\n" +
				"  --version\t\t\tPrint version information.\n";

			Console.WriteLine (usage);
			System.Environment.Exit (0);
		}

		public MainWindow () : base (WindowType.Toplevel)
		{
			Title = Catalog.GetString ("Desktop Search");
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
				entry.Changed += OnEntryChanged;
				entry.MoveCursor += OnEntryMoveCursor;
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

			this.statusbar = new Gtk.Statusbar ();
			vbox.PackEnd (this.statusbar, false, false, 0);
			
			Add (vbox);

			tips = new Gtk.Tooltips ();
			tips.SetTip (entry, Catalog.GetString ("Type in search terms"), "");
			tips.SetTip (button, Catalog.GetString ("Start searching"), "");
			tips.Enable ();

			if (Environment.UserName == "root" &&
			    ! Conf.Daemon.GetOption (Conf.Names.AllowRoot, false)) {
				pages.CurrentPage = pages.PageNum (rootuser);
				entry.Sensitive = button.Sensitive = uim.Sensitive = false;
			} else {
				pages.CurrentPage = pages.PageNum (quicktips);
			}

			if (IconEnabled) {
				tray = new Search.Tray.TrayIcon ();
				tray.Clicked += OnTrayActivated;
				tray.Search += OnTraySearch;

				Config config = Conf.Get (Conf.Names.BeagleSearchConfig);
				bool binding_ctrl = config.GetOption (Conf.Names.KeyBinding_Ctrl, false);
				bool binding_alt = config.GetOption (Conf.Names.KeyBinding_Alt, false);
				string binding_key = config.GetOption (Conf.Names.KeyBinding_Key, "F12");

				string binding = new KeyBinding (binding_key, binding_ctrl, binding_alt).ToString ();
				string tip_text = Catalog.GetString ("Desktop Search");

				if (binding != String.Empty) {
					tip_text += String.Format (" ({0})", binding);

					// Attach the hide/show keybinding
					keybinder.Bind (binding, OnTrayActivated);
				}

				tray.TooltipText = tip_text;
			} else {
				ShowAll ();
			}

			StartCheckingIndexingStatus ();
		}

		private void SetWindowTitle (string query)
		{
			Title = String.Format ( Catalog.GetString ("Desktop Search: {0}"), query);
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

		// Whether we should grab focus from the text entry
		private bool grab_focus;

		private void Search (bool grab_focus)
		{
			if (timeout != 0) {
				GLib.Source.Remove (timeout);
				timeout = 0;
			}

			string query = query_text = entry.Text;
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
				if (current_query != null) {
					TotalMatches = -1;
					current_query.HitsAddedEvent -= OnHitsAdded;
					current_query.HitsSubtractedEvent -= OnHitsSubtracted;
					current_query.Close ();
				}

				TotalMatches = 0;

				current_query = new Query ();
				current_query.AddDomain (QueryDomain.Neighborhood);

				// Don't search documentation by default
				QueryPart_Property part = new QueryPart_Property ();
				part.Logic = QueryPartLogic.Prohibited;
				part.Type = PropertyType.Keyword;
				part.Key = "beagle:Source";
				part.Value = "documentation";
				current_query.AddPart (part);

				current_query.AddText (query);
				current_query.HitsAddedEvent += OnHitsAdded;
				current_query.HitsSubtractedEvent += OnHitsSubtracted;
				current_query.FinishedEvent += OnFinished;

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
			show_details = active;
			if (panes.Details != null)
				panes.ToggleDetails (show_details);
			else
				panes.ToggleDetails (false);
		}

		private void OnShowQuickTips ()
		{
			if (current_query != null) {
				TotalMatches = -1;
				current_query.HitsAddedEvent -= OnHitsAdded;
				current_query.HitsSubtractedEvent -= OnHitsSubtracted;
				current_query.Close ();
				current_query = null;
			}

			pages.CurrentPage = pages.PageNum (quicktips);
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
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "beagle-settings";
			p.StartInfo.Arguments = "--networking";

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Could not start beagle-settings: {0}", e);
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

		//////////////////////////////////////

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
	}
}
