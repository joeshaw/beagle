//
// LastCrawlTime.cs
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

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public class LastCrawlTime {

		const string last_crawl_attr = "LastCrawl";

		public static DateTime Get (string path)
		{
			string last_crawl_str = ExtendedAttribute.Get (path, last_crawl_attr);
			if (last_crawl_str == null)
				return DateTime.MinValue;
			
			DateTime last_crawl;
			try {
				last_crawl = StringFu.StringToDateTime (last_crawl_str);
			} catch {
				// Treat malformed strings as missing
				last_crawl = DateTime.MinValue;
			}

			return last_crawl;
		}

		public static void Set (string path, DateTime last_crawl)
		{
			string last_crawl_str;
			last_crawl_str = StringFu.DateTimeToString (last_crawl);
			ExtendedAttribute.Set (path, last_crawl_attr, last_crawl_str);
		}

		private class LastCrawlTimeClosure {
			private string path;
			private DateTime crawl_time;
			
			public LastCrawlTimeClosure (string path, DateTime crawl_time)
			{
				this.path = path;
				this.crawl_time = crawl_time;
			}

			public void Set ()
			{
				LastCrawlTime.Set (path, crawl_time);
			}
		}

		// Creates a task group where the given path's last crawl time
		// will be set to crawl_time when the last task in the group
		// is executed.
		public static Scheduler.TaskGroup NewTaskGroup (string path, DateTime crawl_time)
		{
			LastCrawlTimeClosure ctc = new LastCrawlTimeClosure (path, crawl_time);
			Scheduler.Hook post_hook = new Scheduler.Hook (ctc.Set);
			return Scheduler.NewTaskGroup ("crawl " + path, null, post_hook);
		}

	}
}
