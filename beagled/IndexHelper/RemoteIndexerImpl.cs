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
		NameIndex name_index;
		NextFlush next_flush;
		bool is_open;
		bool queued_close;
		bool safe_to_close;

		public bool is_flushing; // FIXME: shouldn't really be public
		
		public override event ChangedHandler ChangedEvent;
		public override event FlushCompleteHandler FlushCompleteEvent;

		static object [] empty_collection = new object [0];

		public RemoteIndexerImpl (string name, IIndexer indexer, NameIndex name_index)
		{
			this.name = name;
			this.indexer = indexer;
			this.name_index = name_index;

			if (indexer != null) {
				indexer.ChangedEvent += OnIndexerChanged;
			}

			next_flush = NewNextFlush ();
		}
		
		public string Name { get { return name; } }

#if DBUS_IS_BROKEN_BROKEN_BROKEN
		private class HoistChanged {
			public RemoteIndexerImpl Sender;
			public string AddedUris;
			public string RemovedUris;
			public string RenamedUris;

			private bool IdleHandler ()
			{
				try {
					Sender.ChangedEvent (AddedUris, RemovedUris, RenamedUris);
				} catch (Exception ex) {
					Logger.Log.Debug ("Caught exception in RemoteIndexerImpl.HoistChanged.IdleHandler"); 
					Logger.Log.Debug (ex);
				}
				return false;
			}

			public void Run ()
			{
				GLib.Idle.Add (new GLib.IdleHandler (IdleHandler));
			}
		}
