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

		static public readonly string Name = "com.novell.Beagle";
		static public readonly string FactoryPath = "/com/novell/Beagle/Factory";
		static public readonly string RemoteControlPath = "/com/novell/Beagle/RemoteControl";
		static public readonly string WebHistoryIndexerPath = "/com/novell/Beagle/WebHistoryIndexer";

		public delegate void Callback ();
		static public event Callback BeagleUpAgain;
		static public event Callback BeagleDown;

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
#if HAVE_OLD_DBUS
					Driver.ServiceOwnerChanged += OnNameOwnerChanged;
#else
					Driver.NameOwnerChanged += OnNameOwnerChanged;
#endif
					service = DBus.Service.Get (Connection, Name);
				}
				return service;
			}
		}

		internal static BusDriver Driver {
			get {
				if (driver == null) {
					driver = BusDriver.New (Connection);
				}
				return driver;
			}
		}

		internal static void OnNameOwnerChanged (string name,
							 string oldOwner,
							 string newOwner)
		{
			if (name == Name) {

				if (oldOwner == "") { // New service added
					//System.Console.WriteLine ("BeagleDaemon up");

					if (BeagleUpAgain != null)
						BeagleUpAgain ();

				} else if (newOwner == "") { // Existing service deleted
					//System.Console.WriteLine ("BeagleDaemon down");

					service = null;
					
					if (BeagleDown != null)
						BeagleDown ();
				}
			}
		}
	}
}
