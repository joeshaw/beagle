//
// Crawler.cs
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
using System.Collections;
using System.IO;

using Mono.Posix;

using Beagle.Daemon;
using BU = Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public class Crawler {

		LuceneDriver driver;
		Hashtable mtimeCache = new Hashtable ();
		ArrayList stuffToIndex = new ArrayList ();
		ArrayList dirsToCrawl = new ArrayList ();

		public Crawler (LuceneDriver _driver, FileSystemInfo root)
		{
			driver = _driver;

			if (! root.Exists)
				return;
			
			if (root is FileInfo) {

				if (NeedsIndexing (root))
					stuffToIndex.Add (root);

			} else if (root is DirectoryInfo) {

				DirectoryInfo dir = (DirectoryInfo) root;

				// Scan all files and subdirectories.
				foreach (FileSystemInfo info in dir.GetFileSystemInfos ()) {
					if (! IsSymLink (info) && ! Exclude (info)) {
						if (NeedsIndexing (info))
							stuffToIndex.Add (info);
						if (info is DirectoryInfo)
							dirsToCrawl.Add (info);
					}
				}

			} else {
				// This shouldn't happen, right?
			}
		}

		public ICollection /* of FileSystemInfo */ FilesToIndex {
			get { return stuffToIndex; }
		}

		public ICollection /* of DirectoryInfo */ DirectoriesToCrawl {
			get { return dirsToCrawl; }
		}

		//////////////////////////////////////////////////////////////////////////
	
		private static bool IsSymLink (FileSystemInfo info)
		{
			Stat stat = new Stat ();
			Syscall.lstat (info.FullName, out stat);
			int mode = (int) stat.Mode & (int)StatModeMasks.TypeMask;
			return mode == (int) StatMode.SymLink;
		}

		//////////////////////////////////////////////////////////////////////////

		// This needs to be implemented properly.

		private bool Exclude (FileSystemInfo info)
		{
			if (info is FileInfo)
				return ExcludeFileByName (info.Name);
			else if (info is DirectoryInfo)
				return ExcludeDirectoryByName (info.Name);

			// This shouldn't happen.
			return true;
		}

		private bool ExcludeFileByName (string name)
		{
			if (name [0] == '.')
				return true;

			int last = name.Length - 1;
			if (name [last] == '~')
				return true;

			if (name [0] == '#' && name [last] == '#')
				return true;

			return false;
		}

		private bool ExcludeDirectoryByName (string name)
		{
			if (name [0] == '.')
				return true;
			
			if (name == "CVS" || name == "po")
				return true;

			return false;

		}

		//////////////////////////////////////////////////////////////////////////

		//
		// We set extended attributes on the file to indicate when it was indexed
		// and what index it was stored in.  This allows us to recognize files that
		// have changed since they were last indexed or which have never been indexed
		// without having to do any lucene queries.
		//
		
		const string fingerprintAttr = "Fingerprint";
		const string timestampAttr = "Timestamp";

		// Return true if this item needs to be indexed.
		private bool NeedsIndexing (FileSystemInfo info)
		{
			if (! info.Exists)
				return false;

			// Check the file's mtime.  If it doesn't agree with the timestamp
			// stored in the extended attribute, we need to reindex.
			string mtimeStr = info.LastWriteTime.Ticks.ToString ();
			mtimeCache [info.FullName] = mtimeStr;
			string timestampStr = BU.ExtendedAttribute.Get (info.FullName, timestampAttr);
			if (mtimeStr != timestampStr)
				return true;
			
			// Check the fingerprint.  If they don't match up, we
			// need to reindex.
			string fingerprint = BU.ExtendedAttribute.Get (info.FullName, fingerprintAttr);
			if (fingerprint != driver.Fingerprint)
				return true;


			return false;
		}

		// Mark a file system item as having been indexed.
		public void MarkAsIndexed (FileSystemInfo info)
		{
			if (! info.Exists)
				return;

			// It is a good thing that setting an EA doesn't change
			// the file's mtime...

			BU.ExtendedAttribute.Set (info.FullName, fingerprintAttr,
						  driver.Fingerprint);

			// We use the cached mtime value, just in case the
			// file gets changed between indexing and when
			// MarkAsIndexed gets called.
			string mtimeStr = (string) mtimeCache [info.FullName];
			BU.ExtendedAttribute.Set (info.FullName, timestampAttr, mtimeStr);

			// Since this file was just indexed, we don't need to keep
			// it around in the page cache.
			//BU.PageCache.DoNotNeed (info.FullName);
		}

	}

}
