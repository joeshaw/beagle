//
// Beagle tray icon.
//
//
// Copyright 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;

using Gtk;
using Gdk;

using Mono.Posix;

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
				    IntPtr.Zero, 
				    (ev == null) ? 0 : ev.Button, 
				    (ev == null) ? Gtk.Global.CurrentEventTime : ev.Time);
		}	
	}
	
	
	public class BestTray : Gtk.Plug
	{
		BestWindow win;
		
		Gtk.EventBox eventbox;
		Gtk.Tooltips tips;
		Beagle.Util.XKeybinder keybinder;

		[DllImport ("libtrayiconglue")]
		private static extern IntPtr egg_tray_icon_new (string name);

		public BestTray (BestWindow bw)
		{
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
			Application.Quit ();
		}
		
		void QuickSearchEvent (object sender, EventArgs args) 
		{			
			string quickQuery = (string)((Gtk.Widget) sender).Data ["Query"];
						
			if (! win.WindowIsVisible) {
				win.Present ();
				win.QuickSearch (quickQuery);
			} else {
				win.QuickSearch (quickQuery);
			}
			
		}
		
		private Gtk.Menu MakeMenu (Gtk.Widget parent) 
		{
			Gtk.Menu menu = new Gtk.Menu ();
			menu.AttachToWidget (parent, null);
			
			Gtk.ImageMenuItem item;
						
			// Quick Search menu items
			ArrayList list = win.RetriveSearches ();
			if (list == null || list.Count == 0 ) {
				item = new Gtk.ImageMenuItem (Catalog.GetString ("No Recent Searches"));
				item.Sensitive = false;
				menu.Append (item);
			} else {
				item = new Gtk.ImageMenuItem (Catalog.GetString ("Recent Searches"));
				item.Sensitive = false;
				item.Image = new Gtk.Image (Images.GetPixbuf ("icon-search.png"));
				menu.Append (item);

				foreach (string s in list) {
					item = new Gtk.ImageMenuItem (s);
					item.Data ["Query"] = s;
					item.Activated += new EventHandler (QuickSearchEvent);
					menu.Append (item);
				}
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
