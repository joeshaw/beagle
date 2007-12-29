//
// XesamAdaptor.cs : A adaptor to translate Xesam calls to native Beagle calls
//
// Copyright (C) 2007 Arun Raghavan <arunissatan@gmail.com>
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
using GLib;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Beagle {
	namespace Xesam {
		class Adaptor {
			private static void BusIterate()
			{
				Bus bus = Bus.Session;
				while (true) {
					bus.Iterate();
				}
			}
			public static int Main(string[] args)
			{
				// Is Beagle up?
				DaemonInformationRequest infoReq = new DaemonInformationRequest();
				try {
					infoReq.Send();
				} catch {
					Console.Error.WriteLine("Error: beagled does not appear to be running");
					return -1;
				}

				Bus bus = Bus.Session;
				ObjectPath opath = new ObjectPath("/org/freedesktop/xesam/searcher/main");
				string service = "org.freedesktop.xesam.searcher";
				Searcher search = new Searcher();

				bus.Register(service, opath, search);
				RequestNameReply nameReply = bus.RequestName(service);

				System.Threading.Thread t = new System.Threading.Thread(BusIterate);
				t.Start();

				MainLoop ml = new MainLoop();
				ml.Run();

				return 0;
			}
		}
	}
}
