//
// SystemInformation.cs
//
// Copyright (C) 2004-2006 Novell, Inc.
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
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Beagle.Util {

	public class SystemInformation {

		[DllImport ("libc", SetLastError=true)]
		static extern int getloadavg (double[] loadavg, int nelem);

		const double loadavg_poll_delay = 3;
		static DateTime proc_loadavg_time  = DateTime.MinValue;
		static double cached_loadavg_1min  = -1;
		static double cached_loadavg_5min  = -1;
		static double cached_loadavg_15min = -1;

		static private void CheckLoadAverage ()
		{
			// Only call getloadavg() at most once every 10 seconds
			if ((DateTime.Now - proc_loadavg_time).TotalSeconds < loadavg_poll_delay)
				return;

			double [] loadavg = new double [3];
			int retval = getloadavg (loadavg, 3);

			if (retval == -1)
				throw new IOException ("Could not get system load average: " + Mono.Unix.Native.Stdlib.strerror (Mono.Unix.Native.Stdlib.GetLastError ()));
			else if (retval != 3)
				throw new IOException ("Could not get system load average: getloadavg() returned an unexpected number of samples");

			cached_loadavg_1min  = loadavg [0];
			cached_loadavg_5min  = loadavg [1];
			cached_loadavg_15min = loadavg [2];

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

		[DllImport ("libbeagleglue.so")]
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

		const double acpi_poll_delay = 30;
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
							System.IO.FileMode.Open,
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

		///////////////////////////////////////////////////////////////

		[DllImport ("libbeagleglue")]
		extern static int get_vmsize ();

		[DllImport ("libbeagleglue")]
		extern static int get_vmrss ();

		static public int VmSize {
			get { return get_vmsize (); }
		}

		static public int VmRss {
			get { return get_vmrss (); }
		}

		///////////////////////////////////////////////////////////////

		static private int disk_stats_read_reqs;
		static private int disk_stats_write_reqs;
		static private int disk_stats_read_bytes;
		static private int disk_stats_write_bytes;

		static private DateTime disk_stats_time = DateTime.MinValue;
		static private double disk_stats_delay = 1.0;

		static private uint major, minor;

		// Update the disk statistics with data for block device on the (major,minor) pair.
		static private void UpdateDiskStats ()
		{
			string buffer;

			if (major == 0)
				return;

			// We only refresh the stats once per second
			if ((DateTime.Now - disk_stats_time).TotalSeconds < disk_stats_delay)
				return;

			// Read in all of the disk stats
			using (StreamReader stream = new StreamReader ("/proc/diskstats"))
				buffer = stream.ReadToEnd ();

			// Find our partition and parse it
			const string REGEX = "[\\s]+{0}[\\s]+{1}[\\s]+[a-zA-Z0-9]+[\\s]+([0-9]+)[\\s]+([0-9]+)[\\s]+([0-9]+)[\\s]+([0-9]+)";
			string regex = String.Format (REGEX, major, minor);
			Regex r = new Regex (regex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			for (System.Text.RegularExpressions.Match m = r.Match (buffer); m.Success; m = m.NextMatch ()) {
				disk_stats_read_reqs = Convert.ToInt32 (m.Groups[1].ToString ());
				disk_stats_read_bytes = Convert.ToInt32 (m.Groups[2].ToString ());
				disk_stats_write_reqs = Convert.ToInt32 (m.Groups[3].ToString ());
				disk_stats_write_bytes = Convert.ToInt32 (m.Groups[4].ToString ());
			}

			disk_stats_time = DateTime.Now;
		}

		// Get the (major,minor) pair for the block device from which the index is mounted.
		static private void GetIndexDev ()
		{
			Mono.Unix.Native.Stat stat;
			if (Mono.Unix.Native.Syscall.stat (PathFinder.StorageDir, out stat) != 0)
				return;

			major = (uint) stat.st_dev >> 8;
			minor = (uint) stat.st_dev & 0xff;
		}

		static public int DiskStatsReadReqs {
			get {
				if (major == 0)
					 GetIndexDev ();
				UpdateDiskStats ();
				return disk_stats_read_reqs;
			}
		}

		static public int DiskStatsReadBytes {
			get {
				if (major == 0)
					 GetIndexDev ();
				UpdateDiskStats ();
				return disk_stats_read_bytes;
			}
		}

		static public int DiskStatsWriteReqs {
			get {
				if (major == 0)
					 GetIndexDev ();
				UpdateDiskStats ();
				return disk_stats_write_reqs;
			}
		}

		static public int DiskStatsWriteBytes {
			get {
				if (major == 0)
					 GetIndexDev ();
				UpdateDiskStats ();
				return disk_stats_write_bytes;
			}
		}

		static public bool IsPathOnBlockDevice (string path)
		{
			Mono.Unix.Native.Stat stat;
			if (Mono.Unix.Native.Syscall.stat (path, out stat) != 0)
				return false;
			
			return (stat.st_dev >> 8 != 0);
		}

		///////////////////////////////////////////////////////////////

		[DllImport("libc")]
		private static extern int prctl (int option, byte [] arg2, ulong arg3, ulong arg4, ulong arg5);

		// From /usr/include/linux/prctl.h
		private const int PR_SET_NAME = 15;

		public static void SetProcessName(string name)
		{
#if OS_LINUX
			if (prctl (PR_SET_NAME, Encoding.ASCII.GetBytes (name + '\0'), 0, 0, 0) < 0) {
				Logger.Log.Warn ("Couldn't set process name to '{0}': {1}", name,
						 Mono.Unix.Native.Stdlib.GetLastError ());
			}
#endif
		}

		///////////////////////////////////////////////////////////////

#if false
		static void Main ()
		{
			Gtk.Application.Init ();
			while (true) {
				Console.WriteLine ("{0} {1} {2} {3} {4} {5} {6} {7}",
						   LoadAverageOneMinute,
						   LoadAverageFiveMinute,
						   LoadAverageFifteenMinute,
						   ScreenSaverRunning,
						   InputIdleTime,
						   UsingBattery,
						   DiskStatsReadReqs,
						   VmSize);
				System.Threading.Thread.Sleep (1000);
			}
		}
#endif
	}
}
