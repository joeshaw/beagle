//
// EventStatistics.cs
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

namespace Beagle.Daemon.FileSystemQueryable {

	public class EventStatistics {

		const double decay = 0.5;

		// OK, this is crack.  We fix a very large
		// constant that is returned as the gap before
		// the second event comes in.  This is a hack
		// that lets us pretty much ignore the special
		// case of the initial state in our backoff code.
		const double first_gap = 1.0e+8;

		private class ItemInfo {
			private string path;
			private int event_count = 0;
			private DateTime previous_event_time;
			private double average_gap;

			public ItemInfo (string _path)
			{
				path = _path;
			}

			public string Path {
				get { return path; }
			}

			public int EventCount {
				get { return event_count; }
			}

			public DateTime PreviousEventTime {
				get { return previous_event_time; }
			}

			public double Gap {
				get {
					if (event_count == 0)
						return first_gap;
					else
						return (DateTime.Now - previous_event_time).TotalSeconds;
				}
			}
			

			public double AverageGap {
				get {
					return average_gap;
				}
			}

			// What would the new average gap be if an event
			// came in right now?
			public double ImpliedAverageGap {
				get {
					return ComputeAverageGap (DateTime.Now);
				}
			}

			// The amount of time, in seconds, that has to pass
			// without an event before the implied average gap
			// drops below the specified target.
			public double TimeToTargetLevel (double target_iag)
			{
				if (event_count < 2 || target_iag < average_gap)
					return 0;

				// Compute time required for level to decay.
				double t;
				t = (target_iag - decay * average_gap) / (1 - decay);

				// Adjust for time elapsed since last event
				t -= Gap;

				return Math.Max (t, 0);
			}

			private double ComputeAverageGap (DateTime now)
			{
				if (event_count <= 1)
					return first_gap;
				
				double gap = (now - previous_event_time).TotalSeconds;

				if (event_count == 2)
					return gap;

				return decay * average_gap + (1 - decay) * gap;
			}
				

			public void Touch ()
			{
				DateTime now = DateTime.Now;
				average_gap = ComputeAverageGap (now);
				previous_event_time = now;
				++event_count;
			}
		}

		private Hashtable items = new Hashtable ();

		public void AddEvent (string path)
		{
			ItemInfo info;

			info = items [path] as ItemInfo;
			if (info == null) {
				info = new ItemInfo (path);
				items [path] = info;
			}
			
			info.Touch ();

#if false
			Console.WriteLine ("{0}: {1} {2} {3}",
					   info.Path,
					   info.EventCount,
					   info.Gap,
					   info.AverageGap);
#endif
		}

		public void ForgetPath (string path)
		{
			if (items.Contains (path))
				items.Remove (path);
		}

	}

}
