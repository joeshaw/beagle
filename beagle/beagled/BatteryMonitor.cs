//
//  BatteryMonitor.cs
//
//  Copyright (c) 2007 Lukas Lipka, <lukaslipka@gmail.com>
//

using System;
using System.Collections;
using System.Collections.Generic;

using Hal;

using Beagle.Util;

namespace Beagle.Daemon {

	public class BatteryMonitor {

		private static Device adapter = null;
		private static bool prev_on_battery = false;

		public static void Init ()
		{
			try {
				Manager manager = new Manager (new Context ());

				foreach (Device device in manager.FindDeviceByCapability ("ac_adapter")) {
					Log.Debug ("Found HAL device AC adapter for battery monitoring.");
					
					device.PropertyModified += OnPropertyModified;
					adapter = device;
					
					prev_on_battery = ! device.GetPropertyBoolean ("ac_adapter.present");
					
					break;
				}
			} catch (Exception e) {
				Log.Error (e, "Failed to acquire a HAL device for battery monitoring");
			}
		}

		private static void OnPropertyModified (int num_changes, PropertyModification[] props)
		{
			foreach (PropertyModification p in props) {
				if (p.Key == "ac_adapter.present")
					CheckStatus ();
			}
		}

		private static void CheckStatus ()
		{
			bool on_ac = adapter.GetPropertyBoolean ("ac_adapter.present");
			bool index_on_battery = Conf.Daemon.GetOption (Conf.Names.IndexOnBattery, false);

			if (prev_on_battery && (on_ac || index_on_battery)) {
				if (on_ac) {
					Log.Info ("Detected a switch from battery to AC power. Restarting scheduler.");
				}

				Scheduler.Global.Start ();
				prev_on_battery = false;
			} else if (! prev_on_battery && ! on_ac && ! index_on_battery) {
				Log.Info ("Detected a switch from AC power to battery.  Stopping scheduler.");
				Scheduler.Global.Stop ();
				prev_on_battery = true;
			}
		}

		public static bool UsingAC {
			get {
				if (adapter == null)
					return true;

				return adapter.GetPropertyBoolean ("ac_adapter.present");
			}
		}
	}
}