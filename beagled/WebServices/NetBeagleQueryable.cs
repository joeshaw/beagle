//
// NetBeagleQueryable.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Authors:
//	Vijay K. Nanjundaswamy (knvijay@novell.com)
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
using System.Timers;
using System.Threading;
using System.Collections;

using Beagle;
using Beagle.Util;

namespace Beagle.Daemon {

	[QueryableFlavor (Name="NetworkedBeagle", Domain=QueryDomain.Global, RequireInotify=false)]
	public class NetworkedBeagle : IQueryable 
	{					
		static Logger log = Logger.Get ("NetworkedBeagle");
	    static readonly string NetBeagleConfigFile = "netbeagle.cfg";
		public static readonly string BeagleNetPrefix = "netbeagle://";
		public static bool NetBeagleListActive = false;
				
		ArrayList NetBeagleList;
		
		public NetworkedBeagle ()
		{
			NetBeagleList = new ArrayList ();	
			NetBeagleListActive = false;	
		}

/////////////////////////////////////////////////////////////////////////////////////////			
		private static Hashtable requestTable 	= Hashtable.Synchronized(new Hashtable());
		private static Hashtable timerTable 	= Hashtable.Synchronized(new Hashtable());
		
		private static int TimerInterval = 15000; 	//15 sec timer
		private static int RequestCacheTime = 20;  //Cache requests for 5 minutes
		
		private static System.Timers.Timer cTimer = null;
		
		static NetworkedBeagle () {
			cTimer = new System.Timers.Timer(TimerInterval);
			cTimer.Elapsed += new ElapsedEventHandler(TimerEventHandler);
			cTimer.AutoReset = true;
			cTimer.Enabled = false;
		}	
		
		~NetworkedBeagle()
		{
			cTimer.Close();		
		}		
/////////////////////////////////////////////////////////////////////////////////////////				
		public string Name {
			get { return "NetworkedBeagle"; }
		}

		public void Start () 
		{
			SetupNetBeagleList ();
			Conf.Subscribe (typeof (Conf.NetworkingConfig), new Conf.ConfigUpdateHandler (NetBeagleConfigurationChanged));
		}

		void SetupNetBeagleList ()
		{	
			//First check for ~/.beagle/config/networking.xml configuration
			ArrayList NetBeagleNodes = Conf.Networking.NetBeagleNodes;
			
			if ((NetBeagleNodes != null) && (NetBeagleNodes.Count > 0)) {
					
					foreach (string nb in NetBeagleNodes) {
						if (nb == null || nb == "")
							continue;					
						string[] data = nb.Split (':');
						if (data.Length < 2) {
							log.Warn("NetBeagleQueryable: Ignoring improper NetBeagle entry: {0}", nb);
							continue; 
						}
						string host = data[0];
						string port = data[1];		
						NetBeagleList.Add (new NetBeagleHandler (host, port, this));
					}
					
				if (NetBeagleList.Count > 0) {
					NetBeagleListActive = true;
					if (File.Exists (Path.Combine (PathFinder.StorageDir, NetBeagleConfigFile)))
					{
						log.Warn("NetBeagleQueryable: Duplicate configuration of networked Beagles detected!");
						log.Info("NetBeagleQueryable: Remove '~/.beagle/netbeagle.cfg' file. Use 'beagle-config' instead to setup networked Beagle nodes.");		
						log.Info("Using ~/.beagle/config/networking.xml");	
					}
					return;
				}
			}
			
			//Fallback to ~/.beagle/netbeagle.cfg
			if (!File.Exists (Path.Combine (PathFinder.StorageDir, NetBeagleConfigFile)))
				return;

			StreamReader reader = new StreamReader(Path.Combine 
										(PathFinder.StorageDir, NetBeagleConfigFile));

			string entry;
			while ( ((entry = reader.ReadLine ()) != null) && (entry.Trim().Length > 1)) {
			
				if ((entry[0] != '#') && (entry.IndexOf(':') > 0)) {
					string[] data = entry.Split (':');
					if (data.Length < 2) {
						log.Warn("NetBeagleQueryable: Ignoring improper NetBeagle entry: {0}", entry);
						continue; 
					}					

					string host = data[0];
					string port = data[1];		
					NetBeagleList.Add (new NetBeagleHandler (host, port, this));					
				}								
			}

			if (NetBeagleList.Count > 0) {
				NetBeagleListActive = true;
				//log.Warn("NetBeagleQueryable: 'netbeagle.cfg' based configuration deprecated.\n Use 'beagle-config' or 'beagle-settings' instead to configure Networked Beagles");
				log.Warn("NetBeagleQueryable: 'netbeagle.cfg' based configuration deprecated.\n Use 'beagle-config' instead to configure Networked Beagles");				
			}
		}

