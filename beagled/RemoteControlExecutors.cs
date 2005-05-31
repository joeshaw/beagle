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

			return response;
		}
	}

	[RequestMessage (typeof (ShutdownRequest))]
	public class ShutdownExecutor : RequestMessageExecutor {

		private bool DoShutdown ()
		{
			Beagle.Daemon.Shutdown.BeginShutdown ();
			return false;
		}
		
		public override ResponseMessage Execute (RequestMessage req)
		{
			// Defer the shutdown to the main loop so that we
			// don't wreak havoc with the Server IO stuff and the
			// shutdown process.
			GLib.Idle.Add (new GLib.IdleHandler (DoShutdown));

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
}
