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
using System.Collections.Generic;
using System.Threading;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Beagle {
	namespace Xesam {
		public delegate void HitsAddedMethod (string searchId, int count);
		public delegate void HitsRemovedMethod (string searchId, int[] hitIds);
		public delegate void SearchDoneMethod (string searchId);

		[Interface("org.freedesktop.xesam.Search")]
		public interface ISearcher {
			string NewSession();
			void CloseSession(string s);
			object GetProperty(string s, string prop);
			object SetProperty(string s, string prop, object val);
			string NewSearch(string s, string xmlSearch);
			void StartSearch(string s);
			void CloseSearch(string s);
			string[] GetState();
			int GetHitCount(string s);
			object[][] GetHits(string s, int num);
			event HitsAddedMethod HitsAdded;
			event HitsRemovedMethod HitsRemoved;
			// XXX: We don't implement HitsModified and StateChanged because there is no
			// simple corresponding entity in Beagle
			event SearchDoneMethod SearchDone;
		}

		public class Searcher : ISearcher {
			static private bool Debug = true;
			// XXX: Assuming that you won't change the beagled version in between
			static private uint beagleVersion = 0;
			// XXX: Start worrying about threads?
			private int sessionCount = 0;
			private int searchCount = 0;
			private Dictionary<string, Session> sessions = new Dictionary<string, Session>();
			private Dictionary<string, Search> searches = new Dictionary<string, Search>();

			public event HitsAddedMethod HitsAdded;
			public event HitsRemovedMethod HitsRemoved;
			public event SearchDoneMethod SearchDone;

			public string NewSession()
			{
				if (beagleVersion == 0) {
					DaemonInformationRequest infoReq = new DaemonInformationRequest(true, false, false, false);
					DaemonInformationResponse infoResp = (DaemonInformationResponse) infoReq.Send();
					beagleVersion = (uint)VersionStringToInt(infoResp.Version);
				}

				Session session = new Session();
				session.VendorId = "Beagle";
				session.VendorVersion = beagleVersion;
				session.VendorDisplay = "The Beagle desktop search tool";
				// XXX: populate fieldnames, extensions
				sessions.Add(Convert.ToString(sessionCount), session);

				if (Debug) 
					Console.Error.WriteLine("NewSession() -- {0}", sessionCount);

				return Convert.ToString(sessionCount++);
			}

			public void CloseSession(string s)
			{
				Session session = sessions[s];

				if (s == null) {
					if (Debug) 
						Console.Error.WriteLine("Error: CloseSession() -- {0} is not a valid session", s);
					return;
				}

				session.Close();
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
					case "vendor.ontology.fields":
						ret =  session.VendorOntologyFields;
						break;
					case "vendor.ontology.contents":
						ret =  session.VendorOntologyContents;
						break;
					case "vendor.ontology.sources":
						ret =  session.VendorOntologySources;
						break;
					case "vendor.extensions":
						ret =  session.VendorExtensions;
						break;
					case "vendor.ontologies":
						ret = session.VendorOntologies;
						break;
					case "vendor.maxhits":
						ret = session.VendorMaxHits;
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

			public object SetProperty(string s, string prop, object val)
			{
				Session session = sessions[s];
				object ret;

				if (Debug) 
					Console.Error.WriteLine("GetProperty() -- {0}, {1}", s, prop);

				switch (prop) {
					case "search.live": 
						session.SearchLive = (bool)val;
						ret =  session.SearchLive;
						break;
					case "hit.fields":
						session.HitFields = (string[])val;
						ret =  session.HitFields;
						break;
					case "hit.fields.extended":
						session.HitFieldsExtended = (string[])val;
						ret =  session.HitFieldsExtended;
						break;
					case "hit.snippet.length":
						session.HitSnippetLength = (uint)val;
						ret =  session.HitSnippetLength;
						break;
					case "sort.primary":
						session.SortPrimary = (string)val;
						ret =  session.SortPrimary;
						break;
					case "sort.secondary":
						session.SortSecondary = (string)val;
						ret =  session.SortSecondary;
						break;
					case "sort.order":
						session.SortOrder = (string)val;
						ret =  session.SortOrder;
						break;
					case "vendor.id":
						/* read-only */
						ret =  session.VendorId;
						break;
					case "vendor.version":
						/* read-only */
						ret =  session.VendorVersion;
						break;
					case "vendor.display":
						/* read-only */
						ret =  session.VendorDisplay;
						break;
					case "vendor.xesam":
						/* read-only */
						ret =  session.VendorXesam;
						break;
					case "vendor.ontology.fields":
						/* read-only */
						ret =  session.VendorOntologyFields;
						break;
					case "vendor.ontology.contents":
						/* read-only */
						ret =  session.VendorOntologyContents;
						break;
					case "vendor.ontology.sources":
						/* read-only */
						ret =  session.VendorOntologySources;
						break;
					case "vendor.extensions":
						/* read-only */
						ret =  session.VendorExtensions;
						break;
					case "vendor.ontologies":
						/* read-only */
						ret = session.VendorOntologies;
						break;
					case "vendor.maxhits":
						/* read-only */
						ret = session.VendorMaxHits;
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

				// These two are emitted even if search.live is false
				// If you touch these, look at Close() too
				search.HitsAddedHandler += HitsAdded;
				search.SearchDoneHandler += SearchDone;
				if (session.SearchLive) {
					search.HitsRemovedHandler += HitsRemoved;
				}

				searches.Add(searchId, search);

				if (Debug) 
					Console.Error.WriteLine("NewSearch() -- {0}, {1}, {2}", s, searchId, xmlQuery);

				return searchId;
			}

			public void StartSearch(string s)
			{
				Search search = searches[s];

				if (search == null) {
					if (Debug) 
						Console.Error.WriteLine("Error: StartSearch() -- {0} is not a valid search", s);
					return;
				}

				search.Start();
				if (Debug) 
					Console.Error.WriteLine("StartSearch() -- {0}", s);
			}

			public void CloseSearch(string s)
			{
				Search search = searches[s];

				if (search == null) {
					if (Debug) 
						Console.Error.WriteLine("Error: CloseSearch() -- {0} is not a valid search", s);
					return;
				}

				search.Close();
				searches.Remove(s);

				if (Debug) 
					Console.Error.WriteLine("CloseSearch() -- {0}", s);
			}

			public string[] GetState()
			{
				// XXX: Is there any way to find out if we're doing a FULL_INDEX ?
				string[] ret = new string[] { null, null };
				if (Debug) 
					Console.Error.WriteLine("GetState(): {0} - {1}", ret[0], ret[1]);

				DaemonInformationRequest infoReq = new DaemonInformationRequest(false, false, true, true);
				DaemonInformationResponse infoResp = (DaemonInformationResponse) infoReq.Send();

				if (infoResp.IsIndexing) {
					ret[0] = "IDLE";
					return ret;
				} else {
					ret[0] = "UPDATE";

					// XXX: We're just building total progress percentage as an average of all
					// queryables' percentages
					int qCount = 0;
					int progress = 0;
					foreach (QueryableStatus status in infoResp.IndexStatus) {
						if (status.ProgressPercent != -1) {
							qCount++;
							progress += status.ProgressPercent;
						}
					}

					ret[1] = (progress/qCount).ToString();
				}

				if (Debug) 
					Console.Error.WriteLine("GetState(): {0} - {1}", ret[0], ret[1]);
				return ret;
			}

			public int GetHitCount(string s)
			{
				Search search = searches[s];

				if (search == null) {
					if (Debug) 
						Console.Error.WriteLine("Error: GetHitCount() -- {0} is not a valid search", s);
					return 0;
				}

				int ret = search.GetHitCount();

				if (Debug) 
					Console.Error.WriteLine("GetHitCount() -- {0}, {1}", s, ret);

				return ret;
			}

			public object[][] GetHits(string s, int num)
			{
				return searches[s].GetHits(num);
			}

			private int VersionStringToInt(string version)
			{
				// FIXME: Ewwww!
				if (version.StartsWith("0.2"))
					return 2;
				if (version.StartsWith("0.3"))
					return 3;
				else
					// Is this really Beagle?
					return 0;
			}
		}
	}
}
