//
// GNOME Dashboard
//
// gnome.cs: Bindings for some miscellaneous GNOME classes.
//
// Authors:
//    Miguel de Icaza <miguel@ximian.com>
//    Nat Friedman <nat@nat.ximian.com>
//

using Gtk;
using System;
using System.Runtime.InteropServices;

namespace Dewey.Util {

	namespace VFS {

		public class Mime {
		
			[DllImport ("libgnomevfs-2")] extern static bool gnome_vfs_init ();
			[DllImport ("libgnomevfs-2")] extern static string gnome_vfs_get_mime_type (string text_uri);

			static Mime ()
			{
				gnome_vfs_init ();
			}

			public static string GetMimeType (string text_uri)
			{
				return gnome_vfs_get_mime_type (text_uri);
			}

		}

		[Flags]
		public enum FileInfoOptions {
			DEFAULT = 0, 
			GET_MIME_TYPE = 1 << 0,
			FORCE_FAST_MIME_TYPE = 1 << 1,
			FORCE_SLOW_MIME_TYPE = 1 << 2,
			FOLLOW_LINKS = 1 << 3
		}
		public enum Result {
			OK,
			ERROR_NOT_FOUND,
			ERROR_GENERIC,
			ERROR_INTERNAL,
			ERROR_BAD_PARAMETERS,
			ERROR_NOT_SUPPORTED,
			ERROR_IO,
			ERROR_CORRUPTED_DATA,
			ERROR_WRONG_FORMAT,
			ERROR_BAD_FILE,
			ERROR_TOO_BIG,
			ERROR_NO_SPACE,
			ERROR_READ_ONLY,
			ERROR_INVALID_URI,
			ERROR_NOT_OPEN,
			ERROR_INVALID_OPEN_MODE,
			ERROR_ACCESS_DENIED,
			ERROR_TOO_MANY_OPEN_FILES,
			ERROR_EOF,
			ERROR_NOT_A_DIRECTORY,
			ERROR_IN_PROGRESS,
			ERROR_INTERRUPTED,
			ERROR_FILE_EXISTS,
			ERROR_LOOP,
			ERROR_NOT_PERMITTED,
			ERROR_IS_DIRECTORY,
			ERROR_NO_MEMORY,
			ERROR_HOST_NOT_FOUND,
			ERROR_INVALID_HOST_NAME,
			ERROR_HOST_HAS_NO_ADDRESS,
			ERROR_LOGIN_FAILED,
			ERROR_CANCELLED,
			ERROR_DIRECTORY_BUSY,
			ERROR_DIRECTORY_NOT_EMPTY,
			ERROR_TOO_MANY_LINKS,
			ERROR_READ_ONLY_FILE_SYSTEM,
			ERROR_NOT_SAME_FILE_SYSTEM,
			ERROR_NAME_TOO_LONG,
			ERROR_SERVICE_NOT_AVAILABLE,
			ERROR_SERVICE_OBSOLETE,
			ERROR_PROTOCOL_ERROR,
			NUM_ERRORS
		}
        
		public class FileInfo {
			internal IntPtr handle;

			[DllImport ("libgnomevfs-2")]
				extern static IntPtr gnome_vfs_file_info_new ();

			[DllImport ("libgnomevfs-2")]
				extern static void gnome_vfs_file_info_unref (IntPtr handle);

			[DllImport ("libgnomevfs-2")]
				extern static Result gnome_vfs_get_file_info_uri (IntPtr uri, IntPtr file_info, FileInfoOptions options);

			[DllImport ("libgnomevfs-2")] extern static IntPtr gnome_vfs_uri_new (string text_uri);
			[DllImport ("libgnomevfs-2")]	extern static void gnome_vfs_uri_unref (IntPtr gnomeuri);

			public FileInfo ()
			{
			}

			~FileInfo ()
			{
				gnome_vfs_file_info_unref (handle);
				handle = (IntPtr) 0;
			}
                
