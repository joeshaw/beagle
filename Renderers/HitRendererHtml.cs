//
// HtmlRenderer.cs
//
// Copyright (C) 2004 Novell, Inc.
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

	public abstract class HitRendererHtml : HitRenderer {

		private Gtk.HTML html;
		
		public HitRendererHtml ()
		{
			html = new Gtk.HTML ();
			this.html.LinkClicked += new LinkClickedHandler (LinkClicked);
			this.html.UrlRequested += new UrlRequestedHandler (UrlRequested);
		}

		public override Gtk.Widget Widget {
			get { return html; }
		}

		protected abstract String HitsToHtml (ArrayList hits);

		protected override void DoRenderHits (ArrayList hits)
		{
			Gtk.HTMLStream stream = html.Begin ();
			stream.Write ("<html><body>");
			stream.Write (HitsToHtml (hits));
			stream.Write ("</body></html>");
		}

		///////////////////////////////////

		private Stream GetImageResource (string name)
		{
			Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
			System.IO.Stream s = assembly.GetManifestResourceStream (name);

			return s;
		}

		private Stream GetFile (string uri)
		{
			return File.Open (uri, FileMode.Open, FileAccess.Read);
		}

		//
		// Provides data for urls requested (images).  Things prefixed
		// with `internal:' we pull for one of the embedded streams
		//
		private void UrlRequested (object o, UrlRequestedArgs args)
		{
			Stream s = null;

			if (args.Url.IndexOf ("/") == 0) {
				s = GetFile (args.Url);
			} else {
				if (args.Url.StartsWith ("internal:")) {
					try {
						s = GetImageResource (args.Url.Substring (args.Url.IndexOf (':') + 1));
					} catch {
						Console.WriteLine ("Could not find image: " + args.Url);
						return;
					}
				} else {
					try {
						HttpWebRequest req = (HttpWebRequest)WebRequest.Create (args.Url);
						req.UserAgent = "Beagle HTML Renderer";
						WebResponse resp = req.GetResponse ();
						s = resp.GetResponseStream ();
					} catch (Exception e) {
						Console.WriteLine ("Do not know how to handle " + args.Url);
						return;
					}
				}
			}

			if (s == null) {
				Console.WriteLine ("Could not obtain image " + args.Url);
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
			} else if (args.Url.StartsWith ("file://")) {
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
