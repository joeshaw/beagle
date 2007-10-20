
//
// FilterMail.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
//
//
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using GMime;

using Beagle;
using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Filters {

	[PropertyKeywordMapping (Keyword="mailfrom",     PropertyName="fixme:from_name",    IsKeyword=false, Description="Name of email sender")]
	[PropertyKeywordMapping (Keyword="mailfrom",     PropertyName="parent:fixme:from_name",    IsKeyword=false)]
	[PropertyKeywordMapping (Keyword="mailfromaddr", PropertyName="fixme:from_address", IsKeyword=false, Description="Email address of sender")]
	[PropertyKeywordMapping (Keyword="mailfromaddr", PropertyName="parent:fixme:from_address", IsKeyword=false)]
	[PropertyKeywordMapping (Keyword="mailto",       PropertyName="fixme:to_name",      IsKeyword=false, Description="Name of receipient")]
	[PropertyKeywordMapping (Keyword="mailto",       PropertyName="parent:fixme:to_name",      IsKeyword=false)]
	[PropertyKeywordMapping (Keyword="mailtoaddr",   PropertyName="fixme:to_address",   IsKeyword=false, Description="Email address of receipient")]
	[PropertyKeywordMapping (Keyword="mailtoaddr",   PropertyName="parent:fixme:to_address",   IsKeyword=false)]
	[PropertyKeywordMapping (Keyword="mailinglist",  PropertyName="fixme:mlist",        IsKeyword=false, Description="Mailing list id e.g. dashboard-hackers.gnome.org")]
	[PropertyKeywordMapping (Keyword="mailinglist",  PropertyName="parent:fixme:mlist",        IsKeyword=false)]
	[PropertyKeywordMapping (Keyword="inattachment",  PropertyName="parent:fixme:hasAttachments", IsKeyword=true, Description="Use 'inattachment:true' for email attachments.")]
	public class FilterMail : Beagle.Daemon.Filter, IDisposable {

		private static bool gmime_initialized = false;
		private static bool gmime_broken = false;

		private GMime.Message message;
		private PartHandler handler;

		public FilterMail ()
		{
			// 1: Make email addresses non-keyword, add sanitized version
			//    for eaching for parts of an email address.
			// 2: No need to separately add sanitized version of emails.
			//    BeagleAnalyzer uses a tokenfilter taking care of this.
			// 3: Add standard file properties to attachments.
			// 4: Store snippets
			// 5: Store mailing list id as tokenized
			SetVersion (5);

			SnippetMode = true;
			SetFileType ("mail");
		}

		protected override void RegisterSupportedTypes ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("message/rfc822"));

			// Add the list of user requested maildir directories
			// This is useful if beagle (xdgmime) does not correctly detect the mimetypes
			// of several maildir files as message/rfc822

			List<string[]> values = Conf.Daemon.GetListOptionValues (Conf.Names.Maildirs);
			if (values == null)
				return;

			foreach (string[] maildir in values) {
				FilterFlavor flavor =
					new FilterFlavor (new Uri (maildir [0] + "/*").ToString (),
							  null, // No meaning of extension for maildir files
							  null,
							  1 /* Should be more than priority of text filter */);
				AddSupportedFlavor (flavor);
			}
		}

		protected override void DoOpen (FileInfo info)
		{
			if (!gmime_initialized) {
				gmime_initialized = true;

				// Only try initialization once; if it fails, it'll never work.
				try {
					GMime.Global.Init ();
				} catch (Exception e) {
					Log.Error (e, "Unable to initialize GMime");
					gmime_broken = true;
					Error ();
					return;
				}
			}

			if (gmime_broken) {
				Error ();
				return;
			}

			int mail_fd = Mono.Unix.Native.Syscall.open (info.FullName, Mono.Unix.Native.OpenFlags.O_RDONLY);
			
			if (mail_fd == -1)
				throw new IOException (String.Format ("Unable to read {0} for parsing mail", info.FullName));

			GMime.StreamFs stream = new GMime.StreamFs (mail_fd);
			GMime.Parser parser = new GMime.Parser (stream);
			this.message = parser.ConstructMessage ();
			stream.Dispose ();
			parser.Dispose ();

			if (this.message == null)
				Error ();
		}

		private bool HasAttachments (GMime.Object mime_part)
		{
			if (mime_part is GMime.MessagePart)
				return true;

			// Messages that are multipart/alternative shouldn't be considered as having
			// attachments.  Unless of course they do.
			if (mime_part is GMime.Multipart && mime_part.ContentType.Subtype.ToLower () != "alternative")
				return true;

			return false;
		}

		protected override void DoPullProperties ()
		{
			string subject = GMime.Utils.HeaderDecodePhrase (this.message.Subject);
			AddProperty (Property.New ("dc:title", subject));

			AddProperty (Property.NewDate ("fixme:date", message.Date.ToUniversalTime ()));

			GMime.InternetAddressList addrs;
			addrs = this.message.GetRecipients (GMime.Message.RecipientType.To);
			foreach (GMime.InternetAddress ia in addrs) {
				AddProperty (Property.NewUnsearched ("fixme:to", ia.ToString (false)));
				if (ia.AddressType != GMime.InternetAddressType.Group)
					AddProperty (Property.New ("fixme:to_address", ia.Addr));

				AddProperty (Property.New ("fixme:to_name", ia.Name));
			}
			addrs.Dispose ();

			addrs = this.message.GetRecipients (GMime.Message.RecipientType.Cc);
			foreach (GMime.InternetAddress ia in addrs) {
				AddProperty (Property.NewUnsearched ("fixme:cc", ia.ToString (false)));
				if (ia.AddressType != GMime.InternetAddressType.Group)
					AddProperty (Property.New ("fixme:cc_address", ia.Addr));

				AddProperty (Property.New ("fixme:cc_name", ia.Name));
			}
			addrs.Dispose ();

			addrs = GMime.InternetAddressList.ParseString (GMime.Utils.HeaderDecodePhrase (this.message.Sender));
			foreach (GMime.InternetAddress ia in addrs) {
				AddProperty (Property.NewUnsearched ("fixme:from", ia.ToString (false)));
				if (ia.AddressType != GMime.InternetAddressType.Group)
					AddProperty (Property.New ("fixme:from_address", ia.Addr));

				AddProperty (Property.New ("fixme:from_name", ia.Name));
			}
			addrs.Dispose ();

			if (HasAttachments (this.message.MimePart))
				AddProperty (Property.NewFlag ("fixme:hasAttachments"));

			// Store the message ID and references are unsearched
			// properties.  They will be used to generate
			// conversations in the frontend.
			string msgid = this.message.GetHeader ("Message-Id");
		        if (msgid != null)
				AddProperty (Property.NewUnsearched ("fixme:msgid", GMime.Utils.DecodeMessageId (msgid)));

			foreach (GMime.References refs in this.message.References)
				AddProperty (Property.NewUnsearched ("fixme:reference", refs.Msgid));

			string list_id = this.message.GetHeader ("List-Id");
			if (list_id != null)
				AddProperty (Property.New ("fixme:mlist", GMime.Utils.HeaderDecodePhrase (list_id)));

			// KMail can store replies in the same folder
			// Use issent flag to distinguish between incoming
			// and outgoing message
			string kmail_msg_sent = this.message.GetHeader ("X-KMail-Link-Type");
			bool issent_is_set = false;
			foreach (Property property in Indexable.Properties) {
				if (property.Key == "fixme:isSent") {
					issent_is_set = true;
					break;
				}
			}
			if (!issent_is_set && kmail_msg_sent != null && kmail_msg_sent == "reply")
				AddProperty (Property.NewFlag ("fixme:isSent"));
		}

		protected override void DoPullSetup ()
		{
			this.handler = new PartHandler (Indexable);
			using (GMime.Object mime_part = this.message.MimePart)
				this.handler.OnEachPart (mime_part);

			AddIndexables (this.handler.ChildIndexables);
		}

		protected override void DoPull ()
		{
			if (handler.Reader == null) {
				Finished ();
				return;
			}

			string l = handler.Reader.ReadLine ();

			if (l == null)
				Finished ();
			else if (l.Length > 0) {
				AppendText (l);
				AppendStructuralBreak ();
			}
		}

		protected override void DoClose ()
		{
			Dispose ();
		}
		
		public void Dispose ()
		{
			if (this.handler != null && this.handler.Reader != null)
				this.handler.Reader.Close ();
			this.handler = null;

			if (this.message != null) {
				this.message.Dispose ();
				this.message = null;
			}
		}

		private class PartHandler {
			private Indexable indexable;
			private int count = 0; // parts handled so far
			private int depth = 0; // part recursion depth
			private ArrayList child_indexables = new ArrayList ();
			private TextReader reader;

			// Blacklist a handful of common MIME types that are
			// either pointless on their own or ones that we don't
			// have filters for.
			static private string[] blacklisted_mime_types = new string[] {
				"application/pgp-signature",
				"application/x-pkcs7-signature",
				"application/ms-tnef",
				"text/x-vcalendar",
				"text/x-vcard"
			};

			public PartHandler (Indexable parent_indexable)
			{
				this.indexable = parent_indexable;
			}

			private bool IsMimeTypeHandled (string mime_type)
			{
				foreach (FilterFlavor flavor in FilterFlavor.Flavors) {
					if (flavor.IsMatch (null, null, mime_type.ToLower ()))
						return true;
				}

				return false;
			}

			public void OnEachPart (GMime.Object mime_part)
			{
				GMime.Object part = null;
				bool part_needs_dispose = false;

				//for (int i = 0; i < this.depth; i++)
				//  Console.Write ("  ");
				//Console.WriteLine ("Content-Type: {0}", mime_part.ContentType);
			
				++depth;

				if (mime_part is GMime.MessagePart) {
					GMime.MessagePart msg_part = (GMime.MessagePart) mime_part;

					using (GMime.Message message = msg_part.Message) {
						using (GMime.Object subpart = message.MimePart)
							this.OnEachPart (subpart);
					}
				} else if (mime_part is GMime.Multipart) {
					GMime.Multipart multipart = (GMime.Multipart) mime_part;

					int num_parts = multipart.Number;

					// If the mimetype is multipart/alternative, we only want to index
					// one part -- the richest one we can filter.
					if (mime_part.ContentType.Subtype.ToLower () == "alternative") {
						// The richest formats are at the end, so work from there
						// backward.
						for (int i = num_parts - 1; i >= 0; i--) {
							GMime.Object subpart = multipart.GetPart (i);

							if (IsMimeTypeHandled (subpart.ContentType.ToString ())) {
								part = subpart;
								part_needs_dispose = true;
								break;
							} else {
								subpart.Dispose ();
							}
						}
					}

					// If it's not alternative, or we don't know how to filter any of
					// the parts, treat them like a bunch of attachments.
					if (part == null) {
						for (int i = 0; i < num_parts; i++) {
							using (GMime.Object subpart = multipart.GetPart (i))
								this.OnEachPart (subpart);
						}
					}
				} else if (mime_part is GMime.Part)
					part = mime_part;
				else
					throw new Exception (String.Format ("Unknown part type: {0}", part.GetType ()));

				if (part != null) {
					System.IO.Stream stream = null;
					
					using (GMime.DataWrapper content_obj = ((GMime.Part) part).ContentObject)
						stream = content_obj.Stream;

					// If this is the only part and it's plain text, we
					// want to just attach it to our filter instead of
					// creating a child indexable for it.
					bool no_child_needed = false;

					string mime_type = part.ContentType.ToString ().ToLower ();

					if (this.depth == 1 && this.count == 0) {
						if (mime_type == "text/plain") {
							no_child_needed = true;

							this.reader = new StreamReader (stream);
						}
					}

					if (!no_child_needed) {
						// Check the mime type against the blacklist and don't index any
						// parts that are contained within.  That way the user doesn't
						// get flooded with pointless signatures and vcard and ical
						// attachments along with (real) attachments.

						if (Array.IndexOf (blacklisted_mime_types, mime_type) == -1) {
							string sub_uri = this.indexable.Uri.ToString () + "#" + this.count;
							Indexable child = new Indexable (new Uri (sub_uri));

							child.DisplayUri = new Uri (this.indexable.DisplayUri.ToString () + "#" + this.count);

							// This is a special case.
							// Even for mails found on disk, MailMessage hitype is set
							child.HitType = "MailMessage";
							child.MimeType = mime_type;

							// If this is the richest part we found for multipart emails, add its content to textcache
							if (this.depth == 1 && this.count == 0)
								child.CacheContent = true;
							else
								child.CacheContent = false;

							string filename = ((GMime.Part) part).Filename;

							if (! String.IsNullOrEmpty (filename)) {
								child.AddProperty (Property.NewKeyword ("fixme:attachment_title", filename));

								foreach (Property prop in Property.StandardFileProperties (filename, false))
									child.AddProperty (prop);
							}

							if (part.ContentType.Type.ToLower () == "text")
								child.SetTextReader (new StreamReader (stream));
							else
								child.SetBinaryStream (stream);

							child.SetChildOf (this.indexable);
							child.StoreStream ();
							child.CloseStreams ();
							this.child_indexables.Add (child);
						} else {
							Log.Debug ("Skipping attachment {0}#{1} with blacklisted mime type {2}",
								   this.indexable.Uri, this.count, mime_type);
						}
					}

					this.count++;
				}

				if (part_needs_dispose)
					part.Dispose ();

				--depth;
			}

			public ICollection ChildIndexables {
				get { return this.child_indexables; }
			}

			public TextReader Reader {
				get { return this.reader; }
			}
		}

				       
	}

}
