//
// WebServiceFrontEnd.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Authors:
//   Vijay K. Nanjundaswamy (knvijay@novell.com)
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
using System.Threading;
using System.Collections;

using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using Beagle;
using Beagle.Daemon;
using Beagle.WebService;

namespace WebService_CodeBehind {

	struct remoteChannel {
		//.Net Remoting channel to beagledWeb, WebServiceBackEnd within beagled
		private static bool registerChannelDone = false;
			
		public static void Register() {

		   	if (registerChannelDone)
				return;

			ChannelServices.RegisterChannel(new TcpChannel());

		    	WellKnownClientTypeEntry WKCTE_Web = 
			new WellKnownClientTypeEntry(typeof(WebBackEnd),
				"tcp://localhost:8347/WebBackEnd.rem");
		    	RemotingConfiguration.RegisterWellKnownClientType(WKCTE_Web);

		    	WellKnownClientTypeEntry WKCTE_WebService = 
			new WellKnownClientTypeEntry(typeof(WebServiceBackEnd),
				"tcp://localhost:8347/WebServiceBackEnd.rem");
		    	RemotingConfiguration.RegisterWellKnownClientType(WKCTE_WebService);

			registerChannelDone = true;
		}
	}

    [WebService(Description = "Web Service Interface to Beagle",
     Namespace = "http://www.gnome.org/projects/beagle/webservices",
     Name = "BeagleWebService")]
    public class BeagleWebService: System.Web.Services.WebService {
	
		private WebServiceBackEnd remoteObj = null;

		private searchResult initialQuery(searchRequest req) 
		{
		try {
			searchResult sr;
			
			if (req.text == null || req.text.Length == 0 ||
				(req.text.Length == 1 && req.text[0].Trim()== "")) {
			
				sr = new searchResult();
			    sr.statusCode = WebServiceBackEnd.SC_INVALID_QUERY;
			    sr.statusMsg = "No search terms specified";		
			    return sr;
			}
				
			remoteChannel.Register(); 

			if (remoteObj == null)
				remoteObj = new WebServiceBackEnd();

			if (Application["allowGlobalAccess"] == null)
				Application["allowGlobalAccess"] =  remoteObj.allowGlobalAccess;
				
			if ((remoteObj == null) || !((bool)Application["allowGlobalAccess"] ||
				HttpContext.Current.Request.Url.IsLoopback) ) {

				return restrictedAccessResult();
			}

			sr = remoteObj.doQuery(req); 				
			return sr; 
		 }
		 catch (Exception e) {	
			throw e;
		 }	 
	  } 
	
	[WebMethod(Description = "Full object interface to Beagle")]
	[System.Web.Services.Protocols.SoapDocumentMethodAttribute(
	"http://www.gnome.org/projects/beagle/webservices/BeagleQuery",
	RequestNamespace="http://www.gnome.org/projects/beagle/webservices",
	ResponseNamespace="http://www.gnome.org/projects/beagle/webservices")]
	public searchResult BeagleQuery(searchRequest req)
	{
		return initialQuery(req);
	}
	
	[WebMethod(Description = "Simple Interface to Beagle")]
	[System.Web.Services.Protocols.SoapDocumentMethodAttribute(
	"http://www.gnome.org/projects/beagle/webservices/SimpleQuery",
	RequestNamespace="http://www.gnome.org/projects/beagle/webservices",
	ResponseNamespace="http://www.gnome.org/projects/beagle/webservices")]
	public searchResult SimpleQuery(string text) 
	{
		searchResult sr;
			
		if (text == null || text.Trim() == "") {
			
			sr = new searchResult();
			sr.statusCode = WebServiceBackEnd.SC_INVALID_QUERY;
			sr.statusMsg = "No search terms specified";
			return sr;		
		}
			
		searchRequest srq = new searchRequest();
		
		srq.text = new string[1];
		srq.text[0] = text.Trim();
		
		srq.qdomain = Beagle.QueryDomain.Neighborhood; 
	
		return initialQuery(srq);
	}

