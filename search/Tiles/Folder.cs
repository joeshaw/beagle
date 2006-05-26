using System;
using Mono.Unix;

namespace Search.Tiles {

	public class FolderActivator : TileActivator {

		public FolderActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, null, "inode/directory"));
			AddSupportedFlavor (new HitFlavor (null, null, "x-directory/normal"));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new Folder (hit, query);
		}
	}

	public class Folder : TileTemplate {

		public Folder (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Folder;
			Title = Hit ["beagle:ExactFilename"];
			EnableOpenWith = true;
			
			int n = Hit.DirectoryInfo.GetFileSystemInfos ().Length;

			if (n == 0)
				Description = Catalog.GetString ("Empty");
			else
				Description = String.Format (Catalog.GetPluralString ("Contains {0} Item",
										      "Contains {0} Items", n), n);

			// FIXME: s/"gtk-info"/Gtk.Stock.Info/ when we can depend on gtk# 2.8
			//AddAction (new TileAction (Catalog.GetString ("Show Information"), "gtk-info", ShowInformation));
			AddAction (new TileAction (Catalog.GetString ("Move to Trash"), Gtk.Stock.Delete, MoveToTrash));
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			image.Pixbuf = WidgetFu.LoadThemeIcon ("gnome-fs-directory", size);
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddTitleLabel (Title, 0, 1);
			details.AddLabelPair (Catalog.GetString ("Last Edited:"),
					      Utils.NiceLongDate (Hit.DirectoryInfo.LastWriteTime),
					      1, 1);
			details.AddLabelPair (Catalog.GetString ("Full Path:"),
					      Hit.Uri.LocalPath,
					      2, 1);

			//details.AddLabel ("", 3, 1);
			details.AddLabel (Description, 3, 1);

			return details;
		}

		public override void Open ()
		{
			base.OpenFromMime (Hit);
		}

		public void MoveToTrash ()
		{
			// FIXME: Ask for confirmation

			try {
				// FIXME: Check if KDE uses ~/.Trash too
				string trash_name = System.IO.Path.Combine (".Trash", Hit.DirectoryInfo.Name);
				Hit.DirectoryInfo.MoveTo (System.IO.Path.Combine (Beagle.Util.PathFinder.HomeDir, trash_name));
			} catch (Exception e) {
				Console.WriteLine (e);
			}
		}
	}
}
