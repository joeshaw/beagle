//
// WebBackEnd.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Authors:
//   Vijay K. Nanjundaswamy (knvijay@novell.com)
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
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.IO;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using Beagle;
using Beagle.Util;
using BT = Beagle.Tile;
using Beagle.Daemon;

namespace Beagle.WebService {

	[Serializable()]
	public struct webArgs 
	{
		public string 	sessId;
		public string 	searchString;
		public string 	searchSource;
		public bool		isLocalReq;
		public bool 	globalSearch;
	}
	
	public class WebBackEnd: MarshalByRefObject{

		static WebBackEnd instance = null;		
		static Logger log = Logger.Get ("WebBackEnd");
		
		private Hashtable result;
		private Hashtable sessionResp;

		public WebBackEnd() {
			result = Hashtable.Synchronized(new Hashtable());
			sessionResp = Hashtable.Synchronized(new Hashtable());
		}

		~WebBackEnd() {
			result.Clear(); 
			sessionResp.Clear();
		}		

		public bool allowGlobalAccess {
			get { return WebServiceBackEnd.web_global;  }
		}

		public string HostName {
			get { return WebServiceBackEnd.hostname; }
		}
		
		static TcpChannel tch1 = null;
		public static void init() 
		{		   
		   if (instance == null) {
			  instance = new WebBackEnd();

			  if (tch1 == null) {
			  	
			  	tch1 = new TcpChannel(8347);

			  	//Register TCP Channel Listener
		  	  	ChannelServices.RegisterChannel(tch1);	

			  	WellKnownServiceTypeEntry WKSTE = 
			  		new WellKnownServiceTypeEntry(typeof(WebBackEnd),
				 		"WebBackEnd.rem", WellKnownObjectMode.Singleton);
			  	RemotingConfiguration.ApplicationName="beagled";
			  	RemotingConfiguration.RegisterWellKnownServiceType(WKSTE);
			  }
		   }
		}
		
		public static void cleanup() 
		{
			if (tch1 != null) {
				tch1.StopListening(null);
				ChannelServices.UnregisterChannel(tch1);
				tch1 = null;
			}

			instance = null;
		}
		
		void OnHitsAdded (QueryResult qres, ICollection hits)
		{	
			if (result.Contains(qres)) {
			
				Resp resp = ((Resp) result[qres]);
				BT.SimpleRootTile root = resp.resultPair.rootTile;
				ArrayList hitsCopy = resp.resultPair.hitsCopy;
				
				lock (root)  {
					if (resp.isLocalReq) {
						root.Add(hits);
						lock (hitsCopy.SyncRoot)
							hitsCopy.AddRange(hits);
					}
					else {
							foreach (Hit h in hits)							
							   if (h.UriAsString.StartsWith(NetworkedBeagle.BeagleNetPrefix) ||
							 	 			WebServiceBackEnd.AccessFilter.FilterHit(h)) {
									root.Add(h);
									lock (hitsCopy.SyncRoot)
										hitsCopy.Add(h);
								}
					}
				}
			}
		}
		
		void removeUris(ArrayList res, ICollection uris)
		{
			foreach(Uri u in uris)
			   foreach(Hit h in res)
				if (h.Uri.Equals (u) && h.Uri.Fragment == u.Fragment) {
					lock (res.SyncRoot) {
						res.Remove(h);
					}
					break;
				}
		}
			
		void OnHitsSubtracted (QueryResult qres, ICollection uris)
		{
			if (result.Contains(qres)) {
				BT.SimpleRootTile root = ((Resp) result[qres]).resultPair.rootTile;
				lock (root) {
					root.Subtract (uris);
					removeUris(((Resp) result[qres]).resultPair.hitsCopy, uris);
				}
			}
		}

		void OnFinished (QueryResult qres)
		{
			if (result.Contains(qres))
				log.Info("WebBackEnd:OnFinished() - Got {0} results from beagled QueryDriver", ((Resp) result[qres]).resultPair.rootTile.HitCollection.NumResults);

			DetachQueryResult(qres);
		}

		void OnCancelled (QueryResult qres)
		{
			DetachQueryResult(qres);			
		}

		private void AttachQueryResult (QueryResult qres, Resp resp)
		{
			if (qres != null) {
			
				qres.HitsAddedEvent += OnHitsAdded;
				qres.HitsSubtractedEvent += OnHitsSubtracted;
				qres.FinishedEvent += OnFinished;
				qres.CancelledEvent += OnCancelled;

				result.Add(qres, resp);
			}
		}