	[WebMethod(Description = "Full text Interface to Beagle")]
	[System.Web.Services.Protocols.SoapDocumentMethodAttribute(
	"http://www.gnome.org/projects/beagle/webservices/SimpleQuery2",
	RequestNamespace="http://www.gnome.org/projects/beagle/webservices",
	ResponseNamespace="http://www.gnome.org/projects/beagle/webservices")]
	public searchResult SimpleQuery2(string text, string mimeType, string source, string queryDomain) 
	{
		searchResult sr;
			
		if (text == null || text.Trim() == "") {
			
			sr = new searchResult();
			sr.statusCode = WebServiceBackEnd.SC_INVALID_QUERY;
			sr.statusMsg = "No search terms specified";
			return sr;		
		}
			
		searchRequest srq = new searchRequest();
		
		srq.text = new string[1];
		srq.text[0] = text.Trim();
	
		if (mimeType != null && mimeType != "")  {
		
			srq.mimeType = new string[1];
			srq.mimeType[0] = mimeType.Trim();		
		}
		
		srq.qdomain = Beagle.QueryDomain.Neighborhood; 
		switch (queryDomain.Trim().ToLower()) {
		
			case "local" : 	srq.qdomain = Beagle.QueryDomain.Local; 
							break;
							
			case "neighborhood" : 		
							srq.qdomain = Beagle.QueryDomain.Neighborhood; 
							break;
							
			case "global" : srq.qdomain = Beagle.QueryDomain.Global; 
							break;
							
			default: 		srq.qdomain = Beagle.QueryDomain.Neighborhood; 
							break;
		}
		
		string sourceSelector = null; 			
		srq.searchSources = new string[1];
		
		switch (source.Trim().ToLower()) {
		
			case "files" : 	sourceSelector = "Files"; 
							break;
							
			case "addressbook" : 		
							sourceSelector = "Contact"; 
							break;
							
			case "mail" : 	sourceSelector = "MailMessage"; 
							break;
							
			case "web": 	sourceSelector = "WebHistory"; 
							break;
							
			case "chats": 	sourceSelector = "IMLog"; 
							break;
							
			default: 		sourceSelector = null;
							break;
		}
		
		srq.searchSources[0] = sourceSelector;		
		return initialQuery(srq);
	}
	
	[WebMethod(Description = "Common Interface to get more results from Beagle")]
	[System.Web.Services.Protocols.SoapDocumentMethodAttribute(
	"http://www.gnome.org/projects/beagle/webservices/GetMoreResults",
	RequestNamespace="http://www.gnome.org/projects/beagle/webservices",
	ResponseNamespace="http://www.gnome.org/projects/beagle/webservices")]
	public searchResult GetMoreResults(string searchToken, int index)
	{
		try {
		
				searchResult sr;
			
			if (searchToken == null | searchToken == "")  {
			
				sr = new searchResult();
				sr.statusCode = WebServiceBackEnd.SC_INVALID_SEARCH_TOKEN;
				sr.statusMsg = "Invalid Search Token";
			}
			
			remoteChannel.Register(); 
			
			if (remoteObj == null)
				remoteObj = new WebServiceBackEnd();

			if (Application["allowGlobalAccess"] == null)
				Application["allowGlobalAccess"] =  remoteObj.allowGlobalAccess;
				
			if ((remoteObj == null) || !((bool)Application["allowGlobalAccess"] ||
				HttpContext.Current.Request.Url.IsLoopback) ) 	{

				return restrictedAccessResult();
			}

			sr = remoteObj.getMoreResults(searchToken, index);
			return sr; 		
		}
		catch (Exception e)
		{
			throw e;
		}
	}

	private static string localReqOnlyMsg = "Beagle web service unavailable or access restricted to local address only !";

	private searchResult restrictedAccessResult()
	{
		searchResult sr = new searchResult();

		sr.totalResults = 0;

		sr.statusCode = WebServiceBackEnd.SC_UNAUTHORIZED_ACCESS;
		sr.statusMsg = localReqOnlyMsg; 

		return sr;
	}
   }
}
