//
// Tile.cs
//
// Copyright (C) 2008 Lukas Lipka <lukaslipka@gmail.com>
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Mono.Unix;

using Gtk;
using Beagle;
using Beagle.Util;

namespace Beagle.Search.Tiles {

	public abstract class Tile : Gtk.EventBox {

		private Beagle.Hit hit = null;
		private Beagle.Query query = null;

		private Gtk.HBox hbox = null;
		private Gtk.Image icon = null;
		private DetailsPane details = null;

		private string title = null;
		private string snippet = null;
		private DateTime timestamp;
		private double score = 0.0f;

		// Default the tile group to documents, it is up
		// to the each tile to set a specific group.

		private TileGroup group = TileGroup.Documents;

		// DND targets

		static Gtk.TargetEntry[] targets = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, 0)
		};

		private List<TileAction> actions = new List<TileAction> ();

		protected bool EnableOpenWith = false;

		public event EventHandler Selected;

		public delegate void GotSnippetHandler (string snippet);
		public event GotSnippetHandler GotSnippet;

		public Tile (Hit hit, Query query) : base ()
		{
			base.AboveChild = true;
			base.AppPaintable = true;
			base.CanFocus = true;

			this.hit = hit;
			this.query = query;
			this.timestamp = hit.Timestamp;
			this.score = hit.Score;

			Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask, targets,
					    Gdk.DragAction.Copy | Gdk.DragAction.Move);

			int pad = (int)StyleGetProperty ("focus-line-width") + (int)StyleGetProperty ("focus-padding") + 1;

			hbox = new Gtk.HBox (false, 5);
			hbox.BorderWidth = (uint)(pad + Style.Xthickness);
			hbox.Show ();

			icon = new Gtk.Image ();
			icon.Show ();
			hbox.PackStart (icon, false, false, 0);

			Add (hbox);
		}

		protected void AddAction (TileAction action)
		{
			actions.Add (action);
		}

		protected void RemoveAction (TileAction action)
		{
			actions.Remove (action);
		}

		private void ShowPopupMenu ()
		{
			Gtk.Menu menu = new Gtk.Menu ();
			
			// Add the default 'Open' menu item

			TileAction open = new TileAction (Catalog.GetString ("Open"), Stock.Open, Open);

			ActionMenuItem open_menu_item = new ActionMenuItem (open);
			menu.Append (open_menu_item);

#if ENABLE_OPEN_WITH
			if (EnableOpenWith) {
				// FIXME: Not sure if going with the parent is
				// the right thing to do in all cases.
				string mimetype = Utils.GetFirstPropertyOfParent (hit, "beagle:MimeType");

				OpenWithMenu owm = new OpenWithMenu (mimetype);
				owm.ApplicationActivated += OpenWith;
				owm.AppendToMenu (menu);
			}
#endif

			if (Actions.Count > 0) {
				SeparatorMenuItem separator = new SeparatorMenuItem ();
				menu.Append (separator);

				foreach (TileAction action in Actions) {
					ActionMenuItem item = new ActionMenuItem (action);
					menu.Append (item);
				}
			}

			menu.ShowAll ();
			menu.Popup ();
		}

		protected override void OnDragBegin (Gdk.DragContext context)
		{
			if (!icon.Visible)
				return;

			WidgetFu.SetDragImage (context, icon);
		}

		protected override void OnDragDataGet (Gdk.DragContext ctx, Gtk.SelectionData data, uint info, uint time)
		{
			byte[] uri = System.Text.Encoding.UTF8.GetBytes (Hit.EscapedUri + "\r\n");
			data.Set (data.Target, 8, uri);
		}

		/*protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			// base.OnSizeRequested (ref req) should work,
			// but it doesn't
			req = hbox.SizeRequest ();

			int pad = (int)StyleGetProperty ("focus-line-width") + (int)StyleGetProperty ("focus-padding") + 1;

			req.Width += 2 * (pad + Style.Xthickness);
			req.Height += 2 * (pad + Style.Ythickness);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle alloc)
		{
			int pad = (int)StyleGetProperty ("focus-line-width") + (int)StyleGetProperty ("focus-padding") + 1;

			alloc.X += pad + Style.Xthickness;
			alloc.Y += pad + Style.Ythickness;
			alloc.Width -= pad + Style.Xthickness;
			alloc.Height -= pad + Style.Ythickness;

			base.OnSizeAllocated (alloc);
			}*/

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			if (!IsDrawable)
				return false;

			Cairo.Context gr = Gdk.CairoHelper.Create (evt.Window);

			Gdk.Color fill = Style.BaseColors [(int)StateType.Normal];

			gr.Rectangle (evt.Area.X, evt.Area.Y, evt.Area.Width, evt.Area.Height);
			gr.Color = CairoFu.GdkColorToCairoColor (fill);
			gr.Fill ();

			if (State == StateType.Selected) {
				CairoFu.RoundedSelection (gr, this, 0, 0, Allocation.Width, Allocation.Height);
			}

			if (HasFocus) {
				int focus_padding = (int)StyleGetProperty ("focus-padding");
				int x = focus_padding + Style.Xthickness;
				int y = focus_padding + Style.Ythickness;
				int width = Allocation.Width - 2 * (focus_padding + Style.Xthickness);
				int height = Allocation.Height - 2 * (focus_padding + Style.Ythickness);
				Style.PaintFocus (Style, GdkWindow, State, evt.Area, this, null, x, y, width, height);
			}
			
			CairoFu.DisposeContext (gr);

			if (base.OnExposeEvent (evt))
				return true;

			return false;
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton b)
		{
			GrabFocus ();

			if (b.Button == 3) {
				ShowPopupMenu ();

				return true;
			} else if (b.Type == Gdk.EventType.TwoButtonPress) {
				Open ();

				if (b.Button == 2 || ((b.State & Gdk.ModifierType.ShiftMask) != 0))
					Gtk.Application.Quit ();

				return true;
			}

			return base.OnButtonPressEvent (b);
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus f)
		{
			if (Selected != null)
				Selected (this, EventArgs.Empty);

			return base.OnFocusInEvent (f);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey k)
		{
			if (k.Key == Gdk.Key.Return || k.Key == Gdk.Key.KP_Enter) {
				Open ();

				if ((k.State & Gdk.ModifierType.ShiftMask) != 0)
					Gtk.Application.Quit ();

				return true;
			}

			return base.OnKeyPressEvent (k);
		}

		protected virtual void LoadIcon (Gtk.Image image, int size)
		{
			// This is a hack to prevent large mime icons when we
			// dont have a thumbnail.
			if (size > 48)
				size = 48;

			image.Pixbuf = WidgetFu.LoadMimeIcon (hit.MimeType, size);
		}

		protected void RequestSnippet ()
		{
			if (snippet != null) {
				EmitGotSnippet ();
			} else {
				SnippetRequest sreq = new SnippetRequest (query, hit);
				sreq.RegisterAsyncResponseHandler (typeof (SnippetResponse), SnippetResponseReceived);
				sreq.SendAsync ();
			}
		}

		private void SnippetResponseReceived (ResponseMessage response)
		{
			// The returned snippet uses <font color="..."><b>blah</b></font>
			// to mark matches. The rest of the snippet might be HTML, or
			// it might be plain text, including unescaped '<'s and '&'s.
			// So we escape it, fix the match highlighting, and leave any
			// other tags escaped.

			// FIXME: Use the new snippeting framework
			
			snippet = GLib.Markup.EscapeText (((SnippetResponse)response).Snippet);
			snippet = Regex.Replace (snippet, "&lt;font color=&quot;.*?&quot;&gt;&lt;b&gt;(.*?)&lt;/b&gt;&lt;/font&gt;", "<b>$1</b>");
			if (snippet.Trim ().Length > 0)
				EmitGotSnippet ();
		}

		private void EmitGotSnippet ()
		{
			if (! String.IsNullOrEmpty (snippet) && GotSnippet != null)
				GotSnippet (snippet);
		}

		protected virtual DetailsPane GetDetails ()
		{
			return null;
		}

		public Gtk.Widget Details {
			get {
				if (details == null) {
					details = GetDetails ();
					if (details != null) {
						if (details.Icon.Pixbuf == null)
							LoadIcon (details.Icon, 128);

						if (details.Snippet != null) {
							GotSnippet += details.GotSnippet;
							RequestSnippet ();
						}
						
						details.Show ();
					}
				}
				return details;
			}
		}

		public virtual void Open ()
		{
			System.Console.WriteLine ("Warning: Open method not implemented for '{0}'", this.GetType ());
		}

