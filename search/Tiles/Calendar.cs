using System;
using Mono.Unix;
using Beagle.Util;

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
			Group = TileGroup.Calendar;

			string summary = hit.GetFirstProperty ("fixme:summary");
			string time = Utils.NiceShortDate (hit.GetFirstProperty ("fixme:starttime"));

			Title = (time == "") ? summary : time + ": " + summary;

			string description = hit.GetFirstProperty ("fixme:description");

			if (description != null && description != "") {
				int newline = description.IndexOf ('\n');
				if (newline == -1)
					Description = description;
				else
					Description = String.Format ("{0}...", description.Substring (0, newline));
			}
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			image.Pixbuf = WidgetFu.LoadThemeIcon ("stock_calendar", size);
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Title:"), Title);

			if (Description != null && Description != "")
				details.AddLabelPair (Catalog.GetString ("Description:"), Description);
			
			if (Hit.GetFirstProperty ("fixme:location") != null)
				details.AddLabelPair (Catalog.GetString ("Location:"), Hit.GetFirstProperty ("fixme:location"));

			string[] attendees = Hit.GetProperties ("fixme:attendee");
			if (attendees != null && attendees.Length > 0)
				details.AddLabelPair (Catalog.GetString ("Attendees:"), String.Join (", ", attendees));

			details.AddFinalLine ();

			return details;
		}

		public override void Open ()
		{
			SafeProcess p = new SafeProcess ();
			p.Arguments = new string [] { "evolution", Hit.EscapedUri };

			try {
				p.Start ();
			} catch (SafeProcessException e) {
				Console.WriteLine (e.Message);
			}
		}
	}
}