			public FileInfo (string uri, FileInfoOptions options)
			{
				handle = gnome_vfs_file_info_new ();
				
				Result r = Get (uri, options);
				if (r != Result.OK)
					throw new Exception ("VFS Error: " + r + ", URI: " + uri);
			}

			public Result Get (string uri, FileInfoOptions options)
			{
				IntPtr gnomeuri = gnome_vfs_uri_new (uri);
				Result r = gnome_vfs_get_file_info_uri (gnomeuri, handle, options);
				gnome_vfs_uri_unref (gnomeuri);
				return r;
			}
		}
	}

	public class MimeApplication {
		[Flags]
		public enum ArgumentType {
			URIS,
			PATHS,
			URIS_FOR_NON_FILES,
		}
		[StructLayout(LayoutKind.Sequential)]
		public class Info {
			public string id;
			public string name;
			public string command;
			public bool can_open_multiple_files;
			public ArgumentType expects_uris;
			public IntPtr supported_uri_schemes;
			public bool requires_terminal;

			public IntPtr reserved1;
			public IntPtr reserved2;
		}

		[DllImport ("libgnomevfs-2")]
			extern static Info gnome_vfs_mime_get_default_application ( string mime_type );
		[DllImport ("libgnomevfs-2")]
			extern static void gnome_vfs_mime_application_free ( Info info );

		public static void Exec (string mime_type, string uri)
		{
			Info info;
			info = gnome_vfs_mime_get_default_application (mime_type);
			System.Diagnostics.Process e = new System.Diagnostics.Process ();
			if (info == null)
			{
				Console.WriteLine ("Unable to open " + uri);
				// Can we please stop hard coding Nautilus!?
				e.Start ("nautilus", "\"" + uri + "\"");
			} else {
				e.Start (info.command, "\"" + uri + "\"");
			}
//FIXME:  Memory leak, causes crashes, dunno why...needs fixed
//			gnome_vfs_mime_application_free (info);
		}

	}


	public class ThumbnailFactory {
		internal IntPtr handle;

		[Flags]
		public enum ThumbnailSize {
			NORMAL,
			LARGE,
		}

		
		[DllImport ("libgnomeui-2")]
			extern static IntPtr gnome_thumbnail_factory_new (ThumbnailSize size);

		public ThumbnailFactory (ThumbnailSize size)
		{
			handle = gnome_thumbnail_factory_new (size);
			if (handle == (IntPtr) 0) {
				Console.WriteLine ("Could not create thumbnail factory");
			}
		}
	}

	public class Icon {
		[Flags]
		public enum LookupFlags {
			NONE = 0,
			EMBEDDING_TEXT = 1,
			SHOW_SMALL_IMAGES_AS_THEMSELVES = 2,
		}

		[Flags]
		public enum LookupResultFlags {
			NONE = 0,
			THUMBNAIL = 1
		}

		[DllImport ("libgnomeui-2")]
			extern static string gnome_vfs_escape_path_string (string uri);

		[DllImport ("libgnomeui-2")]
			extern static string gnome_thumbnail_factory_lookup (IntPtr factory, string uri, int mtime);

		[DllImport ("libgnomeui-2")]
			extern static IntPtr gnome_thumbnail_factory_generate_thumbnail (IntPtr factory, string uri, string mime_type); 

		[DllImport ("libgnomeui-2")]
			extern static void gnome_thumbnail_factory_save_thumbnail (IntPtr factory, IntPtr pixbuf, string uri, int mtime);

		[DllImport ("libgnomeui-2")]
			extern static bool gnome_thumbnail_factory_can_thumbnail (IntPtr factory, string uri, string mime_type, int mtime);

		[DllImport ("libgnomeui-2")]
			extern static bool gnome_thumbnail_factory_has_valid_failed_thumbnail (IntPtr factory, string uri, int mtime);

		[DllImport ("libgnomeui-2")]
			extern static IntPtr gnome_icon_theme_new ();

		[DllImport ("libgnomeui-2")]
			extern static string gnome_icon_theme_lookup_icon (IntPtr theme, string icon_name, int size, IntPtr icon_data, out int base_size);

