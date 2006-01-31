using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Search.Tiles {

	public class ApplicationActivator : TileActivator {

		public ApplicationActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, null, "application/x-desktop"));
		}

		[DllImport ("libgnome-desktop-2.so.2")]
		static extern IntPtr gnome_desktop_item_new_from_uri (string uri, int flags, IntPtr error);

		[DllImport ("libgnome-desktop-2.so.2")]
		static extern string gnome_desktop_item_get_string (IntPtr ditem, string attr);

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			IntPtr ditem = gnome_desktop_item_new_from_uri (hit.UriAsString, 0, IntPtr.Zero);
			if (ditem == IntPtr.Zero)
				return null;

			string notshow = gnome_desktop_item_get_string (ditem, "NotShowIn");
			if (notshow != null && notshow.IndexOf ("GNOME") != -1)
				return null;

			string onlyshow = gnome_desktop_item_get_string (ditem, "OnlyShowIn");
			if (onlyshow != null && onlyshow.IndexOf ("GNOME") == -1)
				return null;

			return new Application (hit, query, ditem);
		}
	}

	public class Application : TileTemplate {

		IntPtr ditem;

		public Application (Beagle.Hit hit, Beagle.Query query, IntPtr ditem) : base (hit, query)
		{
			this.ditem = ditem;

			Group = TileGroup.Application;
			Title = Hit.GetFirstProperty ("fixme:Name");
			Description = Hit ["fixme:Comment"];
		
			// FIXME: Some icons do not fit the requested size,
			// should we scale them manually?
			Icon = LookupIcon (Hit);

			AddAction (new TileAction (Catalog.GetString ("Move to trash"), Gtk.Stock.Delete, MoveToTrash));
		}

		private static Gdk.Pixbuf LookupIcon (Beagle.Hit hit)
		{
			Gdk.Pixbuf icon = null;
			string path = hit ["fixme:Icon"];
			
			if (path != null && path != "") {
				if (path.StartsWith ("/")) {
					icon = new Gdk.Pixbuf (path);
				} else {
					if (path.EndsWith (".png")) 
						icon = WidgetFu.LoadThemeIcon (path.Substring (0, path.Length-4), 32);
					else
						icon = WidgetFu.LoadThemeIcon (path, 32);
					
					if (icon == null) {
						string kde_path = Beagle.Util.KdeUtils.LookupIcon (path);
				
						if (System.IO.File.Exists (kde_path))
							icon = new Gdk.Pixbuf (kde_path);
					}
				}
			}

			// FIXME: What icon should we use?
			if (icon == null)
				icon = WidgetFu.LoadMimeIcon (hit ["beagle:MimeType"], 32);

			return icon;
		}

		[DllImport ("libgnome-desktop-2.so.2")]
		static extern int gnome_desktop_item_launch (IntPtr ditem, IntPtr file_list, int flags, IntPtr error);

		public override void Open ()
		{
			if (gnome_desktop_item_launch (ditem, IntPtr.Zero, 0, IntPtr.Zero) == -1)
				Console.WriteLine ("Unable to launch application");
		}

		public void MoveToTrash ()
		{
			// FIXME: What is the default way to uninstall an application
			// in a distro-independent way?

			// FIXME: The chance that the code below works is 1:100 :-)
			ProcessStartInfo pi = new ProcessStartInfo ("rpm");
			pi.Arguments = String.Format ("-e {0}", Hit ["fixme:Exec"]);
			//Process.Start (pi); // FIXME: Safe sex

			Console.WriteLine ("Would run 'rpm {0}'", pi.Arguments);
		}
	}
}
