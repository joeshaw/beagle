//
// SearchWindow.cs
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
using System.Runtime.InteropServices;

using DBus;

namespace Beagle.Daemon
{
	public class DBusisms {

		static Connection connection = null;
		static Service service = null;
		static BusDriver bus_driver = null;
		static FactoryImpl factory = null;

		public static Connection Connection {
			get { 
				if (connection == null)
					connection = Bus.GetSessionBus ();
				return connection;
			}
		}

		public static Service Service {
			get {
				if (service == null)
					service = new Service (Connection,
							       Beagle.DBusisms.ServiceName);
				return service;
			}
		}

		public static BusDriver BusDriver {
			get {
				if (bus_driver == null)
					bus_driver = BusDriver.New (Connection);
				return bus_driver;
			}
		}

		public static FactoryImpl Factory {
			get {
				return factory;
			}
		}

		[DllImport ("dbus-glib-1")]
		private extern static void dbus_g_thread_init ();

		public static void Init ()
		{
			dbus_g_thread_init ();

			factory = new FactoryImpl ();
			DBusisms.Service.RegisterObject (factory,
							 Beagle.DBusisms.FactoryPath);
		}
				
	}
}