#endif

		public void OnIndexerChanged (IIndexer source,
					      ICollection list_of_added_uris,
					      ICollection list_of_removed_uris,
					      ICollection list_of_renamed_uris)
		{
			string added_uris_str = UriFu.UrisToString (list_of_added_uris);
			string removed_uris_str = UriFu.UrisToString (list_of_removed_uris);
			string renamed_uris_str = UriFu.UrisToString (list_of_renamed_uris);

#if DBUS_IS_BROKEN_BROKEN_BROKEN
			HoistChanged hoist = new HoistChanged ();
			hoist.Sender = this;
			hoist.AddedUris = added_uris_str;
			hoist.RemovedUris = removed_uris_str;
			hoist.RenamedUris = renamed_uris_str;
			hoist.Run ();
#else
			this.ChangedEvent (added_uris_str, removed_uris_str, renamed_uris_str);
#endif
		}

		/////////////////////////////////////////////////////////////////////////////////////

		private class NextFlush {
			public RemoteIndexerImpl Impl;
			public IIndexer Indexer;
			public NameIndex NameIndex;
			public ArrayList ToBeAdded = new ArrayList ();
			public ArrayList ToBeRemoved = new ArrayList ();

			// pseudo-FIXME:
			// Right now we can only handle one rename at a time
			public Uri OldUri;
			public Uri NewUri;

#if DBUS_IS_BROKEN_BROKEN_BROKEN
			private bool FlushCompleteIdleHandler ()
			{
				try {
					Impl.FlushComplete ();
					Impl.CloseIfQueued ();
				} catch (Exception ex) {
					Logger.Log.Warn ("Caught exception in FlushCompleteIdleHandler for '{0}'", Impl.Name);
					Logger.Log.Warn (ex);
				}

				return false;
			}
#endif

			public void AddToBeRenamed (Uri old_uri, Uri new_uri)
			{
				if (OldUri != null || NewUri != null) {
					Logger.Log.Error ("Called NextFlush.AddToBeRenamed twice on the same object!");
					return;
				}

				OldUri = old_uri;
				NewUri = new_uri;
			}

			private void RealDoFlush ()
			{
				Impl.is_flushing = true;

				if (OldUri != null && NewUri != null) {

					// This is the code for dealing with renames
					
					if (NameIndex != null
					    && OldUri.Scheme == "uid"
					    && NewUri.IsFile) {
						Guid uid = GuidFu.FromUri (OldUri);
						string name = Path.GetFileName (NewUri.LocalPath);
						NameIndex.Add (uid, name);
					}

				} else {

					// ...and this is for adds and removals

					foreach (Indexable indexable in ToBeAdded) {
						Indexer.Add (indexable);
						if (NameIndex != null
						    && indexable.Uri.Scheme == "uid"
						    && indexable.ContentUri.IsFile) {
							Guid uid = GuidFu.FromUri (indexable.Uri);
							string name = Path.GetFileName (indexable.ContentUri.LocalPath);
							NameIndex.Add (uid, name);
						}
					}
					
					foreach (Uri uri in ToBeRemoved) {
						Indexer.Remove (uri);
						if (NameIndex != null && uri.Scheme == "uid") {
							Guid uid = GuidFu.FromUri (uri);
							NameIndex.Remove (uid);
						}
					}
					
					Indexer.Flush ();

				}

				if (NameIndex != null)
					NameIndex.Flush ();
				
				if (OldUri != null && NewUri != null) {
					Uri [] renamed_uris = new Uri [2];
					renamed_uris [0] = OldUri;
					renamed_uris [1] = NewUri;

					// Fire the change notification
					Impl.OnIndexerChanged (null, empty_collection, empty_collection, renamed_uris);
				}

				Impl.is_flushing = false;

				if (Impl.CloseIfQueued ())
					return;
#if DBUS_IS_BROKEN_BROKEN_BROKEN
				GLib.Idle.Add (new GLib.IdleHandler (FlushCompleteIdleHandler));
#else
				Impl.FlushComplete ();
#endif
			}

			public void DoFlush ()
			{
				try {
					RealDoFlush ();
				} catch (Exception ex) {
					Logger.Log.Warn ("Caught exception while flushing '{0}'", Impl.Name);
					Logger.Log.Warn (ex);
				}
			}
		}

		private NextFlush NewNextFlush ()
		{
			NextFlush next = new NextFlush ();
			next.Impl = this;
			next.Indexer = this.indexer;
			next.NameIndex = this.name_index;
			return next;
		}

		public void FlushComplete ()
		{
			Logger.Log.Debug ("Flush Complete!");
			FlushCompleteEvent ();
			CloseIfQueued ();
		}
		

		/////////////////////////////////////////////////////////////////////////////////////

		static Hashtable remote_indexer_cache = new Hashtable ();

		override public string NewRemoteIndexerPath (string name)
		{
			if (! IndexHelperTool.CheckSenderID (DBus.Message.Current.Sender)) {
				Logger.Log.Error ("NewRemoteIndexerPath: Rejected request from {0}",
				                  DBus.Message.Current.Sender);
				return null;
			}

			string path = Path.Combine (IndexHelperTool.IndexPathPrefix, name);
			if (! remote_indexer_cache.Contains (name)) {

				LuceneDriver driver = new LuceneDriver (name);

				NameIndex new_name_index = null;
				// A hack: if this is the FileSystemIndex, create a new NameIndex.
				if (name == "FileSystemIndex") {
					string dir = Path.Combine (PathFinder.StorageDir, name);
					new_name_index = new NameIndex (dir, driver.Fingerprint);
				}
				
				RemoteIndexerImpl impl = new RemoteIndexerImpl (name, driver, new_name_index);
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
					Logger.Log.Debug ("Actually Closed on CloseIfQueued on {0}", name);
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
				Uri uri = UriFu.UriStringToUri (uri_as_str);
				next_flush.ToBeRemoved.Add (uri);
			}
		}

		override public void Rename (string old_uri_as_str, string new_uri_as_str)
		{
			Uri old_uri = UriFu.UriStringToUri (old_uri_as_str);
			Uri new_uri = UriFu.UriStringToUri (new_uri_as_str);

			if (name_index != null) {
				Logger.Log.Debug ("RENAME {0} => {1}", old_uri, new_uri);
				next_flush.AddToBeRenamed (old_uri, new_uri);
			} else {
				Logger.Log.Error ("Called RemoteIndexerImpl.Rename when name_index == null");
				Logger.Log.Error ("old_uri={0}, new_uri={1}", old_uri, new_uri);
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

		override public bool IsFlushing ()
		{
			return is_flushing;
		}

		override public int GetItemCount ()
		{
			return indexer != null ? indexer.GetItemCount () : -1;
		}

	}

}
