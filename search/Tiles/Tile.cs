using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Mono.Posix;

using Gtk;
using Beagle;
using Beagle.Util;

namespace Search.Tiles {

	public abstract class Tile : Gtk.EventBox {

		public Tile (Hit hit, Query query) : base ()
		{
			AboveChild = true;
			CanFocus = true;
			ModifyBg (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));

			this.hit = hit;
			this.query = query;
			this.group = TileGroup.Documents;

			Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask,
					    targets, Gdk.DragAction.Copy | Gdk.DragAction.Move);

			hbox = new Gtk.HBox (false, 5);
			hbox.BorderWidth = 2;
			hbox.Show ();
			Add (hbox);

			image = new Gtk.Image ();
			hbox.PackStart (image, false, false, 0);
			image.NoShowAll = true;
		}

		private Beagle.Hit hit;
		public Beagle.Hit Hit {
			get { return hit; }
		}

		private Beagle.Query query;
		public Beagle.Query Query {
			get { return query; }
		}

		protected TileGroup group;
		public TileGroup Group {
			get { return group; }
			set { group = value; }
		}

		private Gtk.HBox hbox;
		protected Gtk.HBox HBox { 
			get { return hbox; }
		}

		private Gtk.Image image;
		public Gdk.Pixbuf Icon {
			get { return image.Pixbuf; }
			set {
				image.Pixbuf = value;
				UpdateIcon ();
			}
		}

		static Gtk.TargetEntry[] targets = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, 0)
		};

		public event EventHandler Selected, Deselected;

		private void UpdateIcon ()
		{
			if (image.Pixbuf != null) {
				image.Show ();
			} else {
				image.Hide ();
			}
		}

		protected override void OnDragBegin (Gdk.DragContext context)
		{
			if (!image.Visible)
				return;

			WidgetFu.SetDragImage (context, image);
		}

		protected override void OnDragDataGet (Gdk.DragContext dragContext,
						       Gtk.SelectionData selectionData,
						       uint info, uint time)
		{
			byte[] data = System.Text.Encoding.UTF8.GetBytes (Hit.Uri + "\r\n");
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
			if (base.OnExposeEvent (evt))
				return true;

			if (IsDrawable && HasFocus) {
				Gdk.Rectangle alloc = Allocation;
				int focusPad = (int)StyleGetProperty ("focus-padding");

				int x = focusPad + Style.Xthickness;
				int y = focusPad + Style.Ythickness;
				int width = alloc.Width - 2 * (focusPad + Style.Xthickness);
				int height = alloc.Height - 2 * (focusPad + Style.Ythickness);
				Style.PaintFocus (Style, GdkWindow, State, evt.Area, this,
						  "button", x, y, width, height);
			}

			return false;
		}

		private void ShowPopupMenu ()
		{
			Gtk.Menu menu = new Gtk.Menu ();

			ActionMenuItem mi = new ActionMenuItem (new TileAction (Catalog.GetString ("Open"), Stock.Open, Open));
			menu.Append (mi);

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

		protected override bool OnFocusOutEvent (Gdk.EventFocus f)
		{
			if (Deselected != null && !IsFocus)
				Deselected (this, EventArgs.Empty);
			return base.OnFocusOutEvent (f);
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

		protected void RequestSnippet ()
		{
			SnippetRequest sreq = new SnippetRequest (query, hit);
			sreq.RegisterAsyncResponseHandler (typeof (SnippetResponse), SnippetResponseReceived);
			sreq.SendAsync ();
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
			string snippet = GLib.Markup.EscapeText (((SnippetResponse)response).Snippet);
			snippet = Regex.Replace (snippet, "&lt;font color=&quot;.*?&quot;&gt;&lt;b&gt;(.*?)&lt;/b&gt;&lt;/font&gt;", "<b>$1</b>");
			GotSnippet (snippet, true);
		}

		protected virtual void GotSnippet (string snippet, bool found)
		{
		}

		protected virtual Gtk.Widget GetDetails ()
		{
			return null;
		}

		public Gtk.Widget Details {
			get { return GetDetails (); }
		}

		public ArrayList actions = new ArrayList ();
		public ICollection Actions {
			get { return actions; }
		}

		protected void AddAction (TileAction action)
		{
			actions.Add (action);
		}

		public virtual void Open ()
		{
			System.Console.WriteLine ("Warning: Open method not implemented for this tile type");
		}

		protected void OpenFromMime (Hit hit)
		{
			OpenFromMime (hit, null, null, false);
		}

		protected void OpenFromMime (Hit hit, string command_fallback,
					     string args_fallback, bool expects_uris_fallback)
		{
			string argument;
			string command = command_fallback;
			bool expects_uris = expects_uris_fallback;

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

			if (args_fallback != null)
				argument = args_fallback;
			else 
				argument = "";			

			if (expects_uris) {
				argument = String.Format ("{0} '{1}'", argument, hit.Uri);
			} else {
				argument = String.Format ("{0} {1}", argument, hit.PathQuoted);
			}

			// Sometimes the command is 'quoted'
			if (command.IndexOf ('\'') == 0 && command.LastIndexOf ('\'') == command.Length - 1)
				command = command.Trim ('\'');

			// This won't work if a program really has a space in
			// the filename, but I think other things would break
			// with that too, and in practice it doesn't seem to
			// happen.
			int idx = command.IndexOf (' ');
			if (idx != -1) {
				argument = String.Format ("{0} {1}", command.Substring (idx + 1), argument);
				command = command.Substring (0, idx);
			}

			Console.WriteLine ("Cmd: {0}", command);
			Console.WriteLine ("Arg: {0}", argument);

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = command;
			p.StartInfo.Arguments = argument;

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Error in OpenFromMime: " + e);
			}
		}

		public void OpenFromUri (Uri uri)
		{
			OpenFromUri (uri.ToString ());
		}

		public void OpenFromUri (string uri)
                {
#if ENABLE_DESKTOP_LAUNCH
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "desktop-launch";
			p.StartInfo.Arguments = uri;

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
