//
// TileCanvas.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Text;

namespace Beagle {

	public class TileCanvas : Gtk.HTML {

		public TileCanvas () : base ()
		{
			UrlRequested += new Gtk.UrlRequestedHandler (OnUrlRequested);
			LinkClicked += new Gtk.LinkClickedHandler (OnLinkClicked);
			IframeCreated += new Gtk.IframeCreatedHandler (OnIframeCreated);

			AttachSignalHandlers (this);
		}

		/////////////////////////////////////////////////

		Tile root = null;

		public Tile Root {
			get { return root; }
			set {
				root = value; 
				root.SetChangedHandler (new TileChangedHandler (OnTileChanged));
				Render ();
			}
		}

		/////////////////////////////////////////////////

		Hashtable actionTable = null;
		int actionId = 1;

		private void ClearActions ()
		{
			actionTable = new Hashtable ();
			actionId = 1;
		}
		
		private string AddAction (TileActionHandler handler)
		{
			if (handler == null)
				return "_action_:NULL";
			string key = "_action_:" + actionId.ToString ();
			++actionId;
			actionTable [key] = handler;
			return key;
		}

		private bool IsActionKey (string key)
		{
			return key.StartsWith ("_action_:");
		}

		private void DoAction (string key)
		{
			TileActionHandler handler = (TileActionHandler) actionTable [key];
			if (handler != null)
				handler ();
		}


		/////////////////////////////////////////////////

		Hashtable tileTable = null;

		private void ClearTiles ()
		{
			tileTable = new Hashtable ();
		}

		private void CacheTile (Tile tile)
		{
			tileTable [tile.UniqueKey] = tile;
			tile.SetChangedHandler (new TileChangedHandler (OnTileChanged));
		}

		private Tile GetTile (string key)
		{
			if (key == "")
				return root;
			return (Tile) tileTable [key];
		}

		/////////////////////////////////////////////////

		private void OnUrlRequested (object o, Gtk.UrlRequestedArgs args)
		{
			int i = args.Url.IndexOf (':');
			string tileKey = args.Url.Substring (0, i);
			string name = args.Url.Substring (i+1);

			Tile tile = GetTile (tileKey);

			// If this Url request originates from an iframe,
			// we respond by painting the tile into the HTMLStream.
			if (name == "_iframe_") {
				PaintTile (tile, args.Handle);
				return;
			}

			// Give the original tile an opportunity to
			// service the Url request.
			if (tile.HandleUrlRequest (name, args.Handle))
				return;

			// Maybe this is an image: try the image barn
			Stream s = Images.GetStream (name);
			if (s != null) {
				byte[] buffer = new byte [8192];
				int n;
				while ( (n = s.Read (buffer, 0, 8192)) != 0)
					args.Handle.Write (buffer, n);
				return;
			}

			Console.WriteLine ("Unhandled Url '{0}'", name);
		}

		private void OnLinkClicked (object o, Gtk.LinkClickedArgs args)
		{
			DoAction (args.Url);
		}

		private void OnIframeCreated (object o, Gtk.IframeCreatedArgs args)
		{
			Gtk.HTML iframe = args.Iframe;
			AttachSignalHandlers (iframe);
		}

		/////////////////////////////////////////////////

		private void AttachSignalHandlers (Gtk.Widget w)
		{
			w.ButtonPressEvent += new Gtk.ButtonPressEventHandler (OnButtonPressEvent);
			w.PopupMenu += new Gtk.PopupMenuHandler (OnPopupMenu);
		}

		private Tile TileFromEventSource (object o)
		{
			Gtk.HTML src = (Gtk.HTML) o;
			string url = src.Base;
			if (url.EndsWith (":_iframe_"))
				url = url.Substring (0, url.Length - ":_iframe_".Length);
			Tile tile = GetTile (url);
			if (tile == null)
				Console.WriteLine ("Unable to map event to tile! (base='{0}')",
						   url);
			return tile;
		}

		private void OnButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			Gdk.EventButton ev = (Gdk.EventButton) args.Event;
			Tile tile = TileFromEventSource (o);
			Console.WriteLine ("Clicked button {0}", ev.Button);
		}

		private void OnPopupMenu (object o, Gtk.PopupMenuArgs args)
		{
			Tile tile = TileFromEventSource (o);
			Console.WriteLine ("popup!");
		}

		/////////////////////////////////////////////////

		private class TileCanvasRenderContext : TileRenderContext {

			TileCanvas canvas;
			Tile tileMain;
			StringBuilder html = new StringBuilder ();
			string checkpoint = null;

			public TileCanvasRenderContext (TileCanvas _canvas, Tile _tile)
			{
				canvas = _canvas;
				tileMain = _tile;
				canvas.CacheTile (tileMain);
			}

			public string Html {
				get { return html.ToString (); }
			}

			override public void Write (string markup)
			{
				html.Append (markup);
			}

			override public void Link (string label, TileActionHandler handler)
			{
				string key = canvas.AddAction (handler);
				Write ("<a href=\"{0}\">{1}</a>", key, label);
			}

			override public void Image (string name, TileActionHandler handler)
			{
				if (handler != null) {
					string key = canvas.AddAction (handler);
					Write ("<a href=\"{0}\">", key);
				}
				Write ("<img src=\"{0}:{1}\" border=\"0\">", tileMain.UniqueKey, name);
				if (handler != null)
					Write ("</a>");
			}

			override public void Tile (Tile tile)
			{
				canvas.CacheTile (tile);
				Write ("<iframe");
				Write (" src=\"{0}:_iframe_\"", tile.UniqueKey);
				Write (" marginwidth=\"0\"");
				Write (" marginheight=\"1\"");
				Write (" frameborder=\"0\"");
				Write ("></iframe>");
			}

			override public void Checkpoint ()
			{
				checkpoint = html.ToString ();
			}

			override public void Undo ()
			{
				if (checkpoint != null) {
					html = new StringBuilder (checkpoint);
					checkpoint = null;
				}
			}
		}

		private void PaintTile (Tile tile, Gtk.HTMLStream stream)
		{
			stream.Write ("<html><body>");
			if (tile != null) {
				TileCanvasRenderContext ctx;
				ctx = new TileCanvasRenderContext (this, tile);
				tile.Render (ctx);
				stream.Write (ctx.Html);
			}
			stream.Write ("</body></html>");
		}

		/////////////////////////////////////////////////

		private void DoRender ()
		{
			Console.WriteLine ("DoRender!");
			ClearActions ();
			ClearTiles ();

			Gtk.HTMLStream stream = this.Begin ();
			PaintTile (root, stream);
			this.End (stream, Gtk.HTMLStreamStatus.Ok);
		}

		/////////////////////////////////////////////////

		private uint renderId = 0;

		public void Render ()
		{
			lock (this) {
				if (renderId != 0) {
					GLib.Source.Remove (renderId);
					renderId = 0;
				}
				DoRender ();
			}
		}

		private bool RenderHandler ()
		{
			lock (this) {
				renderId = 0;
				DoRender ();
			}
			return false;
		}

		public void ScheduleRender ()
		{
			lock (this) {
				if (renderId != 0)
					return;
				renderId = GLib.Timeout.Add (100, new GLib.TimeoutHandler (RenderHandler));
			}
		}

		private void OnTileChanged (Tile tile)
		{
			Console.WriteLine ("Tile Changed!");
			ScheduleRender ();
		}
	}

}
