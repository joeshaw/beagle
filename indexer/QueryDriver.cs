//
// QueryDriver.cs
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
using System.Reflection;
using System.Threading;

namespace Beagle {
	
	public class QueryDriver {

		struct QueryableInfo {
			public QueryableFlavor Flavor;
			public Type Type;
		}

		static ArrayList queryableInfo = new ArrayList ();

		static bool ThisApiSoVeryIsBroken (Type m, object criteria)
		{
			return m == (Type) criteria;
		}

		static bool TypeImplementsInterface (Type t, Type iface)
		{
			Type[] impls = t.FindInterfaces (new TypeFilter (ThisApiSoVeryIsBroken),
							 iface);
			return impls.Length > 0;
		}

		static void ScanAssembly (Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes ()) {
				if (TypeImplementsInterface (type, typeof (IQueryable))) {
					foreach (object obj in Attribute.GetCustomAttributes (type)) {
						if (obj is QueryableFlavor) {
							QueryableFlavor flavor = (QueryableFlavor) obj;
							QueryableInfo info = new QueryableInfo ();
							info.Flavor = flavor;
							info.Type = type;
							queryableInfo.Add (info);
							break;
						}
					}
				}
			}
							
							
		}

		static bool initialized = false;

		static void Initialize ()
		{
			lock (queryableInfo) {
				if (initialized)
					return;
				ScanAssembly (Assembly.GetCallingAssembly ());
				initialized = true;
			}
		}

		private ArrayList queryables = new ArrayList ();

		public QueryDriver ()
		{
			Initialize ();
			foreach (QueryableInfo qi in queryableInfo) {

				IQueryable queryable;
				queryable = (IQueryable) Activator.CreateInstance (qi.Type);
				queryables.Add (queryable);
			}
		}

		class QueryClosure {

			IQueryable queryable;
			Query query;
			QueryResult result;
			
			public QueryClosure (IQueryable  _queryable,
					     Query       _query,
					     QueryResult _result)
			{
				queryable = _queryable;
				query     = _query;
				result    = _result;
			}

			public void Start ()
			{
				try {
					queryable.Query (query, result);
				} catch (Exception e) {
					Console.WriteLine ("Query to '{0}' failed with exception:\n{1}:\n{2}", queryable.Name, e.Message, e.StackTrace);
							   
				}
				result.WorkerFinished ();
			}
		}

		class QueryStartClosure {
			
			Query query;
			IEnumerable queryables;
			QueryResult result;

			public QueryStartClosure (Query       _query,
						  IEnumerable _queryables)
			{
				query = _query;
				queryables = _queryables;
			}

			public void Start (QueryResult result)
			{
				// Our iteration over the queryables is preceeded by
				// a call to WorkerStart and followed by a call to
				// WorkerFinished.  This ensures that the QueryResult
				// enters the started and finished states, even if
				// none of the queryables accept the query.
				result.WorkerStart ();
				if (! query.IsEmpty) {
					foreach (IQueryable queryable in queryables) {
						if (queryable.AcceptQuery (query)) {
							QueryClosure qc = new QueryClosure (queryable,
											    query,
											    result);
							Thread th = new Thread (new ThreadStart (qc.Start));
							
							result.WorkerStart ();
							th.Start ();
						}
					}
				}
				result.WorkerFinished ();
			}
		}

		public QueryResult Query (Query query)
		{
			QueryStartClosure qsc = new QueryStartClosure (query, queryables);
			QueryResult.QueryStart start = new QueryResult.QueryStart (qsc.Start);
			QueryResult result = new QueryResult (start);
			return result;
		}
	}
}
