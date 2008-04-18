//
//  NetworkServicesQueryable.cs
//
//  Copyright (c) 2007 Lukas Lipka <lukaslipka@gmail.com>.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

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

			// Anonymous delegates cannot be un-registered ... hence
			Query.HitsAdded hits_added_handler;
			hits_added_handler = delegate (HitsAddedResponse response) {
								//Console.WriteLine ("Adding hits added response");
								result.Add (response.Hits, response.NumMatches);
						};

			Query.HitsSubtracted hits_subtracted_handler;
			hits_subtracted_handler = delegate (HitsSubtractedResponse response) {
								// Console.WriteLine ("Adding hits subtracted response");
								result.Subtract (response.Uris);
						    };

			Query.Finished finished_handler;
			finished_handler = delegate (FinishedResponse response) {
							//Console.WriteLine ("Adding finished response");
							// NO-OP
					    };

			// FIXME: ClosedEvent ? Should be handled by HttpTransport but should we do something more

			query.HitsAddedEvent += hits_added_handler;
			query.HitsSubtractedEvent += hits_subtracted_handler;
			query.FinishedEvent += finished_handler;

			Exception throw_me = null;

			try {
				query.SendAsyncBlocking ();
			} catch (Exception ex) {
				throw_me = ex;
			}

			// FIXME FIXME FIXME: Live query does not work!

			query.HitsAddedEvent -= hits_added_handler;
			query.HitsSubtractedEvent -= hits_subtracted_handler;
			query.FinishedEvent -= finished_handler;

			if (throw_me != null)
				throw throw_me;

			return;
		}

		public int DoCountMatchQuery (Query query)
		{
			return 0;
		}

		public ISnippetReader GetSnippet (string[] query_terms, Hit hit, bool full_text, int ctx_length, int snp_length)
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
