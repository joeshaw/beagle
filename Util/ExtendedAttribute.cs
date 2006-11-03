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
using Mono.Unix.Native;

namespace Beagle.Util {

	public class ExtendedAttribute {

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
			int retval = Syscall.lsetxattr (path, name, buffer);
			if (retval == -1)
				throw new IOException ("Could not set extended attribute on " + path + ": " + Mono.Unix.Native.Stdlib.strerror (Mono.Unix.Native.Stdlib.GetLastError ()));
		}

		public static bool Exists (string path, string name)
		{
			if (! FileSystem.Exists (path))
				throw new IOException (path);

			name = AddPrefix (name);

			long size = Syscall.lgetxattr (path, name, null, 0);
			return size >= 0;
		}

		public static string Get (string path, string name)
		{
			if (! FileSystem.Exists (path))
				throw new IOException (path);

			name = AddPrefix (name);

			byte[] buffer;
			long size = Syscall.lgetxattr (path, name, out buffer);
			if (size <= 0)
				return null;
			return encoding.GetString (buffer);
		}

		public static void Remove (string path, string name)
		{
			if (! FileSystem.Exists (path))
				throw new IOException (path);
			
			name = AddPrefix (name);

			int retval = Syscall.lremovexattr (path, name);
			if (retval != 0)
				throw new IOException ("Could not remove extended attribute on " + path + ": " + Mono.Unix.Native.Stdlib.strerror (Mono.Unix.Native.Stdlib.GetLastError ()));
		}

		// Check to see if it is possible to get and set attributes on a given file.
		public static bool Test (string path)
		{
			const string test_key = "__test_key__";
			const string test_value = "__test_value__";

			try {
				Set (path, test_key, test_value);
				if (Get (path, test_key) != test_value)
					return false;
				Remove (path, test_key);
				return Get (path, test_key) == null;

			} catch {
				return false;
			}
		}

		private static bool ea_support_tested = false;
		private static bool ea_supported = false;

		public static bool Supported {
			get {
				if (ea_support_tested)
					return ea_supported;

				if (Environment.GetEnvironmentVariable ("BEAGLE_DISABLE_XATTR") != null) {
					Logger.Log.Debug ("BEAGLE_DISABLE_XATTR is set");
					ea_supported = false;
					ea_support_tested = true;
					return false;
				}

				ea_supported = Test (PathFinder.HomeDir);
				ea_support_tested = true;

				return ea_supported;
			}
		}
	}
}
