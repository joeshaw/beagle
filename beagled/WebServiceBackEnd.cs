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

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.WebService {
	
	[Serializable()]
	public struct searchRequest  {

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
	}

	[Serializable()]
	public struct hitResult {

		public int id;
		public string uri;
		public string resourceType;
		public string mimeType;
		public string source;
		public double score;
		public HitProperty[] properties;
		//FIXME: public xxx[] data;
	}

	[Serializable()]
	public struct searchResult  {
	
		public int statusCode;			//ReturnCode for programmatic processing
		public string statusMsg;		//User-friendly return message

		public string searchToken;		//Token identifying the query returned, 
										//	    when there are more results
										//		to enable follow-up queries
		public int firstResultIndex; 	//Index of first result in this response
		public int numResults;		 	//No. of results in this response
		public int totalResults;		//Total no. of results from the query
		public hitResult[] hitResults;
	}
	
	public class WebServiceBackEnd: MarshalByRefObject   {

		//KNV: If needed, we can convert this to a Singleton, adding a 
		//	   static Factory method to get the singleton instance reference,
		//	   so that front-end code always gets hold of same instance.
		static WebServiceBackEnd instance = null;
		static bool allow_global_access = false;

		private Hashtable resultTable;
		private Hashtable sessionTable;
		
		public WebServiceBackEnd() {
			 resultTable 	= Hashtable.Synchronized(new Hashtable());
			 sessionTable 	= Hashtable.Synchronized(new Hashtable());
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
				ArrayList results = (ArrayList) resultTable[qres];
				results.AddRange(hits);
			}
		}

		void removeUris(ArrayList results, ICollection uris)
		{	
			foreach(Uri u in uris)
			   foreach(Hit h in results)
				if (h.Uri.Equals (u) && h.Uri.Fragment == u.Fragment) {
					results.Remove(h);	
					break;
				}
		}

		void OnHitsSubtracted (QueryResult qres, ICollection uris)
		{
			if (resultTable.Contains(qres)) {
				ArrayList results = (ArrayList) resultTable[qres];
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

		void AttachQueryResult (QueryResult qres, ArrayList results)
		{
			if (qres != null) {

				qres.HitsAddedEvent += OnHitsAdded;
				qres.HitsSubtractedEvent += OnHitsSubtracted;
				qres.FinishedEvent += OnFinished;
				qres.CancelledEvent += OnCancelled;

				resultTable.Add(qres, results);
			}
		}

		void DetachQueryResult (QueryResult qres)
		{
			if (qres != null) {

				if (resultTable.Contains(qres)) 
					((ArrayList) resultTable[qres]).Sort();

				resultTable.Remove(qres);
				
				qres.HitsAddedEvent -= OnHitsAdded;
				qres.HitsSubtractedEvent -= OnHitsSubtracted;
				qres.FinishedEvent -= OnFinished;
				qres.CancelledEvent -= OnCancelled;

				qres.Dispose ();
			}
		}

		private const int MAX_RESULTS_PER_CALL = 20;
		
		public const int SC_QUERY_SUCCESS = 0;
		public const int SC_INVALID_QUERY = -1;
		public const int SC_UNAUTHORIZED_ACCESS = -2;
		public const int SC_INVALID_SEARCH_TOKEN = -3;

		//Full beagledQuery
		public searchResult doQuery(searchRequest sreq)
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
				
			QueryBody query = new QueryBody();

			foreach (string text in sreq.text)
				query.AddText(text);

			if (sreq.mimeType != null && sreq.mimeType[0] != null)
				foreach (string mtype in sreq.mimeType)
					query.AddMimeType(mtype);

			if (sreq.searchSources != null && sreq.searchSources[0] != null)
				foreach (string src in sreq.searchSources)
					query.AddSource(src);	

			if (sreq.qdomain > 0) 
				query.AddDomain(sreq.qdomain);
							
			return execQuery(query);
		}
				
		private searchResult execQuery(QueryBody query)
		{
			ArrayList results = new ArrayList();
			QueryResult qres = new QueryResult ();
			
			//Console.WriteLine("WebServiceBackEnd: Starting Query for string \"{0}\"",	query.QuotedText);

			AttachQueryResult (qres, results);

			string searchId = TokenGenerator();
/* Include this code, if sessionID passed from front-end:
			if (sessionTable.Contains(searchId)) 
				sessionTable[searchId] = results;
			else
*/			
				sessionTable.Add(searchId, results);
		
			QueryDriver.DoQuery (query, qres);

			while ((resultTable.Contains(qres)) && (results.Count < MAX_RESULTS_PER_CALL)) 
				Thread.Sleep(10);

			//Console.WriteLine("WebServiceBackEnd: Got {0} results from beagled", results.Count);
			searchResult sr = new searchResult();

			sr.numResults = results.Count < MAX_RESULTS_PER_CALL ? results.Count: MAX_RESULTS_PER_CALL;
			sr.hitResults = new hitResult[sr.numResults];

			int i = 0;
			foreach (Hit h in results) {
							
				sr.hitResults[i] = new hitResult();
				sr.hitResults[i].id = h.Id;
				sr.hitResults[i].uri = h.Uri.ToString(); 		
	                        sr.hitResults[i].resourceType = h.Type;		
				sr.hitResults[i].mimeType = h.MimeType;
				sr.hitResults[i].source = h.Source;
				sr.hitResults[i].score = h.Score;

				Hashtable sp = (Hashtable) h.Properties;
				sr.hitResults[i].properties = new HitProperty[sp.Count];

				int j = 0;
				foreach (string key in sp.Keys) {
					sr.hitResults[i].properties[j] = new HitProperty();
					sr.hitResults[i].properties[j].PKey = key;
					sr.hitResults[i].properties[j++].PVal = (string) sp[key];	
				}

				if (++i == sr.numResults)
					break;
			}

			sr.firstResultIndex = 0;
			sr.totalResults = results.Count;
			sr.searchToken = "";
			if (results.Count > MAX_RESULTS_PER_CALL) 
				sr.searchToken = searchId;
				
			sr.statusCode = SC_QUERY_SUCCESS;
			sr.statusMsg = "Success";
			
			return sr;
		}

		public searchResult getMoreResults(string searchToken, int startIndex)
		{
			searchResult sr = new searchResult();							
			sr.numResults = 0;
			
			ArrayList results = (ArrayList) sessionTable[searchToken];
			if (results == null) {
				sr.statusCode = SC_INVALID_SEARCH_TOKEN;
				sr.statusMsg = "Error: Invalid Search Token";
				return sr;
			}

			if (startIndex < results.Count)
				sr.numResults = (results.Count < startIndex + MAX_RESULTS_PER_CALL) ? (results.Count - startIndex): MAX_RESULTS_PER_CALL;

			sr.hitResults = new hitResult[sr.numResults];

			int k = startIndex;
			for (int i = 0; i < sr.numResults; i++)   {
				
				Hit h = (Hit) results[k++];

				sr.hitResults[i] = new hitResult();
				sr.hitResults[i].id = h.Id;
				sr.hitResults[i].uri = h.Uri.ToString(); 		
	            sr.hitResults[i].resourceType = h.Type;		
				sr.hitResults[i].mimeType = h.MimeType;
				sr.hitResults[i].source = h.Source;
				sr.hitResults[i].score = h.Score;

				Hashtable sp = (Hashtable) h.Properties;
				sr.hitResults[i].properties = new HitProperty[sp.Count];

				int j = 0;
				foreach (string key in sp.Keys) {
					sr.hitResults[i].properties[j] = new HitProperty();
					sr.hitResults[i].properties[j].PKey = key;
					sr.hitResults[i].properties[j++].PVal = (string) sp[key];
				}
			}

			sr.firstResultIndex = startIndex;
			sr.totalResults = results.Count;
			sr.searchToken = "";
			if (results.Count > startIndex + MAX_RESULTS_PER_CALL)
				sr.searchToken = searchToken;
				
			sr.statusCode = SC_QUERY_SUCCESS;
			sr.statusMsg = "Success";

			return sr;
		}

		//Returns a 15-char random alpha-numeric string similar to ASP.NET sessionId
		private string TokenGenerator() 
		{
			const int TOKEN_LEN = 15; 
			
			Random r = new Random();
			string token = "";
			char c; int i;
			int multiplier;

			token += (Char)((int)((26 * r.NextDouble()) + 'a'));

			for (i = 1; i < TOKEN_LEN; i++)  {
		
				switch (((int)(1 + r.NextDouble() * 10)) % 3)
				{
					case 0: c = 'a'; multiplier = 26; break;
					case 1: c = 'A'; multiplier = 26; break;	
					case 2: c = '0'; multiplier = 10; break;
					default: c = '0'; multiplier = 10; break;
				}
		
				token += (char)(r.NextDouble() * multiplier + c);
			}
			return token;
		}
    }	
}
