//
// QueryResult.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Diagnostics;
using System.Threading;
using Beagle.Util;
namespace Beagle.Daemon {

	public class QueryResult : IQueryResult {

		public delegate void QueryWorker (QueryResult result);

		class QueryWorkerClosure {
			QueryWorker worker;
			QueryResult result;

			public QueryWorkerClosure (QueryWorker _worker, QueryResult _result)
			{
				worker = _worker;
				result = _result;
			}

			public void Start ()
			{
				try {
					worker (result);
				} catch (Exception e) {
					Logger.Log.Error ("QueryWorker '{0}' failed with exception:\n{1}:\n{2}",
							  worker, e.Message, e.StackTrace);
				}
				result.WorkerFinished ();
			}
		}

		//////////////////////////////////

		public delegate void StartedHandler (QueryResult source);
		public event StartedHandler StartedEvent;

		public delegate void HitsAddedHandler (QueryResult source, ICollection someHits);
		public event HitsAddedHandler HitsAddedEvent;

		public delegate void HitsSubtractedHandler (QueryResult source, ICollection someUris);
		public event HitsSubtractedHandler HitsSubtractedEvent;

		public delegate void FinishedHandler (QueryResult source);
		public event FinishedHandler FinishedEvent;

		public delegate void CancelledHandler (QueryResult source);
		public event CancelledHandler CancelledEvent;

		//////////////////////////////////

		int workers = 0;
		bool cancelled = false;

		public QueryResult ()
		{

		}

		//////////////////////////////////

		public bool Active {
			get { return workers > 0 && ! cancelled; }
		}

		public bool Cancelled {
			get { return cancelled; }
		}

		public void Cancel ()
		{
			lock (this) {
				if (cancelled)
					return;
				cancelled = true;

				if (CancelledEvent != null)
					CancelledEvent (this);
			}
		}

		public void Add (ICollection someHits)
		{
			lock (this) {
				if (cancelled)
					return;

				Debug.Assert (workers > 0, "Adding Hits to idle QueryResult");

				if (someHits.Count == 0)
					return;
		
				ArrayList filteredHits = new ArrayList ();

				foreach (Hit hit in someHits) {
					if (hit.IsValid && Relevancy.AdjustScore (hit))
						filteredHits.Add (hit);
				}
				
				if (HitsAddedEvent != null)
					HitsAddedEvent (this, filteredHits);
			}
		}

		public void Subtract (ICollection someUris)
		{
			lock (this) {
				if (cancelled)
					return;

				Debug.Assert (workers > 0, "Subtracting Hits from idle QueryResult");

				if (someUris.Count == 0)
					return;

				if (HitsSubtractedEvent != null)
					HitsSubtractedEvent (this, someUris);
			}
		}

		public void AttachWorker (QueryWorker worker)
		{
			lock (this) {
				if (cancelled)
					return;

				QueryWorkerClosure qwc;
				qwc = new QueryWorkerClosure (worker, this);

				// QueryDriver has an enclosing WorkerStart,
				// so if we call WorkerStartin this tread, 
				// all the workers will have a chance 
				// to start before Finished is called
				
				WorkerStartNoLock ();

				Thread th;
				th = new Thread (new ThreadStart (qwc.Start));
				th.Start ();
			}
		}

		private void WorkerStartNoLock ()
		{
			++workers;
			if (workers == 1 && StartedEvent != null)
				StartedEvent (this);
			
		}

		internal void WorkerStart ()
		{
			lock (this) {
				WorkerStartNoLock ();
			}
		}

		internal void WorkerFinished ()
		{
			lock (this) {
				Debug.Assert (workers > 0, "Too many calls to WorkerFinished");
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
			lock (this) {
				while (true) {
					if (cancelled || workers == 0)
						return;
					Monitor.Wait (this);
				}
			}
		}
	}
}
