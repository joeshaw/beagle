//
// QueryDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Threading;

namespace Dewey {

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

			public void Start ()
			{
				ICollection hits = queryable.Query (query);
				
				// Make sure that hits are properly sourced and locked.
				foreach (Hit hit in hits) {
					if (hit.Source == null)
						hit.Source = queryable.Name;
					hit.Lockdown ();
				}

				result.Add (hits);
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
