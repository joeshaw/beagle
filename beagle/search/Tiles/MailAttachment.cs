using System;
using System.Diagnostics;
using Mono.Unix;
using Beagle.Util;

namespace Search.Tiles {

	public class MailAttachmentActivator : TileActivator {

		public MailAttachmentActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "MailMessage", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new MailAttachment (hit, query);
		}

		public override bool Validate (Beagle.Hit hit)
		{
			if (! base.Validate (hit))
				return false;

			string str = hit.GetFirstProperty ("parent:fixme:hasAttachments");
			if (hit.ParentUri == null || str == null || str == "false")
				return false;

			Weight += 1;
			
			return true;
		}
	}

	public class MailAttachment : TileTemplate {

		public MailAttachment (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Documents;
			Title = Hit ["fixme:attachment_title"];

			if (String.IsNullOrEmpty (Title))
				Title = Catalog.GetString (String.Format ("Attachment to \"{0}\"", Hit ["parent:dc:title"]));

			Description = Catalog.GetString ("Mail attachment");
		}

		public override void Open ()
		{
			SafeProcess p = MailMessage.GetClientProcess (Hit);
			
			if (p == null) {
				OpenFromMime (Hit);
				return;
			}

			try {
				p.Start ();
			} catch (SafeProcessException e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.Arguments [0], e.Message);
			}
		}
		
	}
}
