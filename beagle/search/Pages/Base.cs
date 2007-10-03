using System;

using Gtk;
using Mono.Unix;

namespace Search.Pages {

	public class Base : EventBox {

		private static Gdk.Pixbuf arrow = Beagle.Images.GetPixbuf ("tip-arrow.png");

		private Gtk.Fixed fixed_widget = null;	
		private Gtk.VBox vbox = null;
		private Gtk.Table table = null;
		private Gtk.Image header_icon = null;
		private Gtk.Label header_label = null;

		public Base ()
		{
			fixed_widget = new Fixed ();
			fixed_widget.HasWindow = true;
			Add (fixed_widget);

			HBox header = new HBox (false, 5);
			
			header_icon = new Gtk.Image ();
			header_icon.Yalign = 0.0f;
			header.PackStart (header_icon, false, true, 0);

			header_label = new Gtk.Label ();
			header_label.SetAlignment (0.0f, 0.5f);
			header.PackStart (header_label, true, true, 0);

			table = new Gtk.Table (1, 2, false);
			table.RowSpacing = table.ColumnSpacing = 12;
			
			vbox = new VBox (false, 5);
			vbox.PackStart (header, false, true, 0);
			vbox.PackStart (table, false, true, 0);

			fixed_widget.Add (vbox);
			fixed_widget.ShowAll ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			ModifyBg (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
		}

		public void Append (string tip)
		{
			uint row = table.NRows;

			Gtk.Image image = new Gtk.Image (arrow);
			image.Yalign = 0.0f;
			image.Xalign = 1.0f;
			image.Show ();
			table.Attach (image, 0, 1, row, row + 1, Gtk.AttachOptions.Fill, Gtk.AttachOptions.Fill, 0, 0);

			Gtk.Label label = new Gtk.Label ();
			label.Markup = tip;
			label.SetAlignment (0.0f, 0.5f);
			label.LineWrap = true;
			label.ModifyFg (Gtk.StateType.Normal, label.Style.Foreground (Gtk.StateType.Insensitive));
			label.Show ();
			table.Attach (label, 1, 2, row, row + 1, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, 0, 0);

		}

		public void Append (Gtk.Widget widget)
		{
			uint row = table.NRows;
			table.Attach (widget, 1, 2, row, row + 1, 0, 0, 0, 0);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			req = vbox.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			Gtk.Requisition req = vbox.ChildRequisition;
			allocation.X = Math.Max ((allocation.Width - req.Width) / 2, 0);
			allocation.Y = Math.Max ((allocation.Height - req.Height) / 2, 0);
			allocation.Width = req.Width;
			allocation.Height = req.Height;
			vbox.SizeAllocate (allocation);
		}

		public Gdk.Pixbuf HeaderIcon {
			set { header_icon.Pixbuf = value; }
		}

		public string HeaderIconStock {
			set { header_icon.SetFromStock (value, Gtk.IconSize.Dnd); }
		}

		public string HeaderMarkup {
			set { header_label.Markup = value; }
		}
	}
}