		private void DetachQueryResult (QueryResult qres)
		{
			if (qres != null) {
			
				if (result.Contains(qres))
				{
					Resp resp = ((Resp) result[qres]);
					ArrayList hitsCopy = resp.resultPair.hitsCopy;
					if (hitsCopy != null)
						hitsCopy.Sort();
					
					resp.bufferContext.maxDisplayed = 0;
					
					result.Remove(qres);
				}
							
				qres.HitsAddedEvent -= OnHitsAdded;
				qres.HitsSubtractedEvent -= OnHitsSubtracted;
				qres.FinishedEvent -= OnFinished;
				qres.CancelledEvent -= OnCancelled;

				qres.Dispose ();
			}
		}

		const string NO_RESULTS = "No results.";
		
		private string getResultsLabel(BT.SimpleRootTile root)
		{
			string label;
			if (root.HitCollection.NumResults == 0)
				label = NO_RESULTS;
			else if (root.HitCollection.FirstDisplayed == 0) 
				label = String.Format ("<b>{0} results of {1}</b> are shown.", 
						       root.HitCollection.LastDisplayed + 1,
						       root.HitCollection.NumResults);
			else 
				label = String.Format ("Results <b>{0} through {1} of {2}</b> are shown.",
						       root.HitCollection.FirstDisplayed + 1, 
						       root.HitCollection.LastDisplayed + 1,
						       root.HitCollection.NumResults);	
			return label;
		}

		public bool canForward(string sessId)
		{	
			Resp resp = (Resp) sessionResp[sessId];
			if (resp == null) 
				return false;

			BT.SimpleRootTile root = resp.resultPair.rootTile;
			return (root != null)? root.HitCollection.CanPageForward:false;
		}

		public string doForward(string sessId)
		{	
			Resp resp = (Resp) sessionResp[sessId];

			if (!canForward(sessId) || (resp == null))
				return NO_RESULTS;
				
			BT.SimpleRootTile root = resp.resultPair.rootTile;
			if (root != null) {
				lock (root) {
					root.HitCollection.PageForward ();

					bufferRenderContext bctx = resp.bufferContext;
					bctx.init();					
					root.Render(bctx);
					return (getResultsLabel(root) + (resp.isLocalReq ? bctx.buffer:bctx.bufferForExternalQuery));
				}
			}

			return NO_RESULTS;
		}

		public bool canBack(string sessId)
		{	
			Resp resp = (Resp) sessionResp[sessId];
			if (resp == null) 
				return false;

			BT.SimpleRootTile root = resp.resultPair.rootTile;
			return (root != null) ? root.HitCollection.CanPageBack:false;
		}

		public string doBack(string sessId)
		{	
			Resp resp = (Resp) sessionResp[sessId];
			if (!canBack(sessId) || (resp == null))
				return NO_RESULTS;
		
			BT.SimpleRootTile root = resp.resultPair.rootTile;
			if (root != null) {
			
				lock (root) {
					root.HitCollection.PageBack();

					bufferRenderContext bctx = resp.bufferContext;
					bctx.init();					
					root.Render(bctx);									
					return (getResultsLabel(root) + (resp.isLocalReq ? bctx.buffer:bctx.bufferForExternalQuery));
				}
			}
			
			return NO_RESULTS;
		}
		
		public bool NetworkBeagleActive
		{
			get {return NetworkedBeagle.NetBeagleListActive;}
		}
	
