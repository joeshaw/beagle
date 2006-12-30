//
// HtmlRenderer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;

using Gtk;

using BU = Beagle.Util;

namespace Beagle {

	public class HitRendererHtml : HitRenderer {

		private Gtk.HTML html = new Gtk.HTML ();
		private Hashtable tiles = new Hashtable ();
		
		public HitRendererHtml ()
		{
			this.html.LinkClicked += new LinkClickedHandler (LinkClicked);
			this.html.UrlRequested += new UrlRequestedHandler (UrlRequested);
		}

		////////////////////////////////////

		private Tile HitToTile (Hit hit)
		{
			Tile t = null;

			// FIXME: We don't want to allow heterogenous containers.

			switch (hit.Type) {

			case "Contact":
				t = new Tile ("contact.html", hit);
				break;

			case "File":
				t = new Tile ("file-generic.html", hit);
				break;

			case "IMLog":
				t = new Tile ("im-log.html", hit);
				break;

			case "MailMessage":
				string icon = "mail.png";
				if (hit ["_IsAnswered"] != null)
					icon = "mail-replied.png";
				else if (hit ["_IsSeen"] != null)
					icon = "mail-read.png";
				hit ["mail:Icon"] = icon;

				if (hit ["_IsSent"] != null)
					t = new Tile ("email-sent.html", hit);
				else
					t = new Tile ("email.html", hit);
				break;

			case "WebHistory":
				t = new Tile ("web-history.html", hit);
				break;

			case "WebLink":
				if (hit.Source == "Google")
					t = new Tile ("google.html", hit);
				break;
			}

			return t;
		}

		protected override bool ProcessHit (Hit hit)
		{
			Tile tile = HitToTile (hit);
			if (tile == null)
				return false;

			tiles [hit] = tile;
			return true;
		}

		protected override void ProcessClear ()
		{
			tiles.Clear ();
		}

		////////////////////////////////////

		public override Gtk.Widget Widget {
			get { return html; }
		}

		protected override void DoRefresh ()
		{
			Gtk.HTMLStream stream = html.Begin ();
			stream.Write ("<html><body>");
			if (DisplayedCount > 0) {
				// FIXME: layout in a table, or something
				for (int i = FirstDisplayed; i <= LastDisplayed; ++i) {
					Hit hit = (Hit) Hits [i];
					Tile t = (Tile) tiles [hit];
					stream.Write (t.Html);
				}
			}
			stream.Write ("</body></html>");
		}

		///////////////////////////////////

		//
		// Provides data for urls requested (images).  Things prefixed
		// with `internal:' we pull for one of the embedded streams
		//
		private void UrlRequested (object o, UrlRequestedArgs args)
		{
			Stream s = DataBarn.GetStream (args.Url);
			if (s == null) {
				Console.WriteLine ("Could not obtain image '{0}'", args.Url);
				return;
			}

			byte [] buffer = new byte [8192];
			int n;
			while ( (n = s.Read (buffer, 0, 8192)) != 0)
				args.Handle.Write (buffer, n);
		}

		private void LinkClicked (object o, LinkClickedArgs args)
		{
			String command = null, arguments = null;

			if (args.Url.StartsWith ("exec:")) {
				command = args.Url.Substring ("exec:".Length);
				int i = command.IndexOf (' ');
				if (i != -1) {
					arguments = command.Substring (i+1);
					command = command.Substring (0, i);
				}
			} else if (args.Url.StartsWith ("http")) {
				command = "epiphany";
				arguments = args.Url;
			} else if (args.Url.StartsWith ("email")) {
				command = "evolution-1.5";
				arguments = args.Url;
			} else if (args.Url.StartsWith ("mailto:")) {
				command = "evolution-1.5";
				arguments = args.Url;
			} else if (args.Url.StartsWith ("file://")) {
				// Hacky: we extract the mime type from inside of
				// the file Url.
				arguments = args.Url.Substring ("file://".Length);
				int i = arguments.IndexOf (' ');
				String mimeType;
				if (i == -1) {
					mimeType = BU.GnomeIconLookup.GetMimeType (arguments);
				} else {
					mimeType = arguments.Substring (i+1);
					arguments = arguments.Substring (0, i);
				}

				// Try to open w/ the default handler
				BU.GnomeVFSMimeApplication app;
				app = BU.GnomeIconLookup.GetDefaultAction (mimeType);
				command = app.command;
			}

			Console.WriteLine ("Command=[{0}]   Args=[{1}]", command, arguments);

			if (command != null) {
				Process p = new Process ();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.FileName = command;
				if (arguments != null)
					p.StartInfo.Arguments = arguments;
				try {
					p.Start ();
				} catch { }
				return;
			}
		}
	}
}
