using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Mono.Unix;

using Gtk;
using Beagle;
using Beagle.Util;

namespace Search.Tiles {

	public abstract class Tile : Gtk.EventBox {

		public Tile (Hit hit, Query query) : base ()
		{
			AboveChild = true;
			AppPaintable = true;
			CanFocus = true;

			this.hit = hit;
			this.timestamp = hit.Timestamp;
			this.score = hit.Score;
			this.query = query;
			this.group = TileGroup.Documents;

			Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask,
					    targets, Gdk.DragAction.Copy | Gdk.DragAction.Move);

			hbox = new Gtk.HBox (false, 5);
			hbox.BorderWidth = 2;
			hbox.Show ();

			icon = new Gtk.Image ();
			icon.Show ();
			HBox.PackStart (icon, false, false, 0);

			Add (hbox);
		}

		private Beagle.Hit hit;
		public Beagle.Hit Hit {
			get { return hit; }
		}

		private Beagle.Query query;
		public Beagle.Query Query {
			get { return query; }
		}

		private TileGroup group;
		public TileGroup Group {
			get { return group; }
			set { group = value; }
		}

		private Gtk.HBox hbox;
		protected Gtk.HBox HBox { 
			get { return hbox; }
		}

		private Gtk.Image icon;
		public Gtk.Image Icon {
			get { return icon; }
			set { icon = value; }
		}

		private string title;
		public virtual string Title {
			get { return title; }
			set { title = value; }
		}

		private DateTime timestamp;
		public virtual DateTime Timestamp {
			get { return timestamp; }
			set { timestamp = value; }
		}

		private double score;
		public virtual double Score {
			get { return score; }
			set { score = value; }
		}

		protected bool EnableOpenWith = false;

		static Gtk.TargetEntry[] targets = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, 0)
		};

		public event EventHandler Selected;

		protected override void OnDragBegin (Gdk.DragContext context)
		{
			if (!icon.Visible)
				return;

			WidgetFu.SetDragImage (context, icon);
		}

		protected override void OnDragDataGet (Gdk.DragContext dragContext,
						       Gtk.SelectionData selectionData,
						       uint info, uint time)
		{
			byte[] data = System.Text.Encoding.UTF8.GetBytes (Hit.EscapedUri + "\r\n");
			selectionData.Set (selectionData.Target, 8, data);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			// FIXME: "base.OnSizeRequested (ref req)" should work,
			// but it doesn't
			req = hbox.SizeRequest ();

			int pad = (int)StyleGetProperty ("focus-line-width") +
				(int)StyleGetProperty ("focus-padding") + 1;
			req.Width += 2 * (pad + Style.Xthickness);
			req.Height += 2 * (pad + Style.Ythickness);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle alloc)
		{
			int pad = (int)StyleGetProperty ("focus-line-width") +
				(int)StyleGetProperty ("focus-padding") + 1;

			alloc.X += pad + Style.Xthickness;
			alloc.Width -= pad + Style.Xthickness;
			alloc.Y += pad + Style.Ythickness;
			alloc.Height -= pad + Style.Ythickness;

			base.OnSizeAllocated (alloc);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			if (!IsDrawable)
				return false;

			GdkWindow.DrawRectangle (Style.BaseGC (State), true,
						 evt.Area.X, evt.Area.Y,
						 evt.Area.Width, evt.Area.Height);

			if (base.OnExposeEvent (evt))
				return true;

			if (HasFocus) {
				Gdk.Rectangle alloc = Allocation;
				int focusPad = (int)StyleGetProperty ("focus-padding");

				int x = focusPad + Style.Xthickness;
				int y = focusPad + Style.Ythickness;
				int width = alloc.Width - 2 * (focusPad + Style.Xthickness);
				int height = alloc.Height - 2 * (focusPad + Style.Ythickness);
				Style.PaintFocus (Style, GdkWindow, State, evt.Area, this,
						  null, x, y, width, height);
			}

			return false;
		}

		///////////////////////////////////////////////////

		public ArrayList actions = new ArrayList ();
		public ICollection Actions {
			get { return actions; }
		}

		protected void AddAction (TileAction action)
		{
			actions.Add (action);
		}

		private void ShowPopupMenu ()
		{
			Gtk.Menu menu = new Gtk.Menu ();

			ActionMenuItem mi = new ActionMenuItem (new TileAction (Catalog.GetString ("Open"), Stock.Open, Open));
			menu.Append (mi);

#if ENABLE_OPEN_WITH
			if (EnableOpenWith) {
				// FIXME: Not sure if going with the parent is
				// the right thing to do in all cases.
				OpenWithMenu owm = new OpenWithMenu (Utils.GetFirstPropertyOfParent (hit, "beagle:MimeType"));
				owm.ApplicationActivated += OpenWith;
				owm.AppendToMenu (menu);
			}
#endif

			if (Actions.Count > 0) {
				SeparatorMenuItem si = new SeparatorMenuItem ();
				menu.Append (si);

				foreach (TileAction action in Actions) {
					mi = new ActionMenuItem (action);
					menu.Append (mi);
				}
			}

			menu.ShowAll ();
			menu.Popup ();
		}

		///////////////////////////////////////////////////

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

		string snippet;

		protected void RequestSnippet ()
		{
			if (snippet != null)
				EmitGotSnippet ();
			else {
				SnippetRequest sreq = new SnippetRequest (query, hit);
				sreq.RegisterAsyncResponseHandler (typeof (SnippetResponse), SnippetResponseReceived);
				sreq.SendAsync ();
			}
		}

		private void SnippetResponseReceived (ResponseMessage response)
		{
			// The returned snippet uses
			// <font color="..."><b>blah</b></font>
			// to mark matches. The rest of the snippet might be HTML, or
			// it might be plain text, including unescaped '<'s and '&'s.
			// So we escape it, fix the match highlighting, and leave any
			// other tags escaped.

			// FIXME: hacky, fix the snippeting in the daemon
			snippet = GLib.Markup.EscapeText (((SnippetResponse)response).Snippet);
			snippet = Regex.Replace (snippet, "&lt;font color=&quot;.*?&quot;&gt;&lt;b&gt;(.*?)&lt;/b&gt;&lt;/font&gt;", "<b>$1</b>");

			EmitGotSnippet ();
		}

		private void EmitGotSnippet ()
		{
			if (snippet != null && snippet != "" && GotSnippet != null)
				GotSnippet (snippet);
		}

		public delegate void GotSnippetHandler (string snippet);
		public event GotSnippetHandler GotSnippet;

		protected virtual DetailsPane GetDetails ()
		{
			return null;
		}

		DetailsPane details;
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
			System.Console.WriteLine ("Warning: Open method not implemented for this tile type");
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

			// FIXME: This is evil.  Nautilus should be handling
			// inode/directory, not just x-directory/normal
			if (hit.MimeType == "inode/directory")
				hit.MimeType = "x-directory/normal";

#if ENABLE_DESKTOP_LAUNCH
			command = "desktop-launch";
			expects_uris = true;
#else		       
			GnomeFu.VFSMimeApplication app;
			app = GnomeFu.GetDefaultAction (hit.MimeType);
			if (app.command != null) {
				command = app.command;
				expects_uris = (app.expects_uris != GnomeFu.VFSMimeApplicationArgumentType.Path);
			}
#endif			
			if (command == null) {
				Console.WriteLine ("Can't open MimeType '{0}'", hit.MimeType);
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
#if ENABLE_DESKTOP_LAUNCH
			SafeProcess p = new SafeProcess ();
			p.Arguments = new string[] { "desktop-launch", uri };

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
	}
}
