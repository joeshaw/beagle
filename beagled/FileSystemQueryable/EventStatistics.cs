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

		const double decayFactor = 0.5;

		private class ItemInfo {
			public string Path;
			public int EventCount = 0;
			public DateTime PreviousEventTime;
			public double AverageEventGap;

			public ItemInfo (string path)
			{
				Path = path;
			}

			public void Touch ()
			{
				DateTime now = DateTime.Now;
				if (EventCount > 0) {
					double gap = (now - PreviousEventTime).TotalSeconds;
					if (EventCount == 1)
						AverageEventGap = gap;
					else
						AverageEventGap = decayFactor * AverageEventGap + (1 - decayFactor) * gap;
				}
				++EventCount;
				PreviousEventTime = now;
			}

			public double TimeSinceLastEvent {
				get { 
					if (EventCount > 0)
						return (DateTime.Now - PreviousEventTime).TotalSeconds;
					return 0;
				}
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
			
			double t = info.TimeSinceLastEvent;
			info.Touch ();
			
#if false
			Console.WriteLine ("{0}: {1} {2} {3}",
					   info.Path,
					   info.EventCount,
					   t,
					   info.AverageEventGap);
#endif
		}

		public void ForgetPath (string path)
		{
			if (items.Contains (path))
				items.Remove (path);
		}

	}

}
