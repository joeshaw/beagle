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

		public UIManager (Gtk.Window mainWindow)
		{
			Gtk.ActionGroup actions = new ActionGroup ("Actions");

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

				new ActionEntry ("Quit", Gtk.Stock.Quit,
						 null, "<control>Q",
						 Catalog.GetString ("Exit Desktop Search"),
						 Quit),
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
						 OnFocusSearchEntry)
			};
			actions.Add (entries);

			Gtk.RadioActionEntry[] scope_entries = new RadioActionEntry[] {
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

			Gtk.RadioActionEntry[] sort_entries = new RadioActionEntry[] {
				new RadioActionEntry ("Relevance", null,
						      Catalog.GetString ("_Relevance"), null,
						      Catalog.GetString ("Sort the best matches first"),
						      (int)SortType.Relevance),
				new RadioActionEntry ("Name", null,
						      Catalog.GetString ("_Name"), null,
						      Catalog.GetString ("Sort matches by name"),
						      (int)SortType.Name),
				new RadioActionEntry ("Modified", null,
						      Catalog.GetString ("Date _Modified"), null,
						      Catalog.GetString ("Sort the most-recently-modified matches first"),
						      (int)SortType.Modified),
			};
			actions.Add (sort_entries, (int)SortType.Relevance, OnSortChanged);

			InsertActionGroup (actions, 0);
			mainWindow.AddAccelGroup (AccelGroup);
			AddUiFromString (ui_def);
		}

		public Gtk.MenuBar MenuBar {
			get {
				return (Gtk.MenuBar)GetWidget ("/MenuBar");
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
		"      <menuitem action='Relevance'/>" +
		"      <menuitem action='Name'/>" +
		"      <menuitem action='Modified'/>" +
		"    </menu>" +
		"    <menu action='Help'>" +
		"      <menuitem action='Contents'/>" +
		"      <menuitem action='QuickTips'/>" +
		"      <menuitem action='About'/>" +
		"    </menu>" +
		"  </menubar>" +
		"  <accelerator action='FocusSearchEntry'/>" +
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

		private void QuickTips (object obj, EventArgs args)
		{
		}

		private void Quit (object obj, EventArgs args)
		{
			Gtk.Application.Quit ();
		}

		private void Help (object obj, EventArgs args)
		{
			Console.WriteLine ("Help!\n");
		}

		private void About (object obj, EventArgs args)
		{
			Gdk.Pixbuf logo = Beagle.Images.GetPixbuf ("system-search.png");

			string[] people = new string[] {"Dan Winship <danw@novell.com>", "Lukas Lipka <lukas@pmad.net>",
		       					 "Fredrik Hedberg <fredrik.hedberg@hedbergs.com>", "Joe Shaw <joeshaw@novell.com>"};
			string[] documentors = new string[] {""};
			
			Gnome.About about = new Gnome.About ("Desktop Search", "0.0",
							     "Copyright 2005 Novell, Inc.",
							     null, people, documentors, null,
							     logo);
			about.Run ();
			about.Dispose ();
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
