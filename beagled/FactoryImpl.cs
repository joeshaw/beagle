//
// FactoryImpl.cs
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
using Beagle.Util;

namespace Beagle.Daemon {

	public class FactoryImpl : Beagle.FactoryProxy {

		public FactoryImpl ()
		{
			DBusisms.BusDriver.ServiceOwnerChanged += this.OnServiceOwnerChanged;
		}

		////////////////////////////////////////////////////

		int path_counter = 1;

		private string RegisterObject (object obj)
		{
			string path;
			lock (this) {
				path = String.Format ("/com/novell/Beagle/{0}/{1}",
						      obj.GetType (),
						      path_counter);
				++path_counter;
			}

			string owner = DBus.Message.Current.Sender;

			DBusisms.RegisterObject (obj, path, owner);

			return path;
		}

		private void OnServiceOwnerChanged (string serviceName,
						    string oldOwner,
						    string newOwner)
		{
			// Clean up associated objects if a base service is deleted.
			if (newOwner == "" && serviceName == oldOwner) {
				//Logger.Log.Debug ("Cleaning up objects associated with '{0}'", serviceName);
				DBusisms.UnregisterByOwner (serviceName);
			}
		}

		////////////////////////////////////////////////////

		private void OnClosedHandler (QueryImpl sender)
		{
			DBusisms.UnregisterObject (sender);
		}

		override public string NewQueryPath ()
		{
			QueryImpl queryImpl = new QueryImpl (Guid.NewGuid ().ToString ());
			// When a query is closed, we need to unregister it
			// and do any necessary local clean-up.
			queryImpl.ClosedEvent += OnClosedHandler;
			return RegisterObject (queryImpl);
		}

	}
}
