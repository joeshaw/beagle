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
using GLib;
using System;
using System.Runtime.InteropServices;

namespace Beagle.Util {

	namespace VFS {

		public class Mime {
		
			[DllImport ("libgnomevfs-2")] extern static bool gnome_vfs_init ();
			[DllImport ("libgnomevfs-2")] extern static IntPtr gnome_vfs_get_mime_type (string text_uri);
			[DllImport ("libgnomevfs-2")] extern static IntPtr gnome_vfs_get_mime_type_for_data (byte[] data, int length);
			[DllImport ("libgnomevfs-2")] extern static IntPtr gnome_vfs_mime_type_from_name_or_default (string filename, string defaultv);

			static Mime ()
			{
				gnome_vfs_init ();
			}

			public static string GetMimeType (string text_path)
			{
				string full_uri = StringFu.PathToQuotedFileUri (text_path);
				string mimeType = GLib.Marshaller.PtrToStringGFree (gnome_vfs_get_mime_type (full_uri));
				return mimeType;
			}

			public static string GetMimeTypeFromData (byte[] buffer, int buffSize, string text_uri)
			{
				string guessedType = Marshal.PtrToStringAnsi (gnome_vfs_get_mime_type_for_data (buffer, buffSize));
				if (text_uri != null 
				    && (guessedType == "text/plain"
					|| guessedType == "application/octet-stream"
					|| guessedType == "application/zip"))
					guessedType = Marshal.PtrToStringAnsi (gnome_vfs_mime_type_from_name_or_default (text_uri, guessedType));
				return guessedType;
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
			if (info == null)
			{
				Console.WriteLine ("Unable to open " + uri);
				// FIXME: Can we please stop hard coding Nautilus!?
				System.Diagnostics.Process.Start ("nautilus", "\"" + uri + "\"");
			} else {
				System.Diagnostics.Process.Start (info.command, "\"" + uri + "\"");
			}
//FIXME:  Memory leak, causes crashes, dunno why...needs fixed
//			gnome_vfs_mime_application_free (info);
		}

	}
}
