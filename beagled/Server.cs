//
// Server.cs
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
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using Mono.Unix;

using Beagle.Util;

namespace Beagle.Daemon {

	class ConnectionHandler {

		private static int connection_count = 0;
		public static XmlSerializer serializer = null;

		private object client_lock = new object ();
		private object blocking_read_lock = new object ();

		private UnixClient client;
		private RequestMessageExecutor executor = null; // Only set in the keepalive case
		private Thread thread;
		private bool in_blocking_read;

		public ConnectionHandler (UnixClient client)
		{
			this.client = client;
		}

		public bool SendResponse (ResponseMessage response)
		{
			lock (this.client_lock) {
				if (this.client == null)
					return false;

				try {
#if ENABLE_XML_DUMP
					MemoryStream mem_stream = new MemoryStream ();
					XmlFu.SerializeUtf8 (serializer, mem_stream, new ResponseWrapper (response));
					mem_stream.Seek (0, SeekOrigin.Begin);
					StreamReader r = new StreamReader (mem_stream);
					Logger.Log.Debug ("Sending response:\n{0}\n", r.ReadToEnd ());
					mem_stream.Seek (0, SeekOrigin.Begin);
					mem_stream.WriteTo (this.client.GetStream ());
					mem_stream.Close ();
#else
					XmlFu.SerializeUtf8 (serializer, this.client.GetStream (), new ResponseWrapper (response));
#endif
					// Send an end of message marker
					this.client.GetStream ().WriteByte (0xff);
					this.client.GetStream ().Flush ();
				} catch (Exception e) {
					Logger.Log.Debug ("Caught an exception sending response; socket shut down: {0}", e.Message);
					return false;
				}

				return true;
			}
		}

		public void CancelIfBlocking ()
		{
			// Work around some crappy .net behavior.  We can't
			// close a socket and have it exit out of a blocking
			// read, so we have to abort the thread it's blocking
			// in.
			lock (this.blocking_read_lock) {
				if (this.in_blocking_read) {
					this.thread.Abort ();
				}
			}
		}
		
		public void Close ()
		{
			CancelIfBlocking ();

			// It's important that we abort the thread before we
			// grab the lock here and close the underlying
			// UnixClient, or else we'd deadlock between here and
			// the Read() in HandleConnection()
			lock (this.client_lock) {
				if (this.client != null) {
					this.client.Close ();
					this.client = null;
				}
			}

			if (this.executor != null) {
				this.executor.Cleanup ();
				this.executor.AsyncResponseEvent -= OnAsyncResponse;
			}
		}

		public void WatchCallback (IAsyncResult ar)
		{
			int bytes_read = 0;

			try {
				bytes_read = this.client.GetStream ().EndRead (ar);
			} catch (SocketException) {
			} catch (IOException) { }

			if (bytes_read == 0)
				Close ();
			else
				SetupWatch ();
		}

		private void SetupWatch ()
		{
			if (this.client == null) {
				this.Close ();
				return;
			}

			this.client.GetStream ().BeginRead (new byte[1024], 0, 1024,
							    new AsyncCallback (WatchCallback), null);
		}

		private void OnAsyncResponse (ResponseMessage response)
		{
			if (!SendResponse (response))
				Close ();
		}

		static XmlSerializer req_serializer = new XmlSerializer (typeof (RequestWrapper), RequestMessage.Types);

