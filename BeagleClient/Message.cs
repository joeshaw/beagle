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
		private static object type_lock = new object ();

		private Hashtable handlers = new Hashtable ();
		private string client_name;
		private Client client;

		[XmlIgnore]
		public bool Keepalive;

		public delegate void AsyncResponseHandler (ResponseMessage response);

		public delegate void Closed ();
		public event Closed ClosedEvent;

		// This is why names arguments (like in python) are a good idea.

		public RequestMessage (bool keepalive, string client_name)
		{
			this.Keepalive = keepalive;
			this.client_name = client_name;
		}

		public RequestMessage (bool keepalive) : this (keepalive, null)
		{

		}

		public RequestMessage (string client_name) : this (false, client_name)
		{

		}

		public RequestMessage () : this (false, null)
		{

		}

		public static Type[] Types {
			get {
				lock (type_lock) {
					if (request_types == null)
						request_types = GetTypes (typeof (RequestMessage));
				}

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
			if (this.client != null)
				this.client.Close ();
			this.client = new Client (this.client_name);
			this.client.AsyncResponseEvent += OnAsyncResponse;
			this.client.ClosedEvent += OnClosedEvent;
			this.client.SendAsync (this);
		}

		public ResponseMessage Send ()
		{
			Client client = new Client (this.client_name);
			Logger.Log.Debug ("Sending message");
			ResponseMessage resp = client.Send (this);
			Logger.Log.Debug ("Got reply");
			client.Close ();
			Logger.Log.Debug ("Closed client");

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
		private static object type_lock = new object ();

		public static Type[] Types {
			get {
				lock (type_lock) {
					if (response_types == null)
						response_types = GetTypes (typeof (ResponseMessage));
				}

				return response_types;
			}
		}
	}

	public class EmptyResponse : ResponseMessage { }

	public class ErrorResponse : ResponseMessage {
		public string Message;

		// Needed by the XmlSerializer for deserialization
		public ErrorResponse () { }

		public ErrorResponse (Exception e)
		{
			this.Message = e.Message;
		}

		public ErrorResponse (string message)
		{
			this.Message = message;
		}
	}

	public class ResponseMessageException : Exception {

		internal ResponseMessageException (ErrorResponse response) : base (response.Message) { }

		internal ResponseMessageException (Exception e) : base (e.Message) { }
	}
}
