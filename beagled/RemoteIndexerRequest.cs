//
// RemoteIndexerRequest.cs
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
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	public class RemoteIndexerRequest : RequestMessage {

		public string RemoteIndexName;
		public int    RemoteIndexMinorVersion;

		public bool   OptimizeIndex = false;

		ArrayList indexables_to_add = new ArrayList ();
		ArrayList uris_to_remove = new ArrayList ();

		public RemoteIndexerRequest () : base ("socket-helper")
		{
		}

		public void Add (Indexable indexable)
		{
			indexables_to_add.Add (indexable);
		}

		public void Remove (Uri uri)
		{
			uris_to_remove.Add (uri);
		}

		[XmlArrayItem (ElementName="Indexable", Type=typeof(Indexable))]
		[XmlArray (ElementName="ToAdd")]
		public ArrayList ToAdd {
			get { return indexables_to_add; }
		}

		[XmlAttribute ("ToRemove")]
		public string ToRemoveString {
			get { return UriFu.UrisToString (uris_to_remove); }
			set { 
				uris_to_remove = new ArrayList ();
				uris_to_remove.AddRange (UriFu.StringToUris (value));
			}
		}

		[XmlIgnore]
		public bool IsEmpty {
			get { return indexables_to_add.Count == 0 
				      && uris_to_remove.Count == 0
				      && ! OptimizeIndex; }
		}

		////////////////////////////////////////////////////////////////////////////

		public IndexerReceipt [] Process (IIndexer indexer)
		{
			foreach (Indexable indexable in indexables_to_add) 
				indexer.Add (indexable);

			foreach (Uri uri in uris_to_remove) 
				indexer.Remove (uri);

			if (OptimizeIndex)
				indexer.Optimize ();

			return indexer.FlushAndBlock ();
		}
	}
}
