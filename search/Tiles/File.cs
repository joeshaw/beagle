using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mono.Posix;

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
			Icon = WidgetFu.LoadThumbnailIcon (Hit.Uri, 32);

			if (Icon == null)
				Icon = WidgetFu.LoadMimeIcon (Hit ["beagle:MimeType"], 32);

			Title = Hit ["beagle:ExactFilename"];
			
			if (Hit.FileInfo != null)
				Description = Utils.NiceShortDate (Hit.FileInfo.LastWriteTime);

			AddAction (new TileAction (Catalog.GetString ("Open With"), OpenWith));
			AddAction (new TileAction (Catalog.GetString ("Reveal in Folder"), RevealInFolder));
			AddAction (new TileAction (Catalog.GetString ("E-Mail"), Email));
			AddAction (new TileAction (Catalog.GetString ("Instant-Message"), InstantMessage));
			AddAction (new TileAction (Catalog.GetString ("Move to Trash"), Gtk.Stock.Delete, MoveToTrash));
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

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;

			if ((! path.StartsWith ("\"")) && (! path.EndsWith ("\"")))
				path = "\"" + path + "\"";

			// FIXME: When nautilus implements this, then we should
			// also select the file in the folder.

#if ENABLE_DESKTOP_LAUNCH
			p.StartInfo.FileName = "desktop-launch";
			p.StartInfo.Arguments = path;
#else
			p.StartInfo.FileName = "nautilus";
			p.StartInfo.Arguments = "--no-desktop " + path;
#endif
			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Cannot open folder: " + e);
			}
		}

		public void Email ()
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";
			p.StartInfo.Arguments = String.Format ("\"mailto:?attach={0}\"", Hit.FileInfo.FullName);

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

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		private Gtk.Label snippet_label;
		private string snippet;
		private bool found_snippet;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (4, 4, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			Gtk.Label label;

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Title:"));
			table.Attach (label, 0, 1, 0, 1, fill, fill, 0, 0);

			string title = Hit.GetFirstProperty ("dc:title");
			if (title == null || title == "")
				title = Hit.GetFirstProperty ("beagle:ExactFilename");
			
			label = WidgetFu.NewBoldLabel (title);
			table.Attach (label, 1, 4, 0, 1, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Last Edited:"));
			table.Attach (label, 0, 1, 1, 2, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Utils.NiceLongDate (Hit.Timestamp));
			table.Attach (label, 1, 2, 1, 2, expand, fill, 0, 0);

			if (Hit ["dc:author"] != null) {
				label = WidgetFu.NewGrayLabel (Catalog.GetString ("Author:"));
				table.Attach (label, 2, 3, 1, 2, fill, fill, 0, 0);

				label = WidgetFu.NewBoldLabel (Hit ["dc:author"]);
				table.Attach (label, 3, 4, 1, 2, expand, fill, 0, 0);
			}

			Gtk.Image icon = new Gtk.Image (Icon);
			table.Attach (icon, 0, 1, 2, 3, fill, fill, 0, 0);
			
			snippet_label = WidgetFu.NewLabel (snippet);
			WidgetFu.EllipsizeLabel (snippet_label);
			table.Attach (snippet_label, 1, 4, 2, 3, expand, expand, 48, 0);

			if (! found_snippet)
				RequestSnippet ();

			table.WidthRequest = 0;
			table.ShowAll ();

			return table;
		}

		protected override void GotSnippet (string snippet, bool found)
		{
			found_snippet = found;
			this.snippet = snippet;
			snippet_label.Markup = snippet;
		}
	}
}
