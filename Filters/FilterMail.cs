
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
using System.IO;

using GMime;

using Beagle;
using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterMail : Beagle.Daemon.Filter, IDisposable {

		private static bool gmime_initialized = false;

		private GMime.Stream stream;
		private GMime.Parser parser;
		private GMime.Message message;
		private PartHandler handler;

		public FilterMail ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("message/rfc822"));
		}

		protected override void DoOpen (FileInfo info)
		{
			if (!gmime_initialized) {
				try {
					GMime.Global.Init ();
					gmime_initialized = true;
				} catch {
					Error ();
					return;
				}
			}

			int mail_fd = Mono.Unix.Native.Syscall.open (info.FullName, Mono.Unix.Native.OpenFlags.O_RDONLY);
			
			if (mail_fd == -1)
				throw new IOException (String.Format ("Unable to read {0} for parsing mail", info.FullName));

			this.stream = new GMime.StreamFs (mail_fd);
			this.parser = new GMime.Parser (stream);
		}

		protected override void DoPullProperties ()
		{
			this.message = this.parser.ConstructMessage ();

			string subject = GMime.Utils.HeaderDecodePhrase (this.message.Subject);
			AddProperty (Property.New ("dc:title", subject));

			AddProperty (Property.NewDate ("fixme:date", message.Date.ToUniversalTime ()));

			GMime.InternetAddressList addrs;
			addrs = this.message.GetRecipients (GMime.Message.RecipientType.To);
			foreach (GMime.InternetAddress ia in addrs) {
				AddProperty (Property.NewKeyword ("fixme:to", ia.ToString (false)));
				AddProperty (Property.NewKeyword ("fixme:to_address", ia.Addr));
				AddProperty (Property.New ("fixme:to_name", ia.Name));
			}
			addrs.Dispose ();

			addrs = this.message.GetRecipients (GMime.Message.RecipientType.Cc);
			foreach (GMime.InternetAddress ia in addrs) {
				AddProperty (Property.NewKeyword ("fixme:cc", ia.ToString (false)));
				AddProperty (Property.NewKeyword ("fixme:cc_address", ia.Addr));
				AddProperty (Property.New ("fixme:cc_name", ia.Name));
			}
			addrs.Dispose ();

			addrs = GMime.InternetAddressList.ParseString (GMime.Utils.HeaderDecodePhrase (this.message.Sender));
			foreach (GMime.InternetAddress ia in addrs) {
				AddProperty (Property.NewKeyword ("fixme:from", ia.ToString (false)));
				AddProperty (Property.NewKeyword ("fixme:from_address", ia.Addr));
				AddProperty (Property.New ("fixme:from_name", ia.Name));
			}
			addrs.Dispose ();

			if (this.message.MimePart is GMime.Multipart || this.message.MimePart is GMime.MessagePart)
				AddProperty (Property.NewFlag ("fixme:hasAttachments"));

			string list_id = this.message.GetHeader ("List-Id");

			if (list_id != null) {
				// FIXME: Might need some additional parsing.
				AddProperty (Property.NewKeyword ("fixme:mlist", GMime.Utils.HeaderDecodePhrase (list_id)));
			}
		}

		protected override void DoPullSetup ()
		{
			this.handler = new PartHandler (this);
			using (GMime.Object mime_part = this.message.MimePart)
				this.handler.OnEachPart (mime_part);

			AddChildIndexables (this.handler.ChildIndexables);
		}

		protected override void DoPull ()
		{
			if (handler.Reader == null) {
				Finished ();
				return;
			}

			string l = handler.Reader.ReadLine ();

			if (l != null)
				AppendText (l);
			else
				Finished ();
		}

		protected override void DoClose ()
		{
			Dispose ();
		}
		
		public void Dispose ()
		{
			if (this.message != null)
				this.message.Dispose ();

			if (this.stream != null) {
				this.stream.Close ();
				this.stream.Dispose ();
			}

			if (this.parser != null)
				this.parser.Dispose ();
		}

		private class PartHandler {
			private Beagle.Daemon.Filter filter;
			private int count = 0; // parts handled so far
			private int depth = 0; // part recursion depth
			private ArrayList child_indexables = new ArrayList ();
			private TextReader reader;

			public PartHandler (Beagle.Daemon.Filter filter)
			{
				this.filter = filter;
			}

			public void OnEachPart (GMime.Object part)
			{
				//for (int i = 0; i < depth; i++)
				//  Console.Write ("  ");
				//Console.WriteLine ("Content-Type: {0}", part.ContentType);
			
				++depth;

				if (part is GMime.MessagePart) {
					GMime.MessagePart msg_part = (GMime.MessagePart) part;

					using (GMime.Message message = msg_part.Message) {
						using (GMime.Object subpart = message.MimePart)
							this.OnEachPart (subpart);
					}
				} else if (part is GMime.Multipart) {
					GMime.Multipart multipart = (GMime.Multipart) part;

					int num_parts = multipart.Number;
					for (int i = 0; i < num_parts; i++) {
						using (GMime.Object subpart = multipart.GetPart (i))
							this.OnEachPart (subpart);
					}
				} else if (part is GMime.Part) {
					MemoryStream stream = null;
					
					using (GMime.DataWrapper content_obj = ((GMime.Part) part).ContentObject) {
						stream = new MemoryStream ();
						content_obj.WriteToStream (stream);
						stream.Seek (0, SeekOrigin.Begin);
					}

					// If this is the only part and it's plain text, we
					// want to just attach it to our filter instead of
					// creating a child indexable for it.
					bool no_child_needed = false;

					if (this.depth == 1 && this.count == 0) {
						if (part.ContentType.ToString ().ToLower () == "text/plain") {
							no_child_needed = true;

							this.reader = new StreamReader (stream);
						}
					}

					if (!no_child_needed) {
						string sub_uri = this.filter.Uri.ToString () + "#" + this.count;
						Indexable child = new Indexable (new Uri (sub_uri));

						child.HitType = "MailMessage";
						child.MimeType = part.ContentType.ToString ();
						child.CacheContent = false;

						child.AddProperty (Property.NewKeyword ("fixme:attachment_title", ((GMime.Part)part).Filename));

						if (part.ContentType.Type.ToLower () == "text")
							child.SetTextReader (new StreamReader (stream));
						else
							child.SetBinaryStream (stream);

						this.child_indexables.Add (child);
					}

					this.count++;
				} else {
					throw new Exception (String.Format ("Unknown part type: {0}", part.GetType ()));
				}

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
