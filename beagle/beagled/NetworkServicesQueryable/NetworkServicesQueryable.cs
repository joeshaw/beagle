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
			List<string[]> services = Conf.Networking.GetListOptionValues (Conf.Names.NetworkServices);
			return (services != null && services.Count > 0);
		}

		public void DoQuery (Query query, IQueryResult result, IQueryableChangeData data)
		{
			// Get rid of the standard UnixTransport so that we can
			// forward our local query to remote hosts.
			query.Transports.Clear ();

			List<string[]> network_services = Conf.Networking.GetListOptionValues (Conf.Names.NetworkServices);
			if (network_services != null) {
				foreach (string[] service in network_services)
					query.RegisterTransport (new HttpTransport (service [1]));
			}

			ArrayList hits = new ArrayList ();

			query.Keepalive = false;
			query.SendAsync ();

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
