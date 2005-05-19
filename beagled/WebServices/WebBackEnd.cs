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
	
	public class WebBackEnd: MarshalByRefObject{

		static WebBackEnd instance = null;
		static bool allow_global_access = false;

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

		public bool allowGlobalAccess{
			get { return allow_global_access; }
		}

		public static void init(bool web_global) 
		{
		   allow_global_access = web_global;

		   if (instance == null) {
			  instance = new WebBackEnd();

			  //Register TCP Channel Listener
		  	  ChannelServices.RegisterChannel(new TcpChannel(8347));	

			  WellKnownServiceTypeEntry WKSTE = 
				new WellKnownServiceTypeEntry(typeof(WebBackEnd),
				 "WebBackEnd.rem", WellKnownObjectMode.Singleton);
			  RemotingConfiguration.ApplicationName="beagled";
			  RemotingConfiguration.RegisterWellKnownServiceType(WKSTE);
		   }
		}

		void OnHitsAdded (QueryResult qres, ICollection hits)
		{	
			//Console.WriteLine("WebBackEnd: OnHitsAdded() invoked with {0} hits", hits.Count);

			if (result.Contains(qres)) {
				BT.SimpleRootTile root = ((Resp) result[qres]).rootTile;
				root.Add(hits);
				//Console.WriteLine("Hit Added to Root Tile");
			}
		}

		
		void OnHitsSubtracted (QueryResult qres, ICollection uris)
		{
			if (result.Contains(qres)) {
				BT.SimpleRootTile root = ((Resp) result[qres]).rootTile;
				root.Subtract (uris);
			}
		}

		void OnFinished (QueryResult qres)
		{
			if (result.Contains(qres))
				Console.WriteLine("WebBackEnd:OnFinished() - Got {0} results from beagled QueryDriver", ((Resp) result[qres]).rootTile.HitCollection.NumResults);

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
					result.Remove(qres);
				
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

			BT.SimpleRootTile root = resp.rootTile;
			return (root != null)? root.HitCollection.CanPageForward:false;
		}

		public string doForward(string sessId)
		{	
			Resp resp = (Resp) sessionResp[sessId];
			if (!canForward(sessId) || (resp == null))
				return NO_RESULTS;
				
			BT.SimpleRootTile root = resp.rootTile;
			if (root != null) {
				root.HitCollection.PageForward ();
			
				bufferRenderContext bctx = new bufferRenderContext();
				resp.bufferContext = bctx;
				root.Render(bctx);
				return (getResultsLabel(root) + bctx.buffer);
			}

			return NO_RESULTS;
		}

		public bool canBack(string sessId)
		{	
			Resp resp = (Resp) sessionResp[sessId];
			if (resp == null) 
				return false;

			BT.SimpleRootTile root = resp.rootTile;
			return (root != null) ? root.HitCollection.CanPageBack:false;
		}

		public string doBack(string sessId)
		{	
			Resp resp = (Resp) sessionResp[sessId];
			if (!canBack(sessId) || (resp == null))
				return NO_RESULTS;
		
			BT.SimpleRootTile root = resp.rootTile;
			if (root != null) {
				root.HitCollection.PageBack();
			
				bufferRenderContext bctx = new bufferRenderContext();
				resp.bufferContext = bctx;
				root.Render(bctx);
				return (getResultsLabel(root) + bctx.buffer);
			}
			
			return NO_RESULTS;
		}
		
		public string doQuery(string sessId, string searchString, string searchSource)
		{	
			if (sessId == null || searchString == null || searchString == "")
				return NO_RESULTS;
						 
			Console.WriteLine("WebBackEnd: Got Search String: " + searchString); 
			
			Query query = new Query();
			query.AddText (searchString);
			if (searchSource != null && searchSource != "")
				query.AddSource(searchSource);	
			query.AddDomain (QueryDomain.Global);

			QueryResult qres = new QueryResult ();
									
			//Note: QueryDriver.DoQuery() local invocation is used. 
			//The root tile is used only for adding hits and generating html.
			BT.SimpleRootTile root = new BT.SimpleRootTile (); 							
			root.Query = query;
											
			bufferRenderContext bctx = new bufferRenderContext();
			Resp resp = new Resp(root, bctx);

			AttachQueryResult (qres, resp);

			//Add sessionId-Resp mapping
			if (sessionResp.Contains(sessId)) 
				sessionResp[sessId] = resp;
			else
				sessionResp.Add(sessId, resp);	

			Console.WriteLine("WebBackEnd: Starting Query for string \"{0}\"", query.QuotedText);

			QueryDriver.DoQuery (query, qres);

			//Wait only till we have enough results to display
			while ((result.Contains(qres)) && 
					(root.HitCollection.NumResults < 10)) 
				Thread.Sleep(5);

			root.Render(bctx);
			
			return (getResultsLabel(root) + bctx.buffer);
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
			
				Console.WriteLine("tile_id: {0}, action: {1}", tile_id, action);

				BT.Tile t = ((Resp)sessionResp[sessId]).GetTile (tile_id);
			
				if (t == null)
					return;

				MethodInfo info = t.GetType().GetMethod (action,
					BindingFlags.Public | BindingFlags.NonPublic | 	
					BindingFlags.Instance, null, 		
					CallingConventions.Any,	 new Type[] {}, null);

				if (info == null) {
					Console.WriteLine ("Couldn't find method called {0}", action);
					return;
				}

				object[] attrs = info.GetCustomAttributes (false);
				foreach (object attr in attrs) {
					if (attr is BT.TileActionAttribute) {
						info.Invoke (t, null);
						return;
					}
				}
				Console.WriteLine ("{0} does not have the TileAction attribute", t);
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

	private class Resp {

		private BT.SimpleRootTile root;
		private Hashtable tileTab = null;
		private bufferRenderContext bufCtx = null;	

		public Resp(BT.SimpleRootTile rt, bufferRenderContext bCtx)
		{
			this.root = rt;
			this.tileTab = Hashtable.Synchronized(new Hashtable());
			this.bufCtx = bCtx;
			CacheTile(rt);
			bufCtx.table = tileTab;
			bufCtx.ClearActions();
		}
	
		public BT.SimpleRootTile rootTile {			
		 	get {return root;}
		}
		public Hashtable tileTable {			
		 	get { return tileTab; } 
		}
		public bufferRenderContext bufferContext {
			get { return bufCtx; }
			set {
				bufCtx = value;
				bufCtx.table = tileTab;
				bufCtx.ClearActions();
			}
		}
		public void CacheTile (BT.Tile tile) 
		{
			tileTab[tile.UniqueKey] = tile;
		}
		public BT.Tile GetTile (string key)  
		{
			if (key == "")
				return root;
			return (Beagle.Tile.Tile) tileTab[key];
		}
	}

//////////////////////////////////////////////////////////////////////////
        private class bufferRenderContext : BT.TileRenderContext {

		private System.Text.StringBuilder sb;
		private Hashtable tileTable = null;
		private bool renderStylesDone = false;
		
		public bufferRenderContext () 
		{
			sb = new StringBuilder(4096);
			renderStylesDone = false;
		}
		
		public string buffer {
			get { return sb.ToString();  }
		}
		
		public Hashtable table {
			get { return tileTable;  }
			set { tileTable = value; }
		}

		/////////////////////////////////////////////////

		Hashtable actionTable = null;
		int actionId = 1;

		public void ClearActions ()
		{
			actionTable = new Hashtable ();
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
				
			if (tile != null)
				tile.Render (this);
		}

//////////////////////////////////////////////////////////////////////////

string static_stylesheet = "<style type=\"text/css\" media=\"screen\"> body, html { background: white; margin: 0; padding: 0; font-family: Sans, Segoe, Trebuchet MS, Lucida, Sans-Serif;  text-align: left; line-height: 1.5em; } a, a:visited {  text-decoration: none; color: #2b5a8a; } a:hover {  text-decoration: underline; } img {  border: 0px; } table {  width: 100%; border-collapse: collapse;	font-size: 10px; } tr {  border-bottom: 1px dotted #999999; } tr:hover {  background: #f5f5f5; } tr:hover .icon { background-color: #ddddd0; } td {  padding: 6px; } td.icon {  background-color: #eeeee0; min-height: 80px; width: 1%;	min-width: 80px; text-align: center; vertical-align: top; padding: 12px; } .icon img { max-width: 60px; padding: 4px; }	.icon img[src$='.jpg'], img[src$='.jpeg'], img[src*='.thumbnails'] {//  max-width: 48px; border: 1px dotted #bbb; //  padding: 4px;	background: #f9f9f9; } td.content { padding-left: 12px; vertical-align: top; } #hilight {  background-color: #ffee66; color: #000000;  padding-left: 2px; padding-right: 2px; margin-left: -2px; margin-right: -2px; } .name {font-size: 1.3em; font-weight: bold; color: black; } .date { font-size: 1em; color: black; margin-bottom: 0.6em; margin-top: 0.2em; margin-left: 16px; } .snippet {font-size: 1em; color: gray; margin-left: 16px; } .url {font-size: 1em; color: #008200; margin-left: 16px;	} ul {margin-left: 16px; padding: 0px; clear: both;	} .actions {  font-size: 1em; } .actions li {  float: left;  display: block;  vertical-align: middle;  padding: 0;  padding-left: 20px;  padding-right: 12px;  background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/navigation/stock_right.png) no-repeat;  min-height: 16px; -moz-opacity: 0.5; } tr:hover .actions li {  -moz-opacity: 1.0; } #phone { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/generic/stock_landline-phone.png) no-repeat; } #email { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail.png) no-repeat; } #email-forward { background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail-forward.png) no-repeat; } #email-reply {  background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/net/stock_mail-reply.png) no-repeat; }	#message { background: url(file:///opt/gnome/share/icons/hicolor/16x16/apps/im-yahoo.png) no-repeat; } #reveal {  background: url(file:///opt/gnome/share/icons/hicolor/16x16/stock/io/stock_open.png) no-repeat; }	td.footer { text-align: right;   border-bottom: solid 1px white; } </style>";			
		}
    }
}
