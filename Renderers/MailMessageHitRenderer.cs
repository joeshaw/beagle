//
// GNOME Dashboard
//
// MailMessageMatchRenderer.cs: Knows how to render MailMessage matches.
//
// Author:
//   Kevin Godby <godbyk@yahoo.com>
//
// FIXME: Add support for importance flag (if possible)
// FIXME: Output date in local format (internationalization) -- Is this working now?

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Globalization;

//[assembly:Dashboard.MatchRendererFactory ("Dashboard.MailMessageMatchRenderer")]

namespace Beagle {

	public class MailMessageHitRenderer : HitRendererHtml {
		
		public MailMessageHitRenderer ()
		{
			type = "MailMessage";
		}
			
		protected override string HitsToHtml (ArrayList hits)
		{
			DateTime StartExec = DateTime.Now;

			StringWriter sw = new StringWriter ();
			XmlWriter xw = new XmlTextWriter (sw);

			// Start the xhtml block
			xw.WriteStartElement ("div");

			// Title of block
			xw.WriteStartElement ("table");
			xw.WriteAttributeString ("border", "0");
			xw.WriteAttributeString ("width", "100%");
			xw.WriteStartElement ("tr");
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("bgcolor", "#fffa6e");
			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("size", "+2");
			xw.WriteString ("Email Messages");
			xw.WriteEndElement ();	// font
			xw.WriteEndElement ();	// td
			xw.WriteEndElement ();	// tr
			xw.WriteEndElement ();	// table

			// The table of data
			xw.WriteStartElement ("table");
			xw.WriteAttributeString ("border", "0");
			xw.WriteAttributeString ("cellpadding", "0");
			xw.WriteAttributeString ("cellspacing", "0");
			xw.WriteAttributeString ("width", "100%");

			// Sort results by date (newest first)
			IComparer mailmessagedatecomparer = new MailMessageDateComparer ();
			hits.Sort (mailmessagedatecomparer);

			bool color_band = true;
			foreach (Hit hit in hits) {
				HTMLRenderSingleMailMessage (hit, color_band, xw);
				color_band = ! color_band;
			}

			xw.WriteEndElement (); // table
			xw.WriteEndElement (); // div

			// close the xhtml doc
			xw.Close ();

			// Console.WriteLine ("..Renderer: MailMessage.. elapsed time {0}", DateTime.Now - StartExec);

			return sw.ToString ();
		}

		private void HTMLRenderSingleMailMessage (Hit hit, bool color_band, XmlWriter xw)
		{
			// Make the date look pretty
			string maildate = Convert.ToString (hit ["SentDate"]);
			string ParsedDate = ParseMailDate (maildate);

			Message msg = new Message ();
			msg.Initialize (hit);

			// Console.WriteLine ("To: {0}\nFrom: {1}\nSubject: {2}\nDate: {3}",
			// 			msg.Recipient, msg.Sender, msg.Subject, msg.SentDate);

			xw.WriteStartElement ("tr");
			if (color_band)
				xw.WriteAttributeString ("bgcolor", "#eeeeee");

			xw.WriteStartElement ("a");
			xw.WriteAttributeString ("href", "exec:evolution-1.5 " + hit ["URI"]); // FIXME: Probably unsafe
			xw.WriteAttributeString ("style", "text-decoration: none; color: black;");

			// new / read / replied-to icon
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("width", "1%");
			xw.WriteAttributeString ("valign", "top");
			xw.WriteStartElement ("img");
			xw.WriteAttributeString ("border", "0");
			xw.WriteAttributeString ("src", msg.Icon);
			xw.WriteEndElement (); // img
			xw.WriteEndElement (); // td

			// sender (mail from)
			xw.WriteStartElement ("td");
			// xw.WriteAttributeString ("nowrap", "true");
			xw.WriteAttributeString ("width", "98%");
			xw.WriteRaw (MarkupStatus (msg.GetSenderName (), msg));
			xw.WriteEndElement (); // td

			// date
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("colspan", "2");
			xw.WriteAttributeString ("align", "right");
			xw.WriteAttributeString ("nowrap", "1");
			xw.WriteAttributeString ("valign", "top");
			xw.WriteAttributeString ("width", "1%");
			xw.WriteRaw (MarkupStatus (ParsedDate, msg));
			xw.WriteEndElement (); // td
			xw.WriteEndElement (); // a href
			xw.WriteEndElement (); // tr

			xw.WriteStartElement ("tr");
			if (color_band)
				xw.WriteAttributeString ("bgcolor", "#eeeeee");

			xw.WriteStartElement ("a");
			xw.WriteAttributeString ("href", "exec:evolution-1.5 " + hit ["URI"]); // FIXME: Probably unsafe
			xw.WriteAttributeString ("style", "text-decoration: none; color: black");

			// attachment icon
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("width", "1%");
			xw.WriteAttributeString ("valign", "top");
			xw.WriteAttributeString ("align", "right");

			if (msg.HasAttachment) {
				xw.WriteStartElement ("img");
				xw.WriteAttributeString ("border", "0");
				xw.WriteAttributeString ("src", "internal:attachment.png");
				xw.WriteEndElement (); // img
			}
			
			xw.WriteEndElement (); // td

			// subject
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("colspan", "2");
			xw.WriteAttributeString ("width", "98%");
			xw.WriteAttributeString ("valign", "top");
			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("color", "#666666");
			xw.WriteRaw (MarkupStatus (msg.Subject, msg));
			xw.WriteEndElement (); // font
			xw.WriteEndElement (); // td

			xw.WriteEndElement (); // a href
			xw.WriteEndElement (); // tr
		}

