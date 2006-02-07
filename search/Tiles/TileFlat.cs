using System;
using Gtk;

namespace Search.Tiles {

	public abstract class TileFlat : Tile {

		protected Gtk.Label subject, from, date;

		protected TileFlat (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			subject = WidgetFu.NewLabel ();
			WidgetFu.EllipsizeLabel (subject, 40);
			HBox.PackStart (subject, true, true, 3);

			from = WidgetFu.NewLabel ();
			from.UseMarkup = true;
			WidgetFu.EllipsizeLabel (from, 20);
			HBox.PackStart (from, false, false, 3);

			date = WidgetFu.NewLabel ();
			HBox.PackStart (date, false, false, 3);

			HBox.ShowAll ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();

			if ((icon.StorageType == ImageType.Empty ||
			     icon.StorageType == ImageType.Pixbuf) &&
			    icon.Pixbuf == null)
				LoadIcon (icon, 16);
		}

		public Gtk.Label SubjectLabel {
			get { return subject; }
		}

		public Gtk.Label FromLabel {
			get { return from; }
		}

		public Gtk.Label DateLabel {
			get { return date; }
		}
	}
}
