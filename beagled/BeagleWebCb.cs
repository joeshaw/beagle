//
// BeagleWebCb.cs
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
using System.Collections;
using System.Collections.Specialized;

using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using Beagle.Daemon;
using Beagle.websvc;

namespace BWS_CodeBehind {

	public class BeagleWebPage: System.Web.UI.Page {
	
	private static string HeaderMsg = "BeagleWebInterface: ";
	
	private static string enableSessionMsg = HeaderMsg + "Please <b>enable web session tracking</b> (via Cookies or modified URL) ! <p> BeagleWeb access <i>cannot</i> function without session identification information";

	private static string localReqOnlyMsg = HeaderMsg + "Beagle web service unavailable or access restricted to local address only !";
	
	protected HtmlForm SearchForm;
	protected Button  Search, Forward, Back;
	protected DropDownList sourceList;
	protected TextBox SearchBox;
	protected Label Output; 

	protected void Page_Load(Object o, EventArgs e) {

	//note: this web-form relies on availability of Session information either through
	//     cookies or modified Url. It won't work if persistent session id is not available. 
	
		string sessId = Session.SessionID;
		Output.Visible = Back.Visible = Forward.Visible = true;

		string rawUrl = Request.RawUrl;
		int index = rawUrl.IndexOf(".aspx?");

		string actionString = null;
		if (index > 0)
			actionString = rawUrl.Substring(index + ".aspx?".Length);

		if (!IsPostBack) { //HTTP GET request

			if (actionString == null) {			
			  	//HTTP Get without any query string
				if (Session["ResultsOnDisplay"] == null) {
				 	//First access 				
					Output.Visible = false;
					Back.Visible = Forward.Visible = false;
					
					Session["InitialReqUrl"] = (Request.Url).ToString();
					Session["SearchString"] = "";
					Session["Source"] = "Anywhere";
					sourceList.SelectedValue = "Anywhere"; 					}
				else  {

				  //Redirected from Tile-Action invocation, restore Results
				  SearchBox.Text = (string) Session["SearchString"];
				  sourceList.SelectedValue = (string) Session["Source"];
				   
				  Output.Text = (string) Session["ResultsOnDisplay"];
				  beagledWeb remoteObj = (beagledWeb) Session["RemObj"];
				  if (remoteObj != null) {
				  	Back.Enabled = remoteObj.canBack(sessId);
				  	Forward.Enabled = remoteObj.canForward(sessId);
				  }
				}
			}		
		}
	
		//Process Tile!Action, if user has clicked on one:
		if (actionString != null) {

		    beagledWeb remoteObj = (beagledWeb) Session["RemObj"];
		    
		    if (remoteObj != null)
		         remoteObj.dispatchAction(sessId, actionString);
		    else {
			Output.Text = enableSessionMsg;
			Back.Visible = Forward.Visible = false;
			return;
		    }

		    //Redirect client to initial Beagle webaccess URL:
		    Response.Redirect((string)Session["InitialReqUrl"]);
		}
	}

	private bool isLocalReq() {
		//tells whether request originated from local machine
		return Request.Url.IsLoopback;
	}

	private string convertUrls(string buf)
	{
		//Replace all URL's
	  	return buf.Replace("href=\"", "href=\"" + Session["InitialReqUrl"] + "?");

/*	  //Replace specific actions in URL's
	  string buf1 = buf.Replace("href=\"action:", "href=\"" + Session["InitialReqUrl"] + "?action:"); 
	  string buf2 = buf1.Replace("href=\"dynaction:", "href=\"" + Session["InitialReqUrl"] + "?dynaction:"); 
		return buf2;
*/
	}

