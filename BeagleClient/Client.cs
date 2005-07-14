//
// Client.cs
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
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;

using GLib;

using Beagle.Util;

namespace Beagle {


	internal class Client {

		private class EventThrowingClosure {
		
			private Client client;
			private ResponseMessage response;

			public EventThrowingClosure (Client client, ResponseMessage response)
			{
				this.client = client;
				this.response = response;
			}

			public bool ThrowEvent ()
			{
				if (this.client.AsyncResponseEvent != null)
					this.client.AsyncResponseEvent (this.response);

				return false;
			}
		}

		private string socket_name;

		private UnixClient client;

		private byte[] network_data = new byte [4096];
		private MemoryStream buffer_stream = new MemoryStream ();

		private bool closed = false;

		public delegate void AsyncResponse (ResponseMessage response);
		public event AsyncResponse AsyncResponseEvent;

		public delegate void Closed ();
		public event Closed ClosedEvent;

		public Client (string client_name)
		{
			// use the default socket name when passed null
			if (client_name == null)
				client_name = "socket";

			string storage_dir = PathFinder.GetRemoteStorageDir (false);
			if (storage_dir == null)
				throw new System.Net.Sockets.SocketException ();

			this.socket_name = Path.Combine (storage_dir, client_name);
		}

		public Client () : this (null)
		{

		}

		~Client ()
		{
			Close ();
		}

		public void Close ()
		{
			bool previously_closed = this.closed;

			// Important to set this before we close the
			// UnixClient, since that will trigger the
			// ReadCallback() method, reading 0 bytes off the
			// wire, and we check this.closed in there.
			this.closed = true;

			if (this.client != null)
				this.client.Close ();

			if (!previously_closed && this.ClosedEvent != null)
				this.ClosedEvent ();
		}

		static XmlSerializer req_serializer = new XmlSerializer (typeof (RequestWrapper), RequestMessage.Types);

		private void SendRequest (RequestMessage request)
		{
			this.client = new UnixClient (this.socket_name);
			NetworkStream stream = this.client.GetStream ();

			// The socket may be shut down at some point here.  It
			// is the caller's responsibility to handle the error
			// correctly.
			req_serializer.Serialize (stream, new RequestWrapper (request));
			// Send end of message marker
			stream.WriteByte (0xff);
			stream.Flush ();
		}

		static XmlSerializer resp_serializer = new XmlSerializer (typeof (ResponseWrapper), ResponseMessage.Types);
		
		// This function will be called from its own thread
		private void ReadCallback (IAsyncResult ar)
		{
			if (this.closed)
				return;

			try {
				NetworkStream stream = this.client.GetStream ();
				int bytes_read = 0;

				try { 
					bytes_read = stream.EndRead (ar);
				} catch (SocketException) {
					Logger.Log.Debug ("Caught SocketException in ReadCallback");
				} catch (IOException) {
					Logger.Log.Debug ("Caught IOException in ReadCallback");
				}

				// Connection hung up, we're through
				if (bytes_read == 0) {
					this.Close ();
					return;
				}

				int end_index = -1;
				int prev_index = 0;

				do {
					// 0xff signifies end of message
					end_index = ArrayFu.IndexOfByte (this.network_data, (byte) 0xff, prev_index);

					this.buffer_stream.Write (this.network_data, prev_index, (end_index == -1 ? bytes_read : end_index) - prev_index);

					if (end_index != -1) {

						MemoryStream deserialize_stream = this.buffer_stream;
						this.buffer_stream = new MemoryStream ();

						deserialize_stream.Seek (0, SeekOrigin.Begin);

						ResponseWrapper wrapper;
						wrapper = (ResponseWrapper) resp_serializer.Deserialize (deserialize_stream);

						ResponseMessage resp = wrapper.Message;

						deserialize_stream.Close ();

						// Run the handler in an idle handler
						// so that events are thrown in the
						// main thread instead of this inferior
						// helper thread.
						EventThrowingClosure closure = new EventThrowingClosure (this, resp);
						GLib.Idle.Add (new IdleHandler (closure.ThrowEvent));

						// Move past the end-of-message marker
						prev_index = end_index + 1;
					}
				} while (end_index != -1);

				// Check to see if we're still connected, and keep
				// looking for new data if so.
				if (!this.closed)
					BeginRead ();

			} catch (Exception e) {
				Logger.Log.Error ("Got an exception while trying to read data:");
				Logger.Log.Error (e);

			}
		}

		private void BeginRead ()
		{
			NetworkStream stream = this.client.GetStream ();
			Array.Clear (this.network_data, 0, this.network_data.Length);
			stream.BeginRead (this.network_data, 0, this.network_data.Length,
					  new AsyncCallback (ReadCallback), null);
		}

		public void SendAsync (RequestMessage request)
		{
			Exception ex = null;

			try {
				SendRequest (request);
			} catch (IOException e) {
				ex = e;
			} catch (SocketException e) {
				ex = e;
			}

			if (ex != null) {
				ResponseMessage resp = new ErrorResponse (ex);
				
				if (this.AsyncResponseEvent != null)
					this.AsyncResponseEvent (resp);
			} else
				BeginRead ();
		}


		public ResponseMessage Send (RequestMessage request)
		{
			if (request.Keepalive)
				throw new Exception ("A blocking connection on a keepalive request is not allowed");

			Exception throw_me = null;

			try {
				SendRequest (request);
			} catch (IOException e) {
				throw_me = e;
			} catch (SocketException e) {
				throw_me = e;
			}

			if (throw_me != null)
				throw new ResponseMessageException (throw_me);

			NetworkStream stream = this.client.GetStream ();
			int bytes_read, end_index = -1;

			do {
				bytes_read = stream.Read (this.network_data, 0, 4096);

				//Logger.Log.Debug ("Read {0} bytes", bytes_read);

				if (bytes_read > 0) {
					// 0xff signifies end of message
					end_index = ArrayFu.IndexOfByte (this.network_data, (byte) 0xff);
					
					this.buffer_stream.Write (this.network_data, 0,
								  end_index == -1 ? bytes_read : end_index);
				}
			} while (bytes_read > 0 && end_index == -1);

			this.buffer_stream.Seek (0, SeekOrigin.Begin);
			
			ResponseMessage resp = null;

			try {
				ResponseWrapper wrapper;
				wrapper = (ResponseWrapper) resp_serializer.Deserialize (this.buffer_stream);

				resp = wrapper.Message;
			} catch (Exception e) {
				throw_me = new ResponseMessageException (e);
			}

			this.buffer_stream.Close ();

			if (throw_me != null)
				throw throw_me;
			
			return resp;
		}
	}
}
