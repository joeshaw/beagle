//
// Author: 
//   Mikael Hallendal <micke@imendio.com>
//
// (C) 2004 Imendio HB
// 

using System;
using System.Runtime.InteropServices;
using Gtk;
using Pango;

using GLib;

namespace Beagle.Util {

    public delegate bool ButtonPressHandler (IntPtr evnt);
    public delegate bool KeyPressHandler    (IntPtr evnt);
    public delegate int  SortFunc           (IntPtr a, IntPtr b);
    public delegate void DragDataHandler    (IntPtr context, 
					     IntPtr data, 
					     string text,
					     uint   time);
        

	public class GeckoUtils {
		
        [DllImport("libgeckoglue.so")]
            static extern void blam_gecko_utils_init_services ();
        public static void Init ()
        {
            blam_gecko_utils_init_services ();
        }

        [DllImport("libgeckoglue.so")]
            static extern void blam_gecko_utils_set_font (int type, string font);
        public static void SetFont (int type, string font)
        {
            blam_gecko_utils_set_font (type, font);
        }

        [DllImport("libgeckoglue.so")]
            static extern void blam_gecko_utils_set_proxy (bool use_proxy, string host, int port);

        public static void SetProxy (bool useProxy, string host, int port)
        {
            blam_gecko_utils_set_proxy (useProxy, host, port);
        }
    }
}

	
