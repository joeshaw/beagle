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

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			image.Pixbuf = GetIcon (size);
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddBoldLabel (Title, 0, 1);
			details.AddLabel (Hit.GetFirstProperty ("fixme:Org"), 1, 1);
			details.AddLabel (Hit.GetFirstProperty ("fixme:Title"), 2, 1);
			details.AddLabel ("", 3, 1);

			details.AddLabelPair (Catalog.GetString ("E-Mail:"),
					      Hit.GetFirstProperty ("fixme:Email"),
					      4, 1);
			details.AddLabelPair (Catalog.GetString ("Mobile Phone:"),
					      Hit.GetFirstProperty ("fixme:MobilePhone"),
					      5, 1);
			if (Hit.GetFirstProperty ("fixme:BusinessPhone") != null) {
				details.AddLabelPair (Catalog.GetString ("Work Phone:"),
						      Hit.GetFirstProperty ("fixme:BusinessPhone"),
						      6, 1);
			} else {
				details.AddLabelPair (Catalog.GetString ("Home Phone:"),
						      Hit.GetFirstProperty ("fixme:HomePhone"),
						      6, 1);
			}

			return details;
		}
	}
}
