//
// FileAttributesStore_ExtendedAttribute.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.IO;

using Beagle.Util;

namespace Beagle.Daemon {
	
	public class FileAttributesStore_ExtendedAttribute : IFileAttributesStore {

		public static bool Disable = false;

		private string index_fingerprint;
		
		public FileAttributesStore_ExtendedAttribute (string index_fingerprint)
		{
			this.index_fingerprint = index_fingerprint;
		}

		const int EA_VERSION = 1;

		// FIXME: We should probably serialize the data into a lump and attach
		// it to just one EA.  The current method has an inherent race condition:
		// if the file changes out from under us mid-Read or mid-Write, all sorts
		// of weirdness could ensue.

		const string fingerprint_attr = "Fingerprint";
		const string unique_id_attr = "Uid";
		const string path_attr = "Name";
		const string last_mtime_attr = "MTime";
		const string last_indexed_attr = "IndexTime";
		const string filter_attr = "Filter";

		public FileAttributes Read (string path)
		{
			if (Disable)
				return null;

			try {
				string tmp;
				tmp = ExtendedAttribute.Get (path, fingerprint_attr);
				if (tmp == null 
				    || int.Parse (tmp.Substring (0, 2)) != EA_VERSION
				    || tmp.Substring (3) != index_fingerprint)
					return null;

				FileAttributes attr = new FileAttributes ();
				
				attr.UniqueId = ExtendedAttribute.Get (path, unique_id_attr);
				attr.Path = ExtendedAttribute.Get (path, path_attr);
				attr.LastWriteTime = StringFu.StringToDateTime (ExtendedAttribute.Get (path, last_mtime_attr));
				
				attr.LastIndexedTime = StringFu.StringToDateTime (ExtendedAttribute.Get (path, last_indexed_attr));
				tmp = ExtendedAttribute.Get (path, filter_attr);
				if (tmp != null) {
					attr.FilterVersion = int.Parse (tmp.Substring (0, 3));
					attr.FilterName = tmp.Substring (4);
				}
				
				return attr;

			} catch (Exception e) {
				Logger.Log.Debug ("Caught exception reading EAs from {0}", path);
				Logger.Log.Debug (e);
				// FIXME: Do something smarter with the exception.
				return null;
			}
		}

		public bool Write (FileAttributes attr)
		{
			if (Disable)
				return false;

			try {
				string tmp;
				
				tmp = String.Format ("{0:00} {1}", EA_VERSION, index_fingerprint);
				ExtendedAttribute.Set (attr.Path, fingerprint_attr, tmp);

				ExtendedAttribute.Set (attr.Path, unique_id_attr, attr.UniqueId);
				ExtendedAttribute.Set (attr.Path, path_attr, attr.Path);
				ExtendedAttribute.Set (attr.Path, last_mtime_attr,
						       StringFu.DateTimeToString (attr.LastWriteTime));

				ExtendedAttribute.Set (attr.Path, last_indexed_attr,
						       StringFu.DateTimeToString (attr.LastIndexedTime));
				if (attr.HasFilterInfo)
					ExtendedAttribute.Set (attr.Path, filter_attr,
							       String.Format ("{0:000} {1}", attr.FilterVersion, attr.FilterName));
				
				return true;
			} catch (IOException e) {
				// An IOException here probably means that we don't have the right
				// permissions to set the EAs.  We just fail silently and return false rather
				// than spewing a bunch of scary exceptions.
				return false;
			} catch (Exception e) {
				Logger.Log.Debug ("Caught exception writing EAs to {0}", attr.Path);
				Logger.Log.Debug (e);
				// FIXME: Do something smarter with the exception.
				return false;
			}
		}

		public void Drop (string path)
		{
			if (Disable)
				return;

			try {
				ExtendedAttribute.Remove (path, fingerprint_attr);
				ExtendedAttribute.Remove (path, unique_id_attr);
				ExtendedAttribute.Remove (path, path_attr);
				ExtendedAttribute.Remove (path, last_mtime_attr);
				ExtendedAttribute.Remove (path, last_indexed_attr);
				ExtendedAttribute.Remove (path, filter_attr);

			} catch (Exception e) {
				// FIXME: Do something smarter with the exception.
			}
		}

	}
}
