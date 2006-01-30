using Gtk;
using System;
using Mono.Unix;

namespace Search.Pages {

	public class Base : Fixed {

		static Gdk.Pixbuf arrow;

		static Base ()
		{
			arrow = Beagle.Images.GetPixbuf ("tip-arrow.png");
		}

		Gtk.Table table;
		Gtk.Image headerIcon;
		Gtk.Label header;

		public Base ()
		{
			HasWindow = true;

			table = new Gtk.Table (1, 2, false);
			table.RowSpacing = table.ColumnSpacing = 12;

			headerIcon = new Gtk.Image ();
			headerIcon.Yalign = 0.0f;
			table.Attach (headerIcon, 0, 1, 0, 1,
				      0, Gtk.AttachOptions.Fill,
				      0, 0);

			header = new Gtk.Label ();
			header.SetAlignment (0.0f, 0.0f);
			table.Attach (header, 1, 2, 0, 1,
				      Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill,
				      Gtk.AttachOptions.Fill,
				      0, 0);

			table.ShowAll ();
			Add (table);
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			ModifyBg (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
		}

		public Gdk.Pixbuf HeaderIcon {
			set {
				headerIcon.Pixbuf = value;
			}
		}

		public string HeaderIconStock {
			set {
				headerIcon.SetFromStock (value, Gtk.IconSize.Dnd);
			}
		}

		public string HeaderMarkup {
			set {
				header.Markup = value;
			}
		}

		public void Append (string tip)
		{
			uint row = table.NRows;
			Gtk.Image image;
			Gtk.Label label;

			image = new Gtk.Image (arrow);
			image.Yalign = 0.0f;
			image.Xalign = 1.0f;
			image.Show ();
			table.Attach (image, 0, 1, row, row + 1,
				      Gtk.AttachOptions.Fill,
				      Gtk.AttachOptions.Fill,
				      0, 0);

			label = new Gtk.Label ();
			label.Markup = tip;
			label.SetAlignment (0.0f, 0.5f);
			label.LineWrap = true;
			label.ModifyFg (Gtk.StateType.Normal, label.Style.Foreground (Gtk.StateType.Insensitive));
			label.Show ();
			table.Attach (label, 1, 2, row, row + 1,
				      Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill,
				      0, 0, 0);

		}

		public void Append (Gtk.Widget widget)
		{
			uint row = table.NRows;

			table.Attach (widget, 1, 2, row, row + 1, 0, 0, 0, 0);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			req = table.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			Gtk.Requisition tableReq = table.ChildRequisition;
			allocation.X = Math.Max ((allocation.Width - tableReq.Width) / 2, 0);
			allocation.Y = Math.Max ((allocation.Height - tableReq.Height) / 2, 0);
			allocation.Width = tableReq.Width;
			allocation.Height = tableReq.Height;
			table.SizeAllocate (allocation);
		}
	}
}
