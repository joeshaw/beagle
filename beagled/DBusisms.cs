//
// DBusisms.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;

using DBus;
using Beagle.Util;

namespace Beagle.Daemon {

	public class DBusisms {

		static Connection connection = null;
		static Service service = null;
		static BusDriver busDriver = null;

		public static Connection Connection {
			get { return connection; }
		}

		public static Service Service {
			get { return service; }
		}

		public static BusDriver BusDriver {
			get { return busDriver; }
		}

		[DllImport ("dbus-glib-1")]
		private extern static void dbus_g_thread_init ();

		public static void Init ()
		{
			if (connection != null)
				return;
					
			dbus_g_thread_init ();
			
			connection = Bus.GetSessionBus ();

			busDriver = BusDriver.New (connection);
		}
		
		public static bool InitService () {
			if (service == null) {
				if (Service.Exists (connection, Beagle.DBusisms.ServiceName)) {
					return false;
				}
				service = new Service (connection, Beagle.DBusisms.ServiceName);
				return true;
			} else {
				return true;
			}
		}

		// Object Registry

		class ObjectInfo {
			public string Owner;
			public string Path;
			public object Object;
			public bool   Unregistered = false;

			public void Unregister ()
			{
				lock (this) {
					if (! Unregistered) {
						Logger.Log.Debug ("D-BUS unregistered obj={0} path={1} owner={2}",
								  Object, Path, Owner == null ? "(none)" : Owner);
						if (Object is IDBusObject)
							((IDBusObject) Object).UnregisterHook ();
						DBusisms.Service.UnregisterObject (Object);
						Unregistered = true;
					}
				}
			}
		}
		static ArrayList registered_objects = new ArrayList ();

		public static void RegisterObject (object obj, string path, string owner)
		{
			lock (registered_objects) {
				ObjectInfo info = new ObjectInfo ();
				info.Object = obj;
				info.Path = path;
				info.Owner = owner;

				Logger.Log.Debug ("D-BUS registered obj={0} path={1} owner={2}",
						  obj, path, owner == null ? "(none)" : owner);

				registered_objects.Add (info);
				Service.RegisterObject (obj, path);
				if (obj is IDBusObject)
					((IDBusObject) obj).RegisterHook (path);
			}
		}

		public static void RegisterObject (object obj, string path)
		{
			RegisterObject (obj, path, null);
		}

		internal static void CleanObjectList ()
		{
			lock (registered_objects) {
				int i = 0;
				while (i < registered_objects.Count) {
					ObjectInfo info = registered_objects [i] as ObjectInfo;
					if (info.Unregistered)
						registered_objects.RemoveAt (i);
					else
						++i;
				}
			}
		}

		public static void UnregisterObject (object obj)
		{
			lock (registered_objects) {
				foreach (ObjectInfo info in registered_objects) 
					if (info.Object == obj)
						info.Unregister ();
			}

			CleanObjectList ();
		}

		public static void UnregisterByOwner (string owner)
		{
			lock (registered_objects) {
				foreach (ObjectInfo info in registered_objects)
					if (info.Owner == owner)
						info.Unregister ();
			}

			CleanObjectList ();
		}

		public static void UnregisterByPath (string path)
		{
			lock (registered_objects) {
				foreach (ObjectInfo info in registered_objects)
					if (info.Path == path)
						info.Unregister ();
			}

			CleanObjectList ();
		}

		public static void UnregisterAll ()
		{
			lock (registered_objects) {
				foreach (ObjectInfo info in registered_objects)
					info.Unregister ();
			}

			CleanObjectList ();
		}
	}
}
	
