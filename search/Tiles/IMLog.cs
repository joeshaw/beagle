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
			// FIXME: for large size, we should be returning a buddy
			// list picture, if available

			Hashtable icons = (Hashtable)all_icons[size];
			if (icons == null)
				all_icons[size] = icons = IconsForSize (size);

			string protocol = Hit.GetFirstProperty ("fixme:protocol");
			if (icons [protocol] != null)
				image.Pixbuf = (Gdk.Pixbuf)icons [protocol];
			else
				image.Pixbuf = WidgetFu.LoadThemeIcon ("im", size);
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Name:"),
					      FromLabel.Text,
					      0, 1);
			details.AddLabelPair (Catalog.GetString ("Date Received:"),
					      DateLabel.Text,
					      1, 1);

			GotSnippet += SetSubject;
			details.AddSnippet (2, 1);

			return details;
		}

		private void SetSubject (string snippet)
		{
			subject.Markup = snippet;
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
