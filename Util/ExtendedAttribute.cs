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
using Mono.Posix;

namespace Beagle.Util {

	public class ExtendedAttribute {

		[DllImport ("libc", SetLastError=true)]
		static extern int lsetxattr (string path, string name, byte[] value, uint size, int flags);

		[DllImport ("libc", SetLastError=true)]
		static extern int lgetxattr (string path, string name, byte[] value, uint size);

		[DllImport ("libc", SetLastError=true)]
		static extern int lremovexattr (string path, string name);

		private static string AddPrefix (string name)
		{
			return "user.Beagle." + name;
		}

		static Encoding encoding = new UTF8Encoding ();

		public static void Set (string path, string name, string value)
		{
			if (! FileSystem.Exists (path))
				throw new IOException (path);

			name = AddPrefix (name);

			byte[] buffer = encoding.GetBytes (value);
			int retval = lsetxattr (path, name, buffer, (uint) buffer.Length, 0);
			if (retval != 0) 
				throw new Exception ("Could not set extended attribute on " + path + ": " + Syscall.strerror (Marshal.GetLastWin32Error ()));
		}

		public static string Get (string path, string name)
		{
			if (! FileSystem.Exists (path))
				throw new IOException (path);

			name = AddPrefix (name);

			byte[] buffer = null;
			int size = lgetxattr (path, name, buffer, 0);
			if (size <= 0)
				return null;
			buffer = new byte [size];
			int retval = lgetxattr (path, name, buffer, (uint) size);
			if (retval < 0)
				throw new Exception ("Could not get extended attribute on " + path + ": " + Syscall.strerror (Marshal.GetLastWin32Error ()));

			return encoding.GetString (buffer);
		}

		public static void Remove (string path, string name)
		{
			if (! FileSystem.Exists (path))
				throw new IOException (path);
			
			name = AddPrefix (name);

			int retval = lremovexattr (path, name);
			if (retval != 0)
				throw new Exception ("Could not remove extended attribute on " + path + ": " + Syscall.strerror (Marshal.GetLastWin32Error ()));
		}

		//////////////////////////////////////////////////////////////////////

		const string nameAttr = "Name";
		const string fingerprintAttr = "Fingerprint";
		const string mtimeAttr = "MTime";

		public static bool CheckFingerprint (string path, string fingerprint)
		{
			string fingerprintStored = Get (path, fingerprintAttr);
			return fingerprint == fingerprintStored;
		}

		public static bool Check (string path, string fingerprint)
		{
			path = Path.GetFullPath (path);

			// Check the file's mtime to make sure it agrees with
			// the timestamp stored in the extended attribute.
			string mtimeFile = StringFu.DateTimeToString (FileSystem.GetLastWriteTime (path));
			string mtimeStored = Get (path, mtimeAttr);
			if (mtimeFile != mtimeStored)
				return false;

			// Confirm the filename
			string nameStored = Get (path, nameAttr);
			if (path != nameStored)
				return false;

			// Confirm the fingerprint.
			string fingerprintStored = Get (path, fingerprintAttr);
			if (fingerprint != fingerprintStored)
				return false;
			
			return true;
		}

		public static void Mark (string path, string fingerprint, DateTime mtime)
		{
			path = Path.GetFullPath (path);

			// Store the file's mtime and the fingerprint in
			// extended attributes.
			Set (path, fingerprintAttr, fingerprint);
			Set (path, nameAttr, path);
			Set (path, mtimeAttr, StringFu.DateTimeToString (mtime));
		}

		public static void Mark (string path, string fingerprint)
		{
			Mark (path, fingerprint, FileSystem.GetLastWriteTime (path));
		}
		
	}
}
