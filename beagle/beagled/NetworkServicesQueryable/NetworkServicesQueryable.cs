//
//  NetworkServicesQueryable.cs
//
//  Copyright (c) 2007 Lukas Lipka <lukaslipka@gmail.com>.
//

using System;
using System.Collections;
using System.Collections.Generic;

using Beagle;
using Beagle.Util;

namespace Beagle.Daemon.NetworkServicesQueryable {

	[QueryableFlavor (Name="NetworkServices", Domain=QueryDomain.Neighborhood, RequireInotify=false)]
	public class NetworkServicesQueryable : IQueryable {

		public NetworkServicesQueryable ()
		{
		}

		public void Start ()
		{
		}

		public bool AcceptQuery (Query query)
		{
			// FIXME: Disable all queries by default
			return false;
		}

		public void DoQuery (Query query, IQueryResult result, IQueryableChangeData data)
		{
			// Forward our local query to remote hosts
			//query.Transports.Clear ();
			//query.RegisterTransport (new HttpTransport ("http://flikr:4001/"));
			//query.Keepalive = false;
			//query.Send ();
		}

		public ISnippetReader GetSnippet (string[] query_terms, Hit hit, bool full_text)
		{
			return null;
		}

		public QueryableStatus GetQueryableStatus ()
		{
			QueryableStatus status = new QueryableStatus ();
			return status;
		}
	}
}