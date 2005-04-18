//
// DirectoryWalker.cs
//
// Copyright (C) 2005 Novell, Inc.
//

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

using System;
using System.Collections;
using System.IO;

namespace Beagle.Util {

	public class DirectoryWalker {

		private delegate bool   FileFilter      (string path, string name);
		private delegate object FileObjectifier (string path, string name);

		private class FileEnumerator : IEnumerator {

			string path;
			FileFilter file_filter;
			FileObjectifier file_objectifier;
			IntPtr dir_handle = IntPtr.Zero;
			string current;

			public FileEnumerator (string          path,
					       FileFilter      file_filter,
					       FileObjectifier file_objectifier)
			{
				this.path = path;
				this.file_filter = file_filter;
				this.file_objectifier = file_objectifier;
				Reset ();
			}

			~FileEnumerator ()
			{
				if (dir_handle != IntPtr.Zero)
					Mono.Posix.Syscall.closedir (dir_handle);
			}

			public object Current {
				get { 
					object current_obj = null;
					if (current != null) {
						if (file_objectifier != null)
							current_obj = file_objectifier (path, current); 
						else
							current_obj = Path.Combine (path, current);
					}
					return current_obj;
				}
			}

			public bool MoveNext ()
			{
				do {
					current = Mono.Posix.Syscall.readdir (dir_handle);
				} while (current == "."
					 || current == ".." 
					 || (file_filter != null && current != null && ! file_filter (path, current)));
				if (current == null) {
					Mono.Posix.Syscall.closedir (dir_handle);
					dir_handle = IntPtr.Zero;
				}
				return current != null;
			}

			public void Reset ()
			{
				current = null;
				if (dir_handle != IntPtr.Zero)
					Mono.Posix.Syscall.closedir (dir_handle);
				dir_handle = Mono.Posix.Syscall.opendir (path);
				if (dir_handle == IntPtr.Zero)
					throw new DirectoryNotFoundException (path);
			}
		}

		private class FileEnumerable : IEnumerable {

			string path;
			FileFilter file_filter;
			FileObjectifier file_objectifier;

			public FileEnumerable (string          path,
					       FileFilter      file_filter,
					       FileObjectifier file_objectifier)
			{
				this.path = path;
				this.file_filter = file_filter;
				this.file_objectifier = file_objectifier;
			}

			public IEnumerator GetEnumerator ()
			{
				return new FileEnumerator (path, file_filter, file_objectifier);
			}
		}

		static private bool IsFile (string path, string name)
		{
			return File.Exists (Path.Combine (path, name));
		}

		static private object FileInfoObjectifier (string path, string name)
		{
			return new FileInfo (Path.Combine (path, name));
		}

		static public IEnumerable GetFiles (string path)
		{
			return new FileEnumerable (path, new FileFilter (IsFile), null);
		}

		static public IEnumerable GetFiles (DirectoryInfo dirinfo)
		{
			return GetFiles (dirinfo.FullName);
		}

		static public IEnumerable GetFileInfos (string path)
		{
			return new FileEnumerable (path,
						   new FileFilter (IsFile),
						   new FileObjectifier (FileInfoObjectifier));
		}

		static public IEnumerable GetFileInfos (DirectoryInfo dirinfo)
		{
			return GetFileInfos (dirinfo.FullName);
		}

		static private bool IsDirectory (string path, string name)
		{
			return Directory.Exists (Path.Combine (path, name));
		}

		static private object DirectoryInfoObjectifier (string path, string name)
		{
			return new DirectoryInfo (Path.Combine (path, name));
		}

		static public IEnumerable GetDirectories (string path)
		{
			return new FileEnumerable (path, new FileFilter (IsDirectory), null);
		}

		static public IEnumerable GetDirectories (DirectoryInfo dirinfo)
		{
			return GetDirectories (dirinfo.FullName);
		}

		static public IEnumerable GetDirectoryInfos (string path)
		{
			return new FileEnumerable (path,
						   new FileFilter (IsDirectory),
						   new FileObjectifier (DirectoryInfoObjectifier));
		}

		static public IEnumerable GetDirectoryInfos (DirectoryInfo dirinfo)
		{
			return GetDirectoryInfos (dirinfo.FullName);
		}
	}
}
