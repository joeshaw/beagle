using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mono.Unix;
using Beagle.Util;

namespace Search.Tiles {

	public class NoteActivator : TileActivator {

		public NoteActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "Note", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new TileNote (hit, query);
		}
	}

	public class TileNote : TileTemplate {

		public TileNote (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Title = Hit.GetFirstProperty ("dc:title");
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			image.Pixbuf = Beagle.Images.GetPixbuf ("note", size, size);
		}

		public override void Open ()
		{
			SafeProcess p = new SafeProcess ();

			// This doesn't work very well if you have multiple
			// terms that match.  Tomboy doesn't seem to have a way
			// to specify more than one thing to highlight.
			p.Arguments = new string [] { "tomboy",
						      "--open-note", Hit.UriAsString,
						      "--highlight-search", Query.QuotedText };

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Could not invoke Tomboy to open note: " + e);
			}
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Title:"),
					      Title,
					      0, 1);
			details.AddLabelPair (Catalog.GetString ("Last Edited:"),
					      Utils.NiceLongDate (Timestamp),
					      1, 1);
			
			details.AddSnippet (2, 1);

			return details;
		}
	}
}
