using System;
using System.Runtime.InteropServices;

namespace Search {

	public class Entry : Gtk.Entry {

		public Entry (IntPtr raw) : base (raw) {}

		[DllImport("libbeagleuiglue.so")]
		static extern IntPtr search_entry_new ();

		public Entry () : base (IntPtr.Zero)
		{
			if (GetType () != typeof (Entry)) {
				CreateNativeObject (new string [0], new GLib.Value[0]);
				return;
			}
			Raw = search_entry_new ();
		}

		[DllImport("libbeagleuiglue.so")]
		static extern IntPtr search_entry_get_type();

		public static new GLib.GType GType { 
			get {
				return new GLib.GType (search_entry_get_type ());
			}
		}
	}
}
