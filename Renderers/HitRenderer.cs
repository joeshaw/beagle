//
// HitRenderer.cs
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

namespace Beagle {
	
	public abstract class HitRenderer {

		public delegate void RefreshHandler (HitRenderer hr);
		public event RefreshHandler RefreshEvent;

		protected virtual bool ProcessHit (Hit hit)
		{
			return true;
		}

		protected virtual void ProcessClear ()
		{ }

		//
		// Widgety Goodness
		//

		public abstract Gtk.Widget Widget { get; }
		protected abstract void DoRefresh ();

		private uint timeoutId;

		public void Refresh ()
		{
			lock (this) {
				if (timeoutId != 0) {
					GLib.Source.Remove (timeoutId);
					timeoutId = 0;
				}
				DoRefresh ();
				if (RefreshEvent != null)
					RefreshEvent (this);
			}
		}

		public void Flush ()
		{
			lock (this) {
				if (timeoutId != 0)
					Refresh ();
			}
		}

		private bool HandleScheduled ()
		{
			lock (this) {
				timeoutId = 0;
				Refresh ();
			}
			return false;
		}

		public void ScheduleRefresh (uint time)
		{
			lock (this) {
				if (timeoutId != 0)
					return;
			
				if (time == 0) {
					GLib.IdleHandler handler;
					handler = new GLib.IdleHandler (HandleScheduled);
					timeoutId = GLib.Idle.Add (handler);
				} else {
					GLib.TimeoutHandler handler;
					handler = new GLib.TimeoutHandler (HandleScheduled);
					timeoutId = GLib.Timeout.Add (time, handler);
				}
			}
		}

		//
		// Collect & Manage Hits
		//

		private ArrayList hits = new ArrayList ();

		private int first = 0;
		private int displayedCount = 10;

		public int FirstDisplayed {
			get { return first; }

			set {
				int f = (int) value;
				f = Math.Max (f, 0);
				f = Math.Min (f, hits.Count - displayedCount);
				if (f != first) {
					first = f;
					ScheduleRefresh (0);
				}
			}
		}

		public int LastDisplayed {
			get { return FirstDisplayed + DisplayedCount - 1; }
		}

		public int DisplayedCount {
			get { 
				int n = hits.Count - first;
				if (n < 0)
					return 0;
				if (n > displayedCount)
					n = displayedCount;
				return n;
			}
			
			set {
				int dc = (int) value;
				if (dc != displayedCount) {
					displayedCount = dc;
					ScheduleRefresh (0);
				}
			}

		}

		public void DisplayFirst ()
		{
			FirstDisplayed = 0;
		}

		public void DisplayPrev ()
		{
			FirstDisplayed -= displayedCount;
		}

		public void DisplayNext ()
		{
			FirstDisplayed += displayedCount;
		}

		public void DisplayLast ()
		{
			FirstDisplayed = hits.Count;
		}

		/////////////////////////////////////


		public IList Hits {
			get { return hits; }
		}

		public int TotalCount {
			get { return hits.Count; }
		}
		
		public void Clear ()
		{
			lock (this) {
				ProcessClear ();
				hits.Clear ();
			}
			ScheduleRefresh (0);
		}
		
		// Return true if we need a refresh after this add.
		private bool  AddInRightPlace (Hit hit)
		{
			if (hits.Count == 0) {
				hits.Add (hit);
				if (first == 0)
					return true;
			} else {
				int i = hits.BinarySearch (hit);
				hits.Insert (i < 0 ? ~i : i, hit);
				if (hits.Count >= first && i < first + displayedCount)
					return true;
			}

			return false;
		}

		public void Add (Hit hit)
		{
			bool needRefresh = false;
			lock (this) {
				if (ProcessHit (hit))
					needRefresh = AddInRightPlace (hit);
			}
			
			// ScheduleRefresh needs to acquire a lock,
			// so we wait until hits is unlocked before
			// calling it.
			if (needRefresh)
				ScheduleRefresh (100);
		}

		public void Add (ICollection _hits)
		{
			bool needRefresh = false;
			lock (this) {
				foreach (Hit hit in _hits)
					if (ProcessHit (hit))
						if (AddInRightPlace (hit))
							needRefresh = true;
			}
			
			// Again, we do this here to avoid a potential
			// deadlock.
			if (needRefresh)
				ScheduleRefresh (50);
		}
	}
}
