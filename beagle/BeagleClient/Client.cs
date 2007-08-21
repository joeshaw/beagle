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
using System.Net;
using Mono.Unix;

using GLib;

using Beagle.Util;

namespace Beagle {

	internal abstract class Client {

		protected MemoryStream buffer_stream = new MemoryStream ();

		protected bool closed = false;

		public delegate void AsyncResponse (ResponseMessage response);
		public event AsyncResponse AsyncResponseEvent;

		public delegate void Closed ();
		public event Closed ClosedEvent;

		public Client (string id)
		{
		}
	
		public Client () : this (null)
		{
		}

		~Client ()
		{
			Close ();
		}

		abstract public void Close ();
		abstract protected void SendRequest (RequestMessage request);
		abstract protected void ReadCallback (IAsyncResult ar);
		abstract protected void BeginRead ();
		abstract public ResponseMessage Send (RequestMessage request);
		abstract public void SendAsyncBlocking (RequestMessage request);

		protected static XmlSerializer req_serializer = new XmlSerializer (typeof (RequestWrapper), RequestMessage.Types);
		protected static XmlSerializer resp_serializer = new XmlSerializer (typeof (ResponseWrapper), ResponseMessage.Types);
		
		protected void SendRequest (RequestMessage request, Stream stream)
		{
			// The socket may be shut down at some point here.  It
			// is the caller's responsibility to handle the error
			// correctly.
#if ENABLE_XML_DUMP
			MemoryStream mem_stream = new MemoryStream ();
			XmlFu.SerializeUtf8 (req_serializer, mem_stream, new RequestWrapper (request));
			mem_stream.Seek (0, SeekOrigin.Begin);
			StreamReader r = new StreamReader (mem_stream);
			Logger.Log.Debug ("Sending request:\n{0}\n", r.ReadToEnd ());
			mem_stream.Seek (0, SeekOrigin.Begin);
			mem_stream.WriteTo (stream);
			mem_stream.Close ();
#else
			XmlFu.SerializeUtf8 (req_serializer, stream, new RequestWrapper (request));
#endif
			// Send end of message marker
			stream.WriteByte (0xff);
			stream.Flush ();
		}

		protected void HandleResponse (Stream deserialize_stream)
		{
#if ENABLE_XML_DUMP
			StreamReader r = new StreamReader (deserialize_stream);
			Logger.Log.Debug ("Received response:\n{0}\n", r.ReadToEnd ());
			deserialize_stream.Seek (0, SeekOrigin.Begin);
#endif

			ResponseWrapper wrapper;
			wrapper = (ResponseWrapper) resp_serializer.Deserialize (deserialize_stream);

			ResponseMessage resp = wrapper.Message;

			deserialize_stream.Close ();

			// Run the handler in an idle handler so that events are thrown
			// in the main thread instead of this inferior helper thread.
			EventThrowingClosure closure = new EventThrowingClosure (this, resp);
			GLib.Idle.Add (new IdleHandler (closure.ThrowEvent));
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
			} catch (NotImplementedException) {
				// FIXME: A workaround for the HttpClient when we can't
				// get a stream so we get at least local results!
				// I'll fix this soon.
				return;
			}

			if (ex != null) {
				ResponseMessage resp = new ErrorResponse (ex);
				
				if (this.AsyncResponseEvent != null)
					this.AsyncResponseEvent (resp);
			} else {
				BeginRead ();
			}
		}

		protected void InvokeClosedEvent ()
		{
			if (this.ClosedEvent != null)
				ClosedEvent ();
		}
		
		protected void InvokeAsyncResponseEvent (ResponseMessage response)
		{
			if (this.AsyncResponseEvent != null)
				AsyncResponseEvent (response);
		}

		private class EventThrowingClosure {
		
			private Client client = null;
			private ResponseMessage response = null;

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
	}

	internal class UnixSocketClient : Client {
		
		private string socket_name = null;
		private UnixClient client = null;
		private byte[] network_data = new byte [4096];

		public UnixSocketClient (string client_name)
		{
			// use the default socket name when passed null
			if (String.IsNullOrEmpty (client_name))
				client_name = "socket";

			string storage_dir = PathFinder.GetRemoteStorageDir (false);

			if (storage_dir == null)
				throw new System.Net.Sockets.SocketException ();

			this.socket_name = Path.Combine (storage_dir, client_name);
		}

		public override void Close ()
		{
			bool previously_closed = this.closed;

			// Important to set this before we close the
			// UnixClient, since that will trigger the
			// ReadCallback() method, reading 0 bytes off the
			// wire, and we check this.closed in there.
			this.closed = true;

			if (this.client != null)
				this.client.Close ();

			if (!previously_closed)
				InvokeClosedEvent ();
		}

