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
using System.Collections;
using System.IO;
using System.Threading;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.IndexHelper {

	public class RemoteIndexerImpl : Beagle.Daemon.RemoteIndexerProxy {

		static int close_count = 0;
		static public int CloseCount { get { return close_count; } }

		string name;
		IIndexer indexer;
		NextFlush next_flush;
		bool is_open;
		bool queued_close;
		bool safe_to_close;
		
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

#if DBUS_IS_BROKEN_BROKEN_BROKEN
		private class HoistChanged {
			public RemoteIndexerImpl Sender;
			public string AddedUris;
			public string RemovedUris;

			private bool IdleHandler ()
			{
				Sender.ChangedEvent (AddedUris, RemovedUris);
				return false;
			}

			public void Run ()
			{
				GLib.Idle.Add (new GLib.IdleHandler (IdleHandler));
			}
		}
#endif

		private void OnIndexerChanged (IIndexer source,
					       ICollection list_of_added_uris,
					       ICollection list_of_removed_uris)
		{
			string added_uris_str = UriFu.UrisToString (list_of_added_uris);
			string removed_uris_str = UriFu.UrisToString (list_of_removed_uris);

#if DBUS_IS_BROKEN_BROKEN_BROKEN
			HoistChanged hoist = new HoistChanged ();
			hoist.Sender = this;
			hoist.AddedUris = added_uris_str;
			hoist.RemovedUris = removed_uris_str;
			hoist.Run ();
#else
			this.ChangedEvent (added_uris_str, removed_uris_str);
#endif
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
				Impl.CloseIfQueued ();
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
				if (Impl.CloseIfQueued ())
					return;
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
			CloseIfQueued ();
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

		static public void QueueCloseForAll ()
		{
			Logger.Log.Debug ("RemoteIndexerImpl.QueueCloseForAll called");
			foreach (RemoteIndexerImpl impl in remote_indexer_cache.Values)
				impl.QueueClose ();
		}

		/////////////////////////////////////////////////////////////////////////////////////

		override public bool Open ()
		{
			lock (this) {

				// We allow the item to be opened multiple times,
				// but close should only be called once.
				if (is_open)
					return true; 

				is_open = true;
				safe_to_close = true;

				return Shutdown.WorkerStart (this, name);
			}
		}

		private void CloseUnlocked ()
		{
			Logger.Log.Debug ("Close on {0}", name);
			if (is_open) {
				Shutdown.WorkerFinished (this);
				is_open = false;
				queued_close = false;
				++close_count;
			}
		}

		override public void Close ()
		{
			lock (this)
				CloseUnlocked ();
		}

		private void QueueClose ()
		{
			lock (this) {
				if (safe_to_close) {
					Logger.Log.Debug ("Safe-to-close QueueClosed on {0}", name);
					CloseUnlocked ();
				} else if (is_open) {
					Logger.Log.Debug ("QueueClosed on {0}", name);
					queued_close = true;
				}
			}
		}

		private bool CloseIfQueued ()
		{
			lock (this) {
				Logger.Log.Debug ("CloseIfQueued on {0}", name);
				safe_to_close = true;
				if (queued_close) {
					Logger.Log.Debug ("Actuallyed Closed on CloseIfQueued on {0}", name);
					CloseUnlocked ();
					return true;
				}
			}
			return false;
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
				safe_to_close = false;
				NextFlush this_flush = next_flush;
				next_flush = NewNextFlush ();
				indexer.Flush ();
				Logger.Log.Debug ("Launching flush thread!");
				Thread th = new Thread (new ThreadStart (this_flush.DoFlush));
				th.Start ();
			}
		}

		override public int GetItemCount ()
		{
			return indexer != null ? indexer.GetItemCount () : -1;
		}

	}

}
