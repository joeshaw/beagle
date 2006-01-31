using System;
using System.Diagnostics;
using Mono.Unix;

using Beagle.Util;

namespace Search.Tiles {

	public class ImageActivator : TileActivator {

		public ImageActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "File", "image/*"));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new Image (hit, query);
		}
	}

	public class Image : TileFile {

		public Image (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Image;

			Title = Hit ["beagle:ExactFilename"];
			Icon = GetIcon (Hit, 32);

			if (Hit ["beagle:FilenameExtension"].Length > 0)
				Description = Hit ["beagle:FilenameExtension"].Substring (1).ToUpper ();
			
			if (Hit ["fixme:width"] != null && Hit ["fixme:width"] != "")
				Description += String.Format (" {0}x{1}", Hit ["fixme:width"], Hit ["fixme:height"]);

			Description += String.Format (" ({0})", StringFu.FileLengthToString (Hit.FileInfo.Length));

			AddAction (new TileAction (Catalog.GetString ("Add to Library"), Gtk.Stock.Add, AddToLibrary));
			AddAction (new TileAction (Catalog.GetString ("Set as Wallpaper"), SetAsWallpaper)); // FIXME: This is not in the spec, is it ok?
		}

		public static Gdk.Pixbuf GetIcon (Beagle.Hit hit, int size)
		{
			Gdk.Pixbuf icon =  WidgetFu.LoadThumbnailIcon (hit.Uri, size);

			if (icon == null)
				return WidgetFu.LoadMimeIcon (hit ["beagle:MimeType"], size);

			// Draw the F-Spot overlay
			if (hit ["fspot:IsIndexed"] == "true") {
				Gdk.Pixbuf emblem = Beagle.Images.GetPixbuf ("emblem-fspot.png", 16, 16);
				emblem.Composite (icon, 0,  icon.Height - emblem.Height, emblem.Width,
						  emblem.Height, 0,  icon.Height - emblem.Height, 1,  1,
						  Gdk.InterpType.Bilinear, 255);
			}

			return icon;
		}

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (3, 4, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			// FIXME: The icon needs a nice frame as in the spec
			Gtk.Image icon = new Gtk.Image (GetIcon (Hit, 96));
			table.Attach (icon, 0, 1, 0, 4, fill, fill, 0, 0);

			Gtk.Label label;
			label = WidgetFu.NewBoldLabel (Title);
			table.Attach (label, 1, 3, 0, 1, expand, fill, 0, 0);
			
			label = WidgetFu.NewLabel (Description);
			table.Attach (label, 1, 3, 1, 2, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Modified:"));
			table.Attach (label, 1, 2, 2, 3, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Utils.NiceVeryLongDate (Hit.FileInfo.LastWriteTime));
			table.Attach (label, 2, 3, 2, 3, expand, fill, 0, 0);			

			if (Hit ["fspot:Description"] != null && Hit ["fspot:Description"] != "") {
				label = WidgetFu.NewLabel (Hit ["fspot:Description"]);
				WidgetFu.EllipsizeLabel (label);
				table.Attach (label, 1, 3, 3, 4, expand, expand, 48, 0);
			}

			table.WidthRequest = 0;
			table.ShowAll ();
			
			return table;
		}
		
		// FIXME: fspot doesnt allow to import a particular file
		// only a whole directory
		public void AddToLibrary ()
		{
			// FIXME: check if f-spot is installed

			if (Hit ["fspot:IsIndexed"] == "true")
				return;

			ProcessStartInfo pi = new ProcessStartInfo ("f-spot");
			pi.Arguments = String.Format ("--import {0}", Hit.FileInfo.FullName);
			Process.Start (pi);
		}

		
		public void SetAsWallpaper ()
		{
			int width = 0;
			int height = 0;

			if (Hit ["fixme:width"] != null && Hit ["fixme:width"] == "") {
				width = Int32.Parse (Hit ["fixme:width"]);
				height = Int32.Parse (Hit ["fixme:height"]);
			} else {
				if (! System.IO.File.Exists (Hit.FileInfo.FullName))
					return;

				Gdk.Pixbuf p = new Gdk.Pixbuf (Hit.FileInfo.FullName);
				width = p.Width;
				height = p.Height;
			}

			GConf.Client client = new GConf.Client ();
			client.Set ("/desktop/gnome/background/picture_filename", Hit.FileInfo.FullName);

			if (width <= 640) {
				if (width == height) {
					// Tile
					client.Set ("/desktop/gnome/background/picture_options",
						    "wallpaper");
				} else {
					// Center
					client.Set ("/desktop/gnome/background/picture_options",
						    "centered");
				}
			} else if (height >= width) {
				// Stretch vertically, but not horizontally
				client.Set ("/desktop/gnome/background/picture_options",
					    "scaled");
			} else {
				// Fit to screen
				client.Set ("/desktop/gnome/background/picture_options",
					    "stretched");
			}

			client.SuggestSync ();
		}
	}
}
