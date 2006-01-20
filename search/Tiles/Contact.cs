using System;
using Mono.Unix;
using Beagle.Util;

namespace Search.Tiles {

	public class ContactActivator : TileActivator {

		public ContactActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "Contact", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new Contact (hit, query);
		}
	}

	public class Contact : TileTemplate {

		public Contact (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Contact;

			Icon = GetIcon (32);
			Title = hit.GetFirstProperty ("fixme:Name");
			Description = hit.GetFirstProperty ("fixme:Email");
		}

		private Gdk.Pixbuf GetIcon (int size)
		{
			if (Hit.GetFirstProperty ("beagle:Photo") != null) {
				Gdk.Pixbuf icon = new Gdk.Pixbuf (Hit.GetFirstProperty ("beagle:Photo"));
				return icon.ScaleSimple (size, size, Gdk.InterpType.Bilinear);
			} else
				return WidgetFu.LoadThemeIcon ("stock_person", size);
		}

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (7, 3, false);
			table.RowSpacing = 6;
			table.ColumnSpacing = 12;

			Gtk.Label label;

			label = WidgetFu.NewBoldLabel (Title);
			table.Attach (label, 1, 3, 0, 1, expand, fill, 0, 0);

			label = WidgetFu.NewLabel (Hit.GetFirstProperty ("fixme:Org"));
			table.Attach (label, 1, 3, 1, 2, expand, fill, 0, 0);

			label = WidgetFu.NewLabel (Hit.GetFirstProperty ("fixme:Title"));
			table.Attach (label, 1, 3, 2, 3, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("E-Mail:"));
			table.Attach (label, 1, 2, 4, 5, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Hit.GetFirstProperty ("fixme:Email"));
			table.Attach (label, 2, 3, 4, 5, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Mobile Phone:"));
			table.Attach (label, 1, 2, 5, 6, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Hit.GetFirstProperty ("fixme:MobilePhone"));
			table.Attach (label, 2, 3, 5, 6, expand, fill, 0, 0);

			if (Hit.GetFirstProperty ("fixme:BusinessPhone") != null) {
				label = WidgetFu.NewGrayLabel (Catalog.GetString ("Work Phone:"));
				table.Attach (label, 1, 2, 6, 7, fill, fill, 0, 0);
				label = WidgetFu.NewLabel (Hit.GetFirstProperty ("fixme:BusinessPhone"));
				table.Attach (label, 2, 3, 6, 7, expand, fill, 0, 0);
			} else {
				label = WidgetFu.NewGrayLabel (Catalog.GetString ("Home Phone:"));
				table.Attach (label, 1, 2, 6, 7, fill, fill, 0, 0);
				label = WidgetFu.NewLabel (Hit.GetFirstProperty ("fixme:HomePhone"));
				table.Attach (label, 2, 3, 6, 7, expand, fill, 0, 0);
			}

			Gtk.AspectFrame frame = new Gtk.AspectFrame (null, 0.5f, 0.5f, 1.0f, false);
			Gtk.Image icon = new Gtk.Image (GetIcon (64));
			frame.Add (icon);
			frame.ShadowType = Gtk.ShadowType.None;
			table.Attach (frame, 0, 1, 0, 6, fill, fill, 0, 0);

			table.WidthRequest = 0;
			table.ShowAll ();

			return table;
		}
	}
}
