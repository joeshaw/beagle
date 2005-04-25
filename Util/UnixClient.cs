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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Mono.Posix;

namespace Beagle.Util {

	public class UnixClient : IDisposable {
		private NetworkStream stream;
		private bool active;
		private Socket client;
		private bool disposed = false;

		public PeerCred PeerCredential {
			get { return new PeerCred (client); }
		}
        
		private void Init (AddressFamily family)
		{
			active = false;

			if (client != null) {
				client.Close ();
				client = null;
			}

			client = new Socket (family, SocketType.Stream, 0);
		}

		public UnixClient ()
		{
			Init (AddressFamily.Unix);
		}

		public UnixClient (string path) : this ()
		{
			Connect (path);
		}
                
		protected bool Active {
			get { return active; }
			set { active = value; }
		}
        
		protected Socket Client {
			get { return client; }
			set {
				client = value;
				stream = null;
			}
		}

		internal void SetUnixClient (Socket s)
		{
			Client = s;
		}
        
		public LingerOption LingerState {
			get {
				return (LingerOption) client.GetSocketOption (SocketOptionLevel.Socket,
									      SocketOptionName.Linger);
			}

			set {
				client.SetSocketOption (SocketOptionLevel.Socket,
							SocketOptionName.Linger, value);
			}
		}

		public int ReceiveBufferSize {
			get {
				return (int) client.GetSocketOption (SocketOptionLevel.Socket,
								     SocketOptionName.ReceiveBuffer);
			}

			set {
				client.SetSocketOption (SocketOptionLevel.Socket,
							SocketOptionName.ReceiveBuffer, value);
			}
		}
            
		public int ReceiveTimeout {
			get {
				return (int) client.GetSocketOption (SocketOptionLevel.Socket,
								     SocketOptionName.ReceiveTimeout);
			}

			set {
				client.SetSocketOption (SocketOptionLevel.Socket,
							SocketOptionName.ReceiveTimeout, value);
			}
		}
        
		public int SendBufferSize {
			get {
				return (int) client.GetSocketOption (SocketOptionLevel.Socket,
								     SocketOptionName.SendBuffer);
			}

			set {
				client.SetSocketOption (SocketOptionLevel.Socket,
							SocketOptionName.SendBuffer, value);
			}
		}
        
		public int SendTimeout {
			get {
				return (int) client.GetSocketOption (SocketOptionLevel.Socket,
								     SocketOptionName.SendTimeout);
			}

			set {
				client.SetSocketOption (SocketOptionLevel.Socket,
							SocketOptionName.SendTimeout, value);
			}
		}
        
		public void Close ()
		{
			this.Dispose ();
		}
        
		public void Connect (UnixEndPoint remote_end_point)
		{
			try {
				client.Connect (remote_end_point);
				stream = new NetworkStream (client, true);
				active = true;
			} finally {
				CheckDisposed ();
			}
		}
        
		public void Connect (string path)
		{
			Connect (new UnixEndPoint (path));
		}
        
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				// release managed resources
				NetworkStream s = stream;
				stream = null;
				if (s != null) {
					// This closes the socket as well, as the NetworkStream
					// owns the socket.
					s.Close();
					active = false;
					s = null;
				} else if (client != null){
					client.Close ();
				}
				client = null;
			}
		}
        
		~UnixClient ()
		{
			Dispose (false);
		}
        
		public NetworkStream GetStream()
		{
			try {
				if (stream == null)
					stream = new NetworkStream (client, true);

				return stream;
			} finally {
				CheckDisposed ();
			}
		}
        
		private void CheckDisposed () {
			if (disposed)
				throw new ObjectDisposedException (GetType().FullName);
		}        
	}
}
