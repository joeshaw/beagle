using System;
using System.Diagnostics;
using System.Collections;
using Mono.Unix;

namespace Search.Tiles {

	public class IMLogActivator : TileActivator {

		public IMLogActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "IMLog", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new IMLog (hit, query);
		}
	}

	public class IMLog : TileFlat {

		private static Hashtable all_icons = new Hashtable ();

		public IMLog (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Conversations;

			subject.LabelProp = Catalog.GetString ("IM Conversation");
			from.LabelProp = "<b>" + hit.GetFirstProperty ("fixme:speakingto") + "</b>";
			try {
				Timestamp = Utils.ParseTimestamp (hit.GetFirstProperty ("fixme:starttime"));
				date.LabelProp = Utils.NiceShortDate (Timestamp);
			} catch {}
		}

		private Hashtable IconsForSize (int size)
		{
			Hashtable icons = new Hashtable ();

			icons ["aim"] = WidgetFu.LoadThemeIcon ("im-aim", size);
			icons ["icq"] = WidgetFu.LoadThemeIcon ("im-icq", size);
			icons ["jabber"] = WidgetFu.LoadThemeIcon ("im-jabber", size);
			icons ["msn"] = WidgetFu.LoadThemeIcon ("im-msn", size);
			icons ["novell"] = WidgetFu.LoadThemeIcon ("im-nov", size);
			icons ["yahoo"] = WidgetFu.LoadThemeIcon ("im-yahoo", size);

			return icons;
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			Hashtable icons = (Hashtable)all_icons[size];
			if (icons == null)
				all_icons[size] = icons = IconsForSize (size);

			string protocol = Hit.GetFirstProperty ("fixme:protocol");
			if (icons [protocol] != null)
				image.Pixbuf = (Gdk.Pixbuf)icons [protocol];
			else
				image.Pixbuf = WidgetFu.LoadThemeIcon ("im", size);
		}

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		private Gtk.Label snippet_label;
		private string snippet;
		private bool found_snippet;
		
		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (3, 4, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			Gtk.Label label;
			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Name:"));
			table.Attach (label, 0, 1, 0, 1, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (FromLabel.Text);
			WidgetFu.EllipsizeLabel (label);
			table.Attach (label, 1, 2, 0, 1, expand, fill, 0, 0);
			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Date Received:"));
			table.Attach (label, 2, 3, 0, 1, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (DateLabel.Text);
			table.Attach (label, 3, 4, 0, 1, fill, fill, 0, 0);

			Gtk.Image icon = new Gtk.Image ();
			LoadIcon (icon, 48);
			table.Attach (icon, 0, 1, 2, 3, fill, fill, 0, 0);
			
			snippet_label = WidgetFu.NewLabel ();
			snippet_label.Markup = snippet;
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
			this.snippet = snippet;
			subject.Markup = snippet;
			snippet_label.Markup = snippet;

			found_snippet = found;
		}

		public override void Open ()
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = true;
			p.StartInfo.FileName = "beagle-imlogviewer";
			p.StartInfo.Arguments = String.Format ("--client \"{0}\" --highlight-search \"{1}\" {2}",
							       Hit ["fixme:client"], Query.QuotedText, Hit.Uri.LocalPath);

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
		}
	}
}
