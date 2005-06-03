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
using System.Threading;
using System.Collections;

using Beagle.Util;

namespace Beagle.Daemon {

	[QueryableFlavor (Name="NetworkedBeagle", Domain=QueryDomain.Global, RequireInotify=false)]
	public class NetworkedBeagle : IQueryable 
	{
		static Logger log = Logger.Get ("NetworkedBeagle");
		
	    string NetBeagleConfigFile = "netbeagle.cfg";

		ArrayList NetBeagleList;
		
		public NetworkedBeagle ()
		{
			NetBeagleList = new ArrayList ();		
		}

		public string Name {
			get { return "NetworkedBeagle"; }
		}

		public void Start () 
		{
			SetupNetBeagleList ();
		}

		void SetupNetBeagleList ()
		{
			if (!File.Exists (Path.Combine (PathFinder.StorageDir, NetBeagleConfigFile)))
				return;

			StreamReader reader = new StreamReader(Path.Combine 
										(PathFinder.StorageDir, NetBeagleConfigFile));

			string entry;
			while ( ((entry = reader.ReadLine ()) != null) && (entry.Trim().Length > 1)) {
			
				if ((entry[0] != '#') && (entry.IndexOf(':') > 0)) {
					string[] data = entry.Split (':');
					string host = data[0];
					int port = Convert.ToInt32 (data[1]);		
					NetBeagleList.Add (new NetBeagleHandler (host, port, this));
				}								
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

		public void DoQuery (Query query,IQueryResult result,
				     		 IQueryableChangeData changeData)
		{	
			ArrayList resultHandleList = new ArrayList();
			
			if (NetBeagleList.Count == 0) 
				return;
				
			log.Debug("NetBeagleQueryable: DoQuery ... Starting NetBeagleHandler queries");
			foreach (NetBeagleHandler nb in NetBeagleList)
			{
				IAsyncResult iar = nb.DoQuery (query, result, changeData);
				resultHandleList.Add (iar);
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
	}
}
