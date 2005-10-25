//
// GnomeFu.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using Gtk;
using GLib;
using System;
using System.Runtime.InteropServices;
using System.IO;

namespace Beagle.Util {

	public class GnomeFu {

		private static Gtk.IconTheme icon_theme = null;

		// gnomevfs-sharp does not provide gnome_vfs_get_file_* bindings
		// these functions require a struct stat which gapi does not know how to
		// handle, and there are currently no plans to add these bindings.
		// Using these methods is apparently faster than gnome_vfs_get_mime_type
		[DllImport ("libgnomevfs-2")] extern static IntPtr gnome_vfs_get_file_mime_type (string filename, IntPtr optional_stat_info, bool suffix_only);
		[DllImport ("libgnomevfs-2")] extern static IntPtr gnome_vfs_get_file_mime_type_fast (string filename, IntPtr optional_stat_info);

		static GnomeFu ()
		{
			Gnome.Vfs.Vfs.Initialize ();
		}

		public static string GetMimeType (string text_path)
		{
			if (Path.GetExtension (text_path) == ".xml")
				return Marshal.PtrToStringAnsi (gnome_vfs_get_file_mime_type (text_path, (IntPtr)null, false));
			else
				return Marshal.PtrToStringAnsi (gnome_vfs_get_file_mime_type_fast (text_path, (IntPtr)null));
		}

		public static string GetMimeIconPath (string mimetype)
		{
			if (icon_theme == null)
				icon_theme = Gtk.IconTheme.Default;

			Gnome.IconLookupResultFlags result;

			// FIXME when ximian bug #76540 is fixed
			// change "new Gnome.Vfs.FileInfo (IntPtr.Zero)" to "null"
			string icon_name = Gnome.Icon.Lookup (icon_theme, null, null, null, new Gnome.Vfs.FileInfo (IntPtr.Zero), mimetype, (Gnome.IconLookupFlags) 0, out result);
			if (icon_name == null)
				return null;

			Gtk.IconInfo icon_info = icon_theme.LookupIcon (icon_name, 48, 0);
			if (icon_info == null)
				return null;

			return icon_info.Filename;
		}
	}

}
