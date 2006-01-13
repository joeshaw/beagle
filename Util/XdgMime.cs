using System;
using System.Runtime.InteropServices;

namespace Beagle.Util {
	public class XdgMime {

		[DllImport ("libbeagleglue")]
		static extern IntPtr xdg_mime_get_mime_type_for_file (string file_path, IntPtr optional_stat_info);

		public static string GetMimeType (string file_path)
		{
			return Marshal.PtrToStringAnsi (xdg_mime_get_mime_type_for_file (file_path, (IntPtr)null));
		}
	}
}
