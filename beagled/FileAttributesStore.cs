//
// FileAttributesStore.cs
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

using Beagle.Util;

namespace Beagle.Daemon {

	public class FileAttributesStore {

		private IFileAttributesStore ifas;

		public FileAttributesStore (IFileAttributesStore ifas)
		{
			this.ifas = ifas;
		}

		public FileAttributes Read (string path)
		{
			lock (this) {
				return ifas.Read (path);
			}
		}

		public FileAttributes ReadOrCreate (string path)
		{
			lock (this) {
				FileAttributes attr = ifas.Read (path);
				if (attr == null) {
					attr = new FileAttributes ();
					attr.UniqueId = Guid.NewGuid ().ToString ();
					attr.Path = path;
					ifas.Write (attr);
				}
				return attr;
			}
		}

		public bool Write (FileAttributes attr)
		{
			lock (this) {
				return ifas.Write (attr);
			}
		}

		public void Drop (string path)
		{
			lock (this) {
				ifas.Drop (path);
			}
		}

		//////////////////////////////////////////////////////////

		public bool IsUpToDate (string path)
		{
			FileAttributes attr;

			attr = Read (path);

			// FIXME: This check is incomplete, we should also check
			// filter names, filter versions, etc.
			return attr != null
				&& attr.Path == path
				&& FileSystem.GetLastWriteTime (path) <= attr.LastWriteTime;
		}

		public void AttachTimestamp (string path, DateTime mtime)
		{
			FileAttributes attr = ReadOrCreate (path);

			attr.Path = path;
			attr.LastWriteTime = mtime;
			attr.LastIndexedTime = DateTime.Now;

			if (! Write (attr)) {
				Logger.Log.Warn ("Couldn't store file attributes for {0}", path);
			}
		}

	}
}
