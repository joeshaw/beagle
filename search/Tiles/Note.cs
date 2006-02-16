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

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		private Gtk.Label snippet_label;
		private string snippet;
		private bool found_snippet;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (3, 3, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			Gtk.Image icon = new Gtk.Image ();
			LoadIcon (icon, 96);
			table.Attach (icon, 0, 1, 0, 3, fill, fill, 0, 0);

			Gtk.Label label;

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Title:"));
			table.Attach (label, 1, 2, 0, 1, fill, fill, 0, 0);

			label = WidgetFu.NewBoldLabel (Title);
			table.Attach (label, 2, 3, 0, 1, expand, fill, 0, 0);

			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Last Edited:"));
			table.Attach (label, 1, 2, 1, 2, fill, fill, 0, 0);

			label = WidgetFu.NewLabel (Utils.NiceLongDate (Hit.Timestamp));
			table.Attach (label, 2, 3, 1, 2, expand, fill, 0, 0);
			
			snippet_label = WidgetFu.NewLabel ();
			snippet_label.Markup = snippet;
			WidgetFu.EllipsizeLabel (snippet_label);
			table.Attach (snippet_label, 1, 3, 2, 3, expand, expand, 48, 0);

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