		private void NetBeagleConfigurationChanged (Conf.Section section)
		{			
			Logger.Log.Info("NetBeagleConfigurationChanged EventHandler invoked");		
			if (! (section is Conf.NetworkingConfig))
				return;
				
			Conf.NetworkingConfig nc = (Conf.NetworkingConfig) section;

			//if (nc.NetBeagleNodes.Count == 0)
			//	return;
					
			ArrayList newList = new ArrayList();
			foreach (string nb in nc.NetBeagleNodes) {
					if (nb == null || nb == "")
						continue;					
					string[] data = nb.Split (':');
					if (data.Length < 2) {
						log.Warn("NetBeagleQueryable: Ignoring improper NetBeagle entry: {0}", nb);
						continue; 
					}						
					string host = data[0];
					string port = data[1];		
					newList.Add (new NetBeagleHandler (host, port, this));												
			}	
			
			lock (this) {
				NetBeagleList = newList;
				NetBeagleListActive = (newList.Count == 0) ? false:true;
			}			 
		}
	
		public bool AcceptQuery (Query query)
		{      
		    if (query.Text.Count <= 0)
				return false;

		    if (! query.AllowsDomain (QueryDomain.Global))
				return false;

			return true;
		}
		
		public string GetSnippet (string[] query_terms, Hit hit)
		{
			string s = "";
			
			if (hit is NetworkHit)
				s = ((NetworkHit)hit).snippet;
			
			return s;
		}
		
		public int GetItemCount ()
		{
			return -1;
		}	
		
		public void DoQuery (Query query,IQueryResult result,
				     		 IQueryableChangeData changeData)
		{				
			if (NetBeagleList.Count == 0) 
				return;
				
			ArrayList resultHandleList = new ArrayList(); 
			lock (NetBeagleList) {	
				log.Debug("NetBeagleQueryable: DoQuery ... Starting NetBeagleHandler queries");
				foreach (NetBeagleHandler nb in NetBeagleList)
				{
					IAsyncResult iar = nb.DoQuery (query, result, changeData);
					resultHandleList.Add (iar);
				}
			}
			
			int i = 0;			
			foreach (IAsyncResult iar in resultHandleList) 
				while (! ((ReqContext)(iar.AsyncState)).RequestProcessed) { 
						Thread.Sleep(1000); 
						if (++i > 20) 	//Don't wait more than 20 secs
							break;
				}
				
			log.Debug("NetBeagleQueryable:DoQuery ... Done");
		}	

/////////////////////////////////////////////////////////////////////////////////////////	
		//Methods related to checking & caching of networked search requests,
		//to prevent duplicate queries in cascaded network operation 
		
		class TimerHopCount {
			int ttl = 0;
			int hops = 0;
			
			public TimerHopCount (int h) {
				this.hops = h;
				this.ttl = RequestCacheTime;
			}
			
			public int TTL {
				get {return ttl;}
				set {ttl = value;}
			} 
			
			public int Hops {
				get {return hops;}
				//set {hops = value;}			
			}
		}
		
		public static int AddRequest(Query q)
		{
			int searchId = 0;
			lock (timerTable) {
			
				if (requestTable.Contains(q))
					return  (int) requestTable[q];
			
				searchId = System.Guid.NewGuid().GetHashCode();
					
				if (searchId < 0) 
					searchId = -searchId;

				requestTable.Add(q, searchId);
				timerTable.Add(q, new TimerHopCount(1));
				
				if (!cTimer.Enabled) {
					cTimer.Start();		
					log.Debug("CachedRequestCleanupTimer started");
				}
			}									
			
			return searchId;			
		}
		
		public static int HopCount(Query q) 
		{
			int hops = -1; 
			
			lock (timerTable)
			{
				if (timerTable.Contains(q))
					hops = ((TimerHopCount) timerTable[q]).Hops; 
			}
			
			return hops;		
		}
		
		public static void CacheRequest(Query q, int searchId, int hops)
		{	
			lock (timerTable) {
				
				if (requestTable.Contains(q)) {
					requestTable[q] = searchId; 
					timerTable[q] = new TimerHopCount(hops);
				}
				else {
					requestTable.Add(q, searchId);
					timerTable.Add(q, new TimerHopCount(hops));
				}
				
				if (!cTimer.Enabled) {
					cTimer.Start();		
					log.Info("CachedRequestCleanupTimer started");			
				}
				
				log.Info("CacheRequest: HopCount = " + hops);
			}		
		}		
			
		public static bool IsCachedRequest(int searchId)
		{
			bool cached = false;
			
			lock (timerTable)
				cached = requestTable.ContainsValue(searchId);
			
			return cached;
		}

		private static void TimerEventHandler(object source, ElapsedEventArgs e)
		{
			int c = 0;
			
			ArrayList keys = new ArrayList();
			keys.AddRange(timerTable.Keys);
			
			foreach (Query q in keys)
      		{
      			TimerHopCount thc = (TimerHopCount) timerTable[q];
				c = thc.TTL;
				if (c > 0) 
					c--;  
				
				if (c == 0)
					RemoveRequest(q);
				else 
					thc.TTL = c;				
			}
			
			if ((c % 4) == 0)		//Log status every 1 minute
				log.Info("CachedRequestCleanupTimer-EventHandler: requestTable has {0} elements, Last entry count={1}", requestTable.Count, c);

			if (timerTable.Count == 0) {				
				cTimer.Stop();
				log.Info("Stopping CachedRequestCleanupTimer");
			}			
		}
		
		private static void RemoveRequest(Query q)
		{
			lock (timerTable) {
			
				if (requestTable.Contains(q)) 
					requestTable.Remove(q);
				
				if (timerTable.Contains(q))
					timerTable.Remove(q);
			}
		}						
	}
}
