//
// IndexerRequest.cs
//
// Copyright (C) 2007 Debajyoti Bera <dbera.web@gmail.com>
// Copyright (C) 2005-2006 Novell, Inc.
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
using System.Collections.Generic;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	public class IndexerRequest {

		public bool OptimizeIndex = false;

		private ArrayList indexables = null;
		// The hashtable is needed to remove/merge multiple indexables for the same uri
		private Hashtable indexables_by_uri = null;

		// Used to uniquely determine any indexable
		private static int indexable_id = 0;
		private int base_id;

		public IndexerRequest ()
		{
			base_id = indexable_id;
			indexables = new ArrayList ();
		}

		public void Clear ()
		{
			OptimizeIndex = false;
			indexables_by_uri = null;
			base_id = indexable_id;
			indexables.Clear ();
		}

		public void Add (Indexable indexable)
		{
			if (indexable == null)
				return;

			if (indexables_by_uri == null)
				indexables_by_uri = UriFu.NewHashtable ();

			Indexable prior;
			prior = indexables_by_uri [indexable.Uri] as Indexable;

			if (prior != null) {
				
				switch (indexable.Type) {

				case IndexableType.Add:
				case IndexableType.Remove:
				case IndexableType.Ignore:
					// Clobber the prior indexable.
					indexable.Id = prior.Id;
					indexables [prior.Id - base_id] = indexable;
					indexables_by_uri [indexable.Uri] = indexable;
					break;

				case IndexableType.PropertyChange:
					if (prior.Type != IndexableType.Remove &&
					    prior.Type != IndexableType.Ignore) {
						// Merge with the prior indexable.
						prior.Merge (indexable);
					}
					break;
				}
			} else {
				indexable.Id = indexable_id;
				indexable_id ++;
				indexables.Add (indexable);
				indexables_by_uri [indexable.Uri] = indexable;
			}
		}

		public Indexable GetRequestIndexable (IndexerReceipt r)
		{
			int id = r.Id;

			if (id < base_id || id >= indexable_id)
				return null;

			return (Indexable) indexables [id - base_id];
		}

		[XmlArray (ElementName="Indexables")]
		[XmlArrayItem (ElementName="Indexable", Type=typeof (Indexable))]
		public ArrayList Indexables {
			get {
				indexables_by_uri = null;
				return indexables;
			}
			set { indexables = value; }
		}

		[XmlIgnore]
		public int Count {
			get { return indexables.Count; }
		}
		
		[XmlIgnore]
		public bool IsEmpty {
			get { return indexables.Count == 0 && ! OptimizeIndex; }
		}

		public void Cleanup ()
		{
			foreach (Indexable i in indexables)
				i.Cleanup ();
		}
	}
}
