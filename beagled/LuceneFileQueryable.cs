//
// LuceneFileQueryable.cs
//
// Copyright (C) 2005 Novell, Inc.
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

//
// This queryable just takes the LuceneQueryable and adds some sane
// default behavior for indexing files.
//

using System;
using System.Collections;

using Beagle.Util;

namespace Beagle.Daemon {

	public abstract class LuceneFileQueryable : LuceneQueryable {

		public LuceneFileQueryable (string index_name, int minor_version, bool disable_locking) :
			base (index_name, minor_version, disable_locking)
		{ }

		public LuceneFileQueryable (string index_name) : this (index_name, -1, false) { }

		public LuceneFileQueryable (string index_name, bool disable_locking) : this (index_name, -1, disable_locking) { }

		public LuceneFileQueryable (string index_name, int minor_version) : this (index_name, minor_version, false) { }

		///////////////////////////////////////////////////////////////////////////

		private Hashtable file_references_count = new Hashtable ();

		private void IncrementReferenceCount (string path)
		{
			if (! file_references_count.Contains (path)) {
				file_references_count [path] = 1;
				return;
			}

			file_references_count [path] = (int) file_references_count [path] + 1;
		}
		
		private bool DecrementReferenceCount (string path)
		{
			if (! file_references_count.Contains (path))
				throw new Exception ("Shared file is not referenced: " + path);

			int reference_count = (int) file_references_count [path] - 1;

			if (reference_count == 0) {
				file_references_count.Remove (path);
				return false;
			}

			file_references_count [path] = reference_count;
			return true;
		}

		///////////////////////////////////////////////////////////////////////////

		private class CachedFileInfo {
			public Uri Uri;
			public string Path;
			public DateTime Mtime;
			public bool Shared = false;
		}

		private Hashtable file_info_cache = UriFu.NewHashtable ();
		
		override protected bool PreAddIndexableHook (Indexable indexable)
		{
			// None of this applies for Removes
			if (indexable.Type == IndexableType.Remove)
				return true;

			// Remember the file's info and mtime.
			CachedFileInfo info = new CachedFileInfo ();

			info.Uri = indexable.Uri;

			if (indexable.Uri.IsFile && indexable.IsNonTransient)
				info.Path = indexable.Uri.LocalPath;
			else if (indexable.ContentUri.IsFile && indexable.IsNonTransient)
				info.Path = indexable.ContentUri.LocalPath;
			else if (indexable.ParentUri != null && indexable.ParentUri.IsFile) {
				info.Path = indexable.ParentUri.LocalPath;
				info.Shared = true;
				IncrementReferenceCount (info.Path);
			}

			try {
				info.Mtime = FileSystem.GetLastWriteTime (info.Path);
			} catch (Exception ex) {
				// If we can't get an mtime for the file, it must
				// have disappeared out from under us.  In that case,
				// don't bother adding anything.
				// FIXME: Should we be more specific and only catch
				// a file not found error here?
				return false;
			}

			file_info_cache [info.Uri] = info;

			return true;
		}

		override protected void PostAddHook (Indexable indexable, IndexerAddedReceipt receipt)
		{
			// Retrieve our cached info about the file.
			CachedFileInfo info;
			info = file_info_cache [receipt.Uri] as CachedFileInfo;
			if (info == null)
				return;

			// Yeah, this is ghetto. If it's a file that's shared across multiple
			// indexables, only tag it with when the last indexable has been indexed.
			if (info.Shared && ! DecrementReferenceCount (info.Path))
				return;

			// Since we know that the file has been successfully
			// indexed, update the file attributes accordingly.
			// Don't set filter information on a file if multiple 
			// indexables has been created from it.
			FileAttributes attr;
			attr = FileAttributesStore.ReadOrCreate (info.Path);

			attr.LastWriteTime = info.Mtime;

			// Don't set filter information on a file if multiple indexables has been
			// created from it.
			if (! info.Shared) {
				attr.FilterName = receipt.FilterName;
				attr.FilterVersion = receipt.FilterVersion;
			}

			if (! FileAttributesStore.Write (attr))
				Logger.Log.Error ("Couldn't write attributes for {0}", info.Path);
		}

		override protected void PostRemoveHook (Indexable indexable, IndexerRemovedReceipt receipt)
		{
			file_info_cache.Remove (indexable.Uri);
		}

		override protected bool HitIsValid (Uri uri)
		{
			// Do the right thing if the Uri is a file.
			// If the file Uri we need is the ContentUri, this won't work.
			if (! uri.IsFile)
				return true;

			try {
				return FileSystem.Exists (uri.LocalPath);
			} catch (Exception e) {
				Logger.Log.Warn ("Exception executing HitIsValid on {0}", uri.LocalPath);
				return false;
			}
		}

		///////////////////////////////////////////////////////////////////////////

		// Convenience functions

		public bool IsUpToDate (string path, Filter filter)
		{
			return FileAttributesStore.IsUpToDate (path, filter);
		}

		public bool IsUpToDate (string path)
		{
			return FileAttributesStore.IsUpToDate (path);
		}

	}

}
