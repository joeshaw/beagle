//
// ExtendedAttribute.cs
//
// Copyright (C) 2004 Novell, Inc.
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

// FIXME: This is not portable to Win32

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Beagle.Util {

	public class ExtendedAttribute {

		[DllImport ("libc")]
		static extern int setxattr (string path, string name, byte[] value, uint size, int flags);

		[DllImport ("libc")]
		static extern int getxattr (string path, string name, byte[] value, uint size);

		[DllImport ("libc")]
		static extern int removexattr (string path, string name);

		private static string AddPrefix (string name)
		{
			return "user.Beagle." + name;
		}

		static Encoding encoding = new UTF8Encoding ();

		public static void Set (FileSystemInfo info, string name, string value)
		{
			name = AddPrefix (name);

			byte[] buffer = encoding.GetBytes (value);
			int retval = setxattr (info.FullName, name, buffer, (uint) buffer.Length, 0);
			// FIXME: should check retval, throw an exception if path doesn't exist, etc.
		}

		public static string Get (FileSystemInfo info, string name)
		{
			name = AddPrefix (name);

			byte[] buffer = null;
			int size = getxattr (info.FullName, name, buffer, 0);
			if (size <= 0)
				return null;
			buffer = new byte [size];
			int rv = getxattr (info.FullName, name, buffer, (uint) size);
			// FIXME: should check retval, throw an exception if path doesn't exist, etc.

			return encoding.GetString (buffer);
		}

		public static void Remove (FileSystemInfo info, string name)
		{
			name = AddPrefix (name);

			int retval = removexattr (info.FullName, name);
			// FIXME: should check retval, throw an exception if path doesn't exist, etc.
		}

		//////////////////////////////////////////////////////////////////////

		const string fingerprintAttr = "Fingerprint";
		const string mtimeAttr = "MTime";

		private static string timeToString (DateTime dt)
		{
			return dt.Ticks.ToString ();
		}

		public static bool Check (FileSystemInfo info, string fingerprint)
		{
			// Check the file's mtime to make sure it agrees with
			// the timestamp stored in the extended attribute.
			string mtimeFile = timeToString (info.LastWriteTime);
			string mtimeStored = Get (info, mtimeAttr);
			if (mtimeFile != mtimeStored)
				return false;

			// Confirm the fingerprint.
			string fingerprintStored = Get (info, fingerprintAttr);
			if (fingerprint != fingerprintStored)
				return false;
			
			return true;
		}

		public static void Mark (FileSystemInfo info, string fingerprint, DateTime mtime)
		{
			// Store the file's mtime and the fingerprint in
			// extended attributes.
			Set (info, fingerprintAttr, fingerprint);
			Set (info, mtimeAttr, timeToString (mtime));
		}

		public static void Mark (FileSystemInfo info, string fingerprint)
		{
			Mark (info, fingerprint, info.LastWriteTime);
		}
		
	}
}
