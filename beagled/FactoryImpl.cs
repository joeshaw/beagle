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

namespace Beagle.Daemon {

	public class FactoryImpl : Beagle.FactoryProxy {

		QueryDriver query_driver;

		public FactoryImpl ()
		{
			DBusisms.BusDriver.ServiceDeleted += this.OnServiceDeleted;

			query_driver = new QueryDriver ();
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

			info.Owner = DBus.Message.Current.Sender;

			info.Path = String.Format ("/com/novell/Beagle/{0}/{1}",
						   obj.GetType (),
						   path_counter);
			++path_counter;

			info.Object = obj;

			all_objects.Add (info);

			
			DBusisms.Service.RegisterObject (obj, info.Path);

			Console.WriteLine ("Registered object {0} as belonging to '{1}'",
					   info.Path, info.Owner);

			return info.Path;
		}

		public void UnregisterObject (object obj)
		{
			for (int i = 0; i < all_objects.Count; ++i) {
				ObjectInfo info = (ObjectInfo) all_objects [i];
				if (Object.ReferenceEquals (info.Object, obj)) {
					Console.WriteLine ("Unregistering {0}", info.Path);
					DBusisms.Service.UnregisterObject (obj);
					all_objects.RemoveAt (i);
					return;
				}
			}
		}

		public void UnregisterByOwner (string owner)
		{
			int i = 0;
			while (i < all_objects.Count) {
				ObjectInfo info = (ObjectInfo) all_objects [i];
				if (info.Owner == owner) {
					Console.WriteLine ("Unregistering {0}", info.Path);
					DBusisms.Service.UnregisterObject (info.Object);
					all_objects.RemoveAt (i);
				} else {
					++i;
				}
			}
		}

		public ICollection GetByType (Type type)
		{
			ArrayList by_type = new ArrayList ();

			foreach (ObjectInfo info in all_objects) {
				if (type.IsInstanceOfType (info.Object))
					by_type.Add (info.Object);
			}

			return by_type;
		}

		private void OnServiceDeleted (string serviceName)
		{
			Console.WriteLine ("Cleaning up objects associated with '{0}'", serviceName);
			UnregisterByOwner (serviceName);
		}

		////////////////////////////////////////////////////		

		override public string NewQueryPath ()
		{
			QueryImpl query_impl = new QueryImpl (query_driver);
			return RegisterObject (query_impl);
		}

	}
}
