//
// HitView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Diagnostics;

using Gdk;
using Gtk;
using GtkSharp;

using Dewey;

namespace Best {

	public class HitView : Gtk.VBox {

		Hit hit;
		Box textBox;

		public HitView (Hit _hit)
		{
			hit = _hit;

			HBox hbox = new HBox (false, 0);
			
			Button iconButton = new Button ();
			iconButton.Add (BuildIcon ());
			iconButton.Clicked += new EventHandler (this.DoClicked);

			textBox = new VBox (false, 3);
			BuildText ();

			hbox.PackStart (iconButton, false, true, 3);
			hbox.PackStart (textBox, true, true, 3);

			PackStart (hbox, true, true, 3);
			hbox.ShowAll ();
		}

		private void DoClicked (object o, EventArgs args)
		{
			Launch ();
		}

		private void Launch ()
		{
			Console.WriteLine ("Loading {0}", hit.Uri);
			String command = null, arguments = null;

			if (hit.Uri.StartsWith ("file://")) {
				Dewey.Util.GnomeVFSMimeApplication app;
				app = Dewey.Util.GnomeIconLookup.GetDefaultAction (hit.MimeType);
				command = app.command;
				arguments = hit.Uri.Substring (7);
			} else if (hit.Uri.StartsWith ("http://")) {
				command = "epiphany";
				arguments = hit.Uri;
			} else if (hit.Uri.StartsWith ("email://")) {
				command = "evolution-1.5";
				arguments = hit.Uri;
			}
			
			if (command != null && arguments != null) {
				ProcessStartInfo psi = new ProcessStartInfo ();
				psi.FileName = command;
				psi.Arguments = arguments;
				Process.Start (psi);
			} else {
				Console.WriteLine ("Can't launch {0} (mime type: {1})", hit.Uri, hit.MimeType);
			}
		}

		private Widget BuildIcon ()
		{
			String iconPath = Dewey.Util.GnomeIconLookup.LookupMimeIcon (hit.MimeType, (Gtk.IconSize) 48);
			return new Gtk.Image (iconPath);
		}

		private void AddText (String text)
		{
			if (text == null || text == "")
				return;
			Label label = new Label (text);
			label.Xalign = 0;
			label.UseUnderline = false;
			textBox.PackStart (label, false, true, 1);
		}

		private void AddMarkup (String markup)
		{
			if (markup == null || markup == "")
				return;
			Label label = new Label ("");
			label.Xalign = 0;
			label.UseUnderline = false;
			label.Markup = markup;
			textBox.PackStart (label, false, true, 1);
		}

		private void BuildText ()
		{
			if (hit.Uri.StartsWith ("file:///")) {
				String path = hit.Uri.Substring (7);
				String dir = System.IO.Path.GetDirectoryName (path);
				String name = System.IO.Path.GetFileName (path);

				String home = Environment.GetEnvironmentVariable ("HOME");
				if (dir.StartsWith (home))
					dir = "~" + dir.Substring (home.Length);

				AddMarkup ("<i>" + dir + "</i>");
				AddMarkup (name);
				AddMarkup ("<small>Last modified " + hit.Timestamp.ToString ("g") + "</small>");
				return;
			}

			if (hit.Type == "WebLink") {
				AddText (hit.Uri);
				if (hit["title"] != null)
					AddMarkup ("<i>" + hit["title"] + "</i>");
				if (hit.ValidTimestamp)
					AddMarkup ("<small>Cached " + hit.Timestamp.ToString ("g") + "</small>");
				return;
			}

			if (hit.Type == "MailMessage") {
				AddText (hit ["Subject"]);
				AddText ("From: " + hit ["From"]);
				AddMarkup ("<small>" + hit.Timestamp + "</small>");
				return;
			}

			if (hit.Type == "Contact") {
				AddMarkup ("<b>" + hit ["Name"] + "</b>");
				if (hit ["Nickname"] != null)
					AddMarkup ("<i>\"" + hit ["Nickname"] + "</i>");
				AddText (hit ["Email1"]);
				AddText (hit ["Email2"]);
				AddText (hit ["Email3"]);
				AddText (hit ["HomePhone"]);
				AddText (hit ["MobilePhone"]);
			}

		}
	}
}
