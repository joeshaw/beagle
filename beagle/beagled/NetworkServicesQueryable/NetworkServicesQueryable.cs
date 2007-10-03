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
			return (Conf.Networking.NetworkServices.Count > 0);
		}

		public void DoQuery (Query query, IQueryResult result, IQueryableChangeData data)
		{
			// Get rid of the standard UnixTransport so that we can
			// forward our local query to remote hosts.
			query.Transports.Clear ();

			foreach (NetworkService service in Conf.Networking.NetworkServices)
				query.RegisterTransport (new HttpTransport (service.UriString));

			ArrayList hits = new ArrayList ();

			query.Keepalive = false;
			query.Send ();

			result.Add (hits);
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
