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

			if (Hit ["beagle:FilenameExtension"].Length > 0)
				Description = Hit ["beagle:FilenameExtension"].Substring (1).ToUpper ();
			
			if (Hit ["fixme:width"] != null && Hit ["fixme:width"] != "")
				Description += String.Format (" {0}x{1}", Hit ["fixme:width"], Hit ["fixme:height"]);

			Description += String.Format (" ({0})", StringFu.FileLengthToString (Hit.FileInfo.Length));

			// AddAction (new TileAction (Catalog.GetString ("Add to Library"), Gtk.Stock.Add, AddToLibrary));
			AddAction (new TileAction (Catalog.GetString ("Set as Wallpaper"), SetAsWallpaper));
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			base.LoadIcon (image, size);

			// Draw the F-Spot overlay
			if (size > 32 && Hit ["fspot:IsIndexed"] == "true") {
				Gdk.Pixbuf icon = image.Pixbuf;
				Gdk.Pixbuf emblem = Beagle.Images.GetPixbuf ("emblem-fspot.png", 16, 16);

				if (icon == null || emblem == null)
					return;

				// FIXME: Ideally we'd composite into a fresh new pixbuf of
				// the correct size in this case, but really, who's going to
				// have images shorter or narrower than 16 pixels in f-spot??
				if (icon.Height < emblem.Height || icon.Width < emblem.Width)
					return;

				emblem.Composite (icon, 0,  icon.Height - emblem.Height, emblem.Width,
						  emblem.Height, 0,  icon.Height - emblem.Height, 1,  1,
						  Gdk.InterpType.Bilinear, 255);

				image.Pixbuf = icon;
			}
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			// FIXME: The icon needs a nice frame as in the spec (?)

			details.AddBoldLabel (Title, 0, 1);
			details.AddLabel (Description, 1, 1);
			details.AddLabel ("", 2, 1);

			details.AddLabelPair (Catalog.GetString ("Modified:"),
					      Utils.NiceVeryLongDate (Hit.FileInfo.LastWriteTime),
					      3, 1);
			details.AddLabelPair (Catalog.GetString ("Full Path:"),
					      Hit.Uri.LocalPath,
					      4, 1);

			if (Hit ["fspot:Description"] != null && Hit ["fspot:Description"] != "") {
				details.AddLabel ("", 5, 1);
				details.AddLabel (Hit ["fspot:Description"], 6, 1);
			}

			return details;
		}
		
#if NOT_YET
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
#endif
		
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
