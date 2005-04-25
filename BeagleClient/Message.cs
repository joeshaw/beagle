using System;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle {

	public abstract class Message {
		protected static Type[] GetTypes (Type parent_type)
		{
			ArrayList types = new ArrayList ();
			
			foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies ()) {
				if (ass == null) {
					Console.WriteLine ("THERE IS A NULL ASSEMBLY IN HERE.  WTF");
					continue;
				}

				Type[] ass_types = ass.GetTypes ();
			
				foreach (Type t in ass_types) {
					if (t == null) {
						Console.WriteLine ("THERE IS A NULL TYPE IN HERE.  WTF");
						continue;
					}

					if (t.IsSubclassOf (parent_type))
						types.Add (t);
				}
			}

			return (Type[]) types.ToArray (typeof (Type));
		}
	}

	public class RequestWrapper {
		public RequestMessage Message;

		// Needed by the XmlSerializer for deserialization
		public RequestWrapper () { }

		public RequestWrapper (RequestMessage request)
		{
			this.Message = request;
		}
	}
	
	public abstract class RequestMessage : Message {
		private static Type[] request_types = null;

		private Hashtable handlers = new Hashtable ();
		private Client client;

		[XmlIgnore]
		public bool Keepalive;

		public delegate void AsyncResponseHandler (ResponseMessage response);

		public delegate void Closed ();
		public event Closed ClosedEvent;

		public RequestMessage () : this (false) { }

		public RequestMessage (bool keepalive)
		{
			this.Keepalive = keepalive;
		}

		public static Type[] Types {
			get {
				if (request_types == null)
					request_types = GetTypes (typeof (RequestMessage));

				return request_types;
			}
		}

		public void Close ()
		{
			if (this.client != null)
				this.client.Close ();
		}

		public void RegisterAsyncResponseHandler (Type t, AsyncResponseHandler handler)
		{
			if (!t.IsSubclassOf (typeof (ResponseMessage)))
				throw new ArgumentException ("Type must be a subclass of ResponsePayload");

			this.handlers [t] = handler;
		}

		public void UnregisterAsyncResponseHandler (Type t)
		{
			if (!t.IsSubclassOf (typeof (ResponseMessage)))
				throw new ArgumentException ("Type must be a subclass of ResponsePayload");

			this.handlers.Remove (t);
		}

		private void OnClosedEvent ()
		{
			if (this.ClosedEvent != null)
				this.ClosedEvent ();
		}

		private void OnAsyncResponse (ResponseMessage response)
		{
			AsyncResponseHandler async_response = (AsyncResponseHandler) this.handlers [response.GetType ()];

			if (async_response != null) {
				async_response (response);
			}
		}

		public void SendAsync ()
		{
			this.client = new Client ();
			this.client.AsyncResponseEvent += OnAsyncResponse;
			this.client.ClosedEvent += OnClosedEvent;
			this.client.SendAsync (this);
		}

		public ResponseMessage Send ()
		{
			Client client = new Client ();
			ResponseMessage resp = client.Send (this);
			client.Close ();

			// Add some nice syntactic sugar by throwing an
			// exception if the response is an error.
			ErrorResponse err = resp as ErrorResponse;

			if (err != null)
				throw new ResponseMessageException (err);

			return resp;
		}

	}

	public abstract class RequestMessageExecutor {
		public delegate void AsyncResponse (ResponseMessage response);
		public event AsyncResponse AsyncResponseEvent;

		public abstract ResponseMessage Execute (RequestMessage req);

		protected void SendAsyncResponse (ResponseMessage response)
		{
			if (this.AsyncResponseEvent != null)
				this.AsyncResponseEvent (response);
		}

		// Really only worth overriding if the request is a keepalive
		public virtual void Cleanup () { }
	}

	[AttributeUsage (AttributeTargets.Class)]
	public class RequestMessageAttribute : Attribute {
		private Type message_type;

		public RequestMessageAttribute (Type message_type)
		{
			this.message_type = message_type;
		}

		public Type MessageType {
			get { return this.message_type; }
		}
	}

	public class ResponseWrapper {
		public ResponseMessage Message;

		// Needed by the XmlSerializer for deserialization
		public ResponseWrapper () { }

		public ResponseWrapper (ResponseMessage response)
		{
			this.Message = response;
		}
	}

	public abstract class ResponseMessage : Message {
		private static Type[] response_types = null;

		public static Type[] Types {
			get {
				if (response_types == null)
					response_types = GetTypes (typeof (ResponseMessage));

				return response_types;
			}
		}
	}

	public class ErrorResponse : ResponseMessage {
		public string Message;
		public Exception Exception;

		// Needed by the XmlSerializer for deserialization
		public ErrorResponse () { }

		public ErrorResponse (Exception e)
		{
			this.Message = e.Message;
			this.Exception = e;
		}

		public ErrorResponse (string message)
		{
			this.Message = message;
		}
	}

	public class ResponseMessageException : Exception {
		internal ResponseMessageException (ErrorResponse response) : base (response.Message, response.Exception) { }
	}
}
