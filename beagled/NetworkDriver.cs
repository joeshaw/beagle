//
// NetworkDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Authors:
//   Fredrik Hedberg (fredrik.hedberg@avafan.com)
//

using System;
using System.IO;
using System.Collections;

using System.Net;
using System.Net.Sockets;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

#if ENABLE_RENDEZVOUS
using Mono.P2p.mDnsResponder;
using Mono.P2p.mDnsResponderApi;
#endif 

using Beagle.Util;

namespace Beagle.Daemon {

	[QueryableFlavor (Name="Network", Domain=QueryDomain.Global)]
	public class NetworkDriver : IQueryable 
	{
		static Logger log = Logger.Get ("Network");
		
	        string configuration = "peers.cfg";

		// When the peerlist was last updated with rendezvous
	        DateTime peersUpdated;

		// Available peers
		ArrayList staticPeers;
		ArrayList rendezvousPeers;
		
		// This event is never fired, but needs to be here for us
		// to fully implement the IQueryable interface.
		public event IQueryableChangedHandler ChangedEvent;

		public NetworkDriver ()
		{
			staticPeers = new ArrayList ();		
			rendezvousPeers = new ArrayList ();
		}

		public string Name {
			get { return "Network"; }
		}

		public void Start () 
		{
			LoadPeers ();

			try
			{
				UpdatePeers ();
			}
			catch (RemotingException e)
			{
				log.Error ("Could not fetch peers via rendezvous");
			}
		}

		void LoadPeers ()
		{
			if (!File.Exists (Path.Combine (PathFinder.RootDir, configuration)))
				return;

			StreamReader reader = new StreamReader(
				Path.Combine (PathFinder.RootDir, configuration));

			string line = "";

			while ( (line = reader.ReadLine ()) != null) {
				string[] data = line.Split (':');
				staticPeers.Add (new NetworkPeer (data[0], Convert.ToInt32 (data[1])));
			}
		}

		void UpdatePeers ()
		{
#if ENABLE_RENDEZVOUS
			log.Debug ("Updating rendezvous peers");

			rendezvousPeers.Clear ();		
	
			IRemoteFactory factory = (IRemoteFactory) Activator.GetObject (typeof (IRemoteFactory),
                                "tcp://localhost:8091/mDnsRemoteFactory.tcp");
			
			IResourceQuery query = factory.GetQueryInstance ();                       
			ServiceLocation[] services = null;

                        if (query.GetServiceLocationResources (out services) == 0)
                        {
				foreach (ServiceLocation service in services)
                                {
					// FIXME Dont add myself dammit!

					if (service.Name == "beagle._tcp.local")
						rendezvousPeers.Add (new NetworkPeer(service));
				}
			}

			peersUpdated = DateTime.Now;
#endif
		}

		public bool AcceptQuery (QueryBody body)
		{      
		        if (! body.HasText)
				return false;

		        if (! body.AllowsDomain (QueryDomain.Global))
				return false;

			return true;
		}

		public void DoQuery (QueryBody body,
				     IQueryResult result,
				     IQueryableChangeData changeData)
		{
			// FIXME Change update period to shortest peer TTL

			if (DateTime.Now.Subtract(peersUpdated).Seconds > 120)
				UpdatePeers();

			foreach (NetworkPeer peer in rendezvousPeers)
			{
				peer.DoQuery (body, result, changeData);
			}

			foreach (NetworkPeer peer in staticPeers)
			{
				peer.DoQuery (body, result, changeData);
			}
		}

		public string GetHumanReadableStatus ()
		{
			return "FIXME: Needs Status";
		}
	}

	public class NetworkPeer
	{
		static Logger log = Logger.Get ("Network");

		public NetworkPeer (string hostname, int port)
		{
			Hostname = hostname;
			Port = port;
		}

#if ENABLE_RENDEZVOUS		
		public NetworkPeer (ServiceLocation service) 
		{      
			Hostname = service.Target;
			Port = service.Port;
		}
#endif

		public string Hostname;
		public int Port;
		
		public void DoQuery (QueryBody body,
				     IQueryResult result,
				     IQueryableChangeData changeData)
		{
			log.Debug ("Querying " + Hostname + ":" + Port);
			
			TcpClient client = new TcpClient (Hostname,Port);
			ClientNetworkHandler handler = new ClientNetworkHandler (client, result);
			handler.Start ();
			handler.SendQuery (body);
		}
	}
}
