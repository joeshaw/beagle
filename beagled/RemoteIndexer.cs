//
// RemoteIndexer.cs
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
using System.Threading;
using DBus;

using Beagle.Util;

namespace Beagle.Daemon {

	public class RemoteIndexer : IIndexer {

		static public bool Debug = true;

		static private TimeSpan one_second = new TimeSpan (10000000);

		string remote_index_name;
		RemoteIndexerProxy proxy;
		bool flush_complete;
		object flush_lock = new object ();
		int add_remove_count;

		public event IIndexerChangedHandler ChangedEvent;

		static public IIndexer NewRemoteIndexer (string name)
		{
			return new RemoteIndexer (name);
		}

		public RemoteIndexer (string name)
		{
			this.remote_index_name = name;
			this.proxy = null;
		}

		private RemoteIndexerProxy Proxy {
			get {
				lock (this) {
					if (proxy == null && ! Shutdown.ShutdownRequested) {
						if (Debug)
							Logger.Log.Debug ("Requesting new proxy");
						proxy = IndexHelperFu.NewRemoteIndexerProxy (remote_index_name);
						if (proxy != null) {
							proxy.ChangedEvent += OnProxyChanged;
							proxy.FlushCompleteEvent += OnFlushComplete;
							add_remove_count = 0;
						}
					}
					
					return proxy;
				}
			}
		}

		private void UnsetProxy ()
		{
			lock (this) {
				if (proxy != null) {
					if (Debug)
						Logger.Log.Debug ("Unsetting proxy");
					proxy.ChangedEvent -= OnProxyChanged;
					proxy.FlushCompleteEvent -= OnFlushComplete;
					proxy = null;
				}
			}
		}

			

#if DBUS_IS_BROKEN_BROKEN_BROKEN
		private class UpToTheMainLoop {
			RemoteIndexerProxy proxy;
			int magic_code;
			string data;
			string data2;
			bool finished;

			static public UpToTheMainLoop NewAdd (RemoteIndexerProxy proxy, Indexable indexable)
			{
				UpToTheMainLoop up = new UpToTheMainLoop ();
				up.proxy = proxy;
				up.magic_code = 0;
				up.data = indexable.ToString ();
				return up;
			}

			static public UpToTheMainLoop NewRemove (RemoteIndexerProxy proxy, Uri uri)
			{
				UpToTheMainLoop up = new UpToTheMainLoop ();
				up.proxy = proxy;
				up.magic_code = 1;
				up.data = UriFu.UriToSerializableString (uri);
				return up;
			}

			static public UpToTheMainLoop NewFlush (RemoteIndexerProxy proxy)
			{
				UpToTheMainLoop up = new UpToTheMainLoop ();
				up.proxy = proxy;
				up.magic_code = 2;
				return up;
			}

			static public UpToTheMainLoop NewClose (RemoteIndexerProxy proxy)
			{
				UpToTheMainLoop up = new UpToTheMainLoop ();
				up.proxy = proxy;
				up.magic_code = 3;
				return up;
			}

			static public UpToTheMainLoop NewRename (RemoteIndexerProxy proxy, Uri old_uri, Uri new_uri)
			{
				UpToTheMainLoop up = new UpToTheMainLoop ();
				up.proxy = proxy;
				up.magic_code = 4;
				up.data = UriFu.UriToSerializableString (old_uri);
				up.data2 = UriFu.UriToSerializableString (new_uri);
				return up;
			}

			private bool IdleHandler ()
			{
				lock (this) {
					if (! Shutdown.ShutdownRequested) {

						try {
							switch (magic_code) {
							case 0:
								proxy.Add (data);
								break;
							case 1:
								proxy.Remove (data);
								break;
							case 2:
								proxy.Flush ();
								break;
							case 3:
								proxy.Close ();
								break;
								
							case 4:
								proxy.Rename (data, data2);
								break;
							}
						} catch (Exception ex) {
							Logger.Log.Debug ("Caught exception in RemoteIndexer.IdleHandler");
							Logger.Log.Debug (ex);
						}
					}

					finished = true;
					Monitor.Pulse (this);
				}
				return false;
			}

			public bool Run ()
			{
				lock (this) {
					// If the proxy is null, it means the index helper
					// disconnected unexpectedly --- this can happen
					// with scheduled events on daemon shutdown, so we
					// just silently return.
					if (proxy == null)
						return false;

					GLib.IdleHandler idle_handler = new GLib.IdleHandler (IdleHandler);
					GLib.Idle.Add (idle_handler);
					finished = false;
					while (! finished) {
						if (Debug)
							Logger.Log.Debug ("Waiting code={0}", magic_code);
						// Bale out if a shutdown request has come in.
						if (Shutdown.ShutdownRequested) {
							if (Debug)
								Logger.Log.Debug ("Bailing out while waiting on code={0}", magic_code);
							return false;
						}
						Monitor.Wait (this, one_second);
					}
				}

				return true;
			}
			
		}
#endif

