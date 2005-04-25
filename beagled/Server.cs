using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	class ConnectionHandler {

		private UnixClient client;
		private RequestMessageExecutor executor = null; // Only set in the keepalive case

		private static Hashtable executor_map = null;

		public ConnectionHandler (UnixClient client)
		{
			this.client = client;
		}

		public bool SendResponse (ResponseMessage response)
		{
			XmlSerializer serializer = new XmlSerializer (typeof (ResponseWrapper), ResponseMessage.Types);

			try { 
				serializer.Serialize (this.client.GetStream (), new ResponseWrapper (response));
				// Send an end of message marker
				this.client.GetStream ().WriteByte (0xff);
			} catch (IOException e) {
				// The socket was shut down, so we can't write
				// any more.
				return false;
			}

			return true;
		}
		
		public void Close ()
		{
			if (this.client != null) {
				this.client.Close ();
				this.client = null;
			}

			if (this.executor != null) {
				this.executor.Cleanup ();
				this.executor.AsyncResponseEvent -= OnAsyncResponse;
			}
		}

		public void WatchCallback (IAsyncResult ar)
		{
			int bytes_read = this.client.GetStream ().EndRead (ar);

			if (bytes_read == 0)
				Close ();
			else
				SetupWatch ();
		}

		private void SetupWatch ()
		{
			this.client.GetStream ().BeginRead (new byte[1024], 0, 1024,
							    new AsyncCallback (WatchCallback), null);
		}

		private void OnAsyncResponse (ResponseMessage response)
		{
			if (!SendResponse (response))
				Close ();
		}

		private void PopulateExecutorMap ()
		{
			executor_map = new Hashtable ();
			
			foreach (Type t in Assembly.GetExecutingAssembly ().GetTypes ()) {
				if (!t.IsSubclassOf (typeof (RequestMessageExecutor)))
					continue;

				Attribute attr = Attribute.GetCustomAttribute (t, typeof (RequestMessageAttribute));

				if (attr == null) {
					Console.WriteLine ("No handler attribute for executor {0}", t);
					continue;
				}

				RequestMessageAttribute pra = (RequestMessageAttribute) attr;

				executor_map [pra.MessageType] = t;
			}
		}

		public void HandleConnection ()
		{
			XmlSerializer req_serializer = new XmlSerializer (typeof (RequestWrapper), RequestMessage.Types);
			RequestMessage req = null;
			ResponseMessage resp = null;

			bool force_close_connection = false;
			
			// Read the data off the socket and store it in a
			// temporary memory buffer.  Once the end-of-message
			// character has been read, discard remaining data
			// and deserialize the request.
			byte[] network_data = new byte [4096];
			MemoryStream buffer_stream = new MemoryStream ();
			int bytes_read, end_index = -1;

			do {
				bytes_read = this.client.GetStream ().Read (network_data, 0, 4096);

				if (bytes_read > 0) {
					// 0xff signifies end of message
					end_index = Array.IndexOf (network_data, (byte) 0xff);

					buffer_stream.Write (network_data, 0,
							     end_index == -1 ? bytes_read : end_index);
				}
			} while (bytes_read > 0 && end_index == -1);

			buffer_stream.Seek (0, SeekOrigin.Begin);

			try {
				RequestWrapper wrapper = (RequestWrapper) req_serializer.Deserialize (buffer_stream);

				req = wrapper.Message;
			} catch (Exception e) {
				resp = new ErrorResponse (e);
				force_close_connection = true;
			}

			buffer_stream.Close ();

			// If XmlSerializer can't deserialize the payload, we
			// may get a null payload and not an exception.  Or
			// maybe the client just didn't send one.
			if (req == null && resp == null) {
				resp = new ErrorResponse ("Missing payload");
				force_close_connection = true;
			}

			// And if there are no errors, execute the command
			if (resp == null) {
				if (executor_map == null)
					PopulateExecutorMap ();

				Type t = (Type) executor_map [req.GetType ()];

				if (t == null) {
					resp = new ErrorResponse (String.Format ("No handler available for {0}", req.GetType ()));
					force_close_connection = true;
				} else {
					RequestMessageExecutor exec = (RequestMessageExecutor) Activator.CreateInstance (t);

					if (req.Keepalive) {
						this.executor = exec;
						exec.AsyncResponseEvent += OnAsyncResponse;
					}

					resp = exec.Execute (req);
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
				if (!this.SendResponse (resp))
					force_close_connection = true;
			}

			if (force_close_connection || !req.Keepalive)
				Close ();
			else
				SetupWatch ();
		}
	}

	public class Server {

		private string socket_name;
		private UnixListener listener;
		private bool running = false;

		public Server (string name)
		{
			this.socket_name = name;
		}

		private void Run ()
		{
			this.listener = new UnixListener (Path.Combine (PathFinder.StorageDir, this.socket_name));
			this.listener.Start ();
			this.running = true;

			while (this.running) {
				// This will block for an incoming connection.
				UnixClient client;
				try {
					client = this.listener.AcceptUnixClient ();
				} catch (SocketException) {
					// If the listener is stopped while we
					// wait for a connection, a
					// SocketException is thrown.
					return;
				}
				ConnectionHandler handler = new ConnectionHandler (client);
				ExceptionHandlingThread.Start (new ThreadStart (handler.HandleConnection));
			}
		}

		public void Start ()
		{
			ExceptionHandlingThread.Start (new ThreadStart (this.Run));
		}

		public void Stop ()
		{
			this.running = false;
			this.listener.Stop ();
		}
	}
}