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

			// - Turn an async operation (query.SendAsync)
			// - to a sync operation (result.Add need to be called from this thread)
			// - to an async operation (result.Add sends an async response to the client)

			// To prevent FinishedResponse being sent before HitsAddedResponse
			Queue<ResponseMessage> response_queue = new Queue<ResponseMessage> (2);

			// Anonymous delegates cannot be un-registered ... hence
			Query.HitsAdded hits_added_handler;
			hits_added_handler = delegate (HitsAddedResponse response) {
							lock (response_queue) {
								response_queue.Enqueue (response);
								Monitor.Pulse (response_queue);
							}
						};

			Query.HitsSubtracted hits_subtracted_handler;
			hits_subtracted_handler = delegate (HitsSubtractedResponse response) {
							    lock (response_queue) {
								    response_queue.Enqueue (response);
								    Monitor.Pulse (response_queue);
							    }
						    };

			Query.Finished finished_handler;
			finished_handler = delegate (FinishedResponse response) {
						lock (response_queue) {
							response_queue.Enqueue (response);
							Monitor.Pulse (response_queue);
						}
					    };

			query.HitsAddedEvent += hits_added_handler;
			query.HitsSubtractedEvent += hits_subtracted_handler;
			query.FinishedEvent += finished_handler;
			// FIXME: Need a closed event handler ? In case the remote server is closed ?
			//query.ClosedEvent += delegate () { };

			bool done = false;
			Exception throw_me = null;

			try {
				query.SendAsync ();
			} catch (Exception ex) {
				throw_me = ex;
				done = true;
			}

			while (! done) {
				lock (response_queue) {
					Monitor.Wait (response_queue);
					while (response_queue.Count != 0) {
						//Console.WriteLine ("Time to handle response ({0})", response_queue.Count);
						ResponseMessage query_response = response_queue.Dequeue ();
						if (query_response is FinishedResponse) {
							//Console.WriteLine ("FinishedResponse. Do nothing");
							done = true;
						} else if (query_response is HitsAddedResponse) {
							HitsAddedResponse response = (HitsAddedResponse) query_response;
							//Console.WriteLine ("HitsAddedResponse. Adding {0} hits", response.NumMatches);
							result.Add (response.Hits, response.NumMatches);
						} else if (query_response is HitsSubtractedResponse) {
							HitsSubtractedResponse response = (HitsSubtractedResponse) query_response;
							//Console.WriteLine ("HitsAddedResponse. Removing {0} hits", response.Uris.Count);
							result.Subtract (response.Uris);
						}
					}
				}
			}

			query.HitsAddedEvent -= hits_added_handler;
			query.HitsSubtractedEvent -= hits_subtracted_handler;
			query.FinishedEvent -= finished_handler;

			if (throw_me != null)
				throw throw_me;

			// FIXME FIXME FIXME: Live query does not work!

			return;
		}

		public int DoCountMatchQuery (Query query)
		{
			return 0;
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
