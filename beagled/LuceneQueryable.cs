//
// LuceneQueryable.cs
// A base class for Lucene-based Queryables.
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

namespace Beagle.Daemon {

	public class LuceneQueryable : IQueryable {

		protected class LuceneQueryableChangeData : IQueryableChangeData {
			public Uri UriAdded;
			public Uri UriDeleted;
		}
		
		private void Setup (string dir, bool persistentQueue) 
		{
			ourDriver = new LuceneDriver (dir, persistentQueue);
			ourDriver.AddedEvent += OnLuceneDriverAdded;
			ourDriver.DeletedEvent += OnLuceneDriverDeleted;
		}

		public LuceneQueryable (string dir, bool persistentQueue)
		{
			Setup (dir, persistentQueue);
		}

		public LuceneQueryable (string dir)
		{
			Setup (dir, false);
		}

		private LuceneDriver ourDriver;
		protected LuceneDriver Driver {
			get { return ourDriver; }
		}

		private void OnLuceneDriverAdded (LuceneDriver source, Uri uri)
		{
			if (source == Driver && ChangedEvent != null) {
				LuceneQueryableChangeData cd = new LuceneQueryableChangeData ();
				cd.UriAdded = uri;
				ChangedEvent (this, cd);
			}
		}
		
		private void OnLuceneDriverDeleted (LuceneDriver source, Uri uri)
		{
			if (source == Driver && ChangedEvent != null) {
				LuceneQueryableChangeData cd = new LuceneQueryableChangeData ();
				cd.UriDeleted = uri;
				ChangedEvent (this, cd);
			}
		}

		////////////////////////////////////////////////////////

		virtual protected bool HitIsValid (Hit hit)
		{
			return true;
		}

		////////////////////////////////////////////////////////
		
		//
		// Actually implement the IQueryable interface
		//

		public event IQueryableChangedHandler ChangedEvent;

		public virtual bool AcceptQuery (QueryBody body)
		{
			return true;
		}

		public virtual void DoQuery (QueryBody body,
					     IQueryResult queryResult,
					     IQueryableChangeData data)
		{
			LuceneQueryableChangeData lqcd = (LuceneQueryableChangeData) data;
			ICollection hits = null;

			if (lqcd == null) {
				hits = Driver.DoQuery (body, null);
			} else if (lqcd.UriDeleted != null) {
				ArrayList subtracted = new ArrayList ();
				subtracted.Add (lqcd.UriDeleted);
				queryResult.Subtract (subtracted);
			} else if (lqcd.UriAdded != null) {
				ArrayList added = new ArrayList ();
				added.Add (lqcd.UriAdded);
				hits = Driver.DoQuery (body, added);
			}

			if (hits != null && hits.Count > 0) {

				ArrayList filtererHits = new ArrayList ();
				foreach (Hit hit in hits) {
					if (HitIsValid (hit)) {
						filtererHits.Add (hit);
					} else {
						// Do a low-priority delete --- it shouldn't matter
						// if this lingers in the index for a bit, right?
						Driver.ScheduleDelete (hit.Uri, -1000);
					}
				}
				
				queryResult.Add (filtererHits);
			}
		}

		

	}

}
