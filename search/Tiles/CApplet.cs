using System;
using System.Collections;
using System.Diagnostics;
using Mono.Posix;

namespace Search.Tiles {

	public class CAppletActivator : TileActivator {

		public CAppletActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, null, "application/x-desktop"));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new CApplet (hit, query);
		}

		public override bool Validate (Beagle.Hit hit)
		{
			if (! base.Validate (hit))
				return false;

			ICollection categories = hit.GetProperties ("fixme:Categories");

			if (categories == null || categories.Count < 1)
				return false;

			foreach (string cat in categories) {
				if (cat == "Settings") {
					Weight += 1;
					return true;
				}
			}
			
			return false;
		}
	}

	public class CApplet : TileTemplate {

		public CApplet (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Application;
			Title = Hit.GetFirstProperty ("fixme:Name");
			Description = Hit ["fixme:Comment"];
		
			// FIXME: Some icons do not fit the requested size,
			// should we scale them manually?
			Icon = LookupIcon (Hit ["fixme:Icon"]);
		}

		private static Gdk.Pixbuf LookupIcon (string path)
		{
			if (path == null)
				return null;
			
			Gdk.Pixbuf icon;

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

			if (icon == null)
				icon = WidgetFu.LoadThemeIcon (Gtk.Stock.Execute, 32);

			return icon;
		}

		public override void Open ()
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = true;
			p.StartInfo.FileName = Hit ["fixme:Exec"];
			
			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
		}
	}
}