		public void Add (Indexable indexable)
		{
			indexable.StoreStream ();
			
#if DBUS_IS_BROKEN_BROKEN_BROKEN
			UpToTheMainLoop up = UpToTheMainLoop.NewAdd (Proxy, indexable);
			if (up.Run ())
				++add_remove_count;
#else
			RemoteIndexerProxy p = Proxy;
			if (p != null) {
				p.Add (indexable.ToString ());
				++add_remove_count;
			}
#endif
		}

		public void Remove (Uri uri)
		{
#if DBUS_IS_BROKEN_BROKEN_BROKEN
			UpToTheMainLoop up = UpToTheMainLoop.NewRemove (Proxy, uri);
			if (up.Run ())
				++add_remove_count;
#else
			RemoteIndexerProxy p = Proxy;
			if (p != null) {
				p.Remove (UriFu.UriToSerializableString (uri));
				++add_remove_count;
			}
#endif
		}

		public void Rename (Uri old_uri, Uri new_uri)
		{
#if DBUS_IS_BROKEN_BROKEN_BROKEN
			UpToTheMainLoop up = UpToTheMainLoop.NewRename (Proxy, old_uri, new_uri);
			if (up.Run ())
				++add_remove_count;
#else
			RemoteIndexerProxy p = Proxy;
			if (p != null) {
				p.Rename (UriFu.UriToSerializableString (old_uri),
					  UriFu.UriToSerializableString (new_uri));
				++add_remove_count;
			}
#endif
		}

		// This can only be called from the main thread!
		private void PostFlushComplete ()
		{
			RemoteIndexerProxy p = Proxy;
			if (p != null) {
				Logger.Log.Debug ("Calling close on proxy '{0}'", remote_index_name);
				p.Close ();
			}
			
			Logger.Log.Debug ("Unsetting '{0}'", remote_index_name);
			UnsetProxy ();
		}

#if DBUS_IS_BROKEN_BROKEN_BROKEN

		private uint flush_timeout_handler = 0;

		private bool FlushTimeoutHandler ()
		{
			Logger.Log.Debug ("Checking status of FlushTimeoutHandler for '{0}'", remote_index_name);
			// We know we are in main loop, so we can make any
			// dbus call we want to.
			if (! Proxy.IsFlushing ()) {
				lock (flush_lock) {
					if (! flush_complete) {
						Logger.Log.Debug ("No longer flushing on '{0}', setting flush_complete", remote_index_name);
						flush_complete = true;
						PostFlushComplete ();
					}
					flush_timeout_handler = 0;
				}
				return false;
			}
			return true;
		}
#endif

		public void Flush ()
		{
			Logger.Log.Debug ("RemoteIndexer.Flush");

			if (add_remove_count == 0) {
#if DBUS_IS_BROKEN_BROKEN_BROKEN
				UpToTheMainLoop up = UpToTheMainLoop.NewClose (Proxy);
				up.Run ();
#else
				RemoteIndexerProxy p = Proxy;
				if (p != null)
					p.Close ();
#endif
				UnsetProxy ();
				return;
			}

			lock (flush_lock) {
#if DBUS_IS_BROKEN_BROKEN_BROKEN
				UpToTheMainLoop up = UpToTheMainLoop.NewFlush (Proxy);
				up.Run ();
#else
				RemoteIndexerProxy p = Proxy;
				p.Flush ();
#endif
				// Wait for the flush complete signal, but bail out
				// if a shutdown request comes through.
				flush_complete = false;
				flush_timeout_handler = GLib.Timeout.Add (1000, new GLib.TimeoutHandler (FlushTimeoutHandler));
				while (true) {

					Logger.Log.Debug ("Waiting for flush to complete on '{0}'", remote_index_name);
					if (Shutdown.ShutdownRequested) {
						if (Debug)
							Logger.Log.Debug ("Bailing out while waiting for flush on '{0}'", remote_index_name);
						break;
					}

					lock (flush_lock) {
						if (flush_complete)
							break;
						Monitor.Wait (flush_lock, one_second);
						if (flush_complete)
							break;
					}
				}
				if (flush_timeout_handler != 0) {
					GLib.Source.Remove (flush_timeout_handler);
					flush_timeout_handler = 0;
				}
			}
		}

		public int GetItemCount ()
		{
			RemoteIndexerProxy p = Proxy;
			return p != null ? p.GetItemCount () : -1;
		}

		private void OnProxyChanged (string list_of_added_uris_as_str,
					     string list_of_removed_uris_as_str,
					     string list_of_renamed_uris_as_str)
		{
			if (ChangedEvent != null)
				ChangedEvent (this,
					      UriFu.StringToUris (list_of_added_uris_as_str),
					      UriFu.StringToUris (list_of_removed_uris_as_str),
					      UriFu.StringToUris (list_of_renamed_uris_as_str));
		}

		private void OnFlushComplete ()
		{
			lock (flush_lock) {
				Logger.Log.Debug ("Got flush complete event from proxy '{0}'", remote_index_name);

				if (! flush_complete)
					PostFlushComplete ();
				flush_complete = true;
				
				// Since this event is dispatched by d-bus, we are guaranteed
				// to be in the main loop's thread.  Thus we don't have to
				// jump through the same hoops as we did above.
				Monitor.Pulse (flush_lock);
			}
		}

	}
	

}
