//
// QueryDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.Collections;
using System.Threading;

namespace Beagle {
	
	public class QueryDriver {

		ArrayList queryables = new ArrayList ();
		
		public void Add (IQueryable iq)
		{
			queryables.Add (iq);
		}

		public void AutoPopulateHack ()
		{
			Add (new IndexDriver ());
			Add (new GoogleDriver ());
#if ENABLE_EVO_SHARP
			Add (new EvolutionDataServerDriver ());
#endif
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

			void Collect (ICollection hits)
			{
				// Make sure that hits are properly sourced and locked.
				foreach (Hit hit in hits) {
					if (hit == null) {
						Console.WriteLine ("NULL HIT!");
						continue;
					}
						
					if (hit.Source == null)
						hit.Source = queryable.Name;
					hit.Lockdown ();
				}
				
				result.Add (hits);
			}

			public void Start ()
			{
				try {
					queryable.Query (query, new HitCollector (Collect));
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
