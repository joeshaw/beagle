using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mono.Unix;

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
			// This doesn't work very well if you have multiple
			// terms that match.  Tomboy doesn't seem to have a way
			// to specify more than one thing to highlight.
			string args = String.Format ("--open-note {0} --highlight-search \"{1}\"",
						     Hit.Uri, Query.QuotedText);
			
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "tomboy";
			p.StartInfo.Arguments = args;

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