		public string doQuery(webArgs wargs)
		{				 
			if (wargs.sessId == null || wargs.searchString == null || wargs.searchString == "")
				return NO_RESULTS;
						 
			log.Debug("WebBackEnd: Got Search String: " + wargs.searchString); 
			
			Query query = new Query();
			query.AddText (wargs.searchString);
			if (wargs.searchSource != null && wargs.searchSource != "")
			{
				query.AddSource(wargs.searchSource);
				query.AddDomain(QueryDomain.System);
			}
			else	
				query.AddDomain (wargs.globalSearch ? QueryDomain.Global:QueryDomain.System);

			QueryResult qres = new QueryResult ();
									
			//Note: QueryDriver.DoQuery() local invocation is used. 
			//The root tile is used only for adding hits and generating html.
			BT.SimpleRootTile root = new BT.SimpleRootTile (); 							
			root.Query = query;
			//root.SetSource (searchSource); Do not SetSource on root! 
											
			ResultPair rp = new ResultPair(root);
			bufferRenderContext bctx = new bufferRenderContext(rp);
			Resp resp = new Resp(rp, bctx, wargs.isLocalReq);

			AttachQueryResult (qres, resp);

			//Add sessionId-Resp mapping
			if (sessionResp.Contains(wargs.sessId)) 
				sessionResp[wargs.sessId] = resp;
			else
				sessionResp.Add(wargs.sessId, resp);	

			log.Info("WebBackEnd: Starting Query for string \"{0}\"", wargs.searchString);

			QueryDriver.DoQueryLocal (query, qres);

			//Wait only till we have enough results to display
			while ((result.Contains(qres)) && 
					(root.HitCollection.NumResults < 10)) 
				Thread.Sleep(100);
				
			if (root.HitCollection.IsEmpty)
				return NO_RESULTS;
						
			lock (root) {			
				root.Render(bctx);				
				return (getResultsLabel(root) + (wargs.isLocalReq ? bctx.buffer:bctx.bufferForExternalQuery));
			}			
		}

		public void dispatchAction (string sessId, string actionString)
		{
			string tile_id = null, action = null;
			bool actionDone = false;

			//if (actionString.StartsWith ("dynaction:"))  {
				
			bufferRenderContext b = ((Resp)sessionResp[sessId]).bufferContext;
			if (b != null)
				actionDone = b.DoAction(actionString);
			//}

			if (actionDone)
				return;

			if (actionString.StartsWith ("action:")) {

				int pos1 = "action:".Length;
				int pos2 = actionString.IndexOf ("!");

				if (pos2 <= 0)
					return;
					
				tile_id = actionString.Substring (pos1, pos2 - pos1);
				action = actionString.Substring (pos2 + 1);
			
				log.Debug("WebBackEnd tile_id: {0}, action: {1}", tile_id, action);

				BT.Tile t = ((Resp)sessionResp[sessId]).GetTile (tile_id);
			
				if (t == null)
					return;

				MethodInfo info = t.GetType().GetMethod (action,
					BindingFlags.Public | BindingFlags.NonPublic | 	
					BindingFlags.Instance, null, 		
					CallingConventions.Any,	 new Type[] {}, null);

				if (info == null) {
					log.Warn ("WebBackEnd:dispatchAction couldn't find method called {0}", action);
					return;
				}

				object[] attrs = info.GetCustomAttributes (false);
				foreach (object attr in attrs) {
					if (attr is BT.TileActionAttribute) {
						info.Invoke (t, null);
						return;
					}
				}
				log.Warn ("WebBackEnd:dispatchAction {0} does not have the TileAction attribute", t);
			}

			string command = null;
			string commandArgs = null;

			if (actionString.StartsWith ("http://") || actionString.StartsWith ("file://")) {
				command = "gnome-open";
				commandArgs = "'" + actionString + "'";
			} 
			else if (actionString.StartsWith ("mailto:")) {
				command = "evolution";
				commandArgs = actionString;
			}

			if (command != null) {
				Process p = new Process ();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.FileName = command;
				if (commandArgs != null)
				//if (args != null)
					p.StartInfo.Arguments = commandArgs;
				try {		

					p.Start ();
				} 
				catch { }
			}
		}
		
//////////////////////////////////////////////////////////////////////////

	private class ResultPair {
		private BT.SimpleRootTile 	_rootTile;
		private ArrayList			_hitsCopy;
		
		public ResultPair(BT.SimpleRootTile rootTile) {
			this._rootTile = rootTile;
			_hitsCopy = ArrayList.Synchronized(new ArrayList());
		}
		
		public BT.SimpleRootTile rootTile {
			get { return _rootTile; }
		}
		
		public ArrayList hitsCopy {
			get { return _hitsCopy; }
		}		
	}

	private class Resp {

		private ResultPair _rp; 
		private bufferRenderContext bufCtx = null;			
		private bool _localRequest;
				
		private Hashtable tileTab = null;
		
		public Resp(ResultPair rp, bufferRenderContext bCtx, bool isLocalReq)
		{
			this._rp = rp;
			this.bufCtx = bCtx;
			this._localRequest = isLocalReq;
								
			this.tileTab = bCtx.table;
		}		
		
