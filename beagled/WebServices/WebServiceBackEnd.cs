//
//WebServiceBackEnd.cs
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
using System.Collections;
using System.Threading;
using System.IO;
using System.Net;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using Beagle.Util;
using Beagle.Daemon;

using Mono.ASPNET;

namespace Beagle.WebService {
	
	[Serializable()]
	public class searchRequest  {

		public string[] text;
		public string[] mimeType;
		public string[] searchSources;
		public QueryDomain qdomain;
	}

	[Serializable()]
	public class HitProperty  {

		private string pKey="";
		public string PKey  {
			get {return pKey;}
			set {pKey = value;}
		}

		private string pVal="";
		public string PVal  {
			get {return pVal;}
			set {pVal = value;}
		}
		
		private bool  isKeyword;
		public bool IsKeyword {
			get { return isKeyword; }
			set { isKeyword = value; }
		}
		
		private bool   isSearched;
		public bool IsSearched {
			get { return isSearched; }
			set { isSearched = value; }
		}		
	}

	[Serializable()]
	public class hitResult {

		public int id;
		public string uri;
		public string resourceType;
		public string mimeType;
		public string source;
		public double scoreRaw;
		public double scoreMultiplier;
		public HitProperty[] properties;
		//FIXME: public xxx[] data;
		public string snippet;
	}

	[Serializable()]
	public class searchResult  {
	
		public int statusCode;			//ReturnCode for programmatic processing
		public string statusMsg;		//User-friendly return message

		public string searchToken;		//Token identifying the query,
							//to enable follow-up queries
		public int firstResultIndex; 	//Index of first result in this response
		public int numResults;		 	//No. of results in this response
		public int totalResults;		//Total no. of results from the query
		public hitResult[] hitResults;
	}
					     
	public class WebServicesArgs {
	
		public bool web_global = false;
		public bool web_start = false;
		public string web_port = WebServiceBackEnd.DEFAULT_XSP_PORT;
		public string web_rootDir = WebServiceBackEnd.DEFAULT_XSP_ROOT;
	}
			
	public class WebServiceBackEnd: MarshalByRefObject   {
		
		public static string DEFAULT_XSP_ROOT = Path.Combine (ExternalStringsHack.PkgDataDir, "xsp");
		public static string DEFAULT_XSP_PORT = "8888";
		
		static Mono.ASPNET.ApplicationServer appServer = null;
		//Both "/" and "/beagle" aliased to DEFAULT_XSP_ROOT only for BeagleXSP server
		static string[] xsp_param = {"--port", DEFAULT_XSP_PORT,
					     "--root", DEFAULT_XSP_ROOT, 
					     "--applications", 
					     		"/:" + DEFAULT_XSP_ROOT + 
					     		",/beagle:" + DEFAULT_XSP_ROOT + 
					     		",/beagle/public:" + PathFinder.HomeDir + "/public" +
					     		",/beagle/kde3:" + ExternalStringsHack.KdePrefix +
					     		",/beagle/gnome:" + ExternalStringsHack.GnomePrefix +	
					     		",/beagle/local:" + ExternalStringsHack.Prefix,					     						     		
					     "--nonstop"};
					     
		static string FallBackAppString = "/:" + DEFAULT_XSP_ROOT + ",/beagle:" + DEFAULT_XSP_ROOT;
		 
		public static void Start(WebServicesArgs wsargs)
		{
			//start web-access server first
			Logger.Log.Debug ("Starting WebBackEnd");
			WebBackEnd.init (wsargs.web_global);

			//Next start web-service server
			Logger.Log.Info ("Starting WebServiceBackEnd");
			WebServiceBackEnd.init (wsargs.web_global);

			Logger.Log.Debug ("Global WebAccess {0}", wsargs.web_global ? "Enabled" : "Disabled");

			xsp_param[1] = wsargs.web_port;
			xsp_param[3] = wsargs.web_rootDir;
			
			//Check if web_rootDir_changed:
			if (String.Compare(wsargs.web_rootDir, DEFAULT_XSP_ROOT, true) != 0)
				//Assuming "/beagle" exists as an explicit sub-folder under user specified xsp root directory:
				xsp_param[5] = "/:" + wsargs.web_rootDir + ",/beagle:" + wsargs.web_rootDir + "/beagle" + ",/beagle/public:" + PathFinder.HomeDir + "/public";
				
			if (wsargs.web_start) {
				
				Logger.Log.Debug ("Starting Internal Web Server");
				
				int retVal = 0;
				try {
					//Start beagled internal web server (BeagleXsp)
					retVal = Mono.ASPNET.Server.initXSP(xsp_param, out appServer);
				}
				catch (Exception e) {
					//Retry with reduced application mappings:
					xsp_param[5] = FallBackAppString;
					retVal = Mono.ASPNET.Server.initXSP(xsp_param, out appServer);		
				}
				
				if (retVal != 0) {
					Logger.Log.Warn ("Error starting Internal Web Server (retVal={0})", retVal);
					Logger.Log.Warn ("Check if there is another instance of Beagle running");
				}
			}		
		}
		
