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
			//Console.WriteLine (str);
			if (hit.ParentUri == null || str == null || str == "false")
				return false;

			str = hit.GetFirstProperty ("fixme:attachment_title");
			//Console.WriteLine (str);
			if (str == null || str == "")
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
			Description = Catalog.GetString ("Mail attachment");
		}

		// FIXME: This needs better handling in the daemon, attachments
		// architecture sucks
		private static bool IsAttachment (Beagle.Hit hit)
		{
			// check if there is parent and parent has attachments
			string str = hit ["parent:fixme:hasAttachments"];
			return (hit.ParentUri != null && str != null && (str == "true"));
		}
		
		private static string GetHitProperty (Beagle.Hit hit, string name)
		{
			// FIXME: We should handle this case better, but
			// for now, if we match an attachment, we just want
			// to display the properties for the parent message.
			if (!IsAttachment (hit))
				return hit [name];
			else
				return hit ["parent:" + name];
		}

		public override void Open ()
		{
			SafeProcess p = MailMessage.GetClientProcess (GetHitProperty (Hit, "fixme:client"));

			if (p == null) {
				OpenFromMime (Hit);
				return;
			}

			if (Hit.ParentUriAsString != null)
				p.Arguments [p.Arguments.Length-1] = Hit.ParentUriAsString;
			else
				p.Arguments [p.Arguments.Length-1] = Hit.UriAsString;

			try {
				p.Start ();
			} catch (SafeProcessException e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.Arguments [0], e.Message);
			}
		}
		
	}
}
