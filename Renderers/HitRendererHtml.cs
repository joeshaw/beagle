//
// HtmlRenderer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Net;

using Gtk;

namespace Beagle {

	public abstract class HitRendererHtml : HitRenderer {

		private Gtk.HTML html;
		
		public HitRendererHtml ()
		{
			html = new Gtk.HTML ();
			//this.html.LinkClicked += new LinkClickedHandler (LinkClicked);
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


	
	}
}