		public static void Stop() 
		{
			Logger.Log.Info ("Stopping WebServiceBackEnd");
			if (appServer != null) {
			    appServer.Stop(); 
				appServer = null;
			}
		}
		
		//KNV: If needed, we can convert this to a Singleton, adding a 
		//	   static Factory method to get the singleton instance reference,
		//	   so that front-end code always gets hold of same instance.
		static WebServiceBackEnd instance = null;
		static bool allow_global_access = false;
		public static ExternalAccessFilter AccessFilter;
		
		private Hashtable resultTable;
		private Hashtable sessionTable;
		
		public WebServiceBackEnd() {
			 resultTable 		= Hashtable.Synchronized(new Hashtable());
			 sessionTable 		= Hashtable.Synchronized(new Hashtable());
		}

		~WebServiceBackEnd() {
			resultTable.Clear();
			sessionTable.Clear();
		}
	
		public bool allowGlobalAccess {
			get { return allow_global_access; }
		}
				
		public static void init(bool web_global)
		{
		    allow_global_access = web_global;

			AccessFilter = new ExternalAccessFilter();
			
		    if (instance == null) {

		  	instance = new WebServiceBackEnd();

  		  	//TCP Channel Listener registered in beagledWeb:init()
		  	//ChannelServices.RegisterChannel(new TcpChannel(8347));
		  	WellKnownServiceTypeEntry WKSTE =
				new WellKnownServiceTypeEntry(typeof(WebServiceBackEnd),
				 "WebServiceBackEnd.rem", WellKnownObjectMode.Singleton);
		  	RemotingConfiguration.ApplicationName="beagled";
		  	RemotingConfiguration.RegisterWellKnownServiceType(WKSTE);
		    }	    
		}

		void OnHitsAdded (QueryResult qres, ICollection hits)
		{
			if (resultTable.Contains(qres)) {
			
				SessionData sdata = ((SessionData) resultTable[qres]);	
				ArrayList results = sdata.results;
				bool localReq = sdata.localRequest;
				
				//if (!localReq)
				//	Console.WriteLine("OnHitsAdded invoked with {0} hits", hits.Count); 
			
				if (localReq){
					lock (results.SyncRoot) 
						results.AddRange(hits);
				}
				else {
				
					Query query = sdata.query;
					
					lock (results.SyncRoot) {
						foreach (Hit h in hits)
							results.AddRange(HitToNetworkHits(h, query));
					}
					//Console.WriteLine("OnHitsAdded: Total hits in Results is {0}", results.Count); 												
				}
			}
		}
		
		void removeUris(ArrayList results, ICollection uris)
		{
			foreach(Uri u in uris)
			   foreach(Hit h in results)
				if (h.Uri.Equals (u) && h.Uri.Fragment == u.Fragment) {
					lock (results.SyncRoot) {
						results.Remove(h);
					}
					break;
				}
		}

		void OnHitsSubtracted (QueryResult qres, ICollection uris)
		{
			if (resultTable.Contains(qres)) {
				SessionData sdata = ((SessionData) resultTable[qres]);	
				ArrayList results = sdata.results;
				removeUris(results, uris);
			}
		}

		void OnFinished (QueryResult qres)
		{

			DetachQueryResult (qres);
		}

		void OnCancelled (QueryResult qres)
		{
			DetachQueryResult (qres);
		}

		void AttachQueryResult (QueryResult qres, SessionData sdata)
		{
			if (qres != null) {

				qres.HitsAddedEvent += OnHitsAdded;
				qres.HitsSubtractedEvent += OnHitsSubtracted;
				qres.FinishedEvent += OnFinished;
				qres.CancelledEvent += OnCancelled;

				resultTable.Add(qres, sdata);
			}
		}

