//
// RemoteIndexerExecutor.cs
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
using Beagle.Daemon;

namespace Beagle.IndexHelper {

	[RequestMessage (typeof (RemoteIndexerRequest))]
	public class RemoteIndexerExecutor : RequestMessageExecutor {

		static public int Count = 0;
		static Hashtable indexer_table = new Hashtable ();

		Indexable[] child_indexables;
		FilteredStatus[] uris_filtered;

		public override ResponseMessage Execute (RequestMessage raw_request)
		{
			RemoteIndexerRequest request = (RemoteIndexerRequest) raw_request;

			// Find the appropriate driver for this request.
			IIndexer indexer = indexer_table [request.RemoteIndexName] as IIndexer;
			if (indexer == null) {
				// FIXME: This shouldn't be hard-wired.
				if (request.RemoteIndexName == "FileSystemIndex")
					indexer = new RenamingLuceneDriver (request.RemoteIndexName,
									    request.RemoteIndexMinorVersion);
				else
					indexer = new LuceneDriver (request.RemoteIndexName,
								    request.RemoteIndexMinorVersion);
				indexer_table [request.RemoteIndexName] = indexer;
			}

			indexer.ChildIndexableEvent += new IIndexerChildIndexableHandler (ChildIndexableCallback);
			indexer.UrisFilteredEvent += new IIndexerUrisFilteredHandler (UrisFilteredCallback);

			request.Process (indexer);

			// Construct a response containing the item count and
			// the child indexables created by the filtering
			// process.
			RemoteIndexerResponse response = new RemoteIndexerResponse ();
			response.ItemCount = indexer.GetItemCount ();
			response.ChildIndexables = child_indexables;
			response.UrisFiltered = uris_filtered;

			++Count;

			return response;
		}

		private void ChildIndexableCallback (Indexable[] child_indexables)
		{
			this.child_indexables = child_indexables;
		}

		private void UrisFilteredCallback (FilteredStatus[] uris_filtered)
		{
			this.uris_filtered = uris_filtered;
		}
	}
}
