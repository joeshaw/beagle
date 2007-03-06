//
// ExternalMetadataQueryable.cs
//
// Copyright (C) 2007 Novell, Inc.
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

using Beagle.Daemon;
using Beagle.Util;
using Beagle;

namespace Beagle.Daemon {

	// An abstract class which doesn't have any storage backing it.
	// These backends exist solely to add property change indexables to
	// existing backends.
	public abstract class ExternalMetadataQueryable : IQueryable {

		private string target_name;
		private Queryable target;

		public ExternalMetadataQueryable (string target_name)
		{
			this.target_name = target_name;
		}

		protected Queryable TargetQueryable {
			get { return target; }
		}

		public Scheduler ThisScheduler {
			get { return Scheduler.Global; }
		}

		public virtual void Start ()
		{
			this.target = QueryDriver.GetQueryable (target_name);
		}

		public bool AcceptQuery (Query query)
		{
			// Always return false; there is nothing backing this
			// backend.
			return false;
		}

		public void DoQuery (Query query, IQueryResult result, IQueryableChangeData data)
		{
		}

		public string GetSnippet (string[] query_terms, Hit hit)
		{
			return null;
		}

		public QueryableStatus GetQueryableStatus ()
		{
			QueryableStatus status = new QueryableStatus ();

			// FIXME
			status.ItemCount = -1;
			status.ProgressPercent = -1;
			status.IsIndexing = false;

			return status;
		}
	}
}
