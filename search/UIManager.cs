using Gtk;
using Mono.Unix;
using System;
using System.Diagnostics;

namespace Search {

	public enum ScopeType {
		Everywhere,
		Applications,
		Contacts,
		Documents,
		Conversations,
		Images,
		Media
	}

	public enum SortType {
		Relevance,
		Name,
		Modified
	}

	public class UIManager : Gtk.UIManager {

		private MainWindow main_window;
		
		private Gtk.ActionGroup actions;
		private Gtk.RadioActionEntry[] scope_entries, sort_entries;

		public UIManager (MainWindow main_window)
		{
			this.main_window = main_window;
			
			actions = new ActionGroup ("Actions");

			ActionEntry quit_action_entry;
			if (MainWindow.IconEnabled) {
				quit_action_entry = new ActionEntry ("Quit", Gtk.Stock.Close,
								     null, "<control>Q",
						 		     Catalog.GetString ("Close Desktop Search"),
						 		     Quit);
			} else {
				quit_action_entry = new ActionEntry ("Quit", Gtk.Stock.Quit,
								     null, "<control>Q",
						 		     Catalog.GetString ("Exit Desktop Search"),
						 		     Quit);

			}

			Gtk.ActionEntry[] entries = new ActionEntry[] {
				new ActionEntry ("Search", null,
						 Catalog.GetString ("_Search"),
						 null, null, null),
				new ActionEntry ("Actions", null,
						 Catalog.GetString ("_Actions"),
						 null, null, null),
				new ActionEntry ("SortBy", null,
						 Catalog.GetString ("Sor_t"),
						 null, null, null),
				new ActionEntry ("Help", null,
						 Catalog.GetString ("_Help"),
						 null, null, null),

				quit_action_entry,
				new ActionEntry ("Preferences", Gtk.Stock.Preferences,
						 null, null,
						 Catalog.GetString ("Exit Desktop Search"),
						 Preferences),
				new ActionEntry ("Contents", Gtk.Stock.Help,
						 Catalog.GetString ("_Contents"),
						 "F1",
						 Catalog.GetString ("Help - Table of Contents"),
						 Help),
				new ActionEntry ("About", Gnome.Stock.About,
						 null, null,
						 Catalog.GetString ("About Desktop Search"),
						 About),
				new ActionEntry ("QuickTips", null,
						 Catalog.GetString ("Quick Tips"),
						 null, null, QuickTips),
				new ActionEntry ("FocusSearchEntry", null, "",
						 "<control>K", null,
						 OnFocusSearchEntry),
				new ActionEntry ("FocusSearchEntry2", null, "",
						 "<control>L", null,
						 OnFocusSearchEntry)
			};
			actions.Add (entries);

			scope_entries = new RadioActionEntry[] {
				new RadioActionEntry ("Everywhere", null,
						      Catalog.GetString ("_Everywhere"),
						      "<control>E",
						      Catalog.GetString ("Search everywhere"),
						      (int)ScopeType.Everywhere),
				new RadioActionEntry ("Applications", null,
						      Catalog.GetString ("_Applications"),
						      null,
						      Catalog.GetString ("Search applications"),
						      (int)ScopeType.Applications),
				new RadioActionEntry ("Contacts", null,
						      Catalog.GetString ("_Contacts"),
						      null,
						      Catalog.GetString ("Search contacts"),
						      (int)ScopeType.Contacts),
				new RadioActionEntry ("Documents", null,
						      Catalog.GetString ("_Documents"),
						      null,
						      Catalog.GetString ("Search documents"),
						      (int)ScopeType.Documents),
				new RadioActionEntry ("Conversations", null,
						      Catalog.GetString ("Conve_rsations"),
						      null,
						      Catalog.GetString ("Search E-Mail and Instant Messaging logs"),
						      (int)ScopeType.Conversations),
				new RadioActionEntry ("Images", null,
						      Catalog.GetString ("Images"),
						      null,
						      Catalog.GetString ("Search images"),
						      (int)ScopeType.Images),
				new RadioActionEntry ("Media", null,
						      Catalog.GetString ("Media"),
						      null,
						      Catalog.GetString ("Search sound and video files"),
						      (int)ScopeType.Media),
			};
			actions.Add (scope_entries, (int)ScopeType.Everywhere, OnScopeChanged);

			sort_entries = new RadioActionEntry[] {
				new RadioActionEntry ("Modified", null,
						      Catalog.GetString ("Date _Modified"), null,
						      Catalog.GetString ("Sort the most-recently-modified matches first"),
						      (int)SortType.Modified),
				new RadioActionEntry ("Name", null,
						      Catalog.GetString ("_Name"), null,
						      Catalog.GetString ("Sort matches by name"),
						      (int)SortType.Name),
				new RadioActionEntry ("Relevance", null,
						      Catalog.GetString ("_Relevance"), null,
						      Catalog.GetString ("Sort the best matches first"),
						      (int)SortType.Relevance),
			};
			actions.Add (sort_entries, (int)SortType.Modified, OnSortChanged);

			InsertActionGroup (actions, 0);
			main_window.AddAccelGroup (AccelGroup);
			AddUiFromString (ui_def);
		}