		void DetachQueryResult (QueryResult qres)
		{
			if (qres != null) {

				if (resultTable.Contains(qres)) {
					SessionData sdata = ((SessionData) resultTable[qres]);	
					sdata.results.Sort();
				}
				qres.HitsAddedEvent -= OnHitsAdded;
				qres.HitsSubtractedEvent -= OnHitsSubtracted;
				qres.FinishedEvent -= OnFinished;
				qres.CancelledEvent -= OnCancelled;

				resultTable.Remove(qres);
				qres.Dispose ();
			}
		}
		
		private class SessionData {
		
			private bool _localRequest;
			private ArrayList _results;
			private Query _query;
			
			public SessionData (Query _query, ArrayList _results, bool _localRequest)
			{
				this._localRequest = _localRequest;
				this._results = _results;
				this._query = _query;
			}

			public bool localRequest {
				get { return _localRequest; }
			}				
										
			public ArrayList results {
				get { return _results; }
			}
			

			public Query query  {
				get { return _query; }
			}
		}

		private ArrayList HitToNetworkHits(Hit h, Query query)
		{
			string snippet = "";
			ArrayList authResults = new ArrayList();		
						
			Queryable queryable = h.SourceObject as Queryable;
			
			if (queryable == null)
				snippet = "ERROR: hit.SourceObject is null, uri=" + h.Uri;
			else
				snippet = queryable.GetSnippet (query.Text as string[], h);
										
			ArrayList tempResults = (ArrayList) AccessFilter.TranslateHit(h);

			foreach (Hit h1 in tempResults) {
																		
				NetworkHit h2 = new NetworkHit();
						
				h2.Id = h1.Id;
				h2.Uri = h1.Uri;
   	    		h2.Type = h1.Type;
				h2.MimeType = h1.MimeType;
				h2.Source = h1.Source;
				h2.ScoreRaw = h1.ScoreRaw;
				h2.ScoreMultiplier = h1.ScoreMultiplier;
			
				h2.SourceObject = h1.SourceObject;
					
				//h2.properties = h1.Properties;
				
				foreach (Property p in h1.Properties)
					h2.AddProperty(p);				 
/*				
				Hashtable sp = (Hashtable) h1.Properties;	
				foreach (string key in sp.Keys) 
					h2[key] = (string) sp[key];
*/
				h2.snippet = snippet;
						
				authResults.Add(h2);
			}
			return authResults;
		}				

		private const int MAX_RESULTS_PER_CALL = 20;
		
		public const int SC_QUERY_SUCCESS = 0;
		public const int SC_INVALID_QUERY = -1;
		public const int SC_UNAUTHORIZED_ACCESS = -2;
		public const int SC_INVALID_SEARCH_TOKEN = -3;