		private string ParseMailDate (string maildate)
		{
			// The dates returned by these functions are UTC
			DateTime ParsedDate = DateTime.Parse (maildate);
			DateTime today      = DateTime.Today;

			// Display in the current time zone
			TimeZone localZone = TimeZone.CurrentTimeZone;
			ParsedDate = localZone.ToLocalTime (ParsedDate);

			// Let's see if we can't use the proper date and time formats here...
			CultureInfo ci = new CultureInfo (CultureInfo.CurrentCulture.Name);

			if (today.Date == ParsedDate.Date) {
				// Display the time only
				return ParsedDate.ToString ("t", ci);
			}

			if (today.Year != ParsedDate.Year) {
				// Show the year mm/dd/yyyy or dd/mm/yyyy
				return ParsedDate.ToString ("d", ci);
			}

			return ParsedDate.ToString ("ddd, MMM d");
		}

		private string MakeHTMLSafe (string html)
		{
			StringWriter sw = new StringWriter ();
			XmlWriter xw = new XmlTextWriter (sw);
			xw.WriteString (html);
			string safehtml = sw.ToString ();
			xw.Close ();
			sw.Close ();

			return safehtml;
		}

		private string MarkupStatus (string html, Message msg)
		{
			StringWriter sw = new StringWriter ();
			XmlWriter xw = new XmlTextWriter (sw);

			if (msg.IsDeleted == true)
				xw.WriteStartElement ("strike");
			if (msg.IsNew == true)
				xw.WriteStartElement ("b");

			xw.WriteRaw (MakeHTMLSafe (html));

			if (msg.IsNew == true)
				xw.WriteEndElement (); // b
			if (msg.IsDeleted == true)
				xw.WriteEndElement (); //strike

			html = sw.ToString (); 

			return html;

		}

		// this might fare better in utils/evolution/camel.cs
		public enum CamelFlags {
			ANSWERED     = 1<<0,
			DELETED      = 1<<1,
			DRAFT        = 1<<2,
			FLAGGED      = 1<<3,
			SEEN         = 1<<4,
			ATTACHMENTS  = 1<<5,
			ANSWERED_ALL = 1<<6,
			UNKNOWN_7    = 1<<7,
			UNKNOWN_8    = 1<<8
		}

		private int GetDateStamp (string maildate)
		{
			DateTime xdate = DateTime.Parse (maildate);
			int DateStamp = int.Parse (maildate);
			return DateStamp;
		}

		public class Message {
			private string subject;
			private string sender;
			private DateTime sentdate;
			private string icon;
			private string recipient;
			private string uid;
			private bool hasreply;
			private bool isdeleted;
			private bool isdraft;
			private bool isflagged;
			private bool isnew;
			private bool hasattachment;
			private string sendername;
			private string senderaddress;

			public string Subject {
				get { return this.subject; }
				set { this.subject = value; }
			}

			public string Sender {
				get { return this.sender; }
				set { this.sender = value; }
			}

			public DateTime SentDate {
				get { return this.sentdate; }
				set { this.sentdate = value; }
			}

