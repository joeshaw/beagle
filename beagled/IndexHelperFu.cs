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
using System.Threading;

using DBus;

using Beagle.Util;

namespace Beagle.Daemon {

	public class IndexHelperFu {
		
		const string index_helper_service_name = "com.novell.BeagleIndexHelper";
		const string index_helper_factory_path = "/com/novell/BeagleIndexHelper/Factory";

		static string helper_path;
		static Process current_helper;
		static Service index_helper_service;
		
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

		static private void InitService ()
		{
			Logger.Log.Debug ("Connecting to {0}", index_helper_service_name);
			WaitForService ();
			index_helper_service = DBus.Service.Get (DBusisms.Connection, index_helper_service_name);
		}

		static private void WaitForService ()
		{
			while (! DBusisms.TestService (index_helper_service_name)) {
				Logger.Log.Debug ("Waiting for {0}...", index_helper_service_name);
				Thread.Sleep (250);
			}
		}

		static DateTime last_launch_time;

		static private void LaunchHelperProcess ()
		{
			const double time_between_launch_attempts = 5;

			double t = time_between_launch_attempts - (DateTime.Now - last_launch_time).TotalSeconds;
			// If we launched too recently, wait a bit before trying again
			if (t > 0) {
				Logger.Log.Debug ("Waiting {0:0.00}s before launching helper process", t);
				Thread.Sleep ((int) (1000 * t));
			}

			if (Environment.GetEnvironmentVariable ("BEAGLE_RUN_HELPER_BY_HAND") != null) {			
				Logger.Log.Debug ("Not launching helper process, BEAGLE_RUN_HELPER_BY_HAND is set.");
				return;
			}


			Logger.Log.Debug ("Launching helper process!");


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
			if (name != index_helper_service_name)
				return;

			Logger.Log.Debug ("OnNameOwnerChanged: {0} '{1}' '{2}'", name, old_owner, new_owner);

			// If the helper service goes away, start up a new process
			if (new_owner == "") {
				LaunchHelperProcess ();
			}
		}

		static public void Start ()
		{

			
			Init ();

			BusDriver bus_driver = Beagle.Daemon.DBusisms.BusDriver;

#if HAVE_OLD_DBUS
			bus_driver.ServiceOwnerChanged += OnNameOwnerChanged;
#else
			bus_driver.NameOwnerChanged += OnNameOwnerChanged;
#endif

			LaunchHelperProcess ();
			InitService ();
		}
		
		static public void Stop ()
		{
			BusDriver bus_driver = Beagle.Daemon.DBusisms.BusDriver;

#if HAVE_OLD_DBUS
			bus_driver.ServiceOwnerChanged -= OnNameOwnerChanged;
#else
			bus_driver.NameOwnerChanged -= OnNameOwnerChanged;
#endif

			// FIXME: Shut down the helper process
		}

#if DBUS_IS_BROKEN_BROKEN_BROKEN
		private class GetProxyClosure {

			Service service;
			string name;
			RemoteIndexerProxy proxy;

			public GetProxyClosure (Service service, string name)
			{
				this.service = service;
				this.name = name;
				this.proxy = null;
			}

			private bool IdleHandler ()
			{
				lock (this) {
					bool finished = false;
					int exception_count = 0;

					while (! finished) {

						try {
							RemoteIndexerProxy factory;
							factory = index_helper_service.GetObject (typeof (RemoteIndexerProxy),
												  index_helper_factory_path) as RemoteIndexerProxy;

							string path = factory.NewRemoteIndexerPath (name);
							
							proxy = index_helper_service.GetObject (typeof (RemoteIndexerProxy),
												path) as RemoteIndexerProxy;
							finished = proxy.Open ();
						} catch (Exception ex) {
							if (exception_count == 0) {
								Logger.Log.Debug ("Caught exception fetching proxy '{0}'", name);
								Logger.Log.Debug (ex);
							} else if (exception_count % 10 == 0) {
								Logger.Log.Debug ("More exceptions!");
								// FIXME: Maybe we dropped the OnServiceOwner signal emission,
								// so try launching a new helper process.
								// This is an annoying hack, and should be removed --- OnServiceOwner
								// should reliably be fired when the helper process dies, and the
								// fact that it isn't is probably due to a bug in the d-bus mono bindings...
								// dbus-monitor suggests that the signal is going out across the wire.
								LaunchHelperProcess ();
							}
							
							++exception_count;
							if (exception_count > 200) {
								Logger.Log.Debug ("I surrender!");
								Environment.Exit (-1949);
							}

							Thread.Sleep (200);
						}
					}
					
					Monitor.Pulse (this);
				}

				return false;
			}

			public RemoteIndexerProxy GetProxy ()
			{
				TimeSpan one_second = new TimeSpan (10000000);

				lock (this) {
					GLib.IdleHandler idle_handler = new GLib.IdleHandler (IdleHandler);
					GLib.Idle.Add (idle_handler);
					while (proxy == null) {
						Logger.Log.Debug ("Waiting for proxy '{0}'", name);
						Monitor.Wait (this, one_second);
						if (Shutdown.ShutdownRequested)
							break;
					}
				}

				return proxy;
			}
		}
#endif

		static public RemoteIndexerProxy NewRemoteIndexerProxy (string name)
		{
#if DBUS_IS_BROKEN_BROKEN_BROKEN
			GetProxyClosure gpc = new GetProxyClosure (index_helper_service, name);
			return gpc.GetProxy ();
#else
			RemoteIndexerProxy proxy = null;
			bool finished = false;
			int exception_count = 0;

			while (! finished) {
				try {
					RemoteIndexerProxy factory;
					factory = index_helper_service.GetObject (typeof (RemoteIndexerProxy),
										  index_helper_factory_path) as RemoteIndexerProxy;

					string path = factory.NewRemoteIndexerPath (name);
							
					proxy = index_helper_service.GetObject (typeof (RemoteIndexerProxy),
										path) as RemoteIndexerProxy;
					finished = proxy.Open ();
				} catch (Exception ex) {
					if (exception_count == 0) {
						Logger.Log.Debug ("Caught exception fetching proxy '{0}'", name);
						Logger.Log.Debug (ex);
					} else if (exception_count % 10 == 0) {
						Logger.Log.Debug ("More exceptions!");
						// FIXME: Maybe we dropped the OnServiceOwner signal emission,
						// so try launching a new helper process.
						// This is an annoying hack, and should be removed --- OnServiceOwner
						// should reliably be fired when the helper process dies, and the
						// fact that it isn't is probably due to a bug in the d-bus mono bindings...
						// dbus-monitor suggests that the signal is going out across the wire.
						LaunchHelperProcess ();
					}
							
					++exception_count;
					if (exception_count > 200) {
						Logger.Log.Debug ("I surrender!");
						Environment.Exit (-1949);
					}

					Thread.Sleep (200);
				}
			}

			return proxy;
#endif
		}
	}
}