		[DllImport ("libgnomeui-2")]
			extern static string gnome_icon_lookup (IntPtr theme, IntPtr factory, string uri,
										  string custom_icon, IntPtr file_info_handle,
										  string mime_type, LookupFlags flags,
										  out LookupResultFlags flags_result);

		static IntPtr icon_theme = gnome_icon_theme_new ();
		static ThumbnailFactory thumbnail_factory = new ThumbnailFactory (ThumbnailFactory.ThumbnailSize.NORMAL);

		public static string Lookup (string uri, string custom_icon, VFS.FileInfo fi, string mime_type)
		{
			string euri = gnome_vfs_escape_path_string (uri);
			string icon_data = null;

			if (uri.StartsWith ("/"))
				uri = "file://" + uri;

			// Convert DateTime to UNIX time_t

			int mtime = (int) (DateTime.Now - new DateTime (1970, 1, 1)).TotalSeconds;

			if (uri.StartsWith ("file://"))
				mtime = (int) (System.IO.File.GetLastWriteTime (uri.Substring (7)) - new DateTime (1970, 1, 1)).TotalSeconds;
			
			//
			// Most of this is because some files are thumbnailed as local
			// files, some URI's, and sometimes an escaped either of the two
			// latter
			//

			if ( gnome_thumbnail_factory_can_thumbnail (thumbnail_factory.handle, uri, mime_type, mtime) ) {
				if ( ! ( gnome_thumbnail_factory_has_valid_failed_thumbnail (thumbnail_factory.handle, uri, mtime) || 
					gnome_thumbnail_factory_has_valid_failed_thumbnail (thumbnail_factory.handle, uri, mtime) ||
					gnome_thumbnail_factory_has_valid_failed_thumbnail (thumbnail_factory.handle, euri, mtime) ||
					gnome_thumbnail_factory_has_valid_failed_thumbnail (thumbnail_factory.handle, euri, mtime) ) ) {

					icon_data = gnome_thumbnail_factory_lookup (thumbnail_factory.handle, uri, mtime);
					if (icon_data == null)
						icon_data = gnome_thumbnail_factory_lookup (thumbnail_factory.handle, uri, mtime);
					if (icon_data == null)
						icon_data = gnome_thumbnail_factory_lookup (thumbnail_factory.handle, euri, mtime);
					if (icon_data == null)
						icon_data = gnome_thumbnail_factory_lookup (thumbnail_factory.handle, euri, mtime);

					if (icon_data == null) {
						IntPtr p = gnome_thumbnail_factory_generate_thumbnail (thumbnail_factory.handle, uri, mime_type);
						gnome_thumbnail_factory_save_thumbnail (thumbnail_factory.handle, p, uri, mtime);
						icon_data = gnome_thumbnail_factory_lookup (thumbnail_factory.handle, uri, mtime);
					}
				}
			}

			if (icon_data == null || icon_data.IndexOf ("/") != 0) {
				LookupResultFlags out_flags;
				int base_size;
				string icon_name = gnome_icon_lookup (
							       icon_theme,
							       thumbnail_factory.handle,
							       uri,
							       custom_icon,
							       fi == null ? (IntPtr) 0 : fi.handle,
							       mime_type,
						   	    LookupFlags.SHOW_SMALL_IMAGES_AS_THEMSELVES,
						      	 out out_flags);
				icon_data = gnome_icon_theme_lookup_icon (icon_theme, icon_name, 48, (IntPtr) 0, out base_size);
			}

			return icon_data;
		}

		public static string Lookup (string uri, string custom_icon, string mime_type)
		{
			return Lookup (uri, custom_icon, null, mime_type);
		}

		public static string Lookup (string mime_type)
		{
			return Lookup (null, null, mime_type);
		}

		public static string LookupByURI (string uri)
		{
			string mime_type = VFS.Mime.GetMimeType (uri);
			VFS.FileInfo fi;

			fi = new VFS.FileInfo (uri, 0);

			return Lookup (uri, null, fi, mime_type);
		}
	}

}
