using System;
using System.Diagnostics;
using System.Collections;
using Mono.Unix;
using Beagle.Util;

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

			Subject.LabelProp = Catalog.GetString ("IM Conversation");

			string alias = hit.GetFirstProperty ("fixme:speakingto_alias");
			string name = hit.GetFirstProperty ("fixme:speakingto");

			if (alias != null && alias != "")
				From.LabelProp = "<b>" + alias + "</b>";
			else if (name != null && name != "")
				From.LabelProp = "<b>" + name + "</b>";
			else
				From.LabelProp = "(unknown)";

			try {
				Timestamp = Utils.ParseTimestamp (hit.GetFirstProperty ("fixme:starttime"));
				Date.LabelProp = Utils.NiceShortDate (Timestamp);
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

		private Gdk.Pixbuf LoadBuddyIcon ()
		{
			Gdk.Pixbuf icon = null;

			if (Hit ["fixme:speakingto_icon"] != null && System.IO.File.Exists (Hit ["fixme:speakingto_icon"]))
				icon = new Gdk.Pixbuf (Hit ["fixme:speakingto_icon"]);

			return icon;				
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.Icon.Pixbuf = LoadBuddyIcon ();			
			details.AddLabelPair (Catalog.GetString ("Name:"), FromLabel.Text);
			details.AddLabelPair (Catalog.GetString ("Date Received:"), Utils.NiceLongDate (Timestamp));
			details.AddSnippet ();

			GotSnippet += SetSubject;

			return details;
		}

		private void SetSubject (string snippet)
		{
			Subject.Markup = snippet;
		}

		public override void Open ()
		{
			SafeProcess p = new SafeProcess ();
			p.Arguments = new string [] { "beagle-imlogviewer",
						      "--client", Hit ["fixme:client"],
						      "--highlight-search", Query.QuotedText,
						      Hit.Uri.LocalPath };

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.Arguments [0], e.Message);
			}
		}
	}
}
