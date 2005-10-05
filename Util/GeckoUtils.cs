//
// Author: 
//   Mikael Hallendal <micke@imendio.com>
//
// (C) 2004 Imendio HB
// 

using System;
using System.Runtime.InteropServices;
using Gtk;
using GConf;
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

		private static GConf.Client gconf_client = null;
		private static void EnsureClient ()
		{
			if (gconf_client == null)
				gconf_client = new GConf.Client ();
		}


		private static void SetFonts ()
		{
			string font;

			try {
				font = (string) gconf_client.Get ("/desktop/gnome/interface/font_name");
			} catch (GConf.NoSuchKeyException) {
				font = "sans 12";
			}
			
			GeckoUtils.SetFont (1, font);

			try {
				font = (string) gconf_client.Get ("/desktop/gnome/interface/monospace_font_name");
			} catch (GConf.NoSuchKeyException) {
				font = "monospace 12";
			}

			GeckoUtils.SetFont (2, font);
		}

		public static void FontNotifyHandler (object sender,
						      NotifyEventArgs args)
		{
			if (args.Key == "/desktop/gnome/interface/font_name" ||
			    args.Key == "/desktop/gnome/interface/monospace_font_name")
				SetFonts ();
		}

		public static void SetSystemFonts ()
		{
			EnsureClient ();
			gconf_client = new GConf.Client ();
			gconf_client.AddNotify ("/desktop/gnome/interface",
						new NotifyEventHandler (FontNotifyHandler));			
			SetFonts ();
		}
		
		[DllImport("libbeagleuiglue")]
		static extern void blam_gecko_utils_init_services ();
		public static void Init ()
		{
			blam_gecko_utils_init_services ();
		}
		
		[DllImport("libbeagleuiglue")]
		static extern void blam_gecko_utils_set_font (int type, string font);
		public static void SetFont (int type, string font)
		{
			blam_gecko_utils_set_font (type, font);
		}
		
		[DllImport("libbeagleuiglue")]
		static extern void blam_gecko_utils_set_proxy (bool use_proxy, string host, int port);
		
		public static void SetProxy (bool useProxy, string host, int port)
		{
			blam_gecko_utils_set_proxy (useProxy, host, port);
		}

	}
}


