//
// NetworkService.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Authors:
//   Fredrik Hedberg (fredrik.hedberg@avafan.com)
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
using System.Xml;
using System.Text;
using System.Threading;

using System.Net;
using System.Net.Sockets;

using System.Reflection;
using System.Collections;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

#if ENABLE_RENDEZVOUS
using Mono.P2p.mDnsResponder;
using Mono.P2p.mDnsResponderApi;
#endif

using Beagle;
using Beagle.Util;

namespace Beagle.Daemon 
{
	public class NetworkService
	{
		private static Logger log = Logger.Get ("Network");
		
		QueryDriver queryDriver;
		ArrayList authenticators;
		
		TcpListener server;
		Thread serverThread;
		
		int localPort = 8505;
		string localAddress = "EVO";
		static string serviceName = "beagle._tcp.local";

		public NetworkService (QueryDriver queryDriver, int localPort)
		{
			this.queryDriver = queryDriver;

			if (localPort != 0)
				this.localPort = localPort;

			authenticators = new ArrayList();
		}		

		// Start the show

		public void Start ()
		{
			log.Debug ("Network service starting");

			ScanForAuthenticators ();

			try {
				server = new TcpListener (localPort); 
				server.Start();
			} catch (Exception e) {
				log.Error("Network service could not be started: {0}",e);
				return;
			}
		       
			serverThread = new Thread (new ThreadStart (Listen));
                        serverThread.Start ();

			RegisterWithRendezvous ();

			log.Debug ("Network service started");
		}

		// Shutdown everything

		public void Stop ()
		{
			log.Debug ("Network service stopping");

			serverThread.Suspend ();

			log.Debug ("Network service stopped");
		}

		// Service handling loop

		void Listen ()
		{
			while (true) {
				TcpClient client = server.AcceptTcpClient ();
				ServerNetworkHandler handler = new ServerNetworkHandler (client, queryDriver, this);
				handler.Start ();
			}
		}

		// Register with rendezvous
		
		void RegisterWithRendezvous ()
		{
#if ENABLE_RENDEZVOUS			
			log.Debug ("Starting rendezvous service");
			
			// Should we start the mDnsResponder service if it's not running?

			try {
				IRemoteFactory factory = (IRemoteFactory) Activator.GetObject (typeof (IRemoteFactory),
					       "tcp://localhost:8091/mDnsRemoteFactory.tcp");

				IResourceRegistration rr = factory.GetRegistrationInstance ();		

				rr.RegisterServiceLocation (localAddress,serviceName, localPort, 0, 0);
			}
			catch (RemotingException e)
			{
				log.Debug ("No multicast service found, not advertising via rendezvous");
			}
			finally
			{
				log.Debug ("Done registering rendezvous service");
			}
#endif
		}

		// Scan assembly for authenticators

		void ScanForAuthenticators ()
		{
			log.Debug ("Scanning for network authenticators");

			authenticators.Clear ();

			foreach (Type type in Assembly.GetCallingAssembly ().GetTypes ()) {

				if ( !TypeImplementsInterface (type, typeof (IHitAuthenticator)))
				   continue;
				
				log.Debug ("Found network authenticator: {0}", type.ToString ());
   
				IHitAuthenticator authenticator = (IHitAuthenticator) Activator.CreateInstance (type);

				authenticators.Add (authenticator);
			}

			log.Debug ("Done scanning for network authenticators");
		}

		// From QueryDriver.cs

		static bool ThisApiSoVeryIsBroken (Type m, object criteria) 
		{
                        return m == (Type) criteria;
                }

                static bool TypeImplementsInterface (Type t, Type iface)
                {
                        Type[] impls = t.FindInterfaces (new TypeFilter (ThisApiSoVeryIsBroken),
                                                         iface);
                        return impls.Length > 0;
                }

		// Authenticate a hit

		public ArrayList AuthenticateHit (Hit hit, QueryBody query)
		{
			ArrayList result = new ArrayList ();

			foreach (IHitAuthenticator authenticator in authenticators) {
				try {
					result.AddRange (authenticator.Authenticate (hit, query));
				} catch {
					// Do nothing, hit not authenticated
				}
			}
			return result;
		}
	}

	public interface IHitAuthenticator
	{
		ICollection Authenticate (Hit hit, QueryBody query);
	}
}