		public Gtk.MenuBar MenuBar {
			get {
				return (Gtk.MenuBar)GetWidget ("/MenuBar");
			}
		}

		private bool sensitive = true;
		public bool Sensitive {
			get { return this.sensitive; }
			set {
				this.sensitive = value;

				actions ["QuickTips"].Sensitive = value;

				foreach (Gtk.RadioActionEntry rae in scope_entries)
					actions [rae.name].Sensitive = value;

				foreach (Gtk.RadioActionEntry rae in sort_entries)
					actions [rae.name].Sensitive = value;
			}
		}

		private const string ui_def =
		"<ui>" +
		"  <menubar name='MenuBar'>" +
		"    <menu action='Search'>" +
		"      <menuitem action='Everywhere'/>" +
		"      <menuitem action='Applications'/>" +
		"      <menuitem action='Contacts'/>" +
		"      <menuitem action='Documents'/>" +
		"      <menuitem action='Conversations'/>" +
		"      <menuitem action='Images'/>" +
		"      <menuitem action='Media'/>" +
		"      <separator/>" +
		"      <menuitem action='Preferences'/>" +
		"      <menuitem action='Quit'/>" +
		"    </menu>" +
		"    <menu action='Actions'>" +
		"    </menu>" +
		"    <menu action='SortBy'>" +
		"      <menuitem action='Modified'/>" +
		"      <menuitem action='Name'/>" +
		"      <menuitem action='Relevance'/>" +
		"    </menu>" +
		"    <menu action='Help'>" +
		"      <menuitem action='Contents'/>" +
		"      <menuitem action='QuickTips'/>" +
		"      <menuitem action='About'/>" +
		"    </menu>" +
		"  </menubar>" +
		"  <accelerator action='FocusSearchEntry'/>" +
		"  <accelerator action='FocusSearchEntry2'/>" +
		"</ui>";

		private void Preferences (object obj, EventArgs args)
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "beagle-settings";

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Could not start beagle-settings: {0}", e);
			}
		}

		public delegate void ShowQuickTipsDelegate ();
		public event ShowQuickTipsDelegate ShowQuickTips;

		private void QuickTips (object obj, EventArgs args)
		{
			if (ShowQuickTips != null)
				ShowQuickTips ();
		}

		private void Quit (object obj, EventArgs args)
		{
			if (MainWindow.IconEnabled) {
				main_window.Hide ();
			} else {
				Gtk.Application.Quit ();
			}
		}

		private void Help (object obj, EventArgs args)
		{
			Gnome.Url.Show ("http://www.beagle-project.org/Getting_Started");
		}

		private void About (object obj, EventArgs args)
		{
			Gdk.Pixbuf logo = Beagle.Images.GetPixbuf ("system-search.png");

			string[] people = new string[] { "Anna Dirks <anna@novell.com>",
							 "Fredrik Hedberg <fredrik@avafan.com>",
							 "Lukas Lipka <lukas@pmad.net>",
							 "Joe Shaw <joeshaw@novell.com>", 
							 "Jakub Steiner <jimmac@novell.com>",
							 "Dan Winship <danw@novell.com>" };
			
#pragma warning disable 612 // don't warn that Gnome.About is deprecated
			Gnome.About about = new Gnome.About ("Beagle Search",
							     Beagle.Util.ExternalStringsHack.Version,
							     "Copyright 2005-2006 Novell, Inc.",
							     null, people, null, null,
							     logo);
			about.Run ();
			about.Dispose ();
#pragma warning restore 612
		}

		private void OnFocusSearchEntry (object obj, EventArgs args)
		{
			if (FocusSearchEntry != null)
				FocusSearchEntry ();
		}

		public delegate void FocusSearchEntryDelegate ();
		public event FocusSearchEntryDelegate FocusSearchEntry;

		private void OnScopeChanged (object obj, Gtk.ChangedArgs args)
		{
			if (ScopeChanged != null)
				ScopeChanged ((ScopeType)args.Current.CurrentValue);
		}

		public delegate void ScopeChangedDelegate (ScopeType scope);
		public event ScopeChangedDelegate ScopeChanged;

		private void OnSortChanged (object obj, Gtk.ChangedArgs args)
		{
			if (SortChanged != null)
				SortChanged ((SortType)args.Current.CurrentValue);
		}

		public delegate void SortChangedDelegate (SortType scope);
		public event SortChangedDelegate SortChanged;
	}
}
