//
// EvolutionDataServerQueryable.cs
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
using System.Text;
using System.Threading;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	[QueryableFlavor (Name="EvolutionDataServer", Domain=QueryDomain.Local)]
	public class EvolutionDataServerQueryable : IQueryable {

		public event IQueryableChangedHandler ChangedEvent;

		private Evolution.Book addressbook = null;

		private void OnShutdown () 
		{
			addressbook.Dispose ();
			addressbook = null;
		}
		
		public EvolutionDataServerQueryable ()
		{
			try {
				addressbook = Evolution.Book.NewSystemAddressbook ();
				addressbook.Open (true);
				Shutdown.ShutdownEvent += OnShutdown;
			} catch {
				addressbook = null;
				Logger.Log.Warn ("Could not open Evolution addressbook.  Addressbook searching is disabled.");
			}
		}

		private Evolution.Book Addressbook {
			get { return addressbook; }
		}

		public bool AcceptQuery (QueryBody body)
		{
			if (addressbook == null)
				return false;

			if (! body.HasText)
				return false;

			if (! body.AllowsDomain (QueryDomain.Local))
				return false;

			return true;
		}

		public void DoQuery (QueryBody body, 
				     IQueryResult result,
				     IQueryableChangeData changeData)
		{
			if (addressbook == null)
				return;
			BookViewDriver driver = new BookViewDriver (addressbook,
								    body, 
								    result);

			// Check shutdownRequested in case shutdown
			// was requested between when the worker
			// started and when BookViewDriver setup an
			// OnShutdown callback
			if (Shutdown.ShutdownRequested) 
				return;

			Logger.Log.Debug ("Starting EDS query for {0}", body.Text);

			driver.Start ();
			
			lock (driver) {

				if (!driver.IsShutdown) {
					Monitor.Wait (driver);
				}
			}

			Logger.Log.Debug ("EDS query done");
		}

		public void Start ()
		{
		}
		
		public string GetHumanReadableStatus ()
		{
			return "FIXME: Needs Status";
		}
	}

}