		public ResultPair resultPair {
			get { return _rp; }
		}
		public bufferRenderContext bufferContext {
			get { return bufCtx; }		
		}
		public bool isLocalReq {			
		 	get { return _localRequest; } 
		}
			
		public BT.Tile GetTile (string key)  
		{
			if (key == "")
				return resultPair.rootTile;
				
			return (Beagle.Tile.Tile) tileTab[key];
		}	
	}

//////////////////////////////////////////////////////////////////////////
        private class bufferRenderContext : BT.TileRenderContext {

		private ResultPair _rp;
		private Hashtable tileTable = null;
		private Hashtable actionTable = null;
		int actionId = 1;
				
		private System.Text.StringBuilder sb;		
		private bool renderStylesDone = false;
		
		public bufferRenderContext (ResultPair rp) 
		{
			this._rp = rp;
			this.tileTable = Hashtable.Synchronized(new Hashtable());	
			this.actionTable = new Hashtable ();					
			init();
		}
		
		public string buffer {
			get { return sb.ToString();  }
		}
		
		public Hashtable table {
			get { return tileTable;  }
		}

		public string bufferForExternalQuery {
		
			get { 
				//Substitute "action:_tile_id!Open" with "http://host:port/beagle?xxxx"
				string s;
				string[] list = sb.ToString().Split('\"');		  	  
	  			for (int i = 0; i < list.Length; i++) {
	  
	   				s = list[i];
	  				if (s.StartsWith("action") && s.EndsWith("!Open"))  {
	  				
	  					string[] s1 = s.Split(':');	  					
	  					if (s1.Length > 1) {
	  						string[] s2 = s1[1].Split('!');
	  						if (s2.Length > 1) {
	  							BT.Tile t = (BT.Tile) table[s2[0]];
	  							list[i] =  WebServiceBackEnd.AccessFilter.TranslateHit(t.Hit);
	  							t.Uri = new Uri(list[i]);
	  						}
	  					}
	  				}
	  			}	  					
	  			return String.Join ("\"", list);
	  		}
		}
		
		public void init() 
		{
			lock (this) { 
				sb = new StringBuilder(4096);
				renderStylesDone = false;
				tileTable.Clear();
				ClearActions();
				tileTable[_rp.rootTile.UniqueKey] = _rp.rootTile;
			}				
		}
		/////////////////////////////////////////////////
		public void ClearActions ()
		{
			actionTable.Clear();
			actionId = 1;
		}
		
		private string AddAction (BT.TileActionHandler handler)
		{
			if (handler == null)
				return "dynaction:NULL";
			string key = "dynaction:" + actionId.ToString ();
			++actionId;
			actionTable [key] = handler;
			return key;
		}

		public bool DoAction (string key)
		{
			BT.TileActionHandler handler = (BT.TileActionHandler) actionTable [key];
			if (handler != null) {
				handler ();
				return true;
			}
			return false;
		}
		/////////////////////////////////////////////////

		override public void Write (string markup)
		{
			sb.Append(markup);
		}

		override public void Link (string label, 
					   BT.TileActionHandler handler)
		{
		       	string key = AddAction (handler);
				Write ("<a href=\"{0}\">{1}</a>", key, label);
		}
	
		override public void Tile (BT.Tile tile)
		{
			tileTable [tile.UniqueKey] = tile;

			if (!renderStylesDone) {			
	//KNV: Using static_stylesheet for now. Replace with TileCanvas logic later:
				Write(static_stylesheet);
/*
				Write ("<style type=\"text/css\" media=\"screen\">");
				TileCanvas.RenderStyles (this);
				Write ("</style>");
*/
				renderStylesDone = true;				
			}
				
			if (tile != null) {
			
				if (tile is BT.TileHitCollection) 
					PrefetchSnippetsForNetworkHits((BT.TileHitCollection)tile);
					
				tile.Render (this);
			}
		}
		/////////////////////////////////////////////////
		// Code to scan forward through result set & prefetch/cache Snippets for Network Hits
				
		public int 	maxDisplayed 		=  0;
		const int MAX_HIT_IDS_PER_REQ 	= 20; //Max no. of hits snippets to seek at a time
		const int MAX_HITS_AHEAD		= 40; //No. of hits ahead of lastDisplayed to scan
		
		private bool tenHits = false;		//Flag to do Prefetch check only every 10 hits
		
