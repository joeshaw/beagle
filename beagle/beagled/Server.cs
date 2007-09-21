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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using System.Timers;
using Mono.Unix;

using Beagle.Util;

namespace Beagle.Daemon {

	class HttpItemHandler {

		private Hashtable items = new Hashtable ();

		public void RegisterHit (Hit hit)
		{
			Logger.Log.Debug ("httpitemhandler: registering {0}", hit.Uri);

			if (hit.Uri == null) {
				Logger.Log.Debug ("httpitemhandler: cannot register hits with no URIs");
				return;
			}

			items [hit.Uri] = hit;
		}

		public void HandleRequest (HttpListenerContext context, System.Uri path)
		{
			Hit requested = (Hit) items [path];

			if (requested == null)
				return;

			Logger.Log.Debug ("httpitemhandler: requested: {0}", path);
			context.Response.ContentType = requested.MimeType;
			
			// FIXME: We can only handle files for now
			/*if (requested ["Type"] != "File") {
			  Logger.Log.Debug ("httpitemhandler: can only serve files");
			  return;
			}*/
			
			StreamReader r = new StreamReader (new FileStream (requested.Uri.LocalPath, FileMode.Open));
			StreamWriter w = new StreamWriter (context.Response.OutputStream);
			
			w.Write (r.ReadToEnd ());
			w.Close ();
			
			context.Response.Close ();
		}
	}

	class HttpConnectionHandler : ConnectionHandler {

		private HttpListenerContext context = null;
		private HttpListener listener = null;
		private HttpItemHandler item_handler = null;
		private System.Timers.Timer keepalive_timer = null;
		private System.Guid id = Guid.Empty;

		public HttpConnectionHandler (System.Guid guid, HttpListenerContext context, HttpItemHandler item_handler)
		{
			this.id = guid;
			this.context = context;
			this.item_handler = item_handler;
		}

