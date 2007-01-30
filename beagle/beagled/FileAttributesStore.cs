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
			lock (ifas) {
				return ifas.Read (path);
			}
		}

		public FileAttributes ReadOrCreate (string path, Guid unique_id, out bool created)
		{
			lock (ifas) {
				created = false;

				FileAttributes attr = ifas.Read (path);
				// If we pass in a Guid that doesn't match the one we found in the
				// the attributes, clobber the old attributes and the old unique Guid.
				if (attr == null
				    || (unique_id != Guid.Empty && unique_id != attr.UniqueId)) {
					// First drop the old attribute, if there is one.
					if (attr != null)
						ifas.Drop (path);

					// Now create the new attribute
					attr = new FileAttributes ();
					attr.UniqueId = unique_id;
					attr.Path = path;
					
					// Now add the new attribute
					// Note: New attribute should not be added.
					//ifas.Write (attr);
					created = true;
				}
				return attr;
			}
		}

		public FileAttributes ReadOrCreate (string path, Guid unique_id)
		{
			bool dummy;
			return ReadOrCreate (path, unique_id, out dummy);
		}

		public FileAttributes ReadOrCreate (string path)
		{
			return ReadOrCreate (path, Guid.NewGuid ());
		}

		public bool Write (FileAttributes attr)
		{
			lock (ifas) {
				attr.LastAttrTime = DateTime.UtcNow;
				return ifas.Write (attr);
			}
		}

		public void Drop (string path)
		{
			lock (ifas) {
				ifas.Drop (path);
			}
		}

		public void BeginTransaction ()
		{
			lock (ifas)
				ifas.BeginTransaction ();

		}

		public void CommitTransaction ()
		{
			lock (ifas)
				ifas.CommitTransaction ();
		}

		//////////////////////////////////////////////////////////

		public static bool IsUpToDate (string path, FileAttributes attr)
		{
			if (attr == null)
				return false;

			return (attr.LastWriteTime >= FileSystem.GetLastWriteTimeUtc (path));
		}

		public bool IsUpToDate (string path, Filter filter)
		{
			FileAttributes attr;

			attr = Read (path);

			// If there are no attributes set on the file, there is no
			// way that we can be up-to-date.
			if (attr == null)
				return false;

			// Note that when filter is set to null, we ignore
			// any existing filter data.  That might not be the
			// expected behavior...
			if (filter != null) {

				if (! attr.HasFilterInfo)
					return false;

				if (attr.FilterName != filter.Name)
					return false;
				
				// FIXME: Obviously we want to reindex if
				// attr.FilterVersion < filter.Version.
				// But what if the filter we would use is older
				// than the one that was previously used?
				if (attr.FilterVersion != filter.Version)
					return false;
			} 

			return IsUpToDate (path, attr);
		}

		public bool IsUpToDate (string path)
		{
			return IsUpToDate (path, (Filter) null);
		}

		//////////////////////////////////////////////////////////

		// A convenience routine.
		public void AttachLastWriteTime (string path, DateTime mtime)
		{
			FileAttributes attr = ReadOrCreate (path);
			attr.LastWriteTime = mtime;
			if (! Write (attr))
				Logger.Log.Warn ("Couldn't store file attributes for {0}", path);
		}
	}
}
