//
// BeagleWebSvcCb.cs
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
using Beagle.websvc;

namespace BWS_CodeBehind {

	struct remoteChannel {
		//.Net Remoting channel to beagledWeb, beagledWebSvc within beagled
		private static bool registerChannelDone = false;
			
		public static void Register() {

		   	if (registerChannelDone)
				return;

			ChannelServices.RegisterChannel(new TcpChannel());

		    	WellKnownClientTypeEntry WKCTE_web = 
			new WellKnownClientTypeEntry(typeof(beagledWeb),
				"tcp://localhost:8347/beagledWeb.rem");
		    	RemotingConfiguration.RegisterWellKnownClientType(WKCTE_web);

		    	WellKnownClientTypeEntry WKCTE_ws = 
			new WellKnownClientTypeEntry(typeof(beagledWebSvc),
				"tcp://localhost:8347/beagledWebSvc.rem");
		    	RemotingConfiguration.RegisterWellKnownClientType(WKCTE_ws);

			registerChannelDone = true;
		}
	}

    [WebService(Description = "Web Service Interface to beagled",
     Namespace = "http://www.gnome.org/projects/beagle/webservices",
     Name = "BeagleWebService")]
    public class BeagleWebService: System.Web.Services.WebService {
	
	private beagledWebSvc remoteObj = null;

	[WebMethod(Description = "Full interface to beagled")]
	[System.Web.Services.Protocols.SoapDocumentMethodAttribute(
	"http://www.gnome.org/projects/beagle/webservices/beagledQuery",
	RequestNamespace="http://www.gnome.org/projects/beagle/webservices",
	ResponseNamespace="http://www.gnome.org/projects/beagle/webservices")]
	public searchResult beagleQuery(searchRequest req)
	{
		try {
				searchResult sr;
			
			if (req.text == null || req.text.Length == 0) {
			
				sr = new searchResult();
			    sr.statusCode = beagledWebSvc.SC_INVALID_QUERY;
			    sr.statusMsg = "No search terms specified";		
			}
				
			remoteChannel.Register(); 

			if (remoteObj == null)
				remoteObj = new beagledWebSvc();

			if (Application["allowGlobalAccess"] == null)
				Application["allowGlobalAccess"] =  remoteObj.allowGlobalAccess;
				
			if ((remoteObj == null) || !((bool)Application["allowGlobalAccess"] ||
				HttpContext.Current.Request.Url.IsLoopback) ) {

				return restrictedAccessResult();
			}

			sr = remoteObj.doQuery(req); 				
			return sr; 
		}
		catch (Exception e)
		{	
			throw e;
		}
	}

	[WebMethod(Description = "Simple Interface to beagled")]
	[System.Web.Services.Protocols.SoapDocumentMethodAttribute(
	"http://www.gnome.org/projects/beagle/webservices/simplebeagledQuery",
	RequestNamespace="http://www.gnome.org/projects/beagle/webservices",
	ResponseNamespace="http://www.gnome.org/projects/beagle/webservices")]
	public searchResult simpleBeagleQuery(string text)
	{
		try {
			
				searchResult sr;
			
			if (text == null || text == "")  {
			
				sr = new searchResult();
			    sr.statusCode = beagledWebSvc.SC_INVALID_QUERY;
			    sr.statusMsg = "No search terms specified";		
			}
				
			remoteChannel.Register(); 
			
			if (remoteObj == null)
				remoteObj = new beagledWebSvc();

			if (Application["allowGlobalAccess"] == null)
				Application["allowGlobalAccess"] =  remoteObj.allowGlobalAccess;
				
			if ((remoteObj == null) || !((bool)Application["allowGlobalAccess"] ||
				HttpContext.Current.Request.Url.IsLoopback) ) 	{

				return restrictedAccessResult();
			}

			sr = remoteObj.doQuery(text);
			return sr; 		
		}
		catch (Exception e)
		{
			throw e;
		}
	}

	[WebMethod(Description = "Common Interface to get more results from beagled")]
	[System.Web.Services.Protocols.SoapDocumentMethodAttribute(
	"http://www.gnome.org/projects/beagle/webservices/simplebeagledQuery",
	RequestNamespace="http://www.gnome.org/projects/beagle/webservices",
	ResponseNamespace="http://www.gnome.org/projects/beagle/webservices")]
	public searchResult getMoreResults(string searchToken, int index)
	{
		try {
		
				searchResult sr;
			
			if (searchToken == null | searchToken == "")  {
			
				sr = new searchResult();
				sr.statusCode = beagledWebSvc.SC_INVALID_SEARCH_TOKEN;
				sr.statusMsg = "Invalid Search Token";
			}
			
			remoteChannel.Register(); 
			
			if (remoteObj == null)
				remoteObj = new beagledWebSvc();

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

		sr.statusCode = beagledWebSvc.SC_UNAUTHORIZED_ACCESS;
		sr.statusMsg = localReqOnlyMsg; 

		return sr;
	}
   }
}