#if ENABLE_OPEN_WITH
		private void OpenWith (Gnome.Vfs.MimeApplication mime_application)
		{
			GLib.List uri_list = new GLib.List (typeof (string));
			uri_list.Append (Hit.EscapedUri);
			mime_application.Launch (uri_list);
		}
#endif

		protected void OpenFromMime (Hit hit)
		{
			string command = null, item;
			bool expects_uris = false;

			string mimetype = hit.MimeType;

			// FIXME: This is evil.  Nautilus should be handling
			// inode/directory, not just x-directory/normal
			if (mimetype == "inode/directory")
				mimetype = "x-directory/normal";

#if ENABLE_DESKTOP_LAUNCH
			command = "desktop-launch";
			expects_uris = true;
#elif ENABLE_XDG_OPEN
			command = "xdg-open";
			expects_uris = true;
#else
			GnomeFu.VFSMimeApplication app;
			app = GnomeFu.GetDefaultAction (mimetype);
			if (app.command != null) {
				command = app.command;
				expects_uris = (app.expects_uris != GnomeFu.VFSMimeApplicationArgumentType.Path);
			}
#endif			
			if (command == null) {
				Console.WriteLine ("Can't open MimeType '{0}'", mimetype);
				return;
			}
			
			if (expects_uris) {
				// FIXME: I'm not sure that opening the parent
				// URI (if present) is the right thing to do in
				// all cases, but it does work for all our
				// current cases.
				if (hit.ParentUri != null)
					item = hit.EscapedParentUri;
				else
					item = hit.EscapedUri;
			} else
				item = hit.Path;

			// Sometimes the command is 'quoted'
			if (command.IndexOf ('\'') == 0 && command.LastIndexOf ('\'') == command.Length - 1)
				command = command.Trim ('\'');

			// This won't work if a program really has a space in
			// the command filename, but I think other things would
			// break with that too, and in practice it doesn't seem to
			// happen.
			//
			// A bigger issue is that the arguments are split up by
			// spaces, so quotation marks used to indicate a single
			// entry in the argv won't work.  This probably should
			// be fixed.
			string[] arguments = null;
			int idx = command.IndexOf (' ');
			if (idx != -1) {
				arguments = command.Substring (idx + 1).Split (' ');
				command = command.Substring (0, idx);
			}

			string[] argv;
			if (arguments == null)
				argv = new string [] { command, item };
			else {
				argv = new string [arguments.Length + 2];
				argv [0] = command;
				argv [argv.Length - 1] = item;
				Array.Copy (arguments, 0, argv, 1, arguments.Length);
			}

			Console.WriteLine ("Cmd: {0}", command);
			Console.WriteLine ("Arg: {0}", String.Join (" ", argv, 1, argv.Length - 2));
			Console.WriteLine ("Itm: {0}", item);

			SafeProcess p = new SafeProcess ();
			p.Arguments = argv;

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Error in OpenFromMime: " + e);
			}
		}

		public void OpenFromUri (Uri uri)
		{
			OpenFromUri (UriFu.UriToEscapedString (uri));
		}

		public void OpenFromUri (string uri)
                {
#if ENABLE_DESKTOP_LAUNCH || ENABLE_XDG_OPEN
			SafeProcess p = new SafeProcess ();

#  if ENABLE_DESKTOP_LAUNCH
			p.Arguments = new string[] { "desktop-launch", uri };
#  elif ENABLE_XDG_OPEN
			p.Arguments = new string[] { "xdg-open", uri };
#  endif

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Could not load handler for {0}: {1}", uri, e);
			}
#else			
			try {
				Gnome.Url.Show (uri);
			} catch (Exception e) {
				Console.WriteLine ("Could not load handler for {0}: {1}", uri, e);
			}
#endif
		}

		///////////////////////////////////////////////////////

		public Beagle.Hit Hit {
			get { return hit; }
		}

		public Beagle.Query Query {
			get { return query; }
		}

		public TileGroup Group {
			get { return group; }
			protected set { group = value; }
		}

		protected Gtk.HBox HBox { 
			get { return hbox; }
		}

		public Gtk.Image Icon {
			get { return icon; }
			set { icon = value; }
		}

		public virtual string Title {
			get { return title; }
			set { title = value; }
		}

		public virtual DateTime Timestamp {
			get { return timestamp; }
			set { timestamp = value; }
		}

		public virtual double Score {
			get { return score; }
			set { score = value; }
		}

		public IList<TileAction> Actions {
			get { return actions; }
		}
	}
}
