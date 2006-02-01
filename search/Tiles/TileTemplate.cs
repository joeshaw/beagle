using System;

using Gtk;

namespace Search.Tiles {

	public abstract class TileTemplate : Tile {

		private Gtk.Label title_label;
		private Gtk.Label desc_label;
		private Gtk.Label snippet_label;
		
		public TileTemplate (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Alignment alignment = new Alignment (0.0f, 0.5f, 1.0f, 0.0f);
			HBox.PackStart (alignment, true, true, 0);

			VBox vbox = new VBox (false, 0);
			alignment.Add (vbox);

			title_label = WidgetFu.NewLabel ();
			title_label.LineWrap = true;
			WidgetFu.EllipsizeLabel (title_label, 30);
			vbox.PackStart (title_label, false, false, 0);

			desc_label = WidgetFu.NewGrayLabel ();
			desc_label.NoShowAll = true;
			WidgetFu.EllipsizeLabel (desc_label, 30);
			vbox.PackStart (desc_label, false, false, 0);

			snippet_label = WidgetFu.NewGrayLabel ();
			snippet_label.NoShowAll = true;
			WidgetFu.EllipsizeLabel (snippet_label, 30);
			vbox.PackStart (snippet_label, false, false, 0);

			alignment.ShowAll ();

			// FIXME: We need a constant icon size
			//IconSize = Gtk.IconSize.Dnd;

			// FIXME: Disabled for now. Needs UI touchups
			//RequestSnippet ();
		}

		public override string Title {
			get { return base.Title; }
			set {
				base.Title = value;
				title_label.Markup = "<span weight=\"bold\">" + GLib.Markup.EscapeText (value) + "</span>";
			}
		}

		private string description;
		public string Description {
			get { return description; }
			set {
				description = value;

				if (description != null) {
					desc_label.Markup = "<small>" + GLib.Markup.EscapeText (description) + "</small>";
					desc_label.Show ();
				} else {
					desc_label.Hide ();
				}
			}
		}

		protected override void GotSnippet (string snippet, bool found)
		{
			if (snippet == null || snippet == "")
				return;

			snippet_label.Markup = "<small>" + snippet + "</small>";
			snippet_label.Show ();
		}
	}
}
