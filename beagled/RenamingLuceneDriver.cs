//
// RenamingLuceneDriver.cs
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

using System;
using System.Collections;
using System.IO;

using Beagle.Util;

namespace Beagle.Daemon {
	
	public class RenamingLuceneDriver : IIndexer {

		private LuceneDriver driver;
		private NameIndex name_index;
		private ArrayList renamed_uris = new ArrayList ();

		public RenamingLuceneDriver (string index_name, int minor_version)
		{
			this.driver = new LuceneDriver (index_name, minor_version);
			this.name_index = new NameIndex (this.driver.IndexDirectory,
							 this.driver.Fingerprint);

			this.driver.ChangedEvent += OnLuceneDriverChanged;
		}

		// We assume that
		// (a) indexable.Uri is a uid: URI
		// (b) indexable.ContentUri is a file: URI
		// (c) indexable.ContentUri's filename is meaningful (i.e. isn't a temporary file)
		public void Add (Indexable indexable)
		{
			driver.Add (indexable);
			
			Guid uid = GuidFu.FromUri (indexable.Uri);
			string name = Path.GetFileName (indexable.ContentUri.LocalPath);
			name_index.Add (uid, name);
		}

		// We assume that uri is uid: URI
		public void Remove (Uri uri)
		{
			driver.Remove (uri);

			Guid uid = GuidFu.FromUri (uri);
			name_index.Remove (uid);
		}

		// We assume that
		// (a) old_uri is a uid: URI
		// (b) new_uri is a file: URI
		// (c) new_uri's filename is meaningful
		public void Rename (Uri old_uri, Uri new_uri)
		{
			Guid uid = GuidFu.FromUri (old_uri);
			string name = Path.GetFileName (new_uri.LocalPath);
			name_index.Add (uid, name);
			renamed_uris.Add (old_uri);
			renamed_uris.Add (new_uri);
		}

		static object [] empty_collection = new object [0];

		public void Flush ()
		{
			// FIXME: We should add some paranoid checking here.
			// If one flush succeeds and the other fails, our two
			// indexes will be out of sync!
			name_index.Flush ();
			driver.Flush ();

			// FIXME: If necessary, fire the ChangedEvent to give
			// notification of any renames.
			if (renamed_uris.Count > 0) {
				if (ChangedEvent != null) {
					ChangedEvent (this,
						      empty_collection,
						      empty_collection,
						      renamed_uris);
				}
				renamed_uris.Clear ();
			}
		}

		public int GetItemCount ()
		{
			return driver.GetItemCount ();
		}

		public event IIndexerChangedHandler ChangedEvent;

		public void OnLuceneDriverChanged (IIndexer    source,
						   ICollection list_of_added_uris,
						   ICollection list_of_removed_uris,
						   ICollection list_of_renamed_uris)
		{
			// Since we are proxying events from the LuceneDriver, there
			// will never been any renamed uris.  Thus it is safe to
			// substitute our own internal list of renamed uris.
			if (ChangedEvent != null) {
				ChangedEvent (this,
					      list_of_added_uris,
					      list_of_removed_uris,
					      renamed_uris);
			}
			renamed_uris.Clear ();
		}

	}
}
