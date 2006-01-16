using System;
using System.Diagnostics;
using Mono.Posix;

namespace Search.Tiles {

	public class MailMessageActivator : TileActivator {

		public MailMessageActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "MailMessage", null));
			AddSupportedFlavor (new HitFlavor (null, "File", "message/rfc822"));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new MailMessage (hit, query);
		}
	}

	public class MailMessage : Tile {

		Gtk.Label subject, from, date;

		public MailMessage (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Conversations;

			Icon = GetIcon (Hit, 16);

			subject = WidgetFu.NewLabel (hit.GetFirstProperty ("dc:title"));
			WidgetFu.EllipsizeLabel (subject);
			HBox.PackStart (subject, true, true, 3);

			from = WidgetFu.NewBoldLabel (GetAddress (hit));
			from.UseMarkup = true;
			WidgetFu.EllipsizeLabel (from, 20);
			HBox.PackStart (from, false, false, 3);

			date = WidgetFu.NewLabel (Utils.NiceShortDate (hit.GetFirstProperty ("fixme:date")));
			HBox.PackStart (date, false, false, 3);

			HBox.ShowAll ();

			AddAction (new TileAction (Catalog.GetString ("Send in Mail"), SendInMail));
		}

		private static Gdk.Pixbuf GetIcon (Beagle.Hit hit, int size)
		{
			Gdk.Pixbuf icon;
			
			if (hit.GetFirstProperty ("fixme:isAnswered") != null)
				icon = WidgetFu.LoadThemeIcon ("stock_mail-replied", size);
			else if (hit.GetFirstProperty ("fixme:isSeen") != null)
				icon = WidgetFu.LoadThemeIcon ("stock_mail-open", size);
			else
				icon = WidgetFu.LoadThemeIcon ("stock_mail", size);

			return icon;
		}

		private static string GetAddress (Beagle.Hit hit)
		{
			bool sent = (hit.GetFirstProperty ("fixme:isSent") != null);
			string address = sent ? hit.GetFirstProperty ("fixme:to") : hit.GetFirstProperty ("fixme:from");

			if (address == null)
				return "";
			if (address.IndexOf (" <") != -1)
				address = address.Substring (0, address.IndexOf (" <"));

			return address;
		}

		public Gtk.Label SubjectLabel {
			get { return subject; }
		}

		public Gtk.Label FromLabel {
			get { return from; }
		}

		public Gtk.Label DateLabel {
			get { return date; }
		}

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		Gtk.Label snippetLabel;
		private string snippet;
		private bool found_snippet;

		protected override Gtk.Widget GetDetails ()
		{
			Gtk.Table table = new Gtk.Table (3, 4, false);
			table.RowSpacing = table.ColumnSpacing = 6;

			Gtk.Label label;
			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Subject:"));
			table.Attach (label, 0, 1, 0, 1, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (SubjectLabel.Text);
			WidgetFu.EllipsizeLabel (label);
			table.Attach (label, 1, 2, 0, 1, expand, fill, 0, 0);
			label = WidgetFu.NewGrayLabel (Catalog.GetString ("From:"));
			table.Attach (label, 0, 1, 1, 2, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (GetAddress (Hit));
			WidgetFu.EllipsizeLabel (label);
			table.Attach (label, 1, 2, 1, 2, expand, fill, 0, 0);
			label = WidgetFu.NewGrayLabel (Catalog.GetString ("Date Received:"));
			table.Attach (label, 2, 3, 0, 1, fill, fill, 0, 0);
			label = WidgetFu.NewBoldLabel (DateLabel.Text);
			table.Attach (label, 3, 4, 0, 1, fill, fill, 0, 0);

			Gtk.Image icon = new Gtk.Image (GetIcon (Hit, 48));
			table.Attach (icon, 0, 1, 2, 3, fill, fill, 0, 0);

			snippetLabel = WidgetFu.NewLabel ();
			snippetLabel.Markup = snippet;
			WidgetFu.EllipsizeLabel (snippetLabel);
			table.Attach (snippetLabel, 1, 4, 2, 3, expand, expand, 48, 0);

			if (!found_snippet)
				RequestSnippet ();

			table.WidthRequest = 0;
			table.ShowAll ();

			return table;
		}

		protected override void GotSnippet (string snippet, bool found)
		{
			found_snippet = found;
			this.snippet = snippet;
			snippetLabel.Markup = snippet;
		}

		public override void Open ()
		{
			string uri_str;

			if (Hit.GetFirstProperty ("fixme:client") != "evolution") {
				OpenFromMime (Hit);
				return;
			}

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";

			if (Hit.ParentUriAsString != null)
				uri_str = Hit.ParentUriAsString;
			else
				uri_str = Hit.UriAsString;

			p.StartInfo.Arguments  = "'" + uri_str + "'";

			try {
				p.Start ();
			} catch (System.ComponentModel.Win32Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
		}

		public void SendInMail ()
		{
			if (Hit.GetFirstProperty ("fixme:client") != "evolution")
				return;
			
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";
			p.StartInfo.Arguments = String.Format ("\"{0};forward=attached\"", Hit.Uri);

			try {
				p.Start () ;
			} catch (Exception e) {
				Console.WriteLine ("Error launching Evolution composer: " + e);
			}
		}
	}
}