		protected override void SendRequest (RequestMessage request)
		{
			Logger.Log.Debug ("Sending request to {0}", socket_name);

			this.client = new UnixClient (this.socket_name);
			NetworkStream stream = this.client.GetStream ();
			
			base.SendRequest (request, stream);
		}
		
		// This function will be called from its own thread
		protected override void ReadCallback (IAsyncResult ar)
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

					int bytes_count = (end_index == -1 ? bytes_read : end_index) - prev_index;
					this.buffer_stream.Write (this.network_data, prev_index, bytes_count);

					if (end_index != -1) {
						MemoryStream deserialize_stream = this.buffer_stream;
						this.buffer_stream = new MemoryStream ();
						
						deserialize_stream.Seek (0, SeekOrigin.Begin);
						HandleResponse (deserialize_stream);
						
						// Move past the end-of-message marker
						prev_index = end_index + 1;
					}
				} while (end_index != -1);

				// Check to see if we're still connected, and keep
				// looking for new data if so.
				if (!this.closed)
					BeginRead ();

			} catch (Exception e) {
				Logger.Log.Error (e, "Got an exception while trying to read data:");
			}
		}

		protected override void BeginRead ()
		{
			NetworkStream stream = this.client.GetStream ();
			Array.Clear (this.network_data, 0, this.network_data.Length);
			stream.BeginRead (this.network_data, 0, this.network_data.Length, new AsyncCallback (ReadCallback), null);
		}

		public override void SendAsyncBlocking (RequestMessage request)
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
				InvokeAsyncResponseEvent (resp);
				return;
			}
			

			NetworkStream stream = this.client.GetStream ();
			MemoryStream deserialize_stream = new MemoryStream ();

			// This buffer is annoyingly small on purpose, to avoid
			// having to deal with the case of multiple messages
			// in a single block.
			byte [] buffer = new byte [32];

			while (! this.closed) {

				Array.Clear (buffer, 0, buffer.Length);

				int bytes_read;
				bytes_read = stream.Read (buffer, 0, buffer.Length);
				if (bytes_read == 0)
					break;

				int end_index;
				end_index = ArrayFu.IndexOfByte (buffer, (byte) 0xff);

				if (end_index == -1) {
					deserialize_stream.Write (buffer, 0, bytes_read);
				} else {
					deserialize_stream.Write (buffer, 0, end_index);
					deserialize_stream.Seek (0, SeekOrigin.Begin);
					Logger.Log.Debug ("We're here");

#if ENABLE_XML_DUMP
					StreamReader r = new StreamReader (deserialize_stream);
					Logger.Log.Debug ("Received response:\n{0}\n", r.ReadToEnd ());
					deserialize_stream.Seek (0, SeekOrigin.Begin);
#endif

					ResponseMessage resp;
					try {
						ResponseWrapper wrapper;
						wrapper = (ResponseWrapper) resp_serializer.Deserialize (deserialize_stream);
						
						resp = wrapper.Message;
					} catch (Exception e) {
						resp = new ErrorResponse (e);
					}

					InvokeAsyncResponseEvent (resp);

					deserialize_stream.Close ();
					deserialize_stream = new MemoryStream ();
					if (bytes_read - end_index - 1 > 0)
						deserialize_stream.Write (buffer, end_index + 1, bytes_read - end_index - 1);
				}
			}
		}


		public override ResponseMessage Send (RequestMessage request)
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

			// It's possible that the server side shut down the
			// connection before we had a chance to read any data.
			// If this is the case, throw a rather descriptive
			// exception.
			if (this.buffer_stream.Length == 0) {
				this.buffer_stream.Close ();
				throw new ResponseMessageException ("Socket was closed before any data could be read");
			}

			this.buffer_stream.Seek (0, SeekOrigin.Begin);

#if ENABLE_XML_DUMP
			StreamReader dump_reader = new StreamReader (this.buffer_stream);
			Logger.Log.Debug ("Received response:\n{0}\n", dump_reader.ReadToEnd ());
			this.buffer_stream.Seek (0, SeekOrigin.Begin);
