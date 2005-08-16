//
// WebFrontEnd.cs
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
using System.Collections.Specialized;
using System.Diagnostics;

using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using Beagle.Util;
using Beagle.Daemon;
using Beagle.WebService;

namespace WebService_CodeBehind {

	public class BeagleWebPage: System.Web.UI.Page {
	
	private static string HeaderMsg = "BeagleWebInterface: ";
	
	private static string enableSessionMsg = HeaderMsg + "Please <b>enable web session tracking</b> (via Cookies or modified URL) ! <p> BeagleWeb access <i>cannot</i> function without session identification information";

	private static string localReqOnlyMsg = HeaderMsg + "Beagle web service unavailable or access restricted to local address only !";
	
	protected HtmlForm SearchForm;
	protected Label Output, TitleLabel; 
	protected TextBox SearchBox;
	protected DropDownList sourceList;
	protected CheckBox	GlobalSearchCheckBox;
	protected Button  Search, Forward, Back;
	
	const string NO_RESULTS = "No results.";
	
	protected void Page_Load(Object o, EventArgs e) {

	//note: this web-form relies on availability of Session information either through
	//     cookies or modified Url. It won't work if persistent session id is not available. 
	
		string sessId = Session.SessionID;
		Output.Visible = Back.Visible = Forward.Visible = true;
		
		if (Session["ResultsOnDisplay"] != null
		    && ((string)Session["ResultsOnDisplay"]).StartsWith(HeaderMsg + NO_RESULTS)) 			{
		    Back.Visible = Forward.Visible = false;
		}

		string actionString = null;		
		bool queryStringProcessed = false;
		
		string reqUrl = Request.Url.ToString();
		int index = reqUrl.IndexOf(".aspx?");
		if (index > 0)
			actionString = reqUrl.Substring(index + ".aspx?".Length);

		if (!IsPostBack) { 
			//HTTP GET request
			if (actionString == null) {			
			  	//HTTP Get without any query string
				if (Session["ResultsOnDisplay"] == null) {
				 	//First access 				
					Output.Visible = false;
					Back.Visible = Forward.Visible = false;				

					int index1 = reqUrl.IndexOf(".aspx");
					if ((index1 > 0) && (index1 + ".aspx".Length < reqUrl.Length))
							Session["InitialReqUrl"] = reqUrl.Substring(0, index1 + ".aspx".Length);
					else
							Session["InitialReqUrl"] = reqUrl;
																			
					Session["SearchString"] = "";
					Session["Source"] = "Anywhere";
					sourceList.SelectedValue = "Anywhere"; 
					
					if (Session["GlobalCheckBox"] == null) {
						//GlobalSearchCheckBox.Visible = ((string)Session["Source"]).Equals("Anywhere");
						//By default, NetBeagleSearch checkbox is checked for local access & unchecked for ext. access 
						GlobalSearchCheckBox.Checked = isLocalReq()? true:false; 	
						Session["GlobalCheckBox"] = GlobalSearchCheckBox.Checked;
					}				
				}
				else  {

				  //Redirected from Tile-Action invocation, restore Results
				  SearchBox.Text = (string) Session["SearchString"];
				  sourceList.SelectedValue = (string) Session["Source"];
				 
				  Output.Text = (string) Session["ResultsOnDisplay"];
				  WebBackEnd remoteObj = (WebBackEnd) Session["RemObj"];
				  if (remoteObj != null) {
				  		Back.Enabled = remoteObj.canBack(sessId);
				  		Forward.Enabled = remoteObj.canForward(sessId);
				  		GlobalSearchCheckBox.Visible = (remoteObj.NetworkBeagleActive) && (sourceList.SelectedValue.Equals("Anywhere"));
				  		
						if (!isLocalReq() && (((string)TitleLabel.Text).IndexOf("host") < 0)) 							
			 				TitleLabel.Text += "  from host: " + remoteObj.HostName;				  		
				  }
				  GlobalSearchCheckBox.Checked = (bool) Session["GlobalCheckBox"];
				}
			}
			else    {  //HTTP-Get request with query string:  
				//Initial web query initiated via HTTP Get (firefox search bar):
			
				string searchString = null;
				NameValueCollection nvc = Request.QueryString;
				 
				if ((nvc != null) && (nvc.Count != 0) && 
						((searchString = nvc["text"]) != null )) {

					SearchBox.Text = searchString;		
					Session["SearchString"] = searchString;

					string source = null;
					if ((source = nvc["source"]) != null)	
						
							switch (source.ToLower())	{
							  
								case "files": 		sourceList.SelectedValue = "Files"; 	
													break;
								case "addressbook": sourceList.SelectedValue = "Contact"; 	
													break;
								case "mail"	: 		sourceList.SelectedValue = "MailMessage"; 
													break;
								case "web"	: 		sourceList.SelectedValue = "WebHistory"; 
													break;
								case "chats":		sourceList.SelectedValue = "IMLog"; 	
													break;
								case "anywhere":
								default:			sourceList.SelectedValue = "Anywhere"; 	
													break;
							}
					else 
							sourceList.SelectedValue = "Anywhere"; 
			
					Session["Source"] = sourceList.SelectedValue;	
					GlobalSearchCheckBox.Visible = ((string)Session["Source"]).Equals("Anywhere");
					//By default, NetBeagleSearch checkbox is checked for local access & unchecked for ext. access 					
					GlobalSearchCheckBox.Checked = isLocalReq()? true:false;						
					Session["GlobalCheckBox"] = GlobalSearchCheckBox.Checked;	
																			
					if (Session["ResultsOnDisplay"] == null) {
	
						int index2 = reqUrl.IndexOf(".aspx");
						if ((index2 > 0) && (index2 + ".aspx".Length < reqUrl.Length))
							Session["InitialReqUrl"] = reqUrl.Substring(0, index2 + ".aspx".Length);
						else
							Session["InitialReqUrl"] = reqUrl;						
					}
					
					queryStringProcessed = true;
			 						
					Search_Click(o, e);

					//Redirect client to initial Beagle webaccess URL:
		    		Response.Redirect((string)Session["InitialReqUrl"]);
				}		
			}  //end else for if (actionString == null)  
		}  //end if (!IsPostBack)
	
		//Process Tile!Action HTTP-Get request, if user has clicked on one:
		if (actionString != null && !queryStringProcessed) {

		    	WebBackEnd remoteObj = (WebBackEnd) Session["RemObj"];
		    
		    	if (remoteObj != null) {
		    		if (isLocalReq())
		    	    	remoteObj.dispatchAction(sessId, actionString);
		    	}
		    	else {
					Output.Text = enableSessionMsg;
					Back.Visible = Forward.Visible = GlobalSearchCheckBox.Visible = false;
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
	  //Replace specific actions in URL's
	  string buf1 = buf.Replace("href=\"action:", "href=\"" + Session["InitialReqUrl"] + "?action:"); 
	  string buf2 = buf1.Replace("href=\"dynaction:", "href=\"" + Session["InitialReqUrl"] + "?dynaction:");
	  
	  string initUrl = (string) Session["InitialReqUrl"];

	  //Get the initial part of url: i.e. http://localhost:8888/beagle
	  int i = initUrl.LastIndexOf('/');	  
	  string UrlPrefix = initUrl.Substring(0, i);

	  //Check if initial url was http://localhost:8888/search.aspx & add a trailing "/"
	  if (UrlPrefix.EndsWith("beagle"))
		UrlPrefix += "/";
	  else 
		UrlPrefix += "/beagle/";
	    
	  string[] list = buf2.Split('\"');		  	  
	  for (int k = 0; k < list.Length; k++) {
	  
	  		if (list[k].Length <= 0)
	  				continue;
			
  			list[k] = list[k].Replace("file://" + ExternalStringsHack.KdePrefix, UrlPrefix + "kde3");
  			list[k] = list[k].Replace("file://" + ExternalStringsHack.GnomePrefix, UrlPrefix + "gnome"); 
  			list[k] = list[k].Replace("file://" + ExternalStringsHack.Prefix, 	UrlPrefix + "local");	
	  }

	  string imgId = "0001";	
	  ArrayList prList = new ArrayList(); 
	  const string ImgFilePrefix = "img src=\"file://";	
		  
	  list = (String.Join ("\"", list)).Split('<'); 	  	  	 	  
	  for (int n = 0; n < list.Length; n++) {
	  
	  		if (list[n].StartsWith(ImgFilePrefix))  {
	  			
	  			string[] l = list[n].Split('\"');	  			
	  			//l[1] now contains "file:///home/usr/folder/xyz.png"		
	  			string path = l[1].Remove(0, 7); //remove "file://" prefix
	  			string[] l2 = path.Split('.');
				if (l2.Length < 2)
					continue;
					
				string[] guid = (System.Guid.NewGuid().ToString()).Split('-');
				if (guid.Length > 1)
					imgId = guid[guid.Length - 1]; //last 12 chars of guid

				string linkName = "i" + imgId + "." + l2[l2.Length - 1]; //like ixxxxxx.png, izzzzzz.jpg				 
	  								
				try {
					Process pr = new Process ();
					pr.StartInfo.UseShellExecute = true; 
					pr.StartInfo.FileName = "ln"; 
					pr.StartInfo.Arguments = " -sf " + path + " " + PathFinder.StorageDir  + "/img/" + linkName;
					
					pr.Start ();
					
					prList.Add(pr);		
				} 
				catch (Exception e) 
				{ 
					Console.WriteLine("Error creating symbolic link for image file");
				}
				
				//Replace l[1] with "http://localhost:8888/beagle/img/i1.png
				l[1] = UrlPrefix + "img/" + linkName; 
	  			list[n] = String.Join ("\"", l);				
	  		}  		
	  }
	  
	  //Wait for link ops to complete
	  if (prList.Count > 0) 
	  	foreach (Process pr in prList) {	  										
			pr.WaitForExit(); 	  				
	  		pr.Close();
	  		pr.Dispose();
	  	}
	  
	  return String.Join ("<", list);
	}

	protected void Search_Click(object o, EventArgs e) {
		
		//if (IsPostBack && Session.IsNewSession) 
		if (Session["InitialReqUrl"] == null) {
			Output.Text = enableSessionMsg;
			Back.Visible = Forward.Visible = GlobalSearchCheckBox.Visible = false;
			return;
		} 

		if (SearchBox.Text.Trim() == "") {		
			Output.Text = HeaderMsg + NO_RESULTS;
			Back.Visible = Forward.Visible = false;
			
			Session["SearchString"] = SearchBox.Text;
			Session["ResultsOnDisplay"] = Output.Text;
			Session["GlobalCheckBox"] = GlobalSearchCheckBox.Checked;			
			return;
		}
		
		string searchSrc = sourceList.SelectedItem.Value;
		if (searchSrc.Equals("Anywhere"))
			searchSrc = null;
	
		remoteChannel.Register(); 
		
		WebBackEnd remoteObj = (WebBackEnd) Session["RemObj"];
		if (remoteObj == null) {
			 remoteObj = new WebBackEnd();
		}
			 
		if ( (remoteObj == null) || !(remoteObj.allowGlobalAccess || isLocalReq())) {

			Output.Text = localReqOnlyMsg;
			Back.Visible = Forward.Visible = GlobalSearchCheckBox.Visible = false;
			sourceList.Enabled = SearchBox.Enabled = Search.Enabled = false;
			return;
		} 
		
		if (!isLocalReq() && (((string)TitleLabel.Text).IndexOf("host") < 0))
			 	TitleLabel.Text += "  from host: " + remoteObj.HostName;

		//Show check-box only if we have one or more NetworkedBeagle nodes configured:
		bool showGlobalCheckBox = (remoteObj.NetworkBeagleActive) && (searchSrc == null);

		//Setup arguments for WebBackEnd:doQuery()
		webArgs wargs = new webArgs();
		wargs.sessId = Session.SessionID;
		wargs.searchString = SearchBox.Text;
		wargs.searchSource = searchSrc;
		wargs.isLocalReq = isLocalReq();
		wargs.globalSearch = showGlobalCheckBox?GlobalSearchCheckBox.Checked:false;
		
		string response = remoteObj.doQuery(wargs);
		
		if (response.StartsWith(NO_RESULTS))  {
				Output.Text = HeaderMsg + response;
				Back.Visible = Forward.Visible = false;
		}
		else {
				Output.Text = HeaderMsg + convertUrls(response);
				Back.Enabled = remoteObj.canBack(Session.SessionID);
				Forward.Enabled = remoteObj.canForward(Session.SessionID);
		}
		
		GlobalSearchCheckBox.Visible = showGlobalCheckBox;
		Session["GlobalCheckBox"] = GlobalSearchCheckBox.Checked;			
		Session["RemObj"] = remoteObj;
		Session["ResultsOnDisplay"] = Output.Text;
		Session["SearchString"] = SearchBox.Text;
		Session["Source"] = searchSrc;		
	}
	
	protected void Back_Click(object o, EventArgs e) {

		WebBackEnd remoteObj = (WebBackEnd) Session["RemObj"];
		//if (IsPostBack && HttpContext.Current.Session.IsNewSession) 
		if (remoteObj == null)  {
			Output.Text = enableSessionMsg;
			Back.Visible = Forward.Visible = GlobalSearchCheckBox.Visible = false;
			return;
		} 

		if ( (remoteObj == null) || !(remoteObj.allowGlobalAccess || isLocalReq())) {
		
			Output.Text = localReqOnlyMsg;
			Back.Visible = Forward.Visible = GlobalSearchCheckBox.Visible = false;			
			sourceList.Enabled = SearchBox.Enabled = Search.Enabled = false;
			return;
		} 		

		string sessId = Session.SessionID;
		
		SearchBox.Text = (string) Session["SearchString"];
		sourceList.SelectedValue = (string) Session["Source"];
		
		//if (remoteObj == null)  { Output.Text = NO_RESULTS; return; }
		string response = convertUrls(remoteObj.doBack(sessId));	
		Session["ResultsOnDisplay"] = Output.Text = HeaderMsg + response;
		
		GlobalSearchCheckBox.Checked = (bool)Session["GlobalCheckBox"];
		Back.Enabled = (remoteObj != null) && (remoteObj.canBack(sessId));
		Forward.Enabled = (remoteObj != null) && (remoteObj.canForward(sessId));
	}

	protected void Forward_Click(object o, EventArgs e) {

		WebBackEnd remoteObj = (WebBackEnd) Session["RemObj"];
		//if (IsPostBack && HttpContext.Current.Session.IsNewSession) 
		if (remoteObj == null) {
			Output.Text = enableSessionMsg;
			Back.Visible = Forward.Visible = GlobalSearchCheckBox.Visible = false;			
			return;
		} 

		if ( (remoteObj == null) || !(remoteObj.allowGlobalAccess || isLocalReq())) {

			Output.Text = localReqOnlyMsg;
			Back.Visible = Forward.Visible = GlobalSearchCheckBox.Visible = false;			
			sourceList.Enabled = SearchBox.Enabled = Search.Enabled = false;
			return;
		} 

		string sessId = Session.SessionID;
		SearchBox.Text = (string) Session["SearchString"];
		sourceList.SelectedValue = (string) Session["Source"];
		
		//if (remoteObj == null)  { Output.Text = NO_RESULTS; return; }
		string response = convertUrls(remoteObj.doForward(sessId));
		Session["ResultsOnDisplay"] = Output.Text = HeaderMsg + response;
		
		GlobalSearchCheckBox.Checked = (bool)Session["GlobalCheckBox"];
		Back.Enabled = (remoteObj != null) && (remoteObj.canBack(sessId));
		Forward.Enabled = (remoteObj != null) && (remoteObj.canForward(sessId));
	}
   }
}