		public override void HandleConnection ()
		{
			Logger.Log.Debug ("HTTP Server: Serving request for {0}", context.Request.Url);
			
			// Query request: read content and forward to base.HandleConnection for processing
			context.Response.KeepAlive = true;
			context.Response.ContentType = "text/txt; charset=utf-8";
			context.Response.SendChunked = true;
			
			Shutdown.WorkerStart (this.context.Request.InputStream, String.Format ("HandleConnection ({0})", ++connection_count));

			// Read the data off the socket and store it in a
			// temporary memory buffer.  Once the end-of-message
			// character has been read, discard remaining data
			// and deserialize the request.
			byte[] network_data = new byte [4096];
			MemoryStream buffer_stream = new MemoryStream ();
			int bytes_read, total_bytes = 0, end_index = -1;

			do {
				bytes_read = 0;

				try {
					lock (this.blocking_read_lock)
						this.in_blocking_read = true;

					lock (this.client_lock) {
						// The connection may have been closed within this loop.
						if (this.context != null)
							bytes_read = this.context.Request.InputStream.Read (network_data, 0, 4096);
					}

					lock (this.blocking_read_lock)
						this.in_blocking_read = false;
				} catch (Exception e) {
					// Aborting the thread mid-read will
					// cause an IOException to be thorwn,
					// which sets the ThreadAbortException
					// as its InnerException.MemoryStream
					if (!(e is IOException || e is ThreadAbortException))
						throw;

					// Reset the unsightly ThreadAbortException
					Thread.ResetAbort ();

					Logger.Log.Debug ("Bailing out of HandleConnection -- shutdown requested");
					this.thread = null;
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
			
			Logger.Log.Debug ("HTTP Server: Handling received request message");
			
			buffer_stream.Seek (0, SeekOrigin.Begin);
			base.HandleConnection (buffer_stream);
			
			Server.MarkHandlerAsKilled (this);
			Shutdown.WorkerFinished (context.Request.InputStream);
		}
		
		public override void Close ()
		{
			if (this.executor != null) {
				this.executor.Cleanup ();
				this.executor.AsyncResponseEvent -= OnAsyncResponse;
			}
			
			if (keepalive_timer != null)
				keepalive_timer.Stop ();
				
			CancelIfBlocking ();
			
			//ResponseStream tries to send bytes before closing, so 
			//just ignore the possible exception if the socket's closed.
			try {
				context.Response.OutputStream.Close ();
				context.Response.Close ();
			} catch (IOException) {
			} catch (SocketException) {
			}
		}

		public void WatchCallback (IAsyncResult ar)
		{
			int bytes_read = 0;

			try {
				bytes_read = this.context.Request.InputStream.EndRead (ar);
			} catch (SocketException) {
			} catch (IOException) {
			}

			if (bytes_read == 0)
				Close ();
			else
				SetupWatch ();
		}
		
		public void SendKeepAlive (object o, ElapsedEventArgs args)
		{
			if (this.listener == null) {
			    Log.Debug ("Socket end point closed.");
			    Close ();
			    return;
			}

			//if sending fails, the socket is closed and SendResponse will call Close.
			SendResponse (new EmptyResponse ());
		}

		public override void SetupWatch ()
		{
			keepalive_timer = new System.Timers.Timer ();
			keepalive_timer.Interval = 5000;
			keepalive_timer.Elapsed += new ElapsedEventHandler (SendKeepAlive);
			keepalive_timer.Start ();
		}

		public void TransformResponse (ref ResponseMessage message)
		{
			HitsAddedResponse response = message as HitsAddedResponse;

			if (response == null)
				return;

			foreach (Hit hit in response.Hits) {
				hit.Uri = new System.Uri (context.Request.Url.ToString () + id.ToString ());
				hit ["beagle:Source"] = "Network";

				item_handler.RegisterHit (hit);
			}	
		}

		public override bool SendResponse (ResponseMessage response)
		{	
			//TransformResponse (ref response); Disable for now

			bool r = false;

			lock (this.client_lock) {
				if (this.context == null)
					return false;

				r = base.SendResponse (response, context.Response.OutputStream);
			}

			if (r) {
				Logger.Log.Debug ("httpserver: Sent response = " + response.ToString ());
				//add end-of-document
				context.Response.OutputStream.WriteByte (0xff);
				context.Response.OutputStream.Flush ();
			} else {
				this.Close ();
			}

			return r;
		}
	}

	class UnixConnectionHandler : ConnectionHandler {

		private UnixClient client = null;

		public UnixConnectionHandler (UnixClient client)
		{
			this.client = client;
		}

		public override bool SendResponse(ResponseMessage response)
		{
			bool result = false;
			
			lock (this.client_lock) {
				if (this.client == null) 
					return false;
				result = base.SendResponse(response, this.client.GetStream () );
			}

			if (result) {
				// Send an end of message marker
				this.client.GetStream().WriteByte (0xff);
				this.client.GetStream().Flush ();
			}
			return result;
		}			

		public override void HandleConnection ()
		{
			this.thread = Thread.CurrentThread;

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
					// as its InnerException.MemoryStream
					if (!(e is IOException || e is ThreadAbortException))
						throw;

					// Reset the unsightly ThreadAbortException
					Thread.ResetAbort ();

					Logger.Log.Debug ("Bailing out of HandleConnection -- shutdown requested");
					this.thread = null;
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
			HandleConnection (buffer_stream);

		cleanup:
			buffer_stream.Close ();

			if (force_close_connection)
				Close ();
			else
				SetupWatch ();

			Server.MarkHandlerAsKilled (this);
			Shutdown.WorkerFinished (network_data);
		}

		public override void Close ()
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

		public override void SetupWatch ()
		{
			if (this.client == null) {
				this.Close ();
				return;
			}

			this.client.GetStream ().BeginRead (new byte[1024], 0, 1024,
							    new AsyncCallback (WatchCallback), null);
		}
	}	

	abstract class ConnectionHandler {

		protected static int connection_count = 0;
		private static XmlSerializer resp_serializer = null;
		private static XmlSerializer req_serializer = null;

		protected object client_lock = new object ();
		protected object blocking_read_lock = new object ();

		protected RequestMessageExecutor executor = null; // Only set in the keepalive case
		protected Thread thread;
		protected bool in_blocking_read;

		public static void Init ()
		{
			resp_serializer = new XmlSerializer (typeof (ResponseWrapper), ResponseMessage.Types);
			req_serializer = new XmlSerializer (typeof (RequestWrapper), RequestMessage.Types);
		}

		public bool SendResponse (ResponseMessage response, Stream stream)
		{

				try {
#if ENABLE_XML_DUMP
					MemoryStream mem_stream = new MemoryStream ();
					XmlFu.SerializeUtf8 (resp_serializer, mem_stream, new ResponseWrapper (response));
					mem_stream.Seek (0, SeekOrigin.Begin);
					StreamReader r = new StreamReader (mem_stream);
					Logger.Log.Debug ("Sending response:\n{0}\n", r.ReadToEnd ());
					mem_stream.Seek (0, SeekOrigin.Begin);
					mem_stream.WriteTo (stream);
					mem_stream.Close ();
#else
					XmlFu.SerializeUtf8 (resp_serializer, stream, new ResponseWrapper (response));
#endif
				} catch (Exception e) {
					Logger.Log.Debug (e, "Caught an exception sending {0}.  Shutting down socket.", response.GetType ());
					return false;
				}

				return true;
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

		protected void OnAsyncResponse (ResponseMessage response)
		{
			Logger.Log.Debug ("Sending response of type {0}", response.GetType ());
			if (!SendResponse (response))
				Close ();
		}

		abstract public void HandleConnection ();
		abstract public void Close ();
		abstract public void SetupWatch ();
		abstract public bool SendResponse (ResponseMessage response);

		public void HandleConnection (Stream buffer_stream)
		{
			this.thread = Thread.CurrentThread;

			RequestMessage req = null;
			ResponseMessage resp = null;

			bool force_close_connection = false;
			
#if ENABLE_XML_DUMP
			StreamReader r = new StreamReader (buffer_stream);
			Logger.Log.Debug ("Received request:\n{0}\n", r.ReadToEnd ());
			buffer_stream.Seek (0, SeekOrigin.Begin);
#endif

			try {
				RequestWrapper wrapper = (RequestWrapper) req_serializer.Deserialize (buffer_stream);
				
				req = wrapper.Message;
			} catch (InvalidOperationException e) {
				// Undocumented: Xml Deserialization exceptions
				if (e.InnerException != null)
					resp = new ErrorResponse (e.InnerException);
				else
					resp = new ErrorResponse (e);
				force_close_connection = true;
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

				if (exec != null) {
					try {
						resp = exec.Execute (req);
					} catch (Exception e) {
						Log.Warn (e, "Caught exception trying to execute {0}.  Sending error response", exec.GetType ());
						resp = new ErrorResponse (e);
					}
				}
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

			// Release our reference to Thread.CurrentThread, which
			// is a static instance (although thread-local)
			this.thread = null;
		}
	}

	public class Server {

		private static bool initialized = false;

		private string socket_path;
		private UnixListener unix_listener;
		private HttpListener http_listener;
		private static Hashtable live_handlers = new Hashtable ();
		private bool running = false;
		private bool enable_http = false;

		public static Hashtable item_handlers = new Hashtable ();

		static Server ()
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies ())
				ScanAssemblyForExecutors (assembly);
		}

		public Server (string name, bool enable_http)
		{
			// Use the default name when passed null
			if (name == null)
				name = "socket";

			this.socket_path = Path.Combine (PathFinder.GetRemoteStorageDir (true), name);
			this.unix_listener = new UnixListener (this.socket_path);
			this.enable_http = enable_http;
		}

		public Server (bool enable_http) : this (null, enable_http)
		{
		}

		// Perform expensive serialization all at once. Do this before signal handler is setup.
		public static void Init ()
		{
			Shutdown.ShutdownEvent += OnShutdown;
			ConnectionHandler.Init ();
			initialized = true;
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
			this.running = true;
			this.unix_listener.Start ();

			if (! Shutdown.WorkerStart (this, String.Format ("server '{0}'", socket_path)))
				return;

			ConnectionHandler handler = null;

			while (this.running) {
				UnixClient client;
				try {
					// This will block for an incoming connection.
					// FIXME: But not really, it'll only wait a second.
					// see the FIXME in UnixListener for more info.
					client = this.unix_listener.AcceptUnixClient ();
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
					handler = new UnixConnectionHandler (client);
					lock (live_handlers)
						live_handlers [handler] = handler;
					ExceptionHandlingThread.Start (new ThreadStart (handler.HandleConnection));
				}
			}

			Shutdown.WorkerFinished (this);

			Logger.Log.Debug ("Server '{0}' shut down", this.socket_path);
		}
		
		private void HttpRun ()
		{
			http_listener = new HttpListener ();
			string prefix = null;
			int port = 4000;
			bool success = false;

			do {
				prefix = String.Format ("http://*:{0}/", port);
				success = true;

				try {	
					http_listener.Prefixes.Add (prefix);
					http_listener.Start();
				} catch (SocketException) {
					http_listener.Prefixes.Remove (prefix);
					success = false;
					port++;
				}
			} while (!success);

			Shutdown.WorkerStart (this.http_listener, String.Format ("HTTP Server '{0}'", prefix));
			Logger.Log.Debug ("HTTP Server: Listening on {0}", prefix);

			while (this.running)
			{
				HttpListenerContext context  = null;

				try {
					context = http_listener.GetContext ();
				} catch (Exception e) {
					// Log a warning if not due to shutdown
					if (this.running)
						Logger.Log.Warn (e, "HTTP Server: Exception while getting context:");
					else
						break;
				}

				if (context == null)
					continue;

				if (context.Request.HttpMethod == "GET") {
					try {
						WebServer.HandleStaticPages (context);
					} catch (IOException ex1) {
						// Socket was shut down
						Log.Debug ("Exception while serving static page: " + ex1.Message);
						// FIXME: Should anything be done here to free "context" ? This context seems to remain in http_listener's ctxt table
					} catch (SocketException ex2) {
						// Socket is not connected anymore
						Log.Debug ("Exception while serving static page: " + ex2.Message);
					}
					continue;
				}

				if (context.Request.HttpMethod != "POST") {
					// FIXME: Send better HTTP error ?
					context.Response.StatusCode = 404;
					context.Response.Close ();
					continue;
				}

				if (context.Request.RawUrl == "/") {
					// We have received a new query request
					Guid guid = Guid.NewGuid ();
					HttpItemHandler item_handler = new HttpItemHandler ();
					item_handlers [guid] = item_handler;
					
					ConnectionHandler handler = new HttpConnectionHandler (guid, context, item_handler);

					lock (live_handlers)
						live_handlers [handler] = handler;

					ExceptionHandlingThread.Start (new ThreadStart (handler.HandleConnection));
				} else {
					// We have received a hit request
					Uri uri = context.Request.Url;
					string path = null;

					// Second Uri segment contains the Guid
					string g = uri.Segments [1];
					
					if (g [g.Length - 1] == '/')
						g = g.Remove (g.Length -1 , 1);
					
					Guid guid = Guid.Empty;
					
					try {
						guid = new Guid (g);
					} catch (FormatException) {
						// FIXME: return HTTP error
						Logger.Log.Debug ("HTTP Server: Invalid query guid '{0}'", g);
						context.Response.Close ();
						continue;
					}
					
					if (uri.Query.Length > 0) {
						path = uri.Query.Remove (0,1);
					} else {
						// FIXME: return HTTP error
						Logger.Log.Debug ("HTTP Server: Empty query string in item request");
						context.Response.Close ();
						continue;
					}
					
					System.Uri item_uri = new Uri (path);					
					HttpItemHandler handler = (HttpItemHandler) item_handlers [guid];
					
					if (handler == null) {
						// FIXME: return HTTP error
						Logger.Log.Debug ("HTTP Server: Query ({0}) does not exist", g);
						context.Response.Close ();
					} else {
						Logger.Log.Debug ("HTTP Server: Asked for item '{0}' on query '{1}'", path, g);
						handler.HandleRequest (context, item_uri);
					}
				}
			}
				

			Shutdown.WorkerFinished (http_listener);
			Logger.Log.Debug ("HTTP Server: '{0}' shut down...", prefix);
		}

		public void Start ()
		{
			if (!initialized)
				throw new Exception ("Server must be initialized before starting");

			if (!Shutdown.ShutdownRequested) {
				if (enable_http)
					ExceptionHandlingThread.Start (new ThreadStart (this.HttpRun));
				ExceptionHandlingThread.Start (new ThreadStart (this.Run));
			}
		}

		public void Stop ()
		{
			if (this.running) {
				this.running = false;
				this.unix_listener.Stop ();
				if (enable_http) {
					// Abort without bothering about anythin' else
					this.http_listener.IgnoreWriteExceptions = true;
					// FIXME FIXME FIXME: this.http_listener.Abort() is throwing
					// IOExceptions when trying
					// to close previous connections which were unexpectedly 
					// closed by the client. That somehow points to a leak; why
					// are those connections still present with listener ?
					// http_listener should really be Abort()-ed and not Close()-ed
					try {
						this.http_listener.Close ();
					} catch (IOException) {
						Log.Debug ("IOException was thrown when trying to close previous connections which were unexpectedly closed by the client. This somehow points to a leak; why are those connections still present with listener ?");
						Log.Debug ("Also this is preventing the abort() to complete and hence http_listener.GetContext() is still waiting for connections.");
					}
				}
			}

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

			private RequestMessageHandler handler;
			
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

			foreach (Type t in ReflectionFu.GetTypesFromAssemblyAttribute (assembly, typeof (RequestMessageExecutorTypesAttribute))) {

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