		private void PrefetchSnippetsForNetworkHits(BT.TileHitCollection thc) 
		{		
			int lastDisplayed = 0;
			
			if (maxDisplayed != 0)
				lastDisplayed = thc.LastDisplayed + 1;
			
			//We have cached snippets for network hits upto maxDisplayed
			if (lastDisplayed < maxDisplayed)	
				return;

			maxDisplayed = thc.LastDisplayed + 1;
			
			//Do Prefetch check once every ten hits
			tenHits = !tenHits;
			if (!tenHits)					
				return; 
				
			if (lastDisplayed < thc.NumResults) {
			
				int limit = 0;
				ArrayList networkHits = new ArrayList();				
				
				if ((thc.NumResults - lastDisplayed) > MAX_HITS_AHEAD)
					limit = lastDisplayed + MAX_HITS_AHEAD;
				else
					limit = thc.NumResults;

				ArrayList hits = _rp.hitsCopy;
				lock (hits.SyncRoot) {
				
					if (limit > hits.Count)
						limit = hits.Count;

					log.Debug("PrefetchSnippets: Scanning result set for Network Hits from {0} to {1}", lastDisplayed, limit); 					
							
					//Get all NetworkHits with snippets field not initialized:
					for (int si = lastDisplayed;  si < limit ; si++)
					{
						if ((hits[si] is NetworkHit) && (((NetworkHit)hits[si]).snippet == null)) 
							networkHits.Add((NetworkHit)hits[si]);
					}
				}

				log.Debug("PrefetchSnippets: Found {0} NetworkHits without snippets", networkHits.Count);
				 
				while (networkHits.Count > 0) {
				
					ArrayList nwHitsPerNode = new ArrayList();
					string hostnamePort = GetHostnamePort((NetworkHit)networkHits[0]);
					
					//Gather NetworkHits from a specific target Networked Beagle
					foreach	(NetworkHit nh in networkHits) 
					{
						string hnp = GetHostnamePort(nh);
						if (hnp == null)
							continue;
							
						if (hnp.Equals(hostnamePort)) {

							if (nwHitsPerNode.Count < MAX_HIT_IDS_PER_REQ)
								nwHitsPerNode.Add(nh);
							else
								break;
						}
					}
											
					//Remove NetworkHits for this Networked Beagle	
					int i = networkHits.Count;
					while (--i >= 0) { 

						string hnp = GetHostnamePort((NetworkHit)networkHits[i]);							
						if ((hnp == null) || hnp.Equals(hostnamePort))
							networkHits.RemoveAt(i);
					}
			
					if (nwHitsPerNode.Count > 0)
					{
						string[] f3 = hostnamePort.Split(':');
						if (f3.Length < 2)
						{
							log.Warn("PrefetchSnippets: Invalid format netBeagle URI in NetworkHit");
							continue; 
						}
						BeagleWebService wsp = new BeagleWebService(f3[0], f3[1]);
							
						string searchToken = GetSearchToken((NetworkHit)nwHitsPerNode[0]);
						
						if (searchToken.Equals("beagle"))  //Check if it is Older version of Beagle networking
							searchToken = null;
							 			
						if (searchToken != null) {
						
							int[] hitHashCodes = new int [nwHitsPerNode.Count];
							for (int j = 0; j < hitHashCodes.Length; j++)
								hitHashCodes[j] = ((NetContext) ((NetworkHit)nwHitsPerNode[j]).context).hashCode;
							
							log.Debug("PrefetchSnippets: Invoking GetSnippets on {0} for {1} hits", wsp.Hostname, nwHitsPerNode.Count);
					
							GetSnippetsRequest sreq = new GetSnippetsRequest();
							sreq.searchToken = searchToken;
							sreq.hitHashCodes = hitHashCodes;
											
							ReqContext2 rc = new ReqContext2(wsp, nwHitsPerNode, thc);
							wsp.BeginGetSnippets(sreq, PrefetchSnippetsResponseHandler, rc);
						}	
						
						//Signal change in TileHitCollection due to addition of snippets:
						//_rp.rootTile.HitCollection.ClearSources(null);										
					}
				} //end while								
			} //end if 
		} 

