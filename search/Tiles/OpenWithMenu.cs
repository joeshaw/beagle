using System;
using System.Collections;
using System.Runtime.InteropServices;
using Gtk;
using Gdk;
using Gnome.Vfs;

namespace Search.Tiles {

	public class OpenWithMenu : Gtk.Menu {

		public delegate void OpenWithHandler (MimeApplication app);
		public event OpenWithHandler ApplicationActivated;

		private ArrayList list = null;

		private bool show_icons = false;
		public bool ShowIcons {
			get { return show_icons; }
			set { show_icons = value; }
		}

		static OpenWithMenu ()
		{
			Gnome.Vfs.Vfs.Initialize ();
		}

		public OpenWithMenu (string mime)
		{
			list = GetApplications (mime);

			foreach (MimeApplication app in list) {
				ApplicationMenuItem i = new ApplicationMenuItem (this, app);
				i.Activated += HandleItemActivated;
				Append (i);
			}
		}
	
		public void AppendToMenu (Gtk.Menu menu)
		{
			if (list.Count < 1)
				return;

			Gtk.MenuItem open_with = new Gtk.MenuItem (Mono.Unix.Catalog.GetString ("Open With"));
			open_with.Submenu = this;
			open_with.ShowAll ();
			menu.Append (open_with);
		}

		private ArrayList GetApplications (string mime)
		{
			if (mime == null || mime == "")
				return null;

			ArrayList list = new ArrayList ();
		
			MimeApplication [] apps = Gnome.Vfs.Mime.GetAllApplications (mime);

			foreach (MimeApplication app in apps) {
				// Skip apps that don't take URIs
				if (! app.SupportsUris ())
					continue;
				
				if (! list.Contains (app))
					list.Add (app);
			}

			return list;
		}
	
		private void HandleItemActivated (object sender, EventArgs args)
		{
			if (ApplicationActivated != null)
				ApplicationActivated ((sender as ApplicationMenuItem).App);
		}
	
		private class ApplicationMenuItem : ImageMenuItem {
			public MimeApplication App;

			public ApplicationMenuItem (OpenWithMenu menu, MimeApplication mime_application) : base (mime_application.Name)
			{
				App = mime_application;
			
				if (menu.ShowIcons) {
					//System.Console.WriteLine ("icon = {0}", mime_application.Icon);
				
					// FIXME this is stupid, the mime_application.Icon is sometimes just a file name
					// and sometimes a full path.
					//int w, h;
					//w = h = (int) IconSize.Menu;
					//string icon = mime_application.Icon;
					//Console.WriteLine ("w/h = {0}", w);

					//Pixbuf img = new Pixbuf (icon, w, h);
					//Image = new Gtk.Image (mime_application.Icon);

					/*if (Image == null)
					  Image = new Gtk.Image ("/usr/share/pixmaps/" + mime_application.Icon);
				
					  if (Image == null)
					  Image = new Gtk.Image ("/usr/share/icons/gnome/24x24/apps/" + mime_application.Icon);

					  if (Image != null)
					  (Image as Gtk.Image).IconSize = Gtk.IconSize.Menu;*/
				}
			}
		}
	}
}
