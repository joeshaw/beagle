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

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		private Gtk.Label snippet_label;
		private string snippet;
		private bool found_snippet;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (4, 4, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			Gtk.Label label;

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Title:"));
			table.Attach (label, 0, 1, 0, 1, fill, fill, 0, 0);

			label = WidgetFu.NewBoldLabel (GetTitle ());
			table.Attach (label, 1, 4, 0, 1, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Last Edited:"));
			table.Attach (label, 0, 1, 1, 2, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Utils.NiceLongDate (Hit.Timestamp));
			table.Attach (label, 1, 2, 1, 2, expand, fill, 0, 0);

			if (Hit ["dc:author"] != null) {
				label = WidgetFu.NewGrayLabel (Catalog.GetString ("Author:"));
				table.Attach (label, 2, 3, 1, 2, fill, fill, 0, 0);

				label = WidgetFu.NewBoldLabel (Hit ["dc:author"]);
				table.Attach (label, 3, 4, 1, 2, expand, fill, 0, 0);
			}

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Full Path:"));
			table.Attach (label, 0, 1, 2, 3, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Hit.Uri.LocalPath);
			WidgetFu.EllipsizeLabel (label, 80);
			table.Attach (label, 1, 2, 2, 3, expand, fill, 0, 0);

			Gtk.Image icon = new Gtk.Image (Icon.Pixbuf);
			table.Attach (icon, 0, 1, 3, 4, fill, fill, 0, 0);
			
			snippet_label = WidgetFu.NewLabel ();
			snippet_label.Markup = snippet;
			WidgetFu.EllipsizeLabel (snippet_label);
			table.Attach (snippet_label, 1, 4, 3, 4, expand, expand, 48, 0);

			if (! found_snippet)
				RequestSnippet ();

			table.WidthRequest = 0;
			table.ShowAll ();

			return table;
		}

		protected override void GotSnippet (string snippet, bool found)
		{
			found_snippet = found;
			this.snippet = snippet;
			snippet_label.Markup = snippet;
		}
	}
}
