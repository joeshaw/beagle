using System;
using Mono.Unix;

using Beagle.Util;

namespace Search.Tiles {

	public class WebHistoryActivator : TileActivator {

		public WebHistoryActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "WebHistory", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new WebHistory (hit, query);
		}
	}


	public class WebHistory : TileTemplate {

		public WebHistory (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Website;
			Title = hit.GetFirstProperty ("dc:title");
			Description = hit.Uri.ToString ();
		}

		// We intentionally use a separate thumbnailer/thread from Tiles.File,
		// because the web thumbnailer is much slower and we don't want it to
		// hold up the image/document thumbnails

		static ThumbnailFactory thumbnailer = new ThumbnailFactory ();

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			if (!thumbnailer.SetThumbnailIcon (image, Hit, size))
				base.LoadIcon (image, size);
		}

		public override void Open ()
		{
			
			base.OpenFromUri (UriFu.UriToEscapedString(Hit.Uri));
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Title:"), Title);
			details.AddLabelPair (Catalog.GetString ("URL:"), Hit.Uri.ToString ());
			details.AddLabelPair (Catalog.GetString ("Accessed:"), Utils.NiceLongDate (Timestamp));
			details.AddSnippet ();

			return details;
		}
	}
}