   		private static void PrefetchSnippetsResponseHandler(IAsyncResult ar) 
    	{   	
    		ReqContext2 rc = (ReqContext2)ar.AsyncState;
    		    		     		
 			ArrayList 	nwHits	 = rc.GetNwHits;  
 			BeagleWebService wsp = rc.GetProxy; 	
 					
 			try
      		{	    		
    			Beagle.Daemon.HitSnippet[] hslist = wsp.EndGetSnippets(ar);	
    				
				int j = 0; 
				if (hslist.Length > 0)
				{	
					log.Debug("PrefetchSnippetsResponseHandler: Got {0} snippet responses from {1}", hslist.Length, wsp.Hostname);    			
								
					foreach (Beagle.Daemon.HitSnippet hs in hslist) {
					
						int i, hitHashCode;
						string snippet;
						
						try {
							hitHashCode 	= hs.hashCode;
							snippet = hs.snippet;
						}
						catch (Exception ex2)
						{
							log.Warn ("Exception in WebBackEnd: PrefetchSnippetsResponseHandler(),  while getting snippet from {1}\n Reason: {2} ", wsp.Hostname + ":" + wsp.Port, ex2.Message);
							continue;						
						}
							
						if (snippet.StartsWith(WebServiceBackEnd.InvalidHitSnippetError))
								continue;
		
						for (i = 0; i < nwHits.Count; i++)						
							if (   ((NetContext) ((NetworkHit)nwHits[i]).context).hashCode == hitHashCode) {	
														
								((NetworkHit)nwHits[i]).snippet = snippet;
								//log.Debug("\nPrefetchSnippetsResponseHandler: URI" + j++ + "=" + ((NetworkHit)nwHits[i]).UriAsString  + "\n     Snippet=" + snippet);																	
								break;		
							}
							
						if (i < nwHits.Count)
							nwHits.RemoveAt(i);	
					} //end foreach
				}
			}
			catch (Exception ex) {
				log.Error ("Exception in WebBackEnd: PrefetchSnippetsResponseHandler() - {0} - for {1} ", ex.Message, wsp.Hostname + ":" + wsp.Port);			
			}
			
			if (nwHits.Count > 0)	{
				//Possible Error in getting snippets for these hitIds 
				log.Warn("WebBackEnd/PrefetchSnippetsResponseHandler(): Didn't get Snippets for some network Hits");
				
				foreach (NetworkHit nh in nwHits) 
					nh.snippet = "";					
			}	

			//Signal change in TileHitCollection due to addition of snippets:
			rc.GetHitCollection.ClearSources(null);		
		}

		private class ReqContext2 {
	
			BT.TileHitCollection _thc;
			BeagleWebService _wsp;
			ArrayList _nwHits;

			public ReqContext2(BeagleWebService wsp, ArrayList nwHits, BT.TileHitCollection thc)
			{
				this._thc = thc;				
				this._wsp = wsp;
				this._nwHits = nwHits;
			}
			
			public BT.TileHitCollection GetHitCollection {
				get { return _thc; }
			}
						
			public BeagleWebService GetProxy {
				get { return _wsp; }
			}
		
			public ArrayList GetNwHits {
				get { return _nwHits; }
			}
							
		}					
   		
   		private string GetSearchToken(NetworkHit nh)
   		{
   			if (nh == null) 
   				return null;
   				
			string netUri = nh.UriAsString;		
			
			//netbeagle://164.99.153.134:8888/searchToken?http:///....	
			string[] f1, f2 = netUri.Split('?');
			if (f2.Length > 1) {
				f1 = f2[0].Split ('/');
				if (f1.Length > 1)
					return (f1[f1.Length - 1]);
			}
			return null;
   		}

