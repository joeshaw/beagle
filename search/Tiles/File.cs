using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mono.Unix;
using Beagle.Util;

namespace Search.Tiles {

	public class FileActivator : TileActivator {

		public FileActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "File", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new TileFile (hit, query);
		}
	}

	public class TileFile : TileTemplate {

		public TileFile (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Title = GetTitle ();
			EnableOpenWith = true;
			
			if (Hit.FileInfo != null) {
				Timestamp = Hit.FileInfo.LastWriteTime;
				Description = Utils.NiceShortDate (Timestamp);
			}

			AddAction (new TileAction (Catalog.GetString ("Reveal in Folder"), RevealInFolder));
			AddAction (new TileAction (Catalog.GetString ("E-Mail"), Email));
			// AddAction (new TileAction (Catalog.GetString ("Instant-Message"), InstantMessage));
			AddAction (new TileAction (Catalog.GetString ("Move to Trash"), Gtk.Stock.Delete, MoveToTrash));
		}

		static ThumbnailFactory thumbnailer = new ThumbnailFactory ();

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			if (!thumbnailer.SetThumbnailIcon (image, Hit, size))
				base.LoadIcon (image, size);
		}

		private string GetTitle ()
		{
			string title = Hit.GetFirstProperty ("dc:title");

			if (title == null || title == "")
				title = Hit.GetFirstProperty ("beagle:ExactFilename");

			return title;
		}

		public override void Open ()
		{
			base.OpenFromMime (Hit);
		}

		public void OpenWith ()
		{
			// FIXME: base.OpenWith
		}

		public void RevealInFolder ()
		{
			string path = Hit.FileInfo.DirectoryName;

			// FIXME: When nautilus implements this, then we should
			// also select the file in the folder.

			SafeProcess p = new SafeProcess ();

#if ENABLE_DESKTOP_LAUNCH
			p.Arguments = new string [] { "desktop-launch", path };
#else
			p.Arguments = new string [] { "nautilus", "--no-desktop", path };
#endif
			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Cannot open folder: " + e);
			}
		}

		public void Email ()
		{
			SafeProcess p = new SafeProcess ();
			p.Arguments = new string [] { "evolution", String.Format ("mailto:?attach{0}", Hit.FileInfo.FullName) };

			try {
				p.Start () ;
			} catch (Exception e) {
				Console.WriteLine ("Error launching Evolution composer: " + e);
			}
		}

		public void InstantMessage ()
		{
			// FIXME: base.InstantMessage
		}

		public void MoveToTrash ()
		{
			// FIXME: Ask for confirmation

			try {
				// FIXME: Check if KDE uses ~/.Trash too (there is a spec at fd.o)
				string trash_dir = System.IO.Path.Combine (Beagle.Util.PathFinder.HomeDir, ".Trash");

				// FIXME: This throws an exception if the file exists
				Hit.FileInfo.MoveTo (System.IO.Path.Combine (trash_dir, Hit.FileInfo.Name));
			} catch (Exception e) {
				Console.WriteLine (e);
			}
		}	

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Title:"),
					      GetTitle (),
					      0, 1);
			details.AddLabelPair (Catalog.GetString ("Last Edited:"),
					      Utils.NiceLongDate (Timestamp),
					      1, 1);
			if (Hit ["dc:author"] != null) {
				details.AddLabelPair (Catalog.GetString ("Author:"),
						      Hit ["dc:author"],
						      1, 3);
			}
			details.AddLabelPair (Catalog.GetString ("Full Path:"),
					      Hit.Uri.LocalPath,
					      2, 1);
			details.AddSnippet (3, 1);

			return details;
		}
	}
}
