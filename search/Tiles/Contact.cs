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
			uint i = 0;
				
			details.AddBoldLabel (Title, i++, 1);
			
			string org = Hit.GetFirstProperty ("fixme:Org");
			string title = Hit.GetFirstProperty ("fixme:Title");
			string email = Hit.GetFirstProperty ("fixme:Email");
			string mobile_phone = Hit.GetFirstProperty ("fixme:MobilePhone");
			string work_phone = Hit.GetFirstProperty ("fixme:BusinessPhone");
			string home_phone = Hit.GetFirstProperty ("fixme:HomePhone");
			
			if (org != null && org != "")
				details.AddLabel (org, i++, 1);
			if (title != null && title != "")
				details.AddLabel (title, i++, 1);

			details.AddLabel ("", i++, 1);

			if (email != null && email != "")
				details.AddLabelPair (Catalog.GetString ("E-Mail:"), email, i++, 1);			
			if (mobile_phone != null && mobile_phone != "")
				details.AddLabelPair (Catalog.GetString ("Mobile Phone:"), mobile_phone, i++, 1);	
			if (work_phone != null && work_phone != "")
				details.AddLabelPair (Catalog.GetString ("Work Phone:"), work_phone, i++, 1);
			if (home_phone != null && home_phone != "")
				details.AddLabelPair (Catalog.GetString ("Home Phone:"), home_phone, i++, 1);
			
			return details;
		}
	}
}
