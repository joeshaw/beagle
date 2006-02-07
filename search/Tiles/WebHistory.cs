using System;
using Mono.Unix;

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
			base.OpenFromMime (Hit);
		}

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		private Gtk.Label snippet_label;
		private string snippet;
		private bool found_snippet;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (3, 4, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			Gtk.Label label;

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Title:"));
			table.Attach (label, 1, 2, 0, 1, fill, fill, 0, 0);

			label = WidgetFu.NewBoldLabel (Title);
			WidgetFu.EllipsizeLabel (label, 80);
			table.Attach (label, 2, 3, 0, 1, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("URL:"));
			table.Attach (label, 1, 2, 1, 2, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Hit.Uri.ToString ());
			WidgetFu.EllipsizeLabel (label, 80);
			table.Attach (label, 2, 3, 1, 2, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Accessed:"));
			table.Attach (label, 1, 2, 2, 3, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Utils.NiceLongDate (Hit.Timestamp));
			table.Attach (label, 2, 3, 2, 3, expand, fill, 0, 0);

			Gtk.Image icon = new Gtk.Image ();
			LoadIcon (icon, 128);
			table.Attach (icon, 0, 1, 0, 4, fill, fill, 0, 0);
			
			snippet_label = WidgetFu.NewLabel ();
			snippet_label.Markup = snippet;
			WidgetFu.EllipsizeLabel (snippet_label);
			table.Attach (snippet_label, 1, 3, 3, 4, expand, expand, 48, 0);

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
