using System;
using System.Diagnostics;
using Mono.Unix;
using Beagle.Util;

namespace Search.Tiles {

	public class ArchivedFileActivator : TileActivator {

		public ArchivedFileActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "File", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new TileArchivedFile (hit, query);
		}

		public override bool Validate (Beagle.Hit hit)
		{
			if (! base.Validate (hit))
				return false;

			string str = hit.GetFirstProperty ("fixme:inside_archive");
			if (hit.ParentUri == null || str == null || str == "false")
				return false;

			Weight += 1;
			
			return true;
		}
	}

	public class TileArchivedFile : TileFile {

		public TileArchivedFile (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Description = String.Format (Catalog.GetString ("Inside archive {0}"), GetTitle (hit, true));
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			base.LoadIcon (image, size);

			string parent_mime_type = XdgMime.GetMimeTypeFromFileName (Hit.EscapedParentUri);

			if (parent_mime_type == null)
				return;

			Gdk.Pixbuf emblem = WidgetFu.LoadMimeIcon (parent_mime_type, 24);

			if (emblem == null)
				return;

			Gdk.Pixbuf icon = image.Pixbuf.Copy ();

			emblem.Composite (icon, 
					  0,                                // dest_x
					  icon.Height - emblem.Height,      // dest_y
					  emblem.Width,                     // dest_width
					  emblem.Height,                    // dest_height
					  0,                                // offset_x
					  icon.Height - emblem.Height,      // offset_y
					  1, 1,                             // scale
					  Gdk.InterpType.Bilinear, 255);

			image.Pixbuf.Dispose ();
			image.Pixbuf = icon;
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Title:"), GetTitle (Hit));
			details.AddLabelPair (Catalog.GetString ("File Name:"), Hit.Uri.Fragment.Substring (1)); // Substring to move past the initial #
			details.AddLabelPair (Catalog.GetString ("Inside File:"), Hit.Uri.LocalPath);

			if (Hit ["dc:author"] != null)
				details.AddLabelPair (Catalog.GetString ("Author:"), Hit ["dc:author"]);

			details.AddSnippet ();

			return details;
		}

	}
}
