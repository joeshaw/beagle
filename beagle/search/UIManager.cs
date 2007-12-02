using Gtk;
using Mono.Unix;
using System;
using System.Diagnostics;

using Beagle;
using Beagle.Util;

namespace Search {
	[Flags]
	public enum ScopeType : ushort {
		Nothing       = 0,
		Applications  = 1 << 0,
		Calendar      = 1 << 1,
		Contacts      = 1 << 2,
		Documents     = 1 << 3,
		Conversations = 1 << 4,
		Images        = 1 << 5,
		Media         = 1 << 6,
		Folders       = 1 << 7,
		Websites      = 1 << 8,
		Feeds         = 1 << 9,
		Archives      = 1 << 10,
		Everything    = UInt16.MaxValue // Lame but there's no way to do ~0 in a ushort way.
	}

	public enum SortType {
		Relevance,
		Name,
		Modified
	}

	public class UIManager : Gtk.UIManager {

		private MainWindow main_window;
		
		private Gtk.ActionGroup actions;
		private Gtk.RadioActionEntry[] sort_entries;
		private Gtk.ToggleActionEntry[] scope_entries, view_entries, domain_entries;

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
				new ActionEntry ("Scope", null,
						 Catalog.GetString ("Show _Categories"),
						 null, null, null),
				new ActionEntry ("Domain", null,
						 Catalog.GetString ("Search _Domains"),
						 null, null, null),
				new ActionEntry ("Actions", null,
						 Catalog.GetString ("_Actions"),
						 null, null, null),
				new ActionEntry ("View", null,
						 Catalog.GetString ("_View"),
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
						 OnFocusSearchEntry),
				new ActionEntry ("HideWindow", null, "",
						 "Escape", null,
						 OnHideWindow),
				new ActionEntry ("HideWindow2", null, "",
						 "<control>W", null,
						 OnHideWindow)
			};
			actions.Add (entries);

			Gtk.ActionEntry[] multiscope_entries = new ActionEntry[] {
				new ActionEntry ("All", null,
						 Catalog.GetString ("_All"),
						 null, null,
						 delegate {
							 if (ScopeChanged != null)
								 ScopeChanged (ScopeType.Everything, true);

							 foreach (ToggleActionEntry t in scope_entries)
								 ((ToggleAction) actions [t.name]).Active = true;
						 }),
				new ActionEntry ("None", null,
						 Catalog.GetString ("_None"),
						 null, null,
						 delegate {
							 if (ScopeChanged != null)
								 ScopeChanged (ScopeType.Nothing, true);

							 foreach (ToggleActionEntry t in scope_entries)
								 ((ToggleAction) actions [t.name]).Active = false;
						 })
			};
			actions.Add (multiscope_entries);

