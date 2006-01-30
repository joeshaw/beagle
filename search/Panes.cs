using Gtk;
using Gdk;
using System;
using System.Collections;

namespace Search {

	public class Panes : Gtk.VPaned {

		Gtk.ScrolledWindow mainSW, detailsSW;
		WhiteBox main, details;

		public Panes ()
		{
			Gtk.Viewport vp;

			mainSW = new Gtk.ScrolledWindow ();
			mainSW.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Always);
			mainSW.ShadowType = Gtk.ShadowType.In;
			Pack1 (mainSW, true, false);

			vp = new Gtk.Viewport (null, null);
			vp.ShadowType = ShadowType.None;
			mainSW.Add (vp);
			vp.Show ();

			main = new WhiteBox ();
			vp.Add (main);
			main.Show ();

			detailsSW = new Gtk.ScrolledWindow ();
			detailsSW.SetPolicy (Gtk.PolicyType.Never, Gtk.PolicyType.Never);
			detailsSW.WidthRequest = 0;
			detailsSW.NoShowAll = true;
			detailsSW.ShadowType = Gtk.ShadowType.In;
			Pack2 (detailsSW, false, false);

			vp = new Gtk.Viewport (null, null);
			vp.ShadowType = ShadowType.None;
			detailsSW.Add (vp);
			vp.Show ();

			details = new WhiteBox ();
			details.BorderWidth = 6;
			vp.Add (details);
			details.Show ();
		}

		public Gtk.Widget MainContents {
			get {
				return main.Child;
			}
			set {
				if (main.Child != null)
					main.Remove (main.Child);
				if (value != null) {
					main.Add (value);
					if (value is Container)
						((Container)value).FocusVadjustment = mainSW.Vadjustment;
				}
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
					detailsSW.Show ();
				} else
					detailsSW.Hide ();
			}
		}

		public class WhiteBox : Gtk.EventBox
		{
			public WhiteBox () : base ()
			{
				AppPaintable = true;
			}

			protected override bool OnExposeEvent (Gdk.EventExpose evt)
			{
				if (!IsDrawable)
					return false;

				if (evt.Window == GdkWindow) {
					GdkWindow.DrawRectangle (Style.BaseGC (State), true,
								 evt.Area.X, evt.Area.Y,
								 evt.Area.Width, evt.Area.Height);
				}

				if (Child != null)
					PropagateExpose (Child, evt);

				return false;
			}
		}
	}
}
