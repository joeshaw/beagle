using System;
using Mono.Posix;

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
			Icon = WidgetFu.LoadThemeIcon ("gnome-fs-directory", 32);
			
			int n = Hit.DirectoryInfo.GetFileSystemInfos ().Length;

			if (n == 0)
				Description = Catalog.GetString ("Empty");
			else
				Description = String.Format (Catalog.GetPluralString ("Contains {0} Item", "Contains {0} Items", n), n);

			AddAction (new TileAction ("Open With", OpenWith));
			// FIXME: s/"gtk-info"/Gtk.Stock.Info/ when we can depend on gtk# 2.8
			AddAction (new TileAction ("Show Information", "gtk-info", ShowInformation));
			AddAction (new TileAction ("Move to Trash", Gtk.Stock.Delete, MoveToTrash));
		}

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (3, 3, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			Gtk.Image icon = new Gtk.Image (WidgetFu.LoadThemeIcon ("gnome-fs-directory", 96));
			table.Attach (icon, 0, 1, 0, 3,
				      fill, fill, 0, 0);

			Gtk.Label label;
			label = WidgetFu.NewBoldLabel (Title);
			table.Attach (label, 1, 3, 0, 1,
				      expand, fill, 0, 0);
			
			label = WidgetFu.NewLabel (Description);
			table.Attach (label, 1, 3, 1, 2,
				      expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Edited:"));
			table.Attach (label, 1, 2, 2, 3,
				      fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Utils.NiceVeryLongDate (Hit.DirectoryInfo.LastWriteTime));
			table.Attach (label, 2, 3, 2, 3,
				      expand, fill, 0, 0);

			table.WidthRequest = 0;
			table.ShowAll ();
			
			return table;
		}

		public override void Open ()
		{
			base.OpenFromMime (Hit);
		}

		public void OpenWith ()
		{
		}

		public void ShowInformation ()
		{
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
