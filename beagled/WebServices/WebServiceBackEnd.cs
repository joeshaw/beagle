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
using System.IO;
using System.Net;
using System.Collections;
using System.Threading;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using Beagle.Util;
using Beagle.Daemon;

using Mono.ASPNET;

namespace Beagle.WebService {

	public class WebServiceBackEnd: MarshalByRefObject   {

		public static string hostname = "localhost";		
		public static string DEFAULT_XSP_ROOT = Path.Combine (ExternalStringsHack.PkgDataDir, "xsp");
		public static string DEFAULT_XSP_PORT = "8888";

		public static bool web_global = false;
		public static bool web_start = false;
		public static string web_port = DEFAULT_XSP_PORT;
		public static string web_rootDir = DEFAULT_XSP_ROOT;

		public static ExternalAccessFilter AccessFilter;																
		static Mono.ASPNET.ApplicationServer appServer = null;
		static string DEFAULT_APP_MAPPINGS = "/:" + DEFAULT_XSP_ROOT + ",/beagle:" + DEFAULT_XSP_ROOT;

		//Both "/" and "/beagle" aliased to DEFAULT_XSP_ROOT only for BeagleXSP server
		static string[] xsp_param = {"--port", DEFAULT_XSP_PORT,
					     "--root", DEFAULT_XSP_ROOT, 
					     "--applications", DEFAULT_APP_MAPPINGS, 
					     "--nonstop"};
		
