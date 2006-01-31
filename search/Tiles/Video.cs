using System;
using Mono.Unix;

namespace Search.Tiles {

	public class VideoActivator : TileActivator {

		public VideoActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "File", "video/*"));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new Video (hit, query);
		}
	}

	public class Video : TileFile {

		public Video (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Video;

			Icon = WidgetFu.LoadThumbnailIcon (Hit.Uri, 32);

			if (Icon == null)
				Icon = WidgetFu.LoadMimeIcon (Hit ["beagle:MimeType"], 32);

			// FIXME: We need filters for video in Beagle.
			// They should land soon when entagged-sharp gets video support.
			Title = Hit.GetFirstProperty ("beagle:ExactFilename");
			Description = Catalog.GetString ("Unknown duration"); // FIXME: Duration from filters
		}
	}
}