		public void HandleConnection ()
		{
			this.thread = Thread.CurrentThread;

			RequestMessage req = null;
			ResponseMessage resp = null;

			bool force_close_connection = false;
			
			// Read the data off the socket and store it in a
			// temporary memory buffer.  Once the end-of-message
			// character has been read, discard remaining data
			// and deserialize the request.
			byte[] network_data = new byte [4096];
			MemoryStream buffer_stream = new MemoryStream ();
			int bytes_read, total_bytes = 0, end_index = -1;

			// We use the network_data array as an object to represent this worker.
			Shutdown.WorkerStart (network_data, String.Format ("HandleConnection ({0})", ++connection_count));

			do {
				bytes_read = 0;

				try {
					lock (this.blocking_read_lock)
						this.in_blocking_read = true;

					lock (this.client_lock) {
						// The connection may have been closed within this loop.
						if (this.client != null)
							bytes_read = this.client.GetStream ().Read (network_data, 0, 4096);
					}

					lock (this.blocking_read_lock)
						this.in_blocking_read = false;
				} catch (Exception e) {
					// Aborting the thread mid-read will
					// cause an IOException to be thorwn,
					// which sets the ThreadAbortException
					// as its InnerException.
					if (!(e is IOException || e is ThreadAbortException))
						throw;

					Logger.Log.Debug ("Bailing out of HandleConnection -- shutdown requested");
					Server.MarkHandlerAsKilled (this);
					Shutdown.WorkerFinished (network_data);
					return;
				}

				total_bytes += bytes_read;

				if (bytes_read > 0) {
					// 0xff signifies end of message
					end_index = ArrayFu.IndexOfByte (network_data, (byte) 0xff);

					buffer_stream.Write (network_data, 0,
							     end_index == -1 ? bytes_read : end_index);
				}
			} while (bytes_read > 0 && end_index == -1);

			// Something just connected to our socket and then
			// hung up.  The IndexHelper (among other things) does
			// this to check that a server is still running.  It's
			// no big deal, so just clean up and close without
			// running any handlers.
			if (total_bytes == 0) {
				force_close_connection = true;
				goto cleanup;
			}

			buffer_stream.Seek (0, SeekOrigin.Begin);

#if ENABLE_XML_DUMP
			StreamReader r = new StreamReader (buffer_stream);
			Logger.Log.Debug ("Received request:\n{0}\n", r.ReadToEnd ());
			buffer_stream.Seek (0, SeekOrigin.Begin);
#endif

			try {
				RequestWrapper wrapper = (RequestWrapper) req_serializer.Deserialize (buffer_stream);
				
				req = wrapper.Message;
			} catch (Exception e) {
				resp = new ErrorResponse (e);
				force_close_connection = true;
			}

			// If XmlSerializer can't deserialize the payload, we
			// may get a null payload and not an exception.  Or
			// maybe the client just didn't send one.
			if (req == null && resp == null) {
				resp = new ErrorResponse ("Missing payload");
				force_close_connection = true;
			}

			// And if there are no errors, execute the command
			if (resp == null) {

				RequestMessageExecutor exec;
				exec = Server.GetExecutor (req);

				if (exec == null) {
					resp = new ErrorResponse (String.Format ("No handler available for {0}", req.GetType ()));
					force_close_connection = true;
				} else if (req.Keepalive) {
					this.executor = exec;
					exec.AsyncResponseEvent += OnAsyncResponse;
				}

				if (exec != null)
					resp = exec.Execute (req);
			}

			// It's okay if the response is null; this means
			// that keepalive is set and that we'll be sending
			// back responses asynchronously.  First, enforce
			// that the response is not null if keepalive isn't
			// set.
			if (resp == null && !req.Keepalive)
				resp = new ErrorResponse ("No response available, but keepalive is not set");

			if (resp != null) {
				//Logger.Log.Debug ("Sending response of type {0}", resp.GetType ());
				if (!this.SendResponse (resp))
					force_close_connection = true;
			}

		cleanup:
			buffer_stream.Close ();

			if (force_close_connection || !req.Keepalive)
				Close ();
			else
				SetupWatch ();

			Server.MarkHandlerAsKilled (this);
			Shutdown.WorkerFinished (network_data);
		}
	}

	public class Server {

		private string socket_path;
		private UnixListener listener;
		private static Hashtable live_handlers = new Hashtable ();
		private bool running = false;

		public Server (string name)
		{
			ScanAssemblyForExecutors (Assembly.GetCallingAssembly ());

			// Use the default name when passed null
			if (name == null)
				name = "socket";

			this.socket_path = Path.Combine (PathFinder.GetRemoteStorageDir (true), name);
			this.listener = new UnixListener (this.socket_path);
		}

		public Server () : this (null)
		{

		}

		static Server ()
		{
			ScanAssemblyForExecutors (Assembly.GetExecutingAssembly ());

			Shutdown.ShutdownEvent += OnShutdown;
		}

		static internal void MarkHandlerAsKilled (ConnectionHandler handler)
		{
			lock (live_handlers) {
				live_handlers.Remove (handler);
			}
		}

		private static void OnShutdown ()
		{
			lock (live_handlers) {
				foreach (ConnectionHandler handler in live_handlers.Values) {
					Logger.Log.Debug ("CancelIfBlocking {0}", handler);
					handler.CancelIfBlocking ();
				}
			}
		}

