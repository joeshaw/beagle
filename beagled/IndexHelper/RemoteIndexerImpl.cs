//
// RemoteIndexerImpl.cs
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

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.IndexHelper {

	public class RemoteIndexerImpl : Beagle.Daemon.RemoteIndexerProxy {

		IIndexer indexer;

		public RemoteIndexerImpl (IIndexer indexer)
		{
			this.indexer = indexer;
		}

		override public void Add (string indexable_as_xml)
		{
			Indexable indexable = Indexable.NewFromXml (indexable_as_xml);
			indexer.Add (indexable);
		}

		override public void Remove (string uri_as_str)
		{
			Uri uri = new Uri (uri_as_str, false);
			indexer.Remove (uri);
		}
		
		override public void Flush ()
		{
			indexer.Flush ();
		}

	}

}
