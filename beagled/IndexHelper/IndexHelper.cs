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

		static private string process_owner;

		static void Main (string [] args)
		{
			bool run_by_hand = (Environment.GetEnvironmentVariable ("BEAGLE_RUN_HELPER_BY_HAND") != null);
			bool log_in_fg = (Environment.GetEnvironmentVariable ("BEAGLE_LOG_IN_THE_FOREGROUND_PLEASE") != null);

			Logger.DefaultLevel = LogLevel.Debug;

			Logger.LogToFile (PathFinder.LogDir, "IndexHelper", run_by_hand || log_in_fg);

			Beagle.Daemon.DBusisms.Init ();
			Application.InitCheck ("IndexHelper", ref args);

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
				if (run_by_hand)
					Environment.Exit (-1);
			}

			// Save the owner ID of the com.novell.Beagle service
#if HAVE_OLD_DBUS
			process_owner = Beagle.Daemon.DBusisms.BusDriver.GetServiceOwner (Beagle.DBusisms.Name);
#else
                        process_owner = Beagle.Daemon.DBusisms.BusDriver.GetNameOwner (Beagle.DBusisms.Name);
#endif

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
			RemoteIndexerImpl factory = new RemoteIndexerImpl ("factory object", null, null);
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

		public static bool CheckSenderID (string sender)
		{
			return sender == process_owner;
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
			int vmrss_original = SystemInformation.VmRss;
			const double threshold = 5.0;
			int last_vmrss = 0;

			
			while (! Shutdown.ShutdownRequested) {

				// Check resident memory usage
				int vmrss = SystemInformation.VmRss;
				double size = vmrss / (double) vmrss_original;
				if (vmrss != last_vmrss)
					Logger.Log.Debug ("Helper Size: VmRSS={0:0.0} MB, size={1:0.00}, {2:0.0}%",
							  vmrss/1024.0, size, 100.0 * (size - 1) / (threshold - 1));
				last_vmrss = vmrss;
				if (size > threshold) {
					if (RemoteIndexerImpl.CloseCount > 0) {
						Logger.Log.Debug ("Process too big, shutting down!");
						Shutdown.BeginShutdown ();
					} else {
						// Paranoia: don't shut down if we haven't done anything yet
						Logger.Log.Debug ("Deferring shutdown until we've actually done something.");
						Thread.Sleep (250);
					}
				} else {
					Thread.Sleep (1000);
				}
			}
		}
	}

}
