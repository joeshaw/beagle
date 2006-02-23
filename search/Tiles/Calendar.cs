using System;
using Mono.Unix;

namespace Search.Tiles {

	public class CalendarActivator : TileActivator {

		public CalendarActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "Calendar", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new Calendar (hit, query);
		}
	}

	public class Calendar : TileTemplate {

		public Calendar (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			string summary = hit.GetFirstProperty ("fixme:summary");
			string time = Utils.NiceShortDate (hit.GetFirstProperty ("fixme:starttime"));
			Title = (time == "") ? summary : time + ": " + summary;
			Description = hit.GetFirstProperty ("fixme:description");
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			image.Pixbuf = WidgetFu.LoadThemeIcon ("stock_calendar", size);
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Title:"),
					      Title,
					      0, 1);
			details.AddLabelPair (Catalog.GetString ("Description:"),
					      Description,
					      1, 1);
			details.AddSnippet (2, 1);

			return details;
		}
	}
}
