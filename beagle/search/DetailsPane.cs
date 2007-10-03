using System;

using Gtk;

namespace Search {

	public class DetailsPane : Gtk.VBox {

		private const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		private const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		private Gtk.Table table = null;
		private Gtk.Image icon = null;
		private Gtk.Label snippet = null;

		private uint current_row = 0;

		public DetailsPane () : base (false, 5)
		{
			this.BorderWidth = 5;

			Gtk.HBox hbox = new Gtk.HBox (false, 5);

			icon = new Gtk.Image ();
			icon.SetAlignment (0.5f, 0.0f);
			hbox.PackStart (icon, false, true, 0);

			table = new Gtk.Table (1, 2, false);
			table.RowSpacing = 5;
			table.ColumnSpacing = 5;
			hbox.PackStart (table, true, true, 0);

			hbox.ShowAll ();

			PackStart (hbox, true, true, 0);
		}

		public Gtk.Label AddTitleLabel (string text)
		{
			Gtk.Label label = WidgetFu.NewBoldLabel (text);
			label.SetAlignment (0.0f, 0.0f);
			label.Show ();
			WidgetFu.EllipsizeLabel (label);
			table.Attach (label, 0, 2, current_row, current_row + 1, expand, fill, 0, 0);

			current_row++;

			return label;
		}

		public Gtk.Label AddTextLabel (string text)
		{
			Gtk.Label label = WidgetFu.NewLabel (text);
			label.Selectable = true;
			label.SetAlignment (0.0f, 0.0f);
			label.Show ();
			table.Attach (label, 0, 2, current_row, current_row + 1, expand, fill, 0, 0);

			current_row++;

			return label;
		}

		public void AddLabelPair (string header, string text)
		{
			Gtk.Label h = WidgetFu.NewGrayLabel (header);
			h.SetAlignment (1.0f, 0.0f);
			h.Show ();
			table.Attach (h, 0, 1, current_row, current_row + 1, fill, fill, 0, 0);

			Gtk.Label t = WidgetFu.NewLabel (text);
			t.Selectable = true;
			t.SetAlignment (0.0f, 0.0f);
			t.Show ();
			table.Attach (t, 1, 2, current_row, current_row + 1, expand, fill, 0, 0);

			current_row++;
		}

		public void AddNewLine ()
		{
			Gtk.Label label = WidgetFu.NewLabel ("");
			label.Show ();
			table.Attach (label, 0, 2, current_row, current_row + 1, fill, fill, 0, 0);

			current_row++;
		}

		public Gtk.Label AddSnippet ()
		{			
			snippet = WidgetFu.NewLabel ();
			snippet.SetAlignment (0.0f, 0.0f);
			snippet.Selectable = true;
			WidgetFu.EllipsizeLabel (snippet);
			snippet.Show ();
			PackStart (snippet, false, true, 5);

			AddTags ("test beagle lukas technology 2007");

			return snippet;
		}

		private Gtk.Widget AddTags (string tags)
		{
			Gtk.HBox hbox = new HBox (false, 5);

			Gtk.Image icon = new Gtk.Image ("gtk-add", IconSize.Menu);
			hbox.PackStart (icon, false, true, 0);
			
			Gtk.Label label = WidgetFu.NewLabel (tags);
			label.Selectable = true;
			label.SetAlignment (0.0f, 0.0f);
			label.Show ();
			WidgetFu.EllipsizeLabel (label);
			hbox.PackStart (label, true, true, 0);

			hbox.ShowAll ();

			PackStart (hbox, false, true, 0);

			return hbox;
		}

		public void GotSnippet (string text)
		{
			snippet.Markup = text;
			snippet.Show ();
		}

		public Gtk.Image Icon {
			get { return icon; }
		}

		public Gtk.Label Snippet {
			get { return snippet; }
		}
	}
}
