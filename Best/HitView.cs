//
// HitView.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

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
			Console.WriteLine ("Launched {0}", hit.Uri);
		}

		private Widget BuildIcon ()
		{
			String iconPath = GnomeIconLookup.LookupMimeIcon (hit.MimeType, (Gtk.IconSize) 48);
			Widget icon = new Image (iconPath);
			return icon;
		}

		private void AddMarkup (String markup)
		{
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

			if (hit.Domain == "web") {
				AddMarkup (hit.Uri);
				if (hit["title"] != null)
					AddMarkup ("<i>" + hit["title"] + "</i>");
				if (hit.ValidTimestamp)
					AddMarkup ("<small>Cached " + hit.Timestamp.ToString ("g") + "</small>");
				return;
			}

			AddMarkup (hit.Uri);
		}
	}
}
