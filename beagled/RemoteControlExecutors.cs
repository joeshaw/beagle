//
// SnippetExecutor.cs
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
using System.Threading;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	[RequestMessage (typeof (DaemonInformationRequest))]
	public class DaemonInformationExecutor : RequestMessageExecutor {

		public override ResponseMessage Execute (RequestMessage req)
		{
			DaemonInformationResponse response = new DaemonInformationResponse ();
			response.Version = ExternalStringsHack.Version;
			response.HumanReadableStatus = Scheduler.Global.GetHumanReadableStatus ();
			response.IndexInformation = QueryDriver.GetIndexInformation ();
			response.IsIndexing = QueryDriver.IsIndexing;

			return response;
		}
	}

	[RequestMessage (typeof (ShutdownRequest))]
	public class ShutdownExecutor : RequestMessageExecutor {

		private void DoShutdown ()
		{
			Shutdown.BeginShutdown ();
		}

		public override ResponseMessage Execute (RequestMessage req)
		{
			// Start the shutdown process in a separate thread
			// to avoid a deadlock: BeginShutdown() waits until
			// all worker process are finished, but this method
			// itself is part of a worker.
			ExceptionHandlingThread.Start (new ThreadStart (DoShutdown));

			return new EmptyResponse ();
		}
	}

	[RequestMessage (typeof (ReloadConfigRequest))]
	public class ReloadConfigExecutor : RequestMessageExecutor {
		
		public override ResponseMessage Execute (RequestMessage req)
		{
			Conf.Load (true);
			return new EmptyResponse ();
		}
	}

	[RequestMessage (typeof (OptimizeIndexesRequest))]
	public class OptimizeIndexesExecutor : RequestMessageExecutor {
		
		public override ResponseMessage Execute (RequestMessage req)
		{
			LuceneQueryable.OptimizeAll ();
			return new EmptyResponse ();
		}
	}
}