	protected void Search_Click(object o, EventArgs e) {

		//if (IsPostBack && Session.IsNewSession) 
		if (Session["InitialReqUrl"] == null) {
			Output.Text = enableSessionMsg;
			Back.Visible = Forward.Visible = false;
			return;
		} 

		if (SearchBox.Text == "") {		
			Output.Text = HeaderMsg + "No Results.";
			Back.Visible = Forward.Visible = false;
			Session["SearchString"] = SearchBox.Text;
			Session["ResultsOnDisplay"] = Output.Text;
			return;
		}
		
		string searchSrc = sourceList.SelectedItem.Value;
		if (searchSrc.Equals("Anywhere"))
			searchSrc = null;

		remoteChannel.Register(); 
		
		beagledWeb remoteObj = (beagledWeb) Session["RemObj"];
		if (remoteObj == null)
			 remoteObj = new beagledWeb();
			 
		if (Application["allowGlobalAccess"] == null) 
			Application["allowGlobalAccess"] = remoteObj.allowGlobalAccess;
		
		if ( (remoteObj == null) || !((bool)Application["allowGlobalAccess"] || isLocalReq())) {
			Output.Text = localReqOnlyMsg;
			Back.Visible = Forward.Visible = false;
			sourceList.Enabled = SearchBox.Enabled = Search.Enabled = false;
			return;
		} 

		string sessId = Session.SessionID;
		string response = remoteObj.doQuery(sessId, SearchBox.Text, searchSrc);
		
		if (response.StartsWith("No results"))  {
				Output.Text = HeaderMsg + response;
				Back.Visible = Forward.Visible = false;
		}
		else {
				Output.Text = HeaderMsg + convertUrls(response);
				Back.Enabled = remoteObj.canBack(sessId);
				Forward.Enabled = remoteObj.canForward(sessId);
		}
			
		Session["RemObj"] = remoteObj;
		Session["SearchString"] = SearchBox.Text;
		Session["Source"] = searchSrc;
		Session["ResultsOnDisplay"] = Output.Text;
	}
	
	protected void Back_Click(object o, EventArgs e) {

		beagledWeb remoteObj = (beagledWeb) Session["RemObj"];
		//if (IsPostBack && HttpContext.Current.Session.IsNewSession) 
		if (remoteObj == null)  {
			Output.Text = enableSessionMsg;
			Back.Visible = Forward.Visible = false;
			return;
		} 

		if (Application["allowGlobalAccess"] == null) 
			Application["allowGlobalAccess"] = remoteObj.allowGlobalAccess;
		
		if ( (remoteObj == null) || !((bool)Application["allowGlobalAccess"] || isLocalReq())) {
			Output.Text = localReqOnlyMsg;
			Back.Visible = Forward.Visible = false;
			sourceList.Enabled = SearchBox.Enabled = Search.Enabled = false;
			return;
		} 		

		string sessId = Session.SessionID;
		
		SearchBox.Text = (string) Session["SearchString"];
		sourceList.SelectedValue = (string) Session["Source"];
		
		//if (remoteObj == null)  { Output.Text = "No Results"; return; }
		string response = convertUrls(remoteObj.doBack(sessId));	
		Session["ResultsOnDisplay"] = Output.Text = HeaderMsg + response;

		Back.Enabled = (remoteObj != null) && (remoteObj.canBack(sessId));
		Forward.Enabled = (remoteObj != null) && (remoteObj.canForward(sessId));
	}

	protected void Forward_Click(object o, EventArgs e) {

		beagledWeb remoteObj = (beagledWeb) Session["RemObj"];
		//if (IsPostBack && HttpContext.Current.Session.IsNewSession) 
		if (remoteObj == null) {
			Output.Text = enableSessionMsg;
			Back.Visible = Forward.Visible = false;
			return;
		} 

		if (Application["allowGlobalAccess"] == null) 
			Application["allowGlobalAccess"] = remoteObj.allowGlobalAccess;
		
		if ( (remoteObj == null) || !((bool)Application["allowGlobalAccess"] || isLocalReq())) {
			Output.Text = localReqOnlyMsg;
			Back.Visible = Forward.Visible = false;
			sourceList.Enabled = SearchBox.Enabled = Search.Enabled = false;
			return;
		} 

		string sessId = Session.SessionID;

		SearchBox.Text = (string) Session["SearchString"];
		sourceList.SelectedValue = (string) Session["Source"];
		
		//if (remoteObj == null)  { Output.Text = "No Results"; return; }
		string response = convertUrls(remoteObj.doForward(sessId));
		Session["ResultsOnDisplay"] = Output.Text = HeaderMsg + response;

		Back.Enabled = (remoteObj != null) && (remoteObj.canBack(sessId));
		Forward.Enabled = (remoteObj != null) && (remoteObj.canForward(sessId));
	}
   }
}