		private void Run ()
		{
			this.listener.Start ();
			this.running = true;

			Shutdown.WorkerStart (this, String.Format ("server '{0}'", socket_path));
			if (ConnectionHandler.serializer == null)
				ConnectionHandler.serializer = new XmlSerializer (typeof (ResponseWrapper), ResponseMessage.Types);

			while (this.running) {
				UnixClient client;
				try {
					// This will block for an incoming connection.
					// FIXME: But not really, it'll only wait a second.
					// see the FIXME in UnixListener for more info.
					client = this.listener.AcceptUnixClient ();
				} catch (SocketException) {
					// If the listener is stopped while we
					// wait for a connection, a
					// SocketException is thrown.
					break;
				}

				// FIXME: This is a hack to work around a mono
				// bug.  See the FIXMEs in UnixListener.cs for
				// more info, but client should never be null,
				// because AcceptUnixClient() should be
				// throwing a SocketException when the
				// listener is shut down.  So when that is
				// fixed, remove the if conditional.

				// If client is null, the socket timed out.
				if (client != null) {
					ConnectionHandler handler = new ConnectionHandler (client);
					lock (live_handlers)
						live_handlers [handler] = handler;
					ExceptionHandlingThread.Start (new ThreadStart (handler.HandleConnection));
				}
			}
			
			Shutdown.WorkerFinished (this);

			Logger.Log.Debug ("Server '{0}' shut down", this.socket_path);
		}

		public void Start ()
		{
			if (!Shutdown.ShutdownRequested)
				ExceptionHandlingThread.Start (new ThreadStart (this.Run));
		}

		public void Stop ()
		{
			this.running = false;
			this.listener.Stop ();
			File.Delete (this.socket_path);
		}

		//////////////////////////////////////////////////////////////////////////////

		//
		// Code to dispatch requests to the correct RequestMessageExecutor.
		//
		
		public delegate ResponseMessage RequestMessageHandler (RequestMessage msg);

		// A simple wrapper class to turn a RequestMessageHandler delegate into
		// a RequestMessageExecutor.
		private class SimpleRequestMessageExecutor : RequestMessageExecutor {

			RequestMessageHandler handler;
			
			public SimpleRequestMessageExecutor (RequestMessageHandler handler)
			{
				this.handler = handler;
			}

			public override ResponseMessage Execute (RequestMessage req)
			{
				return this.handler (req);
			}
		}

		static private Hashtable scanned_assemblies = new Hashtable ();
		static private Hashtable request_type_to_handler = new Hashtable ();
		static private Hashtable request_type_to_executor_type = new Hashtable ();

		static public void RegisterRequestMessageHandler (Type request_type, RequestMessageHandler handler)
		{
			request_type_to_handler [request_type] = handler;
		}

		static public void ScanAssemblyForExecutors (Assembly assembly)
		{
			if (scanned_assemblies.Contains (assembly))
				return;
			scanned_assemblies [assembly] = assembly;

			foreach (Type t in assembly.GetTypes ()) {

				if (!t.IsSubclassOf (typeof (RequestMessageExecutor)))
					continue;

				// Yes, we know it doesn't have a RequestMessageAttribute
				if (t == typeof (SimpleRequestMessageExecutor))
					continue;

				Attribute attr = Attribute.GetCustomAttribute (t, typeof (RequestMessageAttribute));

				if (attr == null) {
					Logger.Log.Warn ("No handler attribute for executor {0}", t);
					continue;
				}

				RequestMessageAttribute pra = (RequestMessageAttribute) attr;

				request_type_to_executor_type [pra.MessageType] = t;
			}
		}

		static internal RequestMessageExecutor GetExecutor (RequestMessage req)
		{
			Type req_type = req.GetType ();

			RequestMessageExecutor exec = null;

			RequestMessageHandler handler;
			handler = request_type_to_handler [req_type] as RequestMessageHandler;

			if (handler != null) {
				exec = new SimpleRequestMessageExecutor (handler);
			} else {
				Type t = request_type_to_executor_type [req_type] as Type;
				if (t != null)
					exec = (RequestMessageExecutor) Activator.CreateInstance (t);
			}

			return exec;
		}
		
		
	}
}
