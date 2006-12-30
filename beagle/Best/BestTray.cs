//
// Beagle tray icon.
//
//
// Copyright 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Gtk;
using Gdk;

using Mono.Unix;

using Beagle;
using Beagle.Tile;
using Beagle.Util;

namespace Best {

	// This class is from Tomboy, removed some functions which weren't needed
	public class GuiUtils 
	{
		public static void GetMenuPosition (Gtk.Menu menu,
						    out int  x, 
						    out int  y, 
						    out bool push_in)
		{
			Gtk.Requisition menu_req = menu.SizeRequest ();

			menu.AttachWidget.GdkWindow.GetOrigin (out x, out y);

			if (y + menu_req.Height >= menu.AttachWidget.Screen.Height)
				y -= menu_req.Height;
			else
				y += menu.AttachWidget.Allocation.Height;

			push_in = true;
		}

		static void DeactivateMenu (object sender, EventArgs args) 
		{
			Gtk.Menu menu = (Gtk.Menu) sender;
			menu.Popdown ();
		}

		// Place the menu underneath an arbitrary parent widget.  The
		// parent widget must be set using menu.AttachToWidget before
		// calling this
		public static void PopupMenu (Gtk.Menu menu, Gdk.EventButton ev)
		{
			menu.Deactivated += DeactivateMenu;
			menu.Popup (null, 
				    null, 
				    new Gtk.MenuPositionFunc (GetMenuPosition), 
				    (ev == null) ? 0 : ev.Button, 
				    (ev == null) ? Gtk.Global.CurrentEventTime : ev.Time);
		}	
	}
	
	
	public class BestTray : Gtk.Plug
	{
		BestWindow win;
		bool autostarted = false;
		
		Gtk.EventBox eventbox;
		Gtk.Tooltips tips;
		Beagle.Util.XKeybinder keybinder;

		[DllImport ("libbeagleuiglue")]
		private static extern IntPtr egg_tray_icon_new (string name);

		public BestTray (BestWindow bw, bool autostarted)
		{
			this.autostarted = autostarted;

			Raw = egg_tray_icon_new ("Search");

			win = bw;
			win.DeleteEvent += new DeleteEventHandler (WindowDeleteEvent);
						
			eventbox = new Gtk.EventBox ();
			eventbox.CanFocus = true;
			eventbox.ButtonPressEvent += new ButtonPressEventHandler (ButtonPress);
			
			Gdk.Pixbuf smalldog = Images.GetPixbuf ("best.png");
			eventbox.Add (new Gtk.Image (smalldog.ScaleSimple (24, 24, Gdk.InterpType.Hyper)));

			KeyBinding binding = Conf.Searching.ShowSearchWindowBinding;

			string tooltip = String.Format ("Beagle Search ({0})", binding.ToReadableString ());
			tips = new Gtk.Tooltips ();
			tips.SetTip (eventbox, tooltip, null);
			tips.Enable ();
			
			Add (eventbox);
			eventbox.ShowAll ();

			keybinder = new Beagle.Util.XKeybinder ();
			keybinder.Bind (binding.ToString (),
					new EventHandler (ShowBeaglePressed));
		}

		private void ShowBeaglePressed (object o, EventArgs args)
		{
			if (!win.WindowIsVisible) {
				win.Present ();
				win.FocusEntry ();
			} else {
				win.Hide ();
			}
		}

		void ButtonPress (object sender, Gtk.ButtonPressEventArgs args) 
		{
			Gdk.EventButton eb = args.Event;
			if (eb.Button == 1) {
				if (! win.WindowIsVisible) {
					win.Present ();
					win.FocusEntry ();
				} else {
					win.Hide ();
				}
			} else {
			
				Gtk.Menu recent_menu = MakeMenu ((Gtk.Widget) sender);
				GuiUtils.PopupMenu (recent_menu, args.Event);
			}
		}
				
		void WindowDeleteEvent (object sender, DeleteEventArgs args)
		{
			win.Hide ();
			args.RetVal = (object)true;
		}

		void QuitEvent (object sender, EventArgs args)
		{
			if (autostarted) {
				HigMessageDialog dialog = new HigMessageDialog (win,
										DialogFlags.Modal,
										MessageType.Question,
										ButtonsType.YesNo,
										Catalog.GetString ("Disable Searching"), 
										Catalog.GetString ("You are about to close the search tray. The search tray is automatically started at login-time. Would you like to disable it for future sessions?"));

				Gtk.ResponseType response = (Gtk.ResponseType) dialog.Run ();
				
				if (response == Gtk.ResponseType.Yes) {
					Conf.Searching.Autostart = false;
					Conf.Save (true);
					
					// If the user doesn't want to have Best autostart, he probably doesn't 
					// want to keep the daemon around either. Bad call dude, bad call.
					
					Process p = new Process ();
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.FileName = "beagle-shutdown";

					try {
						p.Start ();
					} catch (Exception ex) {}
				}
			}
			win.StoreSettingsInConf (true);
			Application.Quit ();
		}
		
		void QuickSearchEvent (object sender, EventArgs args) 
		{		
			string quickQuery = (string) menu_to_query_map [sender];
						
			if (! win.WindowIsVisible) {
				win.Present ();
				win.QuickSearch (quickQuery);
			} else {
				win.QuickSearch (quickQuery);
			}
			
		}

		private void ClearEvent (object sender, EventArgs args) 
		{			
			win.ClearHistory ();
		}

		private void DetachWidget (Gtk.Widget attach_widget, Gtk.Menu menu)
		{
		}

		Hashtable menu_to_query_map = null;

		private Gtk.Menu MakeMenu (Gtk.Widget parent) 
		{
			Gtk.Menu menu = new Gtk.Menu ();
			menu.AttachToWidget (parent, new Gtk.MenuDetachFunc (DetachWidget));
			
			Gtk.ImageMenuItem item;
						
			// Quick Search menu items
			ArrayList list = win.RetriveSearches ();
			if (list == null || list.Count == 0 ) {
				item = new Gtk.ImageMenuItem (Catalog.GetString ("No Recent Searches"));
				item.Sensitive = false;
				menu.Append (item);
				menu_to_query_map = null;
			} else {
				item = new Gtk.ImageMenuItem (Catalog.GetString ("Recent Searches"));
				item.Sensitive = false;
				item.Image = new Gtk.Image (Images.GetPixbuf ("icon-search.png"));
				menu.Append (item);

				menu_to_query_map = new Hashtable ();

				foreach (string s in list) {
					item = new Gtk.ImageMenuItem (s);
					item.Activated += new EventHandler (QuickSearchEvent);
					menu.Append (item);
					menu_to_query_map [item] = s;
				}
			}			

			if (list != null && list.Count > 0) {
				item = new Gtk.ImageMenuItem (Catalog.GetString ("Clear"));
				item.Image = new Gtk.Image (Gtk.Stock.Clear, Gtk.IconSize.Menu);
				item.Activated += new EventHandler (ClearEvent);
				menu.Append (item);
			}

			menu.Append (new Gtk.SeparatorMenuItem ());			
		
			item = new Gtk.ImageMenuItem (Catalog.GetString ("Quit"));
			item.Image = new Gtk.Image (Gtk.Stock.Quit, Gtk.IconSize.Menu);
			item.Activated += new EventHandler (QuitEvent);
			menu.Append (item);
			
			menu.ShowAll ();
			return menu;
		}	
	}
}
