//
// Message.cs
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
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle {

	public abstract class Message {

		protected static Type[] GetTypes (Type parent_type, Type attr_type)
		{
			ArrayList types = new ArrayList ();

			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies ())
				types.AddRange (ReflectionFu.GetTypesFromAssemblyAttribute (a, attr_type));

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
		private static object type_lock = new object ();

		private Hashtable handlers = new Hashtable ();

		// A list of clients which will receive this message 
		protected Hashtable clients = new Hashtable ();

		// How many clients have completed
		protected int clients_finished;

		[XmlIgnore]
		public bool Keepalive;

		public delegate void AsyncResponseHandler (ResponseMessage response);

		public delegate void Closed ();
		public event Closed ClosedEvent;

		// This is why names arguments (like in python) are a good idea.

		public RequestMessage (bool keepalive, bool local, string client_name)
		{
			this.Keepalive = keepalive;

			if (local)
				this.clients.Add ("local", new ClientContainer (true, typeof(UnixSocketClient), client_name));
		}

		public RequestMessage (bool keepalive, string client_name)
			: this (keepalive, true, client_name)
		{
		}

		public RequestMessage (bool keepalive) : this (keepalive, true, null)
		{
		}

		// Recommended: Use this only when local = false
		public RequestMessage (bool keepalive, bool local) : this (keepalive, local, null)
		{
		}

		public RequestMessage (string client_name) : this (false, true, client_name)
		{
		}

		public RequestMessage () : this (false, null)
		{
		}
		
		~RequestMessage ()
		{
			this.Close ();
		}

		public static Type[] Types {
			get {
				lock (type_lock) {
					if (request_types == null)
						request_types = GetTypes (typeof (RequestMessage), typeof (RequestMessageTypesAttribute));
				}

				return request_types;
			}
		}

		public void Close ()
		{
			lock (clients) {
				foreach (ClientContainer c in this.clients.Values) {
					if (c.Client != null)
						c.Client.Close ();
				}
			}
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

		// Again an irritating long list of overloaded methods instead of named parameters
		public void SetLocal (bool local)
		{
			SetLocal (local, null);
		}

		public void SetLocal (string client_name)
		{
			SetLocal (true, client_name);
		}

		public void SetLocal (bool local, string client_name)
		{
			lock (this.clients) {
				if (! local) {
					if (this.clients.Contains ("local"))
						this.clients.Remove ("local");
				} else {
					if (! this.clients.Contains ("local"))
						this.clients.Add ("local", new ClientContainer (true, typeof(UnixSocketClient), client_name));
				}
			}
		}

		// url: host:port
		public void SetRemote (string url)
		{
			// Hashtable will replace existing clientcontainer (if any), which will
			// in turn close the clients when GC will collect them
			lock (this.clients)
				this.clients [url] = new ClientContainer (false, typeof (HttpClient), url);
		}

		virtual public void SendAsync ()
		{
			lock (clients) {
				// FIXME: Throw a custom exception and catch it upwards
				if (this.clients.Count == 0)
					throw new Exception ("No clients available for querying");
				
				foreach (ClientContainer c in this.clients.Values) {
					if (c.Client != null)
						c.Client.Close ();
					
					c.CreateClient ();
					c.Client.AsyncResponseEvent += OnAsyncResponse;
					c.Client.ClosedEvent += OnClosedEvent;
					c.Client.SendAsync (this);
					
					// FIXME: Maybe it's not right to throw an exception anymore (silently fail)?
					// Or maybe throw exceptions only for local fails?
				}
			}
		}

		public void SendAsyncBlocking ()
		{
			lock (clients) {
				if (this.clients.Count == 0)
					// FIXME: Throw a custom exception and catch it upwards, also better message
					throw new Exception ("No where to send data, add local querydomain or add neighbourhood domain with some hosts");

				foreach (ClientContainer c in this.clients.Values) {
					c.CreateClient ();
					c.Client.AsyncResponseEvent += OnAsyncResponse;
					c.Client.ClosedEvent += OnClosedEvent;
					c.Client.SendAsyncBlocking (this);
				}
			}
		}

		// FIXME: This still breaks API!!!
		// Was only a ResponseMessage prior to merge, not an array.
		// I'm not sure how to fix it, yet :-)
		public ResponseMessage[] Send ()
		{
			ArrayList responses = new ArrayList ();
			
			foreach (ClientContainer c in clients.Values)
			{
				c.CreateClient ();
				//Logger.Log.Debug ("Sending message");
				ResponseMessage resp = c.Client.Send (this);
				//Logger.Log.Debug ("Got reply");
				c.Client.Close ();
				//Logger.Log.Debug ("Closed client");
	
				// Add some nice syntactic sugar by throwing an
				// exception if the response is an error.
				
				// FIXME: Maybe it's not right to throw an exception anymore (silently fail)? 
				// Or maybe throw exceptions only for local fails?
				ErrorResponse err = resp as ErrorResponse;		

				if (err != null)	
					throw new ResponseMessageException (err);

				responses.Add (resp);
			}
			
			return (ResponseMessage []) responses.ToArray (typeof (ResponseMessage));
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
		private static object type_lock = new object ();

		public static Type[] Types {
			get {
				lock (type_lock) {
					if (response_types == null)
						response_types = GetTypes (typeof (ResponseMessage), typeof (ResponseMessageTypesAttribute));
				}

				return response_types;
			}
		}
	}

	public class EmptyResponse : ResponseMessage { }

	public class ErrorResponse : ResponseMessage {

		public string ErrorMessage;
		public string Details;

		// Needed by the XmlSerializer for deserialization
		public ErrorResponse () { }

		public ErrorResponse (Exception e)
		{
			this.ErrorMessage = e.Message;
			this.Details = e.ToString ();
		}

		public ErrorResponse (string message)
		{
			this.ErrorMessage = message;
		}
	}

	public class ResponseMessageException : Exception {

		private string details;

		internal ResponseMessageException (ErrorResponse response) : base (response.ErrorMessage)
		{ 
			Log.Debug ("Creating a ResponseMessageException from an ErrorResponse");
			details = response.Details;
		}

		internal ResponseMessageException (Exception e) : base (e.Message, e) { }

		internal ResponseMessageException (Exception e, string message) : base (message, e) { }

		internal ResponseMessageException (Exception e, string message, string details) : base (message, e)
		{
			this.details = details;
		}

		internal ResponseMessageException (string message) : base (message) { }

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();

			sb.AppendFormat ("{0}: {1}", this.GetType (), this.Message);

			if (this.details != null)
				sb.AppendFormat ("\n  Details: {0}", this.details);

			if (this.InnerException != null) {
				sb.Append ("\n  Inner exception: ");
				sb.Append (this.InnerException.ToString ());
			}

			return sb.ToString ();
		}
	}

	internal class ClientContainer
	{
		private System.Type client_type;
		private bool local = false;
		private string id = null;
		
		private Client client = null;

		public ClientContainer (bool local, System.Type client_type, string id)
		{
			this.local = local;
			this.client_type = client_type;
			this.id = id;
		}
		
		public void CreateClient ()
		{
			client = (Client) System.Activator.CreateInstance (client_type, new object[] { id });
		}

		public Client Client {
			get { return client; }
		}

		public bool Local {
			get { return local; }
		}
	}

	[AttributeUsage (AttributeTargets.Assembly)]
	public class RequestMessageTypesAttribute : TypeCacheAttribute {
		public RequestMessageTypesAttribute (params Type[] message_types) : base (message_types) { }
	}

	[AttributeUsage (AttributeTargets.Assembly)]
	public class ResponseMessageTypesAttribute : TypeCacheAttribute {
		public ResponseMessageTypesAttribute (params Type[] message_types) : base (message_types) { }
	}

	[AttributeUsage (AttributeTargets.Assembly)]
	public class RequestMessageExecutorTypesAttribute : TypeCacheAttribute {
		public RequestMessageExecutorTypesAttribute (params Type[] executor_types) : base (executor_types) { }
	}
}
