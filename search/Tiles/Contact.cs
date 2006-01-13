using System;

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

			if (hit.GetFirstProperty ("beagle:Photo") != null) {
				Icon = new Gdk.Pixbuf (hit.GetFirstProperty ("beagle:Photo"));
				Icon = Icon.ScaleSimple (32, 32, Gdk.InterpType.Bilinear); // FIXME: Fix scaling
			} else {
				Icon = WidgetFu.LoadThemeIcon ("stock_person", 32); // FIXME:
			}

			Title = hit.GetFirstProperty ("fixme:Name");
			Description = hit.GetFirstProperty ("fixme:Email");

			if (hit.GetFirstProperty ("fixme:BusinessPhone") != null)
				Description += ", " + hit.GetFirstProperty ("fixme:BusinessPhone");
			else if (hit.GetFirstProperty ("fixme:HomePhone") != null)
				Description += ", " + hit.GetFirstProperty ("fixme:HomePhone");
		}
	}
}
