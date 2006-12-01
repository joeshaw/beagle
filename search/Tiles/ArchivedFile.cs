using System;
using System.Diagnostics;
using Mono.Unix;
using Beagle.Util;

namespace Search.Tiles {

	public class ArchivedFileActivator : TileActivator {

		public ArchivedFileActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "File", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new TileArchivedFile (hit, query);
		}

		public override bool Validate (Beagle.Hit hit)
		{
			if (! base.Validate (hit))
				return false;

			string str = hit.GetFirstProperty ("fixme:inside_archive");
			if (hit.ParentUri == null || str == null || str == "false")
				return false;

			Weight += 1;
			
			return true;
		}
	}

	public class TileArchivedFile : TileFile {

		public TileArchivedFile (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Description = String.Format (Catalog.GetString ("Inside archive {0}"), GetTitle (hit, true));
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			// FIXME: Emblemize some sort of archive icon on top of
			// the main icon.
			base.LoadIcon (image, size);
		}
	}
}
