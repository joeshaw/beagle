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

using DBus;

namespace Beagle 
{
	public class DBusisms {

		static public readonly string ServiceName = "com.novell.Beagle";
		static public readonly string FactoryPath = "/com/novell/Beagle/Factory";
		static public readonly string IndexerPath = "/com/novell/Beagle/Indexer";

		static Connection connection = null;
		static Service service = null;

		internal static Connection Connection {
			get { 
				if (connection == null)
					connection = Bus.GetSessionBus ();
				return connection;
			}
		}

		internal static Service Service {
			get {
				if (service == null)
					service = DBus.Service.Get (Connection, ServiceName);
				return service;
			}
		}
	}
}
