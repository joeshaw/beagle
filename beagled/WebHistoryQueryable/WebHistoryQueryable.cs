//
// WebHistoryQueryable.cs
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
using System.IO;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.WebHistoryQueryable {

	[QueryableFlavor (Name="WebHistory", Domain=QueryDomain.Local)]
	public class WebHistoryQueryable : LuceneQueryable {

		public class WebHistoryIndexerImpl : Beagle.WebHistoryIndexerProxy {

			LuceneQueryable queryable;

			public WebHistoryIndexerImpl (LuceneQueryable queryable)
			{
				this.queryable = queryable;
			}
			
			public override void Index (string xml)
			{
				Indexable indexable = FilteredIndexable.NewFromEitherXml (xml);
				Scheduler.Task task = queryable.NewAddTask (indexable);
				task.Priority = Scheduler.Priority.Immediate;
				queryable.ThisScheduler.Add (task);
			}
		}
		
		public WebHistoryQueryable () : base ("WebHistoryIndex")
		{
		}

		public override void Start ()
		{
			base.Start ();

			WebHistoryIndexerImpl indexer = new WebHistoryIndexerImpl (this);
			DBusisms.RegisterObject (indexer, Beagle.DBusisms.WebHistoryIndexerPath);
		}
	}
		

}
