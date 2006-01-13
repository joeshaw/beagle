using Gtk;
using Gdk;
using System;
using System.Collections;

namespace Search {

	public class Panes : Gtk.VPaned {

		Gtk.ScrolledWindow main, detailsSW;
		Gtk.Viewport details;

		public Panes ()
		{
			ModifyBg (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));

			main = new Gtk.ScrolledWindow ();
			main.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Always);
			Pack1 (main, true, false);

			detailsSW = new Gtk.ScrolledWindow ();
			detailsSW.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Never);
			detailsSW.WidthRequest = 0;
			detailsSW.NoShowAll = true;
			detailsSW.ShadowType = Gtk.ShadowType.In;
			Pack2 (detailsSW, false, false);

			details = new Gtk.Viewport (null, null);
			detailsSW.Add (details);
			details.BorderWidth = 6;
			details.ShadowType = Gtk.ShadowType.None;
			details.Show ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			base.OnExposeEvent (evt);

			if (detailsSW.Visible) {
				Gdk.Rectangle rect = detailsSW.Allocation;
				rect.Inflate (-1, -1);
				GdkWindow.DrawRectangle (Style.BaseGC (State), true, rect);
			}

			return false;
		}

		public Gtk.Widget MainContents {
			get {
				if (main.Child is Gtk.Viewport)
					return ((Gtk.Viewport)main.Child).Child;
				else
					return main.Child;
			}
			set {
				if (main.Child != null)
					main.Remove (main.Child);
				if (value != null) {
					if (value.SetScrollAdjustments (null, null))
						main.Add (value);
					else {
						main.AddWithViewport (value);
						if (value is Container)
							((Container)value).FocusVadjustment = main.Vadjustment;
					}
				}
			}
		}

		void RecursiveChangeBg (Gtk.Widget widget)
		{
			widget.ModifyBg (Gtk.StateType.Normal, widget.Style.Base (Gtk.StateType.Normal));
			if (widget is Gtk.Container) {
				Gtk.Container container = (Gtk.Container)widget;
				foreach (Gtk.Widget w in container.Children)
					RecursiveChangeBg (w);
			}
		}

		public Gtk.Widget Details {
			get {
				return details.Child;
			}
			set {
				if (details.Child != null)
					details.Remove (details.Child);
				if (value != null) {
					details.Add (value);
					RecursiveChangeBg (details);
					detailsSW.Show ();
				} else
					detailsSW.Hide ();
			}
		}
	}
}
