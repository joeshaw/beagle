//
// QueryResult.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace Beagle {

	public class QueryResult {

		public delegate void QueryStart (QueryResult result);

		//////////////////////////////////

		public delegate void GotHitsHandler (object src, GotHitsArgs args);

		public class GotHitsArgs : EventArgs {
			ICollection hits;
			
			public GotHitsArgs (Hit hit)
			{
				hits = new Hit[1] { hit };
			}

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

		public delegate void FinishedHandler (object src);

		public event FinishedHandler FinishedEvent;

		//////////////////////////////////

		QueryStart start;
		ArrayList hits = new ArrayList ();
		bool started = false;
		int workers = 0;

		public QueryResult (QueryStart _start)
		{
			start = _start;
		}

		//////////////////////////////////

		public bool Started {
			get { return started; }
		}

		public bool Finished {
			get { lock (this) return started && workers == 0; }
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

		internal void Add (Hit hit)
		{
			lock (this) {
				Debug.Assert (started,
					      "Adding Hit to unstarted QueryResult");
				Debug.Assert (! Finished,
					      "Adding Hit to finished QueryResult");
				hits.Add (hit);
			}

			if (GotHitsEvent != null) {
				GotHitsArgs args = new GotHitsArgs (hit);
				GotHitsEvent (this, args);
			}
		}

		internal void Add (ICollection someHits)
		{
			lock (this) {
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
					hits.Sort ();
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
