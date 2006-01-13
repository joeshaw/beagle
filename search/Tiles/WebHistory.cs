using System;

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
			Icon = WidgetFu.LoadThemeIcon ("gnome-fs-bookmark", 32);
			Title = hit.GetFirstProperty ("dc:title");
			Description = hit.Uri.ToString ();
		}

		public override void Open ()
		{
			base.OpenFromMime (Hit);
		}
	}
}
