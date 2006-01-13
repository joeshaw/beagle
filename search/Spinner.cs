using System;
using System.Runtime.InteropServices;

namespace Search {

	public class Spinner : Gtk.EventBox {

		public Spinner (IntPtr raw) : base (raw) {}

		[DllImport("libbeagleuiglue.so")]
		static extern IntPtr ephy_spinner_new ();

		public Spinner () : base (IntPtr.Zero)
		{
			if (GetType () != typeof (Spinner)) {
				CreateNativeObject (new string [0], new GLib.Value[0]);
				return;
			}
			Raw = ephy_spinner_new ();
		}

		[DllImport("libbeagleuiglue.so")]
		static extern IntPtr ephy_spinner_get_type();

		public static new GLib.GType GType { 
			get {
				return new GLib.GType (ephy_spinner_get_type ());
			}
		}

		[DllImport("libbeagleuiglue.so")]
		static extern void ephy_spinner_start (IntPtr spinner);

		public void Start ()
		{
			ephy_spinner_start (Handle);
		}

		[DllImport("libbeagleuiglue.so")]
		static extern void ephy_spinner_stop (IntPtr spinner);

		public void Stop ()
		{
			ephy_spinner_stop (Handle);
		}

		[DllImport("libbeagleuiglue.so")]
		static extern void ephy_spinner_set_size (IntPtr spinner, IntPtr icon_size);

		public void SetSize (Gtk.IconSize size)
		{
			ephy_spinner_set_size (Handle, (IntPtr)size);
		}
	}
}
