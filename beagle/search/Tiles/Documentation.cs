//
// Documentation.cs
//
// Copyright (C) 2007 Lukas Lipka <lukaslipka@gmail.com>
//

using System;
using Mono.Unix;

using Beagle.Util;

namespace Search.Tiles {

	public class DocumentationActivator : TileActivator {

		public DocumentationActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "File", null));
			AddSupportedFlavor (new HitFlavor (null, "DocbookEntry", null));
		}

		public override bool Validate (Beagle.Hit hit)
		{
			if (! base.Validate (hit))
				return false;
			
			if (hit.Type != "DocbookEntry" && hit ["beagle:FileType"] != "documentation")
				return false;
			
			Weight += 2;

			return true;
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new Documentation (hit, query);
		}
	}

	public class Documentation : TileTemplate {

		string path = null;

		public Documentation (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			if (! String.IsNullOrEmpty (hit.GetFirstProperty ("dc:title")))
				Title = hit.GetFirstProperty ("dc:title");
			else
				Title = hit.GetFirstProperty ("beagle:ExactFilename");

			if (hit ["beagle:IsChild"] == "true")
				path =	hit.ParentUri.LocalPath;
			else
				path = hit.Uri.LocalPath;

			Description = Catalog.GetString ("Documentation");
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			image.Pixbuf = WidgetFu.LoadThemeIcon ("gtk-help", size);
		}

		public override void Open ()
		{
			SafeProcess p = new SafeProcess ();
			p.Arguments = new string [] { "yelp", path };
			
			try {
				p.Start ();
			} catch {
				Console.WriteLine ("Failed to start '{0}'", p.Arguments [0]);
			}
		}
	}
}