   		private string GetHostnamePort(NetworkHit nh)
   		{
   			if (nh == null) 
   				return null;
   				   		
			string netUri = nh.UriAsString;		
			
			//netbeagle://164.99.153.134:8888/searchToken?http:///....	
			string[] f1, f2 = netUri.Split('?');
			if (f2.Length > 1) {
				f1 = f2[0].Split ('/');
				if (f1.Length > 1)
					return (f1[2]);
			}
			return null;
   		}   		   					

//////////////////////////////////////////////////////////////////////////

//string static_stylesheet = "<style type=\"text/css\" media=\"screen\"> body, html { background: white; margin: 0; padding: 0; font-family: Sans, Segoe, Trebuchet MS, Lucida, Sans-Serif;  text-align: left; line-height: 1.5em; } a, a:visited {  text-decoration: none; color: #2b5a8a; } a:hover {  text-decoration: underline; } img {  border: 0px; } table {  width: 100%; border-collapse: collapse;	font-size: 10px; } tr {  border-bottom: 1px dotted #999999; } tr:hover {  background: #f5f5f5; } tr:hover .icon { background-color: #ddddd0; } td {  padding: 6px; } td.icon {  background-color: #eeeee0; min-height: 80px; width: 1%;	min-width: 80px; text-align: center; vertical-align: top; padding: 12px; } .icon img { max-width: 60px; padding: 4px; }	.icon img[src$='.jpg'], img[src$='.jpeg'], img[src*='.thumbnails'] {//  max-width: 48px; border: 1px dotted #bbb; //  padding: 4px;	background: #f9f9f9; } td.content { padding-left: 12px; vertical-align: top; } #hilight {  background-color: #ffee66; color: #000000;  padding-left: 2px; padding-right: 2px; margin-left: -2px; margin-right: -2px; } .name {font-size: 1.3em; font-weight: bold; color: black; } .date { font-size: 1em; color: black; margin-bottom: 0.6em; margin-top: 0.2em; margin-left: 16px; } .snippet {font-size: 1em; color: gray; margin-left: 16px; } .url {font-size: 1em; color: #008200; margin-left: 16px;	} ul {margin-left: 16px; padding: 0px; clear: both;	} .actions {  font-size: 1em; } .actions li {  float: left;  display: block;  vertical-align: middle;  padding: 0;  padding-left: 20px;  padding-right: 12px;  background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/navigation/stock_right.png) no-repeat;  min-height: 16px; -moz-opacity: 0.5; } tr:hover .actions li {  -moz-opacity: 1.0; } #phone { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/generic/stock_landline-phone.png) no-repeat; } #email { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail.png) no-repeat; } #email-forward { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail-forward.png) no-repeat; } #email-reply {  background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail-reply.png) no-repeat; }	#message { background: url(file:///opt/gnome/share/icons/hicolor/16x16/apps/im-yahoo.png) no-repeat; } #reveal {  background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/io/stock_open.png) no-repeat; }	td.footer { text-align: right;   border-bottom: solid 1px white; } </style>";			
string static_stylesheet = "<style type=\"text/css\" media=\"screen\"> body, html { background: white; margin: 0; padding: 0; font-family: Arial KOI-8, Segoe, Trebuchet MS, Lucida, Sans-Serif;   text-align: left; line-height: 1.5em; } a, a:visited { text-decoration: none; color: #2b5a8a; } a:hover { text-decoration: underline; } img { border: 0px; } table { width: 100%; border-collapse: collapse; font-size: 11px; } tr { border-bottom: 1px dotted #999999; } tr:hover { background: #f5f5f5; } tr:hover .icon { background-color: #ddddd0; } td { padding: 6px; } td.icon { background-color: #eeeee0; min-height: 80px;   width: 1%; min-width: 80px; text-align: center;  vertical-align: top; padding: 12px; } .icon img { max-width: 60px; padding: 4px; } .icon img[src$='.jpg'], img[src$='.jpeg'], img[src*='.thumbnails'] { //  max-width: 48px; border: 1px dotted #bbb; //  padding: 4px;  background: #f9f9f9; } td.content { padding-left: 12px;  vertical-align: top;  } #hilight { background-color: #ffee66; color: #000000; padding-left: 2px; padding-right: 2px; margin-left: -2px; margin-right: -2px; } .name { font-size: 1.3em; font-weight: bold;  color: black; } .date { font-size: 1em; color: black;   margin-bottom: 0.6em; margin-top: 0.2em; margin-left:16px; } .snippet { font-size: 1em; color: gray;   margin-left: 16px; } .url { font-size: 1em; color: #008200; margin-left: 16px; } ul { margin-left: 16px;   padding: 0px; clear: both; } .actions { font-size: 1em; } .actions li { float: left; display: block;  vertical-align: middle; padding: 0; padding-left: 20px;  padding-right: 12px; background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/navigation/stock_right.png) no-repeat; min-height: 16px; -moz-opacity: 0.5; } tr:hover .actions li { -moz-opacity: 1.0;} #phone { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/generic/stock_landline-phone.png) no-repeat; } #email { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail.png) no-repeat; } #email-forward { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail-forward.png) no-repeat; } #email-reply { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail-reply.png) no-repeat; } #message { background: url(file:///opt/gnome/share/icons/hicolor/16x16/apps/im-yahoo.png) no-repeat; } #reveal { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/io/stock_open.png) no-repeat; } td.footer { text-align: right; border-bottom: solid 1px white; } </style>";
		}
    }
}