		//Full beagledQuery
		public searchResult doQuery(searchRequest sreq, bool isLocalReq)
		{	
			searchResult sr;
			//if (sreq == (MarshalByRef)(null))
				//return new searchResult();
			if (sreq.text == null || sreq.text.Length == 0 ||
				(sreq.text.Length == 1 && sreq.text[0].Trim() == "") ) {
				
			    sr = new searchResult();
			    sr.statusCode = SC_INVALID_QUERY;
			    sr.statusMsg = "Error: No search terms specified";
				return sr;
			}
				
			Query query = new Query();
			
			foreach (string text in sreq.text) 
				query.AddText(text);				
			
			Console.WriteLine("WebServiceBackEnd: Received {0} WebService Query with search term: {1}", isLocalReq ? "Local":"External", query.QuotedText);

			if (sreq.mimeType != null && sreq.mimeType[0] != null)
				foreach (string mtype in sreq.mimeType)
					query.AddMimeType(mtype);

			if (sreq.searchSources != null && sreq.searchSources[0] != null)
				foreach (string src in sreq.searchSources)
					query.AddSource(src);

			//FIXME: Add check to restrict queryDomain to Local or Neighborhood ?
			//Having this Global can cause cascading/looping of requests.
			if (sreq.qdomain > 0)
				query.AddDomain(sreq.qdomain);

			ArrayList results = ArrayList.Synchronized(new ArrayList());
			
			QueryResult qres = new QueryResult ();
			
			//Console.WriteLine("WebServiceBackEnd: Starting Query for string \"{0}\"",	query.QuotedText);

			string searchId = TokenGenerator();
						
			SessionData sdata = new SessionData(query, results, isLocalReq);
				
			AttachQueryResult (qres, sdata);
			
/* Include this code, if sessionID passed from front-end:
			if (sessionTable.Contains(searchId))
				sessionTable[searchId] = sdata;
			else
*/
			sessionTable.Add(searchId, sdata);
		
			QueryDriver.DoQuery (query, qres);

			while (resultTable.Contains(qres) && (results.Count < MAX_RESULTS_PER_CALL) )
				Thread.Sleep(10);

			//Console.WriteLine("WebServiceBackEnd: Got {0} results from beagled", results.Count);
			sr = new searchResult();

			if (results.Count != 0) 
			 lock (results.SyncRoot) { //Lock results ArrayList to prevent more Hits added till we've processed doQuery
			
				sr.numResults = results.Count < MAX_RESULTS_PER_CALL ? results.Count: MAX_RESULTS_PER_CALL;	
				sr.hitResults = new hitResult[sr.numResults];
				
				int i = 0; 
			    // Console.WriteLine(sr.numResults);
			    				
				for (int n = 0; n < sr.numResults; n++) {
				
					Hit h = (Hit) results[n];
			
					string snippet = ""; 
			
					if (isLocalReq) {
						
						//Get Snippet before AuthenticateHit call changes the Uri:
						Queryable queryable = h.SourceObject as Queryable;
						if (queryable == null)
							snippet = "ERROR: hit.SourceObject is null, uri=" + h.Uri;
						else
							snippet = queryable.GetSnippet (query.Text as string[], h);
					}
					else 
						snippet = ((NetworkHit) h).snippet;					
								
					sr.hitResults[i] = new hitResult();
					sr.hitResults[i].id = h.Id;
					sr.hitResults[i].uri = h.Uri.ToString();
	        	    sr.hitResults[i].resourceType = h.Type;
					sr.hitResults[i].mimeType = h.MimeType;
					sr.hitResults[i].source = h.Source;
					sr.hitResults[i].scoreRaw = h.ScoreRaw;
					sr.hitResults[i].scoreMultiplier = h.ScoreMultiplier;
					
					//sr.hitResults[i].properties = h.Properties;	
					int plen = h.Properties.Count;
					sr.hitResults[i].properties = new HitProperty[plen];
					for (int j = 0; j < plen; j++) {
						Property p = (Property) h.Properties[j];
						sr.hitResults[i].properties[j] = new HitProperty();
						sr.hitResults[i].properties[j].PKey = p.Key;
						sr.hitResults[i].properties[j].PVal = p.Value;				
						sr.hitResults[i].properties[j].IsKeyword = p.IsKeyword;				
						sr.hitResults[i].properties[j].IsSearched = p.IsSearched;							
					}
/*									
					Hashtable sp = (Hashtable) h.Properties;
					sr.hitResults[i].properties = new Property[h.Properties.Count];
	
					int j = 0;
					foreach (string key in sp.Keys) {
						sr.hitResults[i].properties[j] = new HitProperty();
						sr.hitResults[i].properties[j].PKey = key;
						sr.hitResults[i].properties[j++].PVal = (string) sp[key];
					}
*/											
					sr.hitResults[i].snippet = snippet;
	
					i++;
				}					
			 } //end lock
			 else {

			    sr.numResults = 0;
				sr.hitResults = new hitResult[sr.numResults];	
			 }

			 sr.totalResults = results.Count;
			 
			 sr.firstResultIndex = 0;			 
			 sr.searchToken = "";
				
			 if (sr.totalResults > 0)
				sr.searchToken = searchId;
					
			 sr.statusCode = SC_QUERY_SUCCESS;
			 sr.statusMsg = "Success";
			 Console.WriteLine("WebServiceBackEnd: Total Results = "  + sr.totalResults);			
			 return sr;
		}

