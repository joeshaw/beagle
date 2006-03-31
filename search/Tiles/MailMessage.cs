using System;
using System.Diagnostics;
using Mono.Unix;

namespace Search.Tiles {

	public class MailMessageActivator : TileActivator {

		public MailMessageActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, null, "message/rfc822"));
		}

		public override bool Validate (Beagle.Hit hit)
		{
			if (! base.Validate (hit))
				return false;
			
			// This handles a case when a file with the message/rfc822
			// mimetype is indexed without gmime. Thus we fail to extract
			// any info and this tile is useless.
			if (hit ["beagle:HitType"] == "File") {
				string subject = hit.GetFirstProperty ("dc:title");
				
				if (subject != null && subject != "")
					return true;

				return false;
			}

			return true;
		}
		
		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new MailMessage (hit, query);
		}
	}

	public class MailMessage : TileFlat {

		public MailMessage (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Conversations;

			subject.LabelProp = Title = hit.GetFirstProperty ("dc:title");
			from.LabelProp = "<b>" + GetAddress (hit) + "</b>";
			try {
				Timestamp = Utils.ParseTimestamp (hit.GetFirstProperty ("fixme:date"));
				date.LabelProp = Utils.NiceShortDate (Timestamp);
			} catch {}

			AddAction (new TileAction (Catalog.GetString ("Send in Mail"), SendInMail));
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			if (Hit.GetFirstProperty ("fixme:isAnswered") != null)
				image.Pixbuf = WidgetFu.LoadThemeIcon ("stock_mail-replied", size);
			else if (Hit.GetFirstProperty ("fixme:isSeen") != null)
				image.Pixbuf = WidgetFu.LoadThemeIcon ("stock_mail-open", size);
			else
				image.Pixbuf = WidgetFu.LoadThemeIcon ("stock_mail", size);
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

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Subject:"),
					      SubjectLabel.Text,
					      0, 1);
			details.AddLabelPair (Catalog.GetString ("From:"),
					      GetAddress (Hit),
					      1, 1);
			details.AddLabelPair (Catalog.GetString ("Date Received:"),
					      DateLabel.Text,
					      1, 3);

			details.AddSnippet (2, 1);

			return details;
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
