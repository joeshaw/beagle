//
// FileAttributesStore_Mixed.cs
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

namespace Beagle.Daemon {
	
	public class FileAttributesStore_Mixed : IFileAttributesStore {

		FileAttributesStore_ExtendedAttribute store_ea;
		FileAttributesStore_Sqlite store_sqlite;

		public FileAttributesStore_Mixed (string directory, string index_fingerprint)
		{
			store_ea = new FileAttributesStore_ExtendedAttribute (index_fingerprint);
			store_sqlite = new FileAttributesStore_Sqlite (directory, index_fingerprint);
		}

		// We always have to query the Sqlite store first, because of our
		// EA nightmare scenario: a file whose permissions or ownership get
		// changed after the EAs have been attached.  Thus attributes in
		// the database always trump those found in EAs.
		//
		// FIXME: If we have write access to the path but it has attributes
		// stored in the sqlite file attributes db, we should attach them
		// to the file with EAs and delete the record from the db.

		public FileAttributes Read (string path)
		{
			FileAttributes attr;
			attr = store_sqlite.Read (path);
			if (attr == null)
				attr = store_ea.Read (path);
			return attr;
		}

		public bool Write (FileAttributes attr)
		{
			return store_ea.Write (attr) || store_sqlite.Write (attr);
		}

		public void Drop (string path)
		{
			store_ea.Drop (path);
			store_sqlite.Drop (path);
		}

	}
	
}
