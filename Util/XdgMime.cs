//
// XdgMime.cs
//
// Copyright (C) 2006 Debajyoti Bera
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Beagle.Util {
	public class XdgMime {

		[DllImport ("libbeagleglue")]
		static extern IntPtr xdg_mime_get_mime_type_for_file (string file_path, IntPtr optional_stat_info);

		[DllImport ("libbeagleglue")]
		static extern IntPtr xdg_mime_get_mime_type_from_file_name (string file_name);

		public static string GetMimeTypeFromFileName (string file_name)
		{
			return Marshal.PtrToStringAnsi (xdg_mime_get_mime_type_from_file_name (file_name));
		}

		public static string GetMimeType (string file_path)
		{
			string mime_type = Marshal.PtrToStringAnsi (xdg_mime_get_mime_type_for_file (file_path, (IntPtr) null));

			if (mime_type != "application/octet-stream")
				return mime_type;

			// xdgmime recognizes most files without extensions as
			// application/octet-stream.  Check the first 256 bytes
			// to see if it's really plain text.
			if (ValidateUTF8 (file_path))
				return "text/plain";
			else
				return mime_type;
		}

		private static UTF8Encoding validating_encoding = new UTF8Encoding (true, true);

		private static bool ValidateUTF8 (string file_path)
		{
			FileStream fs;

			try {
				fs = new FileStream (file_path, FileMode.Open, FileAccess.Read, FileShare.Read);
			} catch (IOException) {
				return false;
			}

			byte[] byte_buf = new byte [256];
			char[] char_buf = new char [256];

			int buf_length = fs.Read (byte_buf, 0, 256);

			fs.Close ();

			if (buf_length == 0)
				return false; // Don't treat empty files as text/plain

			Decoder d = validating_encoding.GetDecoder ();

			try {
				d.GetChars (byte_buf, 0, buf_length, char_buf, 0);

				// FIXME: UTF8 allows control characters in a file.
				// Should we allow control characters in a text file?
			} catch (ArgumentException) {
				return false;
			}

			return true;
		}
	}
}