#endif
			
			ResponseMessage resp = null;

			try {
				ResponseWrapper wrapper;
				wrapper = (ResponseWrapper) resp_serializer.Deserialize (this.buffer_stream);

				resp = wrapper.Message;
			} catch (Exception e) {
				this.buffer_stream.Seek (0, SeekOrigin.Begin);
				StreamReader r = new StreamReader (this.buffer_stream);
				throw_me = new ResponseMessageException (e, "Exception while deserializing response", String.Format ("Message contents: '{0}'", r.ReadToEnd ()));
				this.buffer_stream.Seek (0, SeekOrigin.Begin);
			}

			this.buffer_stream.Close ();

			if (throw_me != null)
				throw throw_me;
			
			return resp;
		}
	}

	internal class HttpClient : Client {
		
		private string client_url = null;
		private System.Net.HttpWebRequest http_request = null;
		private byte[] network_data = new byte [14096];
	
		public HttpClient (string url)
		{
			Logger.Log.Debug ("Created client for " + url);
			this.client_url = url; 
		}
		
		protected override void SendRequest (RequestMessage request)
		{
			Logger.Log.Debug ("Sending request to {0}", client_url);

			http_request = (HttpWebRequest) System.Net.WebRequest.Create (client_url);
			http_request.Method = "POST";
			http_request.KeepAlive = true;
			http_request.AllowWriteStreamBuffering = false;
			http_request.SendChunked = true;
			
			try {
				Stream stream = http_request.GetRequestStream ();
			
				base.SendRequest (request, stream);
				stream.Flush ();
				stream.Close ();
				
				Logger.Log.Debug ("HttpClient: Sent request");
			} catch (WebException e) {
				// FIXME: A workaround for the HttpClient when we can't
				// get a stream so we get at least local results!
				// I'll fix this soon.
				Logger.Log.Debug (e, "HttpClient: SendRequest failed:");
				throw new NotImplementedException ();
			}
		}
		
		public override ResponseMessage Send (RequestMessage request)
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

			Stream stream = this.http_request.GetResponse ().GetResponseStream ();
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
		
		public override void Close ()
		{
			bool previously_closed = this.closed;
			
			if (http_request != null)
				http_request.Abort ();
				
			this.closed = true;
			http_request = null;
			
			if (previously_closed)
				this.InvokeClosedEvent ();
		}
		
		protected override void BeginRead ()
		{
			Stream stream = this.http_request.GetResponse ().GetResponseStream ();
			
			Array.Clear (this.network_data, 0, this.network_data.Length);
			
			try {
				stream.BeginRead (this.network_data, 0, this.network_data.Length,
						  new AsyncCallback (ReadCallback), stream);
			} catch (IOException) {
				Logger.Log.Debug ("Caught IOException in BeginRead");
				Close ();
			}
			
		}
		
		protected override void ReadCallback (IAsyncResult result)
		{			
			if (this.closed)
				return;

			try {
				Stream stream = (Stream) result.AsyncState;
				int bytes_read = 0;
  				
				try { 
					bytes_read = stream.EndRead (result);
				} catch (SocketException) {
					Logger.Log.Debug ("Caught SocketException in ReadCallback");
					Close ();
				} catch (IOException) {
					Logger.Log.Debug ("Caught IOException in ReadCallback");
					Close ();
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
					
					if (end_index > bytes_read) {
						//I'm not sure how this ever comes to be true, but it does,
						//even though the array is cleared
						end_index = -1;
					}
					
					int bytes_count = ((end_index == -1) ? bytes_read : end_index) - prev_index;
					this.buffer_stream.Write (this.network_data, prev_index, bytes_count);
					
					if (end_index != -1) {
						MemoryStream deserialize_stream = this.buffer_stream;
						
						this.buffer_stream = new MemoryStream ();
						deserialize_stream.Seek (0, SeekOrigin.Begin);
				
						HandleResponse (deserialize_stream);
						
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
				
				ResponseMessage resp = new ErrorResponse (e);
				this.InvokeAsyncResponseEvent (resp);

				return;
			}
		}
		
		public override void SendAsyncBlocking (RequestMessage request)
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
				this.InvokeAsyncResponseEvent (resp);

				return;
			}
			

			Stream stream = this.http_request.GetResponse ().GetResponseStream ();
			MemoryStream deserialize_stream = new MemoryStream ();

			// This buffer is annoyingly small on purpose, to avoid
			// having to deal with the case of multiple messages
			// in a single block.
			byte [] buffer = new byte [32];

			while (! this.closed) {

				Array.Clear (buffer, 0, buffer.Length);

				int bytes_read;
				bytes_read = stream.Read (buffer, 0, buffer.Length);
				if (bytes_read == 0)
					break;

				int end_index;
				end_index = ArrayFu.IndexOfByte (buffer, (byte) 0x00);

				if (end_index == -1) {
					deserialize_stream.Write (buffer, 0, bytes_read);
				} else {
					deserialize_stream.Write (buffer, 0, end_index);
					deserialize_stream.Seek (0, SeekOrigin.Begin);

					ResponseMessage resp;
					try {
						ResponseWrapper wrapper;
						wrapper = (ResponseWrapper) resp_serializer.Deserialize (deserialize_stream);
						
						resp = wrapper.Message;
					} catch (Exception e) {
						resp = new ErrorResponse (e);
					}
					
					this.InvokeAsyncResponseEvent (resp);

					deserialize_stream.Close ();
					deserialize_stream = new MemoryStream ();
					if (bytes_read - end_index - 1 > 0)
						deserialize_stream.Write (buffer, end_index+1, bytes_read-end_index-1);
				}
			}
		}
	}
}
