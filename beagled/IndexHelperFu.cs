//
// IndexHelperFu.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Diagnostics;
using System.IO;

using DBus;

using Beagle.Util;

namespace Beagle.Daemon {

	public class IndexHelperFu {

		static string helper_path;
		static Process current_helper;
		
		static private void Init ()
		{
			string bihp = Environment.GetEnvironmentVariable ("_BEAGLED_INDEX_HELPER_PATH");
			if (bihp == null)
				throw new Exception ("_BEAGLED_INDEX_HELPER_PATH not set!");
			
			helper_path = Path.GetFullPath (Path.Combine (bihp, "beagled-index-helper"));
			if (! File.Exists (helper_path))
				throw new Exception ("Could not find " + helper_path);
			Logger.Log.Debug ("Found index helper at {0}", helper_path);
		}

		static private void LaunchHelperProcess ()
		{

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = helper_path;
			
			p.Start ();

			current_helper = p;
		}

		static public void OnNameOwnerChanged (string name,
						       string old_owner,
						       string new_owner)
		{
			if (name != "com.novell.BeagleIndexHelper")
				return;

			// If the helper service goes away, start up a new process
			if (new_owner == "")
				LaunchHelperProcess ();
		}

		static public void Start ()
		{
			return; // FIXME: Turned off for now

			Init ();

			BusDriver bus_driver = Beagle.Daemon.DBusisms.BusDriver;

#if HAVE_OLD_DBUS
			bus_driver.ServiceOwnerChanged += OnNameOwnerChanged;
#else
			bus_driver.ServiceOwnerChanged += OnNameOwnerChanged;
#endif

			LaunchHelperProcess ();
		}
		
		static public void Stop ()
		{
			return; // FIXME: Turned off for now

			BusDriver bus_driver = Beagle.Daemon.DBusisms.BusDriver;

#if HAVE_OLD_DBUS
			bus_driver.ServiceOwnerChanged -= OnNameOwnerChanged;
#else
			bus_driver.ServiceOwnerChanged -= OnNameOwnerChanged;
#endif

			// FIXME: Shut down the helper process
		}

	}

}