			scope_entries = new ToggleActionEntry[] {
				new ToggleActionEntry ("Applications", null,
						      Catalog.GetString ("A_pplications"),
						      null,
						      Catalog.GetString ("Search applications"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Contacts", null,
						      Catalog.GetString ("_Contacts"),
						      null,
						      Catalog.GetString ("Search contacts"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Calendar", null,
						      Catalog.GetString ("Ca_lendar events"),
						      null,
						      Catalog.GetString ("Search calendar events"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Documents", null,
						      Catalog.GetString ("_Documents"),
						      null,
						      Catalog.GetString ("Search documents"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Conversations", null,
						      Catalog.GetString ("Conve_rsations"),
						      null,
						      Catalog.GetString ("Search E-Mail and Instant Messaging logs"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Images", null,
						      Catalog.GetString ("_Images"),
						      null,
						      Catalog.GetString ("Search images"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Media", null,
						      Catalog.GetString ("_Media"),
						      null,
						      Catalog.GetString ("Search sound and video files"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Folders", null,
						      Catalog.GetString ("_Folders"),
						      null,
						      Catalog.GetString ("Search for folder names"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Websites", null,
						      Catalog.GetString ("_Websites"),
						      null,
						      Catalog.GetString ("Search website history"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Feeds", null,
						      Catalog.GetString ("_News Feeds"),
						      null,
						      Catalog.GetString ("Search news feeds"),
						      OnScopeChanged,
						      true),
				new ToggleActionEntry ("Archives", null,
						      Catalog.GetString ("A_rchives"),
						      null,
						      Catalog.GetString ("Search files in Archives"),
						      OnScopeChanged,
						      true)
			};
			actions.Add (scope_entries);

			sort_entries = new RadioActionEntry[] {
				new RadioActionEntry ("SortModified", null,
						      Catalog.GetString ("Sort by Date _Modified"), null,
						      Catalog.GetString ("Sort the most-recently-modified matches first"),
						      (int)SortType.Modified),
				new RadioActionEntry ("SortName", null,
						      Catalog.GetString ("Sort by _Name"), null,
						      Catalog.GetString ("Sort matches by name"),
						      (int)SortType.Name),
				new RadioActionEntry ("SortRelevance", null,
						      Catalog.GetString ("Sort by _Relevance"), null,
						      Catalog.GetString ("Sort the best matches first"),
						      (int)SortType.Relevance),
			};
			actions.Add (sort_entries, (int)SortType.Modified, OnSortChanged);

			domain_entries = new ToggleActionEntry [] {
				new ToggleActionEntry ("Local", null,
						       Catalog.GetString ("_Local"),
						       null,
						       Catalog.GetString ("Search only on local computer"),
						       OnDomainChanged,
						       true),
				new ToggleActionEntry ("Neighborhood", null,
						       Catalog.GetString ("_Neighborhood"),
						       null,
						       Catalog.GetString ("Search on computers near me"),
						       OnDomainChanged,
						       false)
			};
			actions.Add (domain_entries);

			view_entries = new ToggleActionEntry[] {
				new ToggleActionEntry ("ShowDetails", null,
						       Catalog.GetString ("Show Details"), null, null,
						       OnToggleDetails, true)
			};
			actions.Add (view_entries);

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

				foreach (Gtk.ToggleActionEntry rae in scope_entries)
					actions [rae.name].Sensitive = value;

				foreach (Gtk.RadioActionEntry rae in sort_entries)
					actions [rae.name].Sensitive = value;
			}
		}

		private const string ui_def =
		"<ui>" +
		"  <menubar name='MenuBar'>" +
		"    <menu action='Search'>" +
		"      <menu action='Scope'>" +
		"        <menuitem action='All'/>" +
		"        <menuitem action='None'/>" +
		"        <separator/>" +
		"        <menuitem action='Applications'/>" +
		"        <menuitem action='Contacts'/>" +
		"        <menuitem action='Calendar'/>" +
		"        <menuitem action='Documents'/>" +
		"        <menuitem action='Conversations'/>" +
		"        <menuitem action='Images'/>" +
		"        <menuitem action='Media'/>" +
		"        <menuitem action='Folders'/>" +
		"        <menuitem action='Websites'/>" +
		"        <menuitem action='Feeds'/>" +
		"        <menuitem action='Archives'/>" +
		"      </menu>" +

#if ENABLE_AVAHI
		"      <menu action='Domain'>" +
		"        <menuitem action='Local'/>" +
		"        <menuitem action='Neighborhood'/>" +
		"      </menu>" +
#endif

		"      <menuitem action='Preferences'/>" +
		"      <separator/>" +
		"      <menuitem action='Quit'/>" +
		"    </menu>" +
		"    <menu action='Actions'>" +
		"    </menu>" +
		"    <menu action='View'>" +
		"      <menuitem action='SortModified'/>" +
		"      <menuitem action='SortName'/>" +
		"      <menuitem action='SortRelevance'/>" +
		"      <separator/>" +
		"      <menuitem action='ShowDetails'/>" +
		"    </menu>" +
		"    <menu action='Help'>" +
		"      <menuitem action='Contents'/>" +
		"      <menuitem action='QuickTips'/>" +
		"      <menuitem action='About'/>" +
		"    </menu>" +
		"  </menubar>" +
		"  <accelerator action='FocusSearchEntry'/>" +
		"  <accelerator action='FocusSearchEntry2'/>" +
		"  <accelerator action='HideWindow'/>" +
		"  <accelerator action='HideWindow2'/>" +
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

		public delegate void ToggleDetailsDelegate (bool active);
		public event ToggleDetailsDelegate ToggleDetails;

		private void OnToggleDetails (object obj, EventArgs args)
		{
			if (ToggleDetails != null)
				ToggleDetails (((ToggleAction) obj).Active);
		}

		public delegate void ShowQuickTipsDelegate ();
		public event ShowQuickTipsDelegate ShowQuickTips;

		private void QuickTips (object obj, EventArgs args)
		{
			if (ShowQuickTips != null)
				ShowQuickTips ();
		}

		private void OnHideWindow (object obj, EventArgs args)
		{
			if (MainWindow.IconEnabled)
				main_window.Hide ();
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
			string address = "http://www.beagle-project.org/Getting_Started";

			try {
				Gnome.Url.Show (address);
			} catch {
				HigMessageDialog md = new HigMessageDialog (main_window, Gtk.DialogFlags.DestroyWithParent,
									    Gtk.MessageType.Error, Gtk.ButtonsType.Close,
									    Catalog.GetString ("Couldn't launch web browser"),
									    Catalog.GetString (String.Format ("Please point your web browser to '{0}' manually", address)));
				md.Run ();
				md.Destroy ();
			}
		}

		private void About (object obj, EventArgs args)
		{
			Gdk.Pixbuf logo = WidgetFu.LoadThemeIcon ("system-search", 48);

			string[] people = new string[] { "Anna Dirks <anna@novell.com>",
							 "Fredrik Hedberg <fredrik@avafan.com>",
							 "Lukas Lipka <lukaslipka@gmail.com>",
							 "Joe Shaw <joeshaw@novell.com>", 
							 "Jakub Steiner <jimmac@novell.com>",
							 "Dan Winship <danw@novell.com>" };
			
#pragma warning disable 612 // don't warn that Gnome.About is deprecated
			Gnome.About about = new Gnome.About ("Beagle Search",
							     Beagle.Util.ExternalStringsHack.Version,
							     "Copyright 2005-2007 Novell, Inc.",
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

		private void OnScopeChanged (object obj, EventArgs args)
		{
			if (ScopeChanged == null)
				return;

			ScopeType scope = (ScopeType) System.Enum.Parse (typeof (ScopeType), ((Action) obj).Name);			
			ScopeChanged (scope, ((ToggleAction) obj).Active);
		}

		public delegate void ScopeChangedDelegate (ScopeType scope, bool active);
		public event ScopeChangedDelegate ScopeChanged;

		private void OnSortChanged (object obj, Gtk.ChangedArgs args)
		{
			if (SortChanged != null)
				SortChanged ((SortType)args.Current.CurrentValue);
		}

		public delegate void SortChangedDelegate (SortType scope);
		public event SortChangedDelegate SortChanged;

		private void OnDomainChanged (object o, EventArgs args)
		{
			QueryDomain domain = (QueryDomain)Enum.Parse (typeof (QueryDomain), ((Action)o).Name);

			if (DomainChanged != null)
				DomainChanged (domain, ((ToggleAction)o).Active);
		}

		public delegate void DomainChangedDelegate (QueryDomain domain, bool active);
		public event DomainChangedDelegate DomainChanged;
	}
}
