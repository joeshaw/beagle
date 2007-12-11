//
//  Battery.cs
//
//  Copyright (c) 2007 Lukas Lipka, <lukaslipka@gmail.com>
//

using System;
using System.Collections;
using System.Collections.Generic;

using Hal;

namespace Beagle.Util {

	public class Battery {

		private static Device adapter = null;

		static Battery ()
		{
			try {
				Manager manager = new Manager (new Context ());
				
				foreach (Device device in manager.FindDeviceByCapability ("ac_adapter"))
					adapter = device;

			} catch {
			}
		}

		public static bool UsingBattery {
			get {
				if (adapter == null)
					return false;

				return ! adapter.GetPropertyBoolean ("ac_adapter.present");
			}
		}
	}
}
