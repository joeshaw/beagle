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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Beagle {

	public class TileCanvas : Gtk.HTML {

		public event EventHandler PreRenderEvent;
		public event EventHandler PostRenderEvent;

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
		
		private bool DoAction (string key)
		{
			TileActionHandler handler = (TileActionHandler) actionTable [key];
			if (handler != null) {
				handler ();
				return true;
			}
			return false;
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
			string name = args.Url;
			Tile tile = null;

			if (name.Length > 0 && name [0] == ':') {
				int i = name.IndexOf (':', 1);
				if (i != -1) {
					string tileKey = name.Substring (1, i-1);
					tile = GetTile (tileKey);
					name = args.Url.Substring (i+1);

				}
			}

			// If this Url request originates from an iframe,
			// we respond by painting the tile into the HTMLStream.
			if (tile != null && name == "_iframe_") {
				PaintTile (tile, args.Handle);
				return;
			}

			// Give the original tile an opportunity to
			// service the Url request.
			if (tile != null && tile.HandleUrlRequest (name, args.Handle))
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
			if (DoAction (args.Url))
				return;

			string command = null;
			string commandArgs = null;

			if (args.Url.StartsWith ("http://")) {
				command = "epiphany";
				commandArgs = args.Url;
			} else if (args.Url.StartsWith ("mailto:")) {
				command = "evolution-1.5";
				commandArgs = args.Url;
			}

			if (command != null) {
				Process p = new Process ();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.FileName = command;
				if (args != null)
					p.StartInfo.Arguments = commandArgs;
				try {
					p.Start ();
				} catch { }
				return;
			}
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
				url = url.Substring (1, url.Length - ":_iframe_".Length - 1);
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
			if (tile != null && ev.Button == 3)
				DoPopupMenu (tile, ev.Button, ev.Time);
		}

		private void OnPopupMenu (object o, Gtk.PopupMenuArgs args)
		{
			Tile tile = TileFromEventSource (o);
			if (tile != null)
				DoPopupMenu (tile, 0, Gtk.Global.CurrentEventTime);
		}

		/////////////////////////////////////////////////

		private class ActionWrapper {

			TileActionHandler handler;

			public ActionWrapper (TileActionHandler _handler)
			{
				handler = _handler;
			}

			public void AsEventHandler (object sender, EventArgs e)
			{
				handler ();
			}
		}

		private class TileCanvasMenuContext : TileMenuContext {
			ArrayList items = new ArrayList ();

			public ICollection Items {
				get { return items; }
			}

			override public void Add (string icon, string label,
						  TileActionHandler handler)
			{
				Gtk.Widget img = null;
				if (icon != null)
					img = Images.GetWidget (icon);

				Gtk.MenuItem item;
				if (img != null) {
					Gtk.ImageMenuItem imgItem;
					imgItem = new Gtk.ImageMenuItem (label);
					imgItem.Image = img;
					item = imgItem;
					items.Add (item);
				} else {
					item = new Gtk.MenuItem (label);
					items.Add (item);
				}

				if (handler != null) {
					ActionWrapper thunk = new ActionWrapper (handler);
					item.Activated += new EventHandler (thunk.AsEventHandler);
				}

				item.Sensitive = (handler != null);
			}

		}

		private void DoPopupMenu (Tile tile, uint button, uint activateTime)
		{
			TileCanvasMenuContext ctx = new TileCanvasMenuContext ();
			tile.PopupMenu (ctx);
			if (ctx.Items.Count == 0)
				return;

			Gtk.Menu menu = new Gtk.Menu ();
			foreach (Gtk.MenuItem mi in ctx.Items) {
				menu.Append (mi);
				mi.ShowAll ();
			}

			menu.ShowAll ();
			menu.Popup (null, null, null, (IntPtr) 0,  button, activateTime);
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

			override public void Image (string name, int width, int height,
						    TileActionHandler handler)
			{
				if (handler != null) {
					string key = canvas.AddAction (handler);
					Write ("<a href=\"{0}\">", key);
				}
				Write ("<img src=\":{0}:{1}\"", tileMain.UniqueKey, name);
				if (width > 0)
					Write (" width=\"{0}\"", width);
				if (height > 0)
					Write (" height=\"{0}\"", height);
				Write (" border=\"0\">");
				if (handler != null)
					Write ("</a>");
			}

			override public void Tile (Tile tile)
			{
				canvas.CacheTile (tile);
				if (tile.RenderInline) {
					TileCanvasRenderContext ctx;
					ctx = new TileCanvasRenderContext (canvas, tile);
					tile.Render (ctx);
					html.Append (ctx.Html);
				} else {
					Write ("<iframe");
					Write (" src=\":{0}:_iframe_\"", tile.UniqueKey);
					Write (" marginwidth=\"0\"");
					Write (" marginheight=\"1\"");
					Write (" frameborder=\"0\"");
					Write ("></iframe>");
				}
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
			if (PreRenderEvent != null)
				PreRenderEvent (this, new EventArgs ());
				
			ClearActions ();
			ClearTiles ();

			Gtk.HTMLStream stream = this.Begin ();
			PaintTile (root, stream);
			this.End (stream, Gtk.HTMLStreamStatus.Ok);

			if (PostRenderEvent != null)
				PostRenderEvent (this, new EventArgs ());

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
			ScheduleRender ();
		}
	}

}
