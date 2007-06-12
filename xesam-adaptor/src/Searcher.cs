//
// Searcher.cs : Implementation of the Xesam searching interface
//
// Copyright (C) 2007 Arun Raghavan <arunissatan@gmail.com>
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
using System.Collections.Generic;
using System.Threading;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Beagle {
	namespace Xesam {
		public delegate void HitsAddedMethod (string searchId, int count);
		public delegate void HitsRemovedMethod (string searchId, int[] hitIds);

		[Interface("org.freedesktop.xesam.search")]
		public interface ISearcher {
			string NewSession();
			void CloseSession(string s);
			object GetProperty(string s, string prop);
			string NewSearch(string s, string xmlSearch);
			object[][] GetHits(string s, int num);
			event HitsAddedMethod HitsAdded;
			event HitsRemovedMethod HitsRemoved;
		}

		public class Searcher : ISearcher {
			static private bool Debug = true;
			// Start worrying about threads?
			private int sessionCount = 0;
			private int searchCount = 0;
			private Dictionary<string, Session> sessions = new Dictionary<string, Session>();
			private Dictionary<string, Search> searches = new Dictionary<string, Search>();

			public event HitsAddedMethod HitsAdded;
			public event HitsRemovedMethod HitsRemoved;

			public string NewSession()
			{
				Session session = new Session();
				session.VendorId = "Beagle";
				// XXX: populate this from the Beagle daemon
				//session.VendorVersion = "0";
				session.VendorDisplay = "The Beagle sesktop search tool";
				// XXX: populate fieldnames, extensions
				sessions.Add(Convert.ToString(sessionCount), session);

				if (Debug) 
					Console.Error.WriteLine("NewSession() -- {0}", sessionCount);

				return Convert.ToString(sessionCount++);
			}

			public void CloseSession(string s)
			{
				// XXX: error handling?
				sessions[s].Close();
				sessions.Remove(s);
				if (Debug) 
					Console.Error.WriteLine("CloseSession() -- {0}", s);
			}

			public object GetProperty(string s, string prop)
			{
				Session session = sessions[s];
				object ret;

				if (Debug) 
					Console.Error.WriteLine("GetProperty() -- {0}, {1}", s, prop);

				switch (prop) {
					case "search.live": 
						ret =  session.SearchLive;
						break;
					case "search.blocking": 
						ret =  session.SearchBlocking;
						break;
					case "hit.fields":
						ret =  session.HitFields;
						break;
					case "hit.fields.extended":
						ret =  session.HitFieldsExtended;
						break;
					case "hit.snippet.length":
						ret =  session.HitSnippetLength;
						break;
					case "sort.primary":
						ret =  session.SortPrimary;
						break;
					case "sort.secondary":
						ret =  session.SortSecondary;
						break;
					case "sort.order":
						ret =  session.SortOrder;
						break;
					case "vendor.id":
						ret =  session.VendorId;
						break;
					case "vendor.version":
						ret =  session.VendorVersion;
						break;
					case "vendor.display":
						ret =  session.VendorDisplay;
						break;
					case "vendor.xesam":
						ret =  session.VendorXesam;
						break;
					case "vendor.fieldnames":
						ret =  session.VendorFieldNames;
						break;
					case "vendor.extensions":
						ret =  session.VendorExtensions;
						break;
					default:
						ret =  null;
						break;
				}

				if (Debug) {
					if (ret is string[]) {
						Console.Error.Write(" `-- returning ");
						foreach (string i in (string[])ret)
							Console.Error.Write("\"{0}\" ", i);
						Console.Error.WriteLine("");
					} else {
						Console.Error.WriteLine(" `-- returning {0}", ret);
					}
				}
				return ret;
			}

			public string NewSearch(string s, string xmlQuery)
			{
				Session session = sessions[s];
				string searchId = Convert.ToString(searchCount++);
				Search search;

				search = session.CreateSearch(searchId, xmlQuery);
				search.HitsAddedHandler += HitsAdded;
				search.HitsRemovedHandler += HitsRemoved;
				searches.Add(searchId, search);

				// Don't let HitsAdded events be raised till we
				// return
				search.mutex.WaitOne();
				search.Start();

				if (Debug) 
					Console.Error.WriteLine("NewSearch() -- {0}, {1}, {2}", s, searchId, xmlQuery);

				search.mutex.ReleaseMutex();
				return searchId;
			}

			public object[][] GetHits(string s, int num)
			{
				return searches[s].GetHits(num);
			}
		}
	}
}
