//
// PageCache.cs
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
using System.Runtime.InteropServices;
using Mono.Posix;

namespace Beagle.Util {

	public class PageCache {

		private enum PageCacheAdvice {
			Normal     = 0,
			Random     = 1,
			Sequential = 2,
			WillNeed   = 3,
			DontNeed   = 4,
			NoReuse    = 5
		}
		
		// from bits/fcntl.h:
		// # define POSIX_FADV_NORMAL      0 /* No further special treatment.  */
		// # define POSIX_FADV_RANDOM      1 /* Expect random page references.  */
		// # define POSIX_FADV_SEQUENTIAL  2 /* Expect sequential page references.  */
		// # define POSIX_FADV_WILLNEED    3 /* Will need these pages.  */
		// # define POSIX_FADV_DONTNEED    4 /* Don't need these pages.  */
		// # define POSIX_FADV_NOREUSE     5 /* Data will be accessed once.  */

		[DllImport ("libc")]
		static extern int posix_fadvise (int fd, int offset, int length, int advice);

		static void Advise (string path, int offset, int length, PageCacheAdvice advice)
		{
			int fd = Syscall.open (path, OpenFlags.O_RDONLY);
			if (fd < 0)
				return; // FIXME: probably shouldn't fail silently
			int retval = posix_fadvise (fd, offset, length, (int) advice);
			// FIXME: should check retval, etc.
			Syscall.close (fd);
		}
		
		public static void WillNeed (string path)
		{
			Advise (path, 0, 0, PageCacheAdvice.WillNeed);
		}

		public static void DoNotNeed (string path)
		{
			Advise (path, 0, 0, PageCacheAdvice.DontNeed);
		}
	}
}
