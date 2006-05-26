using System;

namespace Search.Tiles {

	public class DetailsPane : Gtk.Table {

		public DetailsPane () : base (1, 2, false)
		{
			RowSpacing = ColumnSpacing = 6;
			BorderWidth = 6;

			icon = new Gtk.Image ();
			icon.Show ();
			Attach (icon, 0, 1, 0, 1, fill, fill, 0, 0);

			SizeRequested += DetailsSizeRequested;
		}

		private Gtk.Image icon;
		public Gtk.Image Icon {
			get { return icon; }
		}

		private Gtk.Label snippet;
		public Gtk.Label Snippet {
			get { return snippet; }
		}

		bool maximized = false;

		// FIXME: overriding OnSizeRequested directly results in a 0x0 req
		[GLib.ConnectBefore]
		private void DetailsSizeRequested (object obj, Gtk.SizeRequestedArgs args)
		{
			if (maximized)
				return;

			Gtk.Table.TableChild[,] children = new Gtk.Table.TableChild[NColumns, NRows];

			foreach (Gtk.Widget child in Children) {
				Gtk.Table.TableChild tc = this[child] as Gtk.Table.TableChild;
				children[tc.LeftAttach, tc.TopAttach] = tc;
			}

			// Expand the icon down to the bottom or the first label
			if (children[0, 0] != null && children[0, 0].Child == icon) {
				uint max_icon_row;
				for (max_icon_row = 1; max_icon_row < NRows; max_icon_row++) {
					if (children[0, max_icon_row] != null)
						break;
				}

				children[0, 0].BottomAttach = max_icon_row;
			}

			// Expand all labels (except in column 0) rightward
			for (uint row = 0; row < NRows; row++) {
				for (uint col = 1; col < NColumns; col++) {
					if (children[col, row] == null ||
					    !(children[col, row].Child is Gtk.Label))
						continue;
					uint end = col + 1;
					while (end < NColumns &&
					       children[end, row] == null)
						end++;
					if (end > col + 1)
						children[col, row].RightAttach = end;
				}
			}

			// Vertically expand only the bottom row
			for (uint row = 0; row < NRows; row++) {
				for (uint col = 1; col < NColumns; col++) {
					if (children[col, row] == null)
						continue;
					children[col, row].YOptions = (row == NRows - 1) ? expand : fill;
				}
			}

			maximized = true;
		}

		const Gtk.AttachOptions expand = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
		const Gtk.AttachOptions fill = Gtk.AttachOptions.Fill;

		public Gtk.Label AddGrayLabel (string text, uint row, uint column)
		{
			Gtk.Label label = WidgetFu.NewGrayLabel (text);
			label.Show ();
			Attach (label, column, column + 1, row, row + 1, fill, fill, 0, 0);
			maximized = false;
			return label;
		}

		public Gtk.Label AddLabel (string text, uint row, uint column)
		{
			Gtk.Label label = WidgetFu.NewLabel (text);
			label.Selectable = true;
			WidgetFu.EllipsizeLabel (label);
			label.Show ();
			Attach (label, column, column + 1, row, row + 1, expand, fill, 0, 0);
			maximized = false;
			return label;
		}

		public Gtk.Label AddBoldLabel (string text, uint row, uint column)
		{
			Gtk.Label label = WidgetFu.NewBoldLabel (text);
			WidgetFu.EllipsizeLabel (label);
			label.Show ();
			Attach (label, column, column + 1, row, row + 1, expand, fill, 0, 0);
			maximized = false;
			return label;
		}

		public Gtk.Label AddTitleLabel (string text, uint row, uint column)
		{
			Gtk.Label label = AddBoldLabel (text, row, column);
			label.Selectable = true;
			return label;
		}

		public void AddLabelPair (string label, string text, uint row, uint column)
		{
			AddGrayLabel (label, row, column);
			AddLabel (text, row, column + 1);
		}

		public Gtk.Label AddSnippet (uint row, uint column)
		{
			snippet = WidgetFu.NewLabel ();
			snippet.Selectable = true;
			WidgetFu.EllipsizeLabel (snippet);
			snippet.Show ();
			Attach (snippet, column, column + 1, row, row + 1, expand, fill, 48, 0);
			maximized = false;

			return snippet;
		}

		public Gtk.Label AddNewLine (uint row, uint column)
		{
			Gtk.Label label = WidgetFu.NewLabel ("");
			label.Show ();
			Attach (label, column, column + 1, row, row + 1, fill, fill, 0, 0);
			return label;
		}

		public Gtk.Label AddFinalLine (uint row, uint column)
		{
			Gtk.Label label = WidgetFu.NewLabel ("");
			label.Show ();
			Attach (label, column, column + 1, row, row + 1, expand, expand, 0, 0);
			return label;
		}

		public void GotSnippet (string text)
		{
			snippet.Markup = text;
		}
	}
}
