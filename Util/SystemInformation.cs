//
// SystemInformation.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Beagle.Util {

	public class SystemInformation {

		const double loadavg_poll_delay = 3;
		static DateTime proc_loadavg_time  = DateTime.MinValue;
		static double cached_loadavg_1min  = -1;
		static double cached_loadavg_5min  = -1;
		static double cached_loadavg_15min = -1;

		static private void CheckLoadAverage ()
		{
			// Only scan /proc/loadavg at most once every 10 seconds
			if ((DateTime.Now - proc_loadavg_time).TotalSeconds < loadavg_poll_delay)
				return;

			Stream stream = new FileStream ("/proc/loadavg",
							FileMode.Open,
							FileAccess.Read,
							FileShare.Read);
			TextReader reader = new StreamReader (stream);

			string line = reader.ReadLine ();
			
			reader.Close ();
			stream.Close ();

			string [] parts = line.Split (' ');
			cached_loadavg_1min = Double.Parse (parts [0]);
			cached_loadavg_5min = Double.Parse (parts [1]);
			cached_loadavg_15min = Double.Parse (parts [2]);

			proc_loadavg_time = DateTime.Now;
		}

		static public double LoadAverageOneMinute {
			get {
				CheckLoadAverage ();
				return cached_loadavg_1min;
			}
		}
		
		static public double LoadAverageFiveMinute {
			get {
				CheckLoadAverage ();
				return cached_loadavg_5min;
			}
		}

		static public double LoadAverageFifteenMinute {
			get {
				CheckLoadAverage ();
				return cached_loadavg_15min;
			}
		}

		///////////////////////////////////////////////////////////////

		const double screensaver_poll_delay = 1;
		static DateTime screensaver_time = DateTime.MinValue;
		static bool cached_screensaver_running = false;
		static double cached_screensaver_idle_time = 0;

		private enum ScreenSaverState {
			Off      = 0,
			On       = 1,
			Cycle    = 2,
			Disabled = 3
		}

		private enum ScreenSaverKind {
			Blanked  = 0,
			Internal = 1,
			External = 2
		}

		[DllImport ("libscreensaverglue.so")]
		extern static unsafe int screensaver_info (ScreenSaverState *state,
							   ScreenSaverKind *kind,
							   ulong *til_or_since,
							   ulong *idle);

		static private void CheckScreenSaver ()
		{
			if ((DateTime.Now - screensaver_time).TotalSeconds < screensaver_poll_delay)
				return;

			ScreenSaverState state;
			ScreenSaverKind kind;
			ulong til_or_since = 0, idle = 0;
			int retval;

			unsafe {
				retval = screensaver_info (&state, &kind, &til_or_since, &idle);
			}

			if (retval != 0) {
				cached_screensaver_running = (state == ScreenSaverState.On);
				cached_screensaver_idle_time = idle / 1000.0;
			} else {
				cached_screensaver_running = false;
				cached_screensaver_idle_time = 0;
			}

			screensaver_time = DateTime.Now;
		}

		static public bool ScreenSaverRunning {
			get {
				CheckScreenSaver ();
				return cached_screensaver_running;
			}
		}

		// returns number of seconds since input was received
		// from the user on any input device
		static public double InputIdleTime {
			get {
				CheckScreenSaver ();
				return cached_screensaver_idle_time;
			}
		}

		///////////////////////////////////////////////////////////////

		const double acpi_poll_delay = 5;
		const string proc_ac_state_filename = "/proc/acpi/ac_adapter/AC/state";
		const string ac_present_string = "on-line";
		static bool proc_ac_state_exists = true;
		static DateTime using_battery_time = DateTime.MinValue;
		static bool using_battery;

		static public void CheckAcpi ()
		{
			if (! proc_ac_state_exists)
				return;

			if ((DateTime.Now - using_battery_time).TotalSeconds < acpi_poll_delay)
				return;

			if (! File.Exists (proc_ac_state_filename)) {
				proc_ac_state_exists = false;
				using_battery = false;
				return;
			}

			Stream stream = new FileStream (proc_ac_state_filename,
							FileMode.Open,
							FileAccess.Read,
							FileShare.Read);
			TextReader reader = new StreamReader (stream);

			string line = reader.ReadLine ();
			using_battery = (line != null) && (line.IndexOf (ac_present_string) == -1);
			
			reader.Close ();
			stream.Close ();

			using_battery_time = DateTime.Now;
		}

		static public bool UsingBattery {
			get { 
				CheckAcpi ();
				return using_battery;
			}
		}
			

#if false
		static void Main ()
		{
			Gtk.Application.Init ();
			while (true) {
				Console.WriteLine ("{0} {1} {2} {3} {4} {5}",
						   LoadAverageOneMinute,
						   LoadAverageFiveMinute,
						   LoadAverageFifteenMinute,
						   ScreenSaverRunning,
						   InputIdleTime,
						   UsingBattery);
				System.Threading.Thread.Sleep (1000);
			}
		}
#endif

	}

}
