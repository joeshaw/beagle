using System;
using System.Collections;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	[RequestMessage (typeof (Query))]
	public class QueryExecutor : RequestMessageExecutor {

		private Query query;
		private QueryResult result;

		private void DisconnectResult ()
		{
			this.result.HitsAddedEvent -= OnResultHitsAdded;
			this.result.HitsSubtractedEvent -= OnResultHitsSubtracted;
			this.result.FinishedEvent -= OnResultFinished;
			this.result.CancelledEvent -= OnResultCancelled;
					
			this.result.Cancel ();
			this.result.Dispose ();
		}

		private void AttachResult ()
		{
			this.result.HitsAddedEvent += OnResultHitsAdded;
			this.result.HitsSubtractedEvent += OnResultHitsSubtracted;
			this.result.FinishedEvent += OnResultFinished;
			this.result.CancelledEvent += OnResultCancelled;
		}

		public void OnResultHitsAdded (QueryResult result, ICollection some_hits)
		{
			HitsAddedResponse response = new HitsAddedResponse (some_hits);

			this.SendAsyncResponse (response);
		}

		public void OnResultHitsSubtracted (QueryResult result, ICollection some_uris)
		{
			HitsSubtractedResponse response = new HitsSubtractedResponse (some_uris);

			this.SendAsyncResponse (response);
		}

		public void OnResultFinished (QueryResult result)
		{
			this.SendAsyncResponse (new FinishedResponse ());
		}

		public void OnResultCancelled (QueryResult result)
		{
			this.SendAsyncResponse (new CancelledResponse ());
		}

		private void OnQueryDriverChanged (Queryable queryable, IQueryableChangeData change_data)
		{
			if (this.result != null)
				QueryDriver.DoOneQuery (queryable, this.query, this.result, change_data);
		}

		public override ResponseMessage Execute (RequestMessage req)
		{
			this.query = (Query) req;

			this.result = new QueryResult ();
			AttachResult ();

			QueryDriver.ChangedEvent += OnQueryDriverChanged;
			QueryDriver.DoQuery (query, this.result);

			// Don't send a response; we'll be sending them async
			return null;
		}

		public override void Cleanup ()
		{
			DisconnectResult ();
		}
	}
}