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

		QueryDriver queryDriver;

		public FactoryImpl (QueryDriver queryDriver)
		{
			DBusisms.BusDriver.ServiceOwnerChanged += this.OnServiceOwnerChanged;

			this.queryDriver = queryDriver;

			Shutdown.ShutdownEvent += OnShutdown; 
		}

		private void OnShutdown () 
		{
		}

		////////////////////////////////////////////////////

		///
		/// The object registry
		///

		class ObjectInfo {
			public string Owner;
			public string Path;
			public object Object;
		}

		int path_counter = 1;
		ArrayList all_objects = new ArrayList ();

		private string RegisterObject (object obj)
		{
			ObjectInfo info = new ObjectInfo ();

			info.Object = obj;

			info.Owner = DBus.Message.Current.Sender;
			
			lock (this) {
				info.Path = String.Format ("/com/novell/Beagle/{0}/{1}",
							   obj.GetType (),
							   path_counter);
				++path_counter;

				all_objects.Add (info);
				DBusisms.Service.RegisterObject (obj, info.Path);
			}

			Logger.Log.Debug ("Registered object {0} as belonging to '{1}'",
					  info.Path, info.Owner);

			return info.Path;
		}

		private void UnregisterObjectAt (int i)
		{
			// Note: this function does no locking.
			// It is the responsibility of the caller to
			// make sure that 'this' is locked.
			ObjectInfo info = (ObjectInfo) all_objects [i];
			object obj = info.Object;
			Logger.Log.Debug ("Unregistering {0}", info.Path);
			DBusisms.Service.UnregisterObject (obj);
			all_objects.RemoveAt (i);
			if (obj is IDisposable)
				((IDisposable) obj).Dispose ();
		}

		public void UnregisterObject (object obj)
		{
			lock (this) {
				for (int i = 0; i < all_objects.Count; ++i) {
					ObjectInfo info = (ObjectInfo) all_objects [i];
					if (Object.ReferenceEquals (info.Object, obj)) {
						UnregisterObjectAt (i);
						return;
					}
				}
			}
		}

		public void UnregisterByOwner (string owner)
		{
			lock (this) {
				int i = 0;
				while (i < all_objects.Count) {
					ObjectInfo info = (ObjectInfo) all_objects [i];
					if (info.Owner == owner) 
						UnregisterObjectAt (i);
					else
						++i;
				}
			}
		}

		public void UnregisterAll () 
		{
			lock (this) {
				foreach (ObjectInfo info in all_objects) {
					object obj = info.Object;
					Logger.Log.Debug ("Unregistering {0}", info.Path);
					DBusisms.Service.UnregisterObject (obj);
					Logger.Log.Debug ("Disposing {0}", info.Path);
					if (obj is IDisposable)
						((IDisposable) obj).Dispose ();
					Logger.Log.Debug ("Unregistered {0}", info.Path);
				}
				all_objects = new ArrayList ();
			}
		}

		public ICollection GetByType (Type type)
		{
			ArrayList by_type = new ArrayList ();
			
			lock (this) {
				foreach (ObjectInfo info in all_objects) {
					if (type.IsInstanceOfType (info.Object))
						by_type.Add (info.Object);
				}
			}

			return by_type;
		}

		private void OnServiceOwnerChanged (string serviceName,
						    string oldOwner,
						    string newOwner)
		{
			// Clean up associated objects if a base service is deleted.
			if (newOwner == "" && serviceName == oldOwner) {
				//Logger.Log.Debug ("Cleaning up objects associated with '{0}'", serviceName);
				UnregisterByOwner (serviceName);
			}
		}

		////////////////////////////////////////////////////

		private void OnClosedHandler (QueryImpl sender)
		{
			UnregisterObject (sender);
		}

		override public string NewQueryPath ()
		{
			QueryImpl queryImpl = new QueryImpl (queryDriver,
							     Guid.NewGuid ().ToString ());
			// When a query is closed, we need to unregister it
			// and do any necessary local clean-up.
			queryImpl.ClosedEvent += OnClosedHandler;
			return RegisterObject (queryImpl);
		}

	}
}
