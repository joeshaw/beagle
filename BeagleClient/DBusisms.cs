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

namespace Beagle 
{
	public class DBusisms {

		static public readonly string ServiceName = "com.novell.Beagle";
		static public readonly string FactoryPath = "/com/novell/Beagle/Factory";
		static public readonly string WebHistoryIndexerPath = "/com/novell/Beagle/WebHistoryIndexer";
		static public readonly string FileSystemIndexerPath = "/com/novell/Beagle/FileSystemIndexer";

		static Connection connection = null;
		static Service service = null;
		static BusDriver driver = null;

		[DllImport ("dbus-glib-1")]
		private extern static void dbus_g_thread_init ();

		internal static Connection Connection {
			get { 
				if (connection == null) {
					dbus_g_thread_init ();
					connection = Bus.GetSessionBus ();
				}
				return connection;
			}
		}

		internal static Service Service {
			get {
				if (service == null) {
					service = DBus.Service.Get (Connection, ServiceName);
					driver = BusDriver.New (connection);
					driver.ServiceDeleted += OnServiceDeleted;
				}
				return service;
			}
		}

		internal static void OnServiceDeleted (string serviceName)
		{
			// FIXME: It would be nice to do something more graceful than this.
			if (serviceName == ServiceName) {
				Console.WriteLine ("****");
				Console.WriteLine ("****");
				Console.WriteLine ("**** Lost Connection to service {0}", serviceName);
				Console.WriteLine ("**** Shutting Down Beagle Client");
				Console.WriteLine ("****");
				Console.WriteLine ("****");
				
				System.Environment.Exit (-666);
			}
		}
	}
}