			public string Icon {
				get { return this.icon; }
				set { this.icon = value; }
			}

			public string Recipient {
				get { return this.recipient; }
				set { this.recipient = value; }
			}

			public string UID {
				get { return this.uid; }
				set { this.uid = value; }
			}

			public bool HasAttachment {
				get { return this.hasattachment; }
				set { this.hasattachment = value; }
			}

			public bool HasReply {
				get { return this.hasreply; }
				set { this.hasreply = value; }
			}

			public bool IsDeleted {
				get { return this.isdeleted; }
				set { this.isdeleted = value; }
			}

			public bool IsDraft {
				get { return this.isdraft; }
				set { this.isdraft = value; }
			}

			public bool IsFlagged {
				get { return this.isflagged; }
				set { this.isflagged = value; }
			}

			public bool IsNew {
				get { return this.isnew; }
				set { this.isnew = value; }
			}

			public string GetSenderName ()
			{
				if (this.sender.IndexOf ("<") == -1)
					this.sendername = this.sender;
				else
					this.sendername = this.sender.Substring (0, (this.sender.LastIndexOf ("<") - 1));

				return this.sendername;
			}

			public string GetIcon () 
			{
				if (this.hasreply) {
					this.icon = "internal:mail-replied.png";
				} else if (this.IsNew) {
					this.icon = "internal:mail-new.png";
				} else {
					this.icon = "internal:mail-read.png";
				}

				return this.icon;
			}

			public void Initialize (Hit hit)
			{
				// Set all the properties based on info from the provided Match
				this.subject   = Convert.ToString (hit ["Subject"]);
				this.sender    = Convert.ToString (hit ["From"]);
				this.recipient = Convert.ToString (hit ["To"]);
				this.sentdate  = DateTime.Parse (Convert.ToString (hit ["SentDate"]));
				this.uid       = Convert.ToString (hit ["UID"]);

				// this.sendername = this.sender.Substring (0, (this.sender.LastIndexOf ("<") - 1));

				// Parse the message flags
				int flags = int.Parse (Convert.ToString (hit ["Flags"]));

				if ((flags & (int) CamelFlags.ANSWERED) == (int) CamelFlags.ANSWERED)
					this.hasreply = true;

				if ((flags & (int) CamelFlags.DELETED) == (int) CamelFlags.DELETED)
					this.isdeleted = true;

				if ((flags & (int) CamelFlags.DRAFT) == (int) CamelFlags.DRAFT)
					this.isdraft = true;

				if ((flags & (int) CamelFlags.FLAGGED) == (int) CamelFlags.FLAGGED)
					this.isflagged = true;

				if ((flags & (int) CamelFlags.SEEN) == (int) CamelFlags.SEEN) {
					this.isnew = false;
				} else {
					this.isnew = true;
				}

				if ((flags & (int) CamelFlags.ATTACHMENTS) == (int) CamelFlags.ATTACHMENTS)
					this.hasattachment = true;

				if ((flags & (int) CamelFlags.ANSWERED_ALL) == (int) CamelFlags.ATTACHMENTS)
					this.hasreply = true;

				// Generate the icon
				if (this.hasreply) {
					this.icon = "internal:mail-replied.png";
				} else if (this.isnew) {
					this.icon = "internal:mail-new.png";
				} else {
					this.icon = "internal:mail-read.png";
				}
			}
		}
	}


	public enum SortDirection {
		Ascending, 
		Descending
	}

	public class MailMessageDateComparer : IComparer {

		// Reverse-sort -- newest messages first
		private SortDirection m_direction = SortDirection.Descending;

		int IComparer.Compare (Object x, Object y) {

			Hit matchX = (Hit) x;
			Hit matchY = (Hit) y;

			DateTime DateX = DateTime.Parse (Convert.ToString (matchX ["SentDate"]));
			DateTime DateY = DateTime.Parse (Convert.ToString (matchY ["SentDate"]));

			if (matchX == null && matchY == null) {
				return 0;
			} else if (matchX == null && matchY != null) {
				return (this.m_direction == SortDirection.Ascending) ? -1 : 1;
			} else if (matchX != null && matchY == null) {
				return (this.m_direction == SortDirection.Ascending) ? 1 : -1;
			} else {
				return (this.m_direction == SortDirection.Ascending) ?
					DateX.CompareTo (DateY) : 
					DateY.CompareTo (DateX);
			}
		}
	}



}
