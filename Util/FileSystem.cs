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

		static public DateTime GetLastWriteTime (string path)
		{
			if (File.Exists (path))
				return File.GetLastWriteTime (path);
			else if (Directory.Exists (path))
				return Directory.GetLastWriteTime (path);
			else
				throw new IOException (path);
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
			Mono.Unix.Stat stat;
			Mono.Unix.Syscall.lstat (path, out stat);
			return (stat.st_mode & Mono.Unix.FilePermissions.S_IFLNK) == Mono.Unix.FilePermissions.S_IFLNK;
		}

	}

}
