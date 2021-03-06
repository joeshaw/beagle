//
// Mono.ASPNET.ApplicationServer
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net.Sockets;

namespace Mono.ASPNET
{
	public class LingeringNetworkStream : NetworkStream 
	{
		const int useconds_to_linger = 2000000;
		const int max_useconds_to_linger = 30000000;
		bool enableLingering = true;
		// We dont actually use the data from this buffer. So we cache it...
		static byte [] buffer;

		public LingeringNetworkStream (Socket sock, bool owns) : base (sock, owns)
		{
		}
		
		public bool EnableLingering
		{
			get { return enableLingering; }
			set { enableLingering = value; }
		}

		void LingeringClose ()
		{
			int waited = 0;

			if (!Connected)
				return;

			Socket.Shutdown (SocketShutdown.Send);
			while (waited < max_useconds_to_linger) {
				int nread = 0;
				try {
					if (!Socket.Poll (useconds_to_linger, SelectMode.SelectRead))
						break;

					if (buffer == null)
						buffer = new byte [512];

					nread = Socket.Receive (buffer, 0, buffer.Length, 0);
				} catch { }

				if (nread == 0)
					break;

				waited += useconds_to_linger;
			}
		}

		public override void Close ()
		{
			if (enableLingering) {
				try {
					LingeringClose ();
				} finally {
					base.Close ();
				}
			}
			else
				base.Close ();
		}

		public bool Connected {
			get { return Socket.Connected; }
		}
	}
}
