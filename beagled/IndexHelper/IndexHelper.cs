//
// IndexHelper.cs
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
using System.IO;
using System.Threading;

using DBus;
using Gtk;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.IndexHelper {
	
	class IndexHelperTool {

		static readonly public string ServiceName = "com.novell.BeagleIndexHelper";
		static readonly public string FactoryPath = "/com/novell/BeagleIndexHelper/Factory";
		static readonly public string IndexPathPrefix = "/com/novell/BeagleIndexHelper/Index";

		static void Main (string [] args)
		{
			Logger.DefaultEcho = true;
			Logger.DefaultLevel = LogLevel.Debug;

			Beagle.Daemon.DBusisms.Init ();
			Application.Init ();

			// Keep an eye on the BusDriver so that we know if beagled
			// goes away.

			BusDriver bus_driver = Beagle.Daemon.DBusisms.BusDriver;

#if HAVE_OLD_DBUS
			bus_driver.ServiceOwnerChanged += OnNameOwnerChanged;
#else
			bus_driver.NameOwnerChanged += OnNameOwnerChanged;
#endif

			// Check that beagled is running by looking for the com.novell.Beagle service.
			if (! Beagle.Daemon.DBusisms.TestService (Beagle.DBusisms.Name)) {
				Logger.Log.Debug ("Couldn't find d-bus service '{0}' (Is beagled running?)",
						  Beagle.DBusisms.Name);
				if (Environment.GetEnvironmentVariable ("BEAGLE_RUN_HELPER_BY_HAND") == null)
					Environment.Exit (-1);
			}

			// Start monitoring the beagle daemon
			GLib.Timeout.Add (2000, new GLib.TimeoutHandler (BeagleDaemonWatcherTimeoutHandler));

			// Acquire the service
			if (! Beagle.Daemon.DBusisms.InitService (ServiceName)) {
				Logger.Log.Debug ("Couldn't acquire d-bus service '{0}'", ServiceName);
				Environment.Exit (-666);
			}

			// Since the associated RemoteIndexerProxy is null, the only method
			// that can be called on this object that isn't a no-op is NewRemoteIndexerPath.
			// We do this to avoid a separate factory object.
			RemoteIndexerImpl factory = new RemoteIndexerImpl ("factory object", null);
			Beagle.Daemon.DBusisms.RegisterObject (factory, FactoryPath);


			// Start the monitor thread, which keeps an eye on memory usage.
			Thread th = new Thread (new ThreadStart (MemoryMonitorWorker));
			th.Start ();

			Application.Run ();
			Environment.Exit (0);
		}

		static void OnNameOwnerChanged (string name,
						string old_owner,
						string new_owner)
		{
			if (name != Beagle.DBusisms.Name)
				return;

#if false
			// FIXME: This is disabled for now.  We'll let BeagleDaemonWatcherTimeoutHandler
			// deal with our shutdowns for us.
			if (new_owner == "") {
				RemoteIndexerImpl.QueueCloseForAll ();
				Thread th = new Thread (new ThreadStart (Shutdown.BeginShutdown));
				th
			}
#endif
		}

		static bool BeagleDaemonWatcherTimeoutHandler ()
		{

			// FIXME: we only poll the beagled service to work around
			// the dropped OnNameOwnerChanged signals, which is presumably a
			// dbus or dbus-sharp bug.
			if (! Beagle.Daemon.DBusisms.TestService (Beagle.DBusisms.Name)) {
				Logger.Log.Debug ("Shutting down on failed TestService in BeagleDaemonWatcherTimeoutHandler");
				RemoteIndexerImpl.QueueCloseForAll ();
				Shutdown.BeginShutdown ();
				return false;
			}
			
			return true;
		}

		static void MemoryMonitorWorker ()
		{
			const int vmsize_max = 60 * 1024;
			int last_vmsize = 0;

			while (! Shutdown.ShutdownRequested) {

				// Check memory size
				int vmsize = SystemInformation.VmSize;
				if (vmsize != last_vmsize)
					Logger.Log.Debug ("vmsize={0}, max={1}, {2:0.0}%", vmsize, vmsize_max, 100.0 * vmsize / vmsize_max);
				last_vmsize = vmsize;
				if (vmsize > vmsize_max) {
					Logger.Log.Debug ("Process too big, shutting down!");
					Shutdown.BeginShutdown ();
				} else {
					Thread.Sleep (1000);
				}
			}
		}
	}

}
