//
// RemoteIndexerProxy.cs
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
using DBus;

namespace Beagle.Daemon {

	[Interface ("com.novell.BeagleIndexHelper.Indexer")]
	public abstract class RemoteIndexerProxy {

		[Method]
		public abstract string NewRemoteIndexerPath (string name);

		[Method]
		public abstract bool Open ();

		[Method]
		public abstract void Add (string indexable_as_xml);

		[Method]
		public abstract void Remove (string uri_as_str);

		[Method]
		public abstract void Rename (string old_uri_as_str, string new_uri_as_str);

		[Method]
		public abstract void Flush ();

		[Method]
		public abstract bool IsFlushing ();

		[Method]
		public abstract int GetItemCount ();

		[Method]
		public abstract void Close ();

		public delegate void ChangedHandler (string list_of_added_uris_as_str,
						     string list_of_removed_uris_as_str,
						     string list_of_renamed_uris_as_str);

		[Signal]
		public virtual event ChangedHandler ChangedEvent;

		public delegate void FlushCompleteHandler ();
		
		[Signal]
		public virtual event FlushCompleteHandler FlushCompleteEvent;
	}
	

}
