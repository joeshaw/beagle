//
// IndexingServiceQueryable.cs
//
// Copyright (C) 2005 Novell, Inc.
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

namespace Beagle.Daemon.IndexingServiceQueryable {

	[QueryableFlavor (Name="IndexingService", Domain=QueryDomain.Local, RequireInotify=false)]
	public class IndexingServiceQueryable : LuceneQueryable {

		public IndexingServiceQueryable () : base ("IndexingServiceIndex")
		{
			Server.RegisterRequestMessageHandler (typeof (IndexingServiceRequest), new Server.RequestMessageHandler (HandleMessage));
		}

		private class IndexableGenerator : IIndexableGenerator {
			private IEnumerator to_add_enumerator;
			private int count, done_count = 0;

			public IndexableGenerator (ICollection to_add)
			{
				this.count = to_add.Count;
				this.to_add_enumerator = to_add.GetEnumerator ();
			}

			public Indexable GetNextIndexable ()
			{
				return to_add_enumerator.Current as Indexable;
			}
			
			public bool HasNextIndexable ()
			{
				++done_count;
				return to_add_enumerator.MoveNext ();
			}

			public string StatusName {
				get { return String.Format ("IndexingService: {0} of {1}", done_count, count); }
			}
		}

		private ResponseMessage HandleMessage (RequestMessage msg)
		{
			IndexingServiceRequest isr = (IndexingServiceRequest) msg;

			foreach (Uri uri in isr.ToRemove) {
				Scheduler.Task task = NewRemoveTask (uri);
				ThisScheduler.Add (task);
			}

			// FIXME: There should be a way for the request to control the
			// scheduler priority of the task.

			if (isr.ToAdd.Count > 0) {
				IIndexableGenerator ind_gen = new IndexableGenerator (isr.ToAdd);
				Scheduler.Task task = NewAddTask (ind_gen);
				task.Priority = Scheduler.Priority.Immediate;
				ThisScheduler.Add (task);
			}

			// FIXME: There should be an asynchronous response  (fired by a Scheduler.Hook)
			// that fires when all of the items have been added to the index.
			
			// No response
			return new EmptyResponse ();
		}

	}

}
