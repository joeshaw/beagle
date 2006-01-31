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

			string path = Hit ["fixme:cachedimg"];
			if (path != null && File.Exists (path))
				Icon = new Gdk.Pixbuf (path, 32, 32);
			else
				Icon = WidgetFu.LoadThemeIcon ("gnome-fs-bookmark", 32); // FIXME: RSS icon?

			Title = Hit ["dc:title"];
			Description = Hit ["dc:creator"]; // FIXME: Blog name
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
			table.Attach (label, 0, 1, 0, 1, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (Hit ["dc:title"]);
			WidgetFu.EllipsizeLabel (label);
			table.Attach (label, 1, 2, 0, 1, expand, fill, 0, 0);
			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Site:"));
			table.Attach (label, 0, 1, 1, 2, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (Hit ["dc:creater"]); // FIXME: Blog name
			WidgetFu.EllipsizeLabel (label);
			table.Attach (label, 1, 2, 1, 2, expand, fill, 0, 0);
			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Date Viewed:"));
			table.Attach (label, 2, 3, 0, 1, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (Utils.NiceShortDate (Hit.Timestamp));
			table.Attach (label, 3, 4, 0, 1, fill, fill, 0, 0);

			Gdk.Pixbuf img;
			string path = Hit ["fixme:cachedimg"];
			if (path != null && File.Exists (path))
				img = new Gdk.Pixbuf (path, 48, 48);
			else
				img = WidgetFu.LoadThemeIcon ("gnome-fs-bookmark", 48);

			Gtk.Image icon = new Gtk.Image (img);
			table.Attach (icon, 0, 1, 2, 3, fill, fill, 0, 0);

			snippet_label = WidgetFu.NewLabel ();
			snippet_label.Markup = snippet;
			table.Attach (snippet_label, 1, 4, 2, 3, expand, expand, 48, 0);

			if (!found_snippet)
				RequestSnippet ();

			table.WidthRequest = 0;
			table.ShowAll ();

			return table;
		}

		protected override void GotSnippet (string snippet, bool found)
		{
			found_snippet = found;
			snippet_label.Markup = snippet;
		}

		public override void Open ()
		{
			base.OpenFromUri (Hit ["dc:identifier"]);
		}
	}
}
