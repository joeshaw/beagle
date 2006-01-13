using System;

namespace Search.Tiles {

	public class Calendar : TileTemplate {

		public Calendar (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Icon = WidgetFu.LoadThemeIcon ("stock_calendar", 32);
			Title = hit.GetFirstProperty ("fixme:summary");
			Description = hit.GetFirstProperty ("fixme:description");
		}
	}
}
