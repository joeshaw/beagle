//
// QueryResult.cs
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
using System.Diagnostics;
using System.Threading;

namespace Beagle {

	public class QueryResult : IQueryResult {

		public delegate void QueryStart (QueryResult result);

		//////////////////////////////////

		public delegate void GotHitsHandler (QueryResult src, GotHitsArgs args);

		public class GotHitsArgs : EventArgs {
			ICollection hits;
			
			public GotHitsArgs (ICollection someHits)
			{
				hits = someHits;
			}

			public int Count {
				get { return hits.Count; }
			}

			public ICollection Hits {
				get { return hits; }
			}
		}

		public event GotHitsHandler GotHitsEvent;

		//////////////////////////////////

		public delegate void FinishedHandler (QueryResult src);

		public event FinishedHandler FinishedEvent;

		//////////////////////////////////

		public delegate void CancelledHandler (QueryResult src);

		public event CancelledHandler CancelledEvent;

		//////////////////////////////////

		QueryStart start;
		ArrayList hits = new ArrayList ();
		bool started = false;
		bool cancelled = false;
		int workers = 0;

		public QueryResult (QueryStart _start)
		{
			start = _start;
		}

		//////////////////////////////////

		public bool Started {
			get { return started; }
		}

		public bool Cancelled {
			get { return cancelled; }
		}

		public bool Finished {
			get { lock (this) return started && (cancelled || workers == 0); }
		}

		public int Count {
			get { return hits.Count; }
		}

		public ICollection Hits {
			get { return hits; }
		}
		
		public void Start ()
		{
			if (! started) {

				// We increment the worker counter before we start
				// spawning threads...
				WorkerStart ();

				start (this);

				// ...and decrement it when we are done.  This solves a
				// few problems:
				// * If no queryable accepts the query, the result
				//   would never enter the 'started' state.
				// * If the first thread finished before the second one has
				//   time to set up, the worker count could drop to zero and
				//   the result would enter the 'finished' state prematurely.
				WorkerFinished ();
			}
		}

		public void Cancel ()
		{
			lock (this) {
				if (cancelled)
					return;
				cancelled = true;
			}
			CancelledEvent (this);
		}

		public void Add (ICollection someHits)
		{
			lock (this) {
				if (cancelled)
					return;
				Debug.Assert (started, "Adding Hits to unstarted QueryResult");
				Debug.Assert (workers > 0,
					      "Adding Hits to idle QueryResult");
				foreach (Hit hit in someHits)
					hits.Add (hit);
			}

			if (GotHitsEvent != null) {
				GotHitsArgs args = new GotHitsArgs (someHits);
				GotHitsEvent (this, args);
			}
		}

		internal void WorkerStart ()
		{
			lock (this) {
				started = true;
				++workers;
			}
		}

		internal void WorkerFinished ()
		{
			lock (this) {
				Debug.Assert (started, 
					      "WorkerFinished called on unstarted QueryResult");
				Debug.Assert (workers > 0,
					      "Too many calls to WorkerFinished");
				--workers;

				if (workers == 0) {
					if (FinishedEvent != null)
						FinishedEvent (this);
					Monitor.Pulse (this);
				}
			}
		}

		public void Wait ()
		{
			if (! Started)
				Start ();

			lock (this) {
				while (true) {
					if (Finished)
						return;
					Monitor.Wait (this);
				}
			}
		}
	}
}
