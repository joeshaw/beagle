using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using Mono.Unix;

namespace Search.Tiles {

	public class RSSFeedActivator : TileActivator {

		public RSSFeedActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "FeedItem", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new RSSFeed (hit, query);
		}
	}

	public class RSSFeed : TileTemplate {

		public RSSFeed (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Feed;

			Title = Hit ["dc:title"];
			Description = Hit ["dc:creator"]; // FIXME: Blog name
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			string path = Hit ["fixme:cachedimg"];
			if (path != null && File.Exists (path))
				image.Pixbuf = new Gdk.Pixbuf (path, size, size);
			else
				image.Pixbuf = WidgetFu.LoadThemeIcon ("gnome-fs-bookmark", size); // FIXME: RSS icon?
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Title:"),
					      Hit ["dc:title"],
					      0, 1);
			details.AddLabelPair (Catalog.GetString ("Site:"),
					      Hit ["dc:creator"], // FIXME: Blog name
					      1, 1);
			details.AddLabelPair (Catalog.GetString ("Date Viewed:"),
					      Utils.NiceShortDate (Timestamp),
					      1, 3);

			details.AddSnippet (2, 1);

			return details;
		}

		public override void Open ()
		{
			base.OpenFromUri (Hit ["dc:identifier"]);
		}
	}
}