		public searchResult getMoreResults(string searchToken, int startIndex, bool isLocalReq)
		{							

			searchResult sr = new searchResult();
			sr.numResults = 0;
			
			if (!sessionTable.ContainsKey(searchToken)) {
				sr.statusCode = SC_INVALID_SEARCH_TOKEN;
				sr.statusMsg = "Error: Invalid Search Token";
				Console.WriteLine("GetMoreResults: Invalid Search Token received ");
				return sr;
			}
									
			ArrayList results = ((SessionData)sessionTable[searchToken]).results;
			if (results == null) {
				sr.statusCode = SC_INVALID_SEARCH_TOKEN;
				sr.statusMsg = "Error: Invalid Search Token";
				Console.WriteLine("GetMoreResults: Invalid Search Token received ");
				return sr;
			}

			lock (results.SyncRoot) { //Lock results ArrayList to prevent more Hits added till we've processed doQuery
 
 				int i = 0;
 				
				if (startIndex < results.Count)
					sr.numResults = (results.Count < startIndex + MAX_RESULTS_PER_CALL) ? (results.Count - startIndex): MAX_RESULTS_PER_CALL;
				
				sr.hitResults = new hitResult[sr.numResults];
				
				Query query = ((SessionData)sessionTable[searchToken]).query;
			
				for (int k = startIndex; (i < sr.numResults) && (k < results.Count); k++)   {		
				
					Hit h = (Hit) results[k];
			
					string snippet = ""; 
			
					if (isLocalReq) {
						
						//Get Snippet before AuthenticateHit call changes the Uri:
						Queryable queryable = h.SourceObject as Queryable;
						if (queryable == null)
							snippet = "ERROR: hit.SourceObject is null, uri=" + h.Uri;
						else
							snippet = queryable.GetSnippet (query.Text as string[], h);
					}
					else 
						snippet = ((NetworkHit) h).snippet;					
								
					sr.hitResults[i] = new hitResult();
					sr.hitResults[i].id = h.Id;
					sr.hitResults[i].uri = h.Uri.ToString();
	        	    sr.hitResults[i].resourceType = h.Type;
					sr.hitResults[i].mimeType = h.MimeType;
					sr.hitResults[i].source = h.Source;
					sr.hitResults[i].scoreRaw = h.ScoreRaw;
					sr.hitResults[i].scoreMultiplier = h.ScoreMultiplier;
					
					int plen = h.Properties.Count;
					sr.hitResults[i].properties = new HitProperty[plen];
					for (int j = 0; j < plen; j++) {
						Property p = (Property) h.Properties[j];
						sr.hitResults[i].properties[j] = new HitProperty();
						sr.hitResults[i].properties[j].PKey = p.Key;
						sr.hitResults[i].properties[j].PVal = p.Value;				
						sr.hitResults[i].properties[j].IsKeyword = p.IsKeyword;				
						sr.hitResults[i].properties[j].IsSearched = p.IsSearched;							
					}	
/*									
					Hashtable sp = (Hashtable) h.Properties;
					sr.hitResults[i].properties = new HitProperty[sp.Count];
	
					int j = 0;
					foreach (string key in sp.Keys) {
						sr.hitResults[i].properties[j] = new HitProperty();
						sr.hitResults[i].properties[j].PKey = key;
						sr.hitResults[i].properties[j++].PVal = (string) sp[key];
					}
*/																	
					sr.hitResults[i].snippet = snippet;
					i++;
				}												
			} //end lock

			sr.totalResults = results.Count;
													
			sr.firstResultIndex = startIndex;
			sr.searchToken = "";
			
			if (sr.totalResults > 0)
				sr.searchToken = searchToken;
				
			sr.statusCode = SC_QUERY_SUCCESS;
			sr.statusMsg = "Success";
			//Console.WriteLine("WebServiceQuery: Total Results = "  + sr.totalResults);	
			return sr;
		}

		//Returns a 15-char random alpha-numeric string similar to ASP.NET sessionId
		private string TokenGenerator()
		{
			const int TOKEN_LEN = 15;

			Random r = new Random();
			string token = ((Char)((int)((26 * r.NextDouble()) + 'a')) + System.Guid.NewGuid().ToString()).Substring (0, TOKEN_LEN);

			char alpha = (Char)((int)((26 * r.NextDouble()) + 'a'));
				
			return (token.Replace('-', alpha));
		}
	}

	public class NetworkHit: Hit {
	
		private string _snippet;
	
		public string snippet {
			get { return _snippet; }
			set { _snippet = value; }
		}
	} 	
}
