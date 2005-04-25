//
// UnixListener.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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
using System.Net;
using System.Net.Sockets;
using Mono.Posix;

using System.IO;

namespace Beagle.Util {

	public class UnixListener {
		private bool active = false;
		private Socket server;
		private EndPoint savedEP;
        
		private void Init (AddressFamily family, EndPoint ep)
		{
			UnixListener.Cleanup (ep);
			active = false;
			server = new Socket (family, SocketType.Stream, 0);
			server.Bind (ep);
			savedEP = server.LocalEndPoint;
		}
        
		public UnixListener (string path)
		{
			if (!Directory.Exists (Path.GetDirectoryName (path)))
				Directory.CreateDirectory (Path.GetDirectoryName (path));
            
			Init (AddressFamily.Unix, new UnixEndPoint (path));
		}

		public UnixListener (UnixEndPoint local_end_point)
		{
			if (local_end_point == null)
				throw new ArgumentNullException ("local_end_point");

			Init (local_end_point.AddressFamily, local_end_point);
		}
        
		protected bool Active {
			get { return active; }
		}

		public EndPoint LocalEndpoint {
			get { return savedEP; }
		}
        
		protected Socket Server {
			get { return server; }
		}
        
		public Socket AcceptSocket ()
		{
			if (!active)
				throw new InvalidOperationException ("Socket is not listening");

			return server.Accept ();
		}
        
		public UnixClient AcceptUnixClient ()
		{
			if (!active)
				throw new InvalidOperationException ("Socket is not listening");

			UnixClient client = new UnixClient ();
			// use internal method SetTcpClient to make a
			// client with the specified socket
			client.SetUnixClient (AcceptSocket ());

			return client;
		}
        
		~UnixListener ()
		{
			if (active)
				Stop ();
		}
    
		public bool Pending ()
		{
			if (!active)
				throw new InvalidOperationException ("Socket is not listening");

			return server.Poll (1000, SelectMode.SelectRead);
		}
        
		public void Start ()
		{
			if (active)
				return;

			// According to the man page some BSD-derived systems
			// limit the backlog to 5.  This should really be
			// configurable though
			server.Listen (5);

			active = true;
		}
        
		public void Stop ()
		{
			if (active) {
				server.Shutdown (SocketShutdown.Both);
				server.Close ();
				UnixListener.Cleanup (savedEP);
			}

			// Init (AddressFamily.Unix, savedEP);
		}

		private static void Cleanup (EndPoint ep)
		{
			string path = ((UnixEndPoint) ep).Filename;
			if (File.Exists (path))
				File.Delete (path);
			if (File.Exists (path))
				Console.WriteLine ("File {0} still exists");
		}
	}

}
