//
// FileSystem.cs
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

using System;
using System.IO;

namespace Beagle.Util {
	
	public class FileSystem {

		static public bool Exists (string path)
		{
			return File.Exists (path) || Directory.Exists (path);
		}

		static public DateTime GetLastWriteTimeUtc (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			if (File.Exists (path))
				return File.GetLastWriteTimeUtc (path);
			else if (Directory.Exists (path))
				return Directory.GetLastWriteTimeUtc (path);
			else
				throw new FileNotFoundException (path);
		}

		static public FileSystemInfo New (string path)
		{
			if (Directory.Exists (path))
				return new DirectoryInfo (path);
			return new FileInfo (path);
		}

		// I guess this is as good a place for this as any.
		static public bool IsSymLink (string path)
		{
			Mono.Unix.Native.Stat stat;
			Mono.Unix.Native.Syscall.lstat (path, out stat);
			return (stat.st_mode & Mono.Unix.Native.FilePermissions.S_IFLNK) == Mono.Unix.Native.FilePermissions.S_IFLNK;
		}

		// Special version of this function which handles the root directory.
		static public string GetDirectoryNameRootOk (string path)
		{
			// System.IO.Path.GetDirectoryName ("/") returns null.
			// Handle it specially.
			if (path == "/")
				return path;

			return System.IO.Path.GetDirectoryName (path);
		}
	}

}