		public static void Start()
		{			
			try {
				hostname = Dns.GetHostName();
				Console.WriteLine("This Computer Hostname: " + hostname);
			}
			catch (Exception ex) 
			{
				Console.WriteLine("Caught exception {0} in Dns.GetHostName: ", ex.Message);
				Console.WriteLine("Resetting hostname to \"localhost\"");
				hostname = "localhost";
			}
								
			//start web-access server first
			Logger.Log.Debug ("Starting WebBackEnd");
			WebBackEnd.init (web_global);

			//Next start web-service server
			Logger.Log.Info ("Starting WebServiceBackEnd");
			WebServiceBackEnd.init (web_global);

			Logger.Log.Debug ("Global WebAccess {0}", web_global ? "Enabled" : "Disabled");

			xsp_param[1] = web_port;
			xsp_param[3] = web_rootDir;
			
			//Check if web_rootDir_changed:
			if (String.Compare(web_rootDir, DEFAULT_XSP_ROOT, true) != 0)
				//Assuming "/beagle" exists as an explicit sub-folder under user specified xsp root directory:
				xsp_param[5] = "/:" + web_rootDir + ",/beagle:" + web_rootDir + "/beagle";
			
			try {
				//",/beagle/local:" + ExternalStringsHack.Prefix,	
				if (Directory.Exists(ExternalStringsHack.Prefix))
				xsp_param[5] += ",/beagle/local:" + ExternalStringsHack.Prefix;
							
				//",/beagle/gnome:" + ExternalStringsHack.GnomePrefix +
				if (Directory.Exists(ExternalStringsHack.GnomePrefix))
				xsp_param[5] += ",/beagle/gnome:" + ExternalStringsHack.GnomePrefix;
												
				//",/beagle/kde3:" + ExternalStringsHack.KdePrefix +
				if (Directory.Exists(ExternalStringsHack.KdePrefix))
				xsp_param[5] += ",/beagle/kde3:" + ExternalStringsHack.KdePrefix;
				
				//if (!hostname.Equals("localhost")) {

					string[] reserved_suffixes = new string[] {"local", "gnome", "kde3" };
					string BeagleHttpUriBase = "http://" + hostname + ":" + xsp_param[1] + "/beagle/";
				
					AccessFilter = new ExternalAccessFilter(BeagleHttpUriBase, reserved_suffixes);
				
					ArrayList matchers = AccessFilter.Matchers;				
					foreach (SimpleMatcher sm in matchers) 					
						xsp_param[5] += ",/beagle/" + sm.Rewrite +":" + sm.Match;					
							
					AccessFilter.Initialize();
				//}
			}
			catch (Exception e)
			{
				xsp_param[5] = DEFAULT_APP_MAPPINGS;
			}					
					     						
			if (web_start) {
				
				Logger.Log.Debug ("Starting Internal Web Server");

				int retVal = 0;
				try {
					//Start beagled internal web server (BeagleXsp)
					retVal = Mono.ASPNET.Server.initXSP(xsp_param, out appServer);
				}
				catch (ArgumentException e) {
					//Retry with default application mappings:
					xsp_param[5] = DEFAULT_APP_MAPPINGS;
					retVal = Mono.ASPNET.Server.initXSP(xsp_param, out appServer);		
				}

				if (retVal != 0) {
					Logger.Log.Warn ("Error starting Internal Web Server (retVal={0})", retVal);
					Logger.Log.Warn ("Check if there is another instance of Beagle running");
				}
				else
					Logger.Log.Debug("BeagleXSP Applications list: " + xsp_param[5]);
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

/////////////////////////////////////////////////////////////////////////////////////////
		
		//KNV: If needed, we can convert this to a Singleton, adding a 
		//	   static Factory method to get the singleton instance reference,
		//	   so that front-end code always gets hold of same instance.
		static WebServiceBackEnd instance = null;
		static bool allow_global_access = false;
		
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
			
				if (localReq){
					lock (results.SyncRoot) 
						results.AddRange(hits);
				}
				else {
				
					//Query query = sdata.query;					
					lock (results.SyncRoot) {
						foreach (Hit h in hits)
							if (AccessFilter.FilterHit(h))
								results.Add(h);
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

		public string[] ICollection2StringList(ICollection il)
		{
			if (il == null)
				return new string[0] ;
			
			string[] sl = new string[il.Count];
						
			il.CopyTo(sl, 0);
				
			return sl;
		}
		
		private const int MAX_RESULTS_PER_CALL = 20;
		
		public const int SC_QUERY_SUCCESS = 0;
		public const int SC_INVALID_QUERY = -1;
		public const int SC_UNAUTHORIZED_ACCESS = -2;
		public const int SC_INVALID_SEARCH_TOKEN = -3;

		//Full beagledQuery
		public SearchResult doQuery(SearchRequest sreq, bool isLocalReq)
		{	
			SearchResult sr;
			//if (sreq == (MarshalByRef)(null))
				//return new SearchResult();
			if (sreq.text == null || sreq.text.Length == 0 ||
				(sreq.text.Length == 1 && sreq.text[0].Trim() == "") ) {
				
			    sr = new SearchResult();
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
				Thread.Sleep(100);

			//Console.WriteLine("WebServiceBackEnd: Got {0} results from beagled", results.Count);
			sr = new SearchResult();

			if (results.Count > 0)
			{ 
			  lock (results.SyncRoot) { //Lock results ArrayList to prevent more Hits added till we've processed doQuery
			
				sr.numResults = results.Count < MAX_RESULTS_PER_CALL ? results.Count: MAX_RESULTS_PER_CALL;	
				sr.hitResults = new HitResult[sr.numResults];
			    				
				for (int i = 0; i < sr.numResults; i++) {
				
					Hit h = (Hit) results[i];

					string snippet; 
						
					Queryable queryable = h.SourceObject as Queryable;
					if (queryable == null)
						snippet = "ERROR: hit.SourceObject is null, uri=" + h.Uri;
					else
						snippet = queryable.GetSnippet (ICollection2StringList(query.Text), h);				
					
					//snippet == "", implies GetSnippet returned empty snippet
					if (snippet == null)   	
						snippet = "";		
								
					sr.hitResults[i] = new HitResult();
					sr.hitResults[i].id = h.Id;
					
					if (isLocalReq)
						sr.hitResults[i].uri = h.Uri.ToString();
					else 
						sr.hitResults[i].uri = AccessFilter.TranslateHit(h);

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

					sr.hitResults[i].snippet = snippet;
				}					
			   } //end lock
			 }// end if
			 else {

			    sr.numResults = 0;
				sr.hitResults = new HitResult[sr.numResults];	
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

		public SearchResult getMoreResults(string searchToken, int startIndex, bool isLocalReq)
		{							

			SearchResult sr = new SearchResult();
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
				
				sr.hitResults = new HitResult[sr.numResults];
			
				for (int k = startIndex; (i < sr.numResults) && (k < results.Count); k++)   {		
				
					Hit h = (Hit) results[k];	
							
					sr.hitResults[i] = new HitResult();
					
/* 	 GetMoreResults will NOT return Snippets by default. Client must make explicit GetSnippets request to get snippets for these hits.

					string snippet = ""; 						
					Queryable queryable = h.SourceObject as Queryable;
					if (queryable == null)
						snippet = "ERROR: hit.SourceObject is null, uri=" + h.Uri;
					else
						snippet = queryable.GetSnippet (ICollection2StringList(query.Text), h);		
					 = snippet;			
*/
// Not initializing sr.hitResults[i].snippet implies there is no <snippets> element in HitResult XML response
// which implies GetSnippet was not done.
								
					sr.hitResults[i].id = h.Id;
					
					if (isLocalReq)
						sr.hitResults[i].uri = h.Uri.ToString();
					else 
						sr.hitResults[i].uri = AccessFilter.TranslateHit(h);

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
		
		static string InvalidHitSnippetError = "ERROR: Invalid or Duplicate Hit Id";
		public HitSnippet[] getSnippets(string searchToken, int[] hitIds)
		{	
			HitSnippet[] response;
			
			if (!sessionTable.ContainsKey(searchToken)) {
			
				response = new HitSnippet[0];
				Console.WriteLine("GetSnippets: Invalid Search Token received ");
				return response;
			}
									
			ArrayList results = ((SessionData)sessionTable[searchToken]).results;
			if ((results == null) || (results.Count == 0)) {

				response = new HitSnippet[0];
				Console.WriteLine("GetSnippets: Invalid Search Token received ");
				return response;
			}

			int i = 0; 		
			ArrayList IdList = new ArrayList();
			IdList.AddRange(hitIds);	
			response = new HitSnippet[hitIds.Length];

			Query query = ((SessionData)sessionTable[searchToken]).query;
			
			lock (results.SyncRoot)  {
			
				string snippet = null; 
				foreach (Hit h in results)  {
				
					if (IdList.Contains(h.Id)) {
					
							IdList.Remove(h.Id);	
													
							Queryable queryable = h.SourceObject as Queryable;
							if (queryable == null)
								snippet = "ERROR: hit.SourceObject is null, uri=" + h.Uri;
							else
								snippet = queryable.GetSnippet (ICollection2StringList(query.Text), h);		
										
							response[i++] = new HitSnippet(h.Id, snippet);		
					
							if (i == hitIds.Length)
								return response;
					}
				} //end foreach
			} //end lock
			
			foreach (int hitId in IdList) {
					response[i++] = new HitSnippet(hitId, InvalidHitSnippetError);
					if (i == hitIds.Length)
							break;
			}		

			return response;
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
	
////////////////////////////////////////////////////////////////////////////////////////////
/////////////   WebService Request-Response Data Structures   	
////////////////////////////////////////////////////////////////////////////////////////////

	[Serializable()]
	public class SearchRequest  {

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
	public class HitResult {

		public int 		id;
		public string 	uri;
		public string 	resourceType;
		public string 	mimeType;
		public string 	source;
		public double 	scoreRaw;
		public double 	scoreMultiplier;
		public HitProperty[] properties;
		//FIXME: public xxx[] data;
		public string 	snippet;
	}

	[Serializable()]
	public class SearchResult  {
	
		public int statusCode;			//ReturnCode for programmatic processing
		public string statusMsg;		//User-friendly return message

		public string searchToken;		//Token identifying the query,
										//to enable follow-up queries
		public int firstResultIndex; 	//Index of first result in this response
		public int numResults;		 	//No. of results in this response
		public int totalResults;		//Total no. of results from the query
		public HitResult[] hitResults;
	}

	[Serializable()]
	public class HitSnippet {
		public int hitId;
		public string snippet;
		
		public HitSnippet() { hitId = 0; snippet = null; }
		public HitSnippet( int i, string s) {
			this.hitId = i;
			this.snippet =s;
		}

	}
		
}
