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
using System.Diagnostics;
using System.IO;
using System.Threading;

using Beagle;
using Beagle.Util;

namespace Beagle.Daemon {

	public class RemoteIndexer : IIndexer {

		static string helper_path;

		string remote_index_name;
		int last_item_count;

		RemoteIndexerRequest pending_request = new RemoteIndexerRequest ();
		object pending_request_lock = new object ();

		public event IIndexerChangedHandler ChangedEvent;

		static RemoteIndexer ()
		{
			string bihp = Environment.GetEnvironmentVariable ("_BEAGLED_INDEX_HELPER_PATH");
			if (bihp == null)
				throw new Exception ("_BEAGLED_INDEX_HELPER_PATH not set!");
			
			helper_path = Path.GetFullPath (Path.Combine (bihp, "beagled-index-helper"));
			if (! File.Exists (helper_path))
				throw new Exception ("Could not find " + helper_path);
			Logger.Log.Debug ("Found index helper at {0}", helper_path);
		}

		static public IIndexer NewRemoteIndexer (string name)
		{
			return new RemoteIndexer (name);
		}

		public RemoteIndexer (string name)
		{
			this.remote_index_name = name;
		}

		public void Add (Indexable indexable)
		{
			indexable.StoreStream ();
			lock (pending_request_lock)
				pending_request.Add (indexable);
		}

		public void Remove (Uri uri)
		{
			lock (pending_request_lock)
				pending_request.Remove (uri);
		}

		public void Rename (Uri old_uri, Uri new_uri)
		{
			lock (pending_request_lock)
				pending_request.Rename (old_uri, new_uri);
		}

		public void Flush ()
		{
			RemoteIndexerRequest flushed_request = null;
			lock (pending_request) {
				flushed_request = pending_request;
				pending_request = new RemoteIndexerRequest ();
			}

			RemoteIndexerResponse response;
			response = SendRequest (flushed_request);

			if (response == null) {
				Logger.Log.Error ("Something terrible happened --- Flush failed");
			} else {
				flushed_request.FireEvent (this, ChangedEvent);
			}
		}

		public int GetItemCount ()
		{
			if (last_item_count == -1) {
				// Send an empty indexing request to cause the last item count to be
				// initialized.
				RemoteIndexerRequest request = new RemoteIndexerRequest ();
				if (SendRequest (request) == null) 
					Logger.Log.Error ("Something terrible happened --- GetItemCount failed");
			}
			return last_item_count;
		}

		/////////////////////////////////////////////////////////

		private RemoteIndexerResponse SendRequest (RemoteIndexerRequest request)
		{
			RemoteIndexerResponse response = null;
			int exception_count = 0;
			bool start_helper_by_hand = false;

			if (Environment.GetEnvironmentVariable ("BEAGLE_RUN_HELPER_BY_HAND") != null)
				start_helper_by_hand = true;

			request.RemoteIndexName = remote_index_name;
			
			while (response == null
			       && exception_count < 5
				&& ! Shutdown.ShutdownRequested) {

				bool need_helper = false;

				Logger.Log.Debug ("Sending request!");
				try {
					response = request.Send () as RemoteIndexerResponse;
					Logger.Log.Debug ("Done sending request");
				} catch (ResponseMessageException ex) {
					Logger.Log.Debug ("Caught ResponseMessageException: {0}", ex.Message);
				} catch (System.Net.Sockets.SocketException ex) {
					Logger.Log.Debug ("Caught SocketException -- we probably need to launch a helper: {0}", ex.Message);
					need_helper = true;
				} catch (IOException ex) {
					Logger.Log.Debug ("Caught IOException --- we probably need to launch a helper: {0}", ex.Message);
					need_helper = true;
				}

				// If we caught an exception...
				if (response == null) {
					if (! start_helper_by_hand || ! need_helper)
						++exception_count;

					if (start_helper_by_hand) {
						// Sleep briefly before trying again.
						Thread.Sleep (1000);
					} else {
						// Try to activate the helper.
						LaunchHelper ();
					}
				}
			}

			if (response != null) {
				Logger.Log.Debug ("Got response!");
				last_item_count = response.ItemCount;
			} else if (exception_count >= 5) {
				Logger.Log.Error ("Exception limit exceeded trying to activate a helper.  Giving up on indexing!");
			}
	
			return response;
		}

		/////////////////////////////////////////////////////////

		static bool CheckHelper ()
		{
			// FIXME: We shouldn't need to know the path to the helper socket.
			string socket_name = Path.Combine (PathFinder.StorageDir, "socket-helper");

			if (! File.Exists (socket_name))
				return false;

			// Open, and then immediately close, a connection to the helper's socket.
			try {
				UnixClient test_client;
				test_client = new UnixClient (socket_name);
				test_client.Close ();
				return true;
			} catch (Exception ex) {
				return false;
			}
		}

		static object helper_lock = new object ();
		
		static void LaunchHelper ()
		{
			// If we are in the process of shutting down, return immediately.
			if (Shutdown.ShutdownRequested)
				return;

			lock (helper_lock) {

				// If a helper appears to be running, return immediately.
				if (CheckHelper ())
					return;
				
				Logger.Log.Debug ("Launching helper process");

				Process p = new Process ();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.FileName = helper_path;
				p.Start ();

				// Poll the helper's socket.  If the 
				int poll_count = 0;
				bool found_helper;
				do {
					Thread.Sleep (200);
					++poll_count;
					found_helper = CheckHelper ();
				} while (poll_count < 20 
					 && ! found_helper
					 && ! Shutdown.ShutdownRequested);
				
				if (! found_helper)
					throw new Exception ("Couldn't launch helper process");
			}
		}
	}
}
