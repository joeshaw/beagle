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

#define DBUS_IS_BROKEN_BROKEN_BROKEN

using System;
using System.Collections;
using System.IO;
using System.Threading;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.IndexHelper {

	public class RemoteIndexerImpl : Beagle.Daemon.RemoteIndexerProxy {

		string name;
		IIndexer indexer;
		NextFlush next_flush;
		bool is_open;
		
		public override event ChangedHandler ChangedEvent;
		public override event FlushCompleteHandler FlushCompleteEvent;

		public RemoteIndexerImpl (string name, IIndexer indexer)
		{
			this.name = name;
			this.indexer = indexer;
			if (indexer != null) {
				indexer.ChangedEvent += OnIndexerChanged;
			}

			next_flush = NewNextFlush ();
		}

		private void OnIndexerChanged (IIndexer source,
					       ICollection list_of_added_uris,
					       ICollection list_of_removed_uris)
		{
			this.ChangedEvent (UriFu.UrisToString (list_of_added_uris),
					   UriFu.UrisToString (list_of_removed_uris));
		}

		/////////////////////////////////////////////////////////////////////////////////////

		private class NextFlush {
			public RemoteIndexerImpl Impl;
			public IIndexer Indexer;
			public ArrayList ToBeAdded = new ArrayList ();
			public ArrayList ToBeRemoved = new ArrayList ();

#if DBUS_IS_BROKEN_BROKEN_BROKEN
			private bool FlushCompleteIdleHandler ()
			{
				Impl.FlushComplete ();
				return false;
			}
#endif

			public void DoFlush ()
			{
				foreach (Indexable indexable in ToBeAdded)
					Indexer.Add (indexable);
				foreach (Uri uri in ToBeRemoved)
					Indexer.Remove (uri);
				Indexer.Flush ();
#if DBUS_IS_BROKEN_BROKEN_BROKEN
				GLib.Idle.Add (new GLib.IdleHandler (FlushCompleteIdleHandler));
#else
				Impl.FlushComplete ();
#endif
			}
		}

		private NextFlush NewNextFlush ()
		{
			NextFlush next = new NextFlush ();
			next.Impl = this;
			next.Indexer = this.indexer;
			return next;
		}

		public void FlushComplete ()
		{
			Logger.Log.Debug ("Flush Complete!");
			FlushCompleteEvent ();
		}
		

		/////////////////////////////////////////////////////////////////////////////////////

		// FIXME: We should reject calls to NewRemoteIndexerPath from
		// anyone except for the process that started us.

		static Hashtable remote_indexer_cache = new Hashtable ();

		override public string NewRemoteIndexerPath (string name)
		{
			string path = Path.Combine (IndexHelperTool.IndexPathPrefix, name);
			if (! remote_indexer_cache.Contains (name)) {
				LuceneDriver driver = new LuceneDriver (name);
				RemoteIndexerImpl impl = new RemoteIndexerImpl (name, driver);
				remote_indexer_cache [name] = impl;
				Beagle.Daemon.DBusisms.RegisterObject (impl, path);
			}
			return path;
		}

		static public void UnconditionallyCloseAll ()
		{
			Logger.Log.Debug ("RemoteIndexerImpl.UnconditionallyCloseAll");
			foreach (RemoteIndexerImpl impl in remote_indexer_cache.Values)
				impl.Close ();
		}

		/////////////////////////////////////////////////////////////////////////////////////

		override public bool Open ()
		{
			lock (this) {
				Logger.Log.Debug ("Open!");
				if (is_open)
					return true; // FIXME: Is this right?  Does it matter?
				is_open = true;
				return Shutdown.WorkerStart (this, name);
			}
		}

		override public void Close ()
		{
			lock (this) {
				Logger.Log.Debug ("Close!");
				if (is_open) {
					Shutdown.WorkerFinished (this);
					is_open = false;
				}
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////

		override public void Add (string indexable_as_xml)
		{
			if (indexer != null) {
				Indexable indexable = FilteredIndexable.NewFromEitherXml (indexable_as_xml);
				next_flush.ToBeAdded.Add (indexable);
			}
		}

		override public void Remove (string uri_as_str)
		{
			if (indexer != null) {
				Uri uri = new Uri (uri_as_str, true);
				next_flush.ToBeRemoved.Add (uri);
			}
		}
		
		override public void Flush ()
		{
			if (indexer != null) {
				NextFlush this_flush = next_flush;
				next_flush = NewNextFlush ();
				indexer.Flush ();
				Logger.Log.Debug ("Launching flush thread!");
				Thread th = new Thread (new ThreadStart (this_flush.DoFlush));
				th.Start ();
			}
		}

	}

}
