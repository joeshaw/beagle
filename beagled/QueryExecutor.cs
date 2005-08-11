//
// QueryExecutor.cs
//
// Copyright (C) 2005 Novell, Inc.
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
			QueryDriver.DoQuery (query,
					     this.result,
					     new RequestMessageExecutor.AsyncResponse (this.SendAsyncResponse));

			// Don't send a response; we'll be sending them async
			return null;
		}

		public override void Cleanup ()
		{
			DisconnectResult ();
		}
	}
}
