//
// QueryManager.cs
//
// Copyright (C) 2004 Novell, Inc.
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

using DBus;


namespace Beagle.Daemon
{
	public class QueryManager : Beagle.QueryManagerProxy
	{
		private static QueryManager theQueryManager = null;

		private Hashtable liveQueries = new Hashtable ();
		private static ulong numQueries = 0;
		private QueryDriver driver;

		private QueryManager ()
		{
			driver = new QueryDriver ();
		}

		public override string NewQuery () 
		{
			numQueries++;
			
			string path = "/com/novell/Beagle/Queries/" + numQueries;
			
			LiveQuery query = new LiveQuery (path, driver);
			liveQueries[query.Name] = query;
			DBusisms.Service.RegisterObject (query, path);

			return path;
		}

		public ICollection GetLiveQueries () {
			return liveQueries.Values;
		}

		static void SomeRdfXmlWasSunk (string rdfXml)
		{
			Console.WriteLine ("Got '{0}'", rdfXml);
		}

		public override string NewRdfSink ()
		{
			RdfSinkImpl sink = new RdfSinkImpl (new Beagle.GotRdfXmlHandler (SomeRdfXmlWasSunk));
			return sink.Path;
		}

		public override string NewRdfSource ()
		{
			RdfSourceImpl source = new RdfSourceImpl ();
			source.AddRdfXml ("One!");
			source.AddRdfXml ("Two!");
			source.AddRdfXml ("Three!");
			return source.Path;
		}

		public void RemoveQuery (LiveQuery query)
		{
			liveQueries.Remove (query);
			DBusisms.Service.UnregisterObject (query);
		}

		public static QueryManager Get () {
			if (theQueryManager == null) {
				theQueryManager = new QueryManager ();

				DBusisms.Service.RegisterObject (theQueryManager,
								 Beagle.DBusisms.QueryManagerPath);
			}
			
			return theQueryManager;
		}
	}
}
