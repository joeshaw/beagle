//
// NetworkHandler.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Authors:
//	Fredrik Hedberg (fredrik.hedberg@avafan.com)
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

using System.Collections;

using DBus;

namespace Beagle.Daemon
{
	public class NetworkHandler
	{
		protected TcpClient client;
                protected NetworkStream stream;
		protected BinaryWriter writer;
		protected BinaryReader reader;

		public NetworkHandler (TcpClient client)
		{
			this.client = client;
			this.stream = client.GetStream ();
			this.writer = new BinaryWriter (stream);
			this.reader = new BinaryReader (stream);
		}

		public void Start ()
		{			
			Thread t = new Thread (new ThreadStart (Run));
                        t.Start ();
		}

		private void Run ()
		{      
			bool running = true;
			string command = "";

			try {
				while (running)
				{
					command = reader.ReadString ();
					
					switch (command) {
					case "query":
						QueryReceivedEvent (QueryBody.ReadAsBinary(reader)); 
						break;
					case "hits":
						ArrayList hits = new ArrayList();
						int hitCount = reader.ReadInt32 ();
						for (int i = 0; i < hitCount; i++) {
							hits.Add (Hit.ReadAsBinary (reader));
						}
						HitReceivedEvent (hits);
						break;
					case "quit":
						client.Close ();
						running = false;
						break;
					}
				}
			} catch (Exception e) {
				// EOS etc
			}
		}

		public delegate void QueryHandler (QueryBody query);
		public virtual event QueryHandler QueryReceivedEvent;

		public delegate void HitHandler (ICollection hits);
		public virtual event HitHandler HitReceivedEvent;
	}

	public class ClientNetworkHandler : NetworkHandler
	{
		IQueryResult result;

		public ClientNetworkHandler (TcpClient client, IQueryResult result) : base (client)
		{
			this.result = result;
			base.HitReceivedEvent += OnHitsReceived;
		}

		public void SendQuery (QueryBody query)
		{
			writer.Write ("query");
			query.WriteAsBinary (writer);
		}
		
		void OnHitsReceived (ICollection hits)
		{
			foreach (Hit hit in hits)
				result.Add (hit);
		}
	}

        public class ServerNetworkHandler : NetworkHandler
        {
		QueryBody query;
		NetworkService networkService;

		public ServerNetworkHandler (TcpClient client,
					     NetworkService networkService) : base(client) 
		{
			this.networkService = networkService;
			base.QueryReceivedEvent += OnQueryReceived;
		}

		void OnQueryReceived (QueryBody query) {
			this.query = query;
			QueryResult result = new QueryResult ();
			result.HitsAddedEvent += OnHitsAdded;
			QueryDriver.DoQuery (query,result);
		}

		public void SendHits (ICollection hits)
		{
			writer.Write ("hits");
		        writer.Write (hits.Count);
			foreach (Hit hit in hits) {
				hit.WriteAsBinary (writer);
			}
		}

		void OnHitsAdded (QueryResult result, ICollection hits)
                {
			ArrayList results = new ArrayList ();
			foreach (Hit hit in hits) {
				results.AddRange( networkService.AuthenticateHit (hit, query));
			}
                        this.SendHits (results); 
                }
        }
}
