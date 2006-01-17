using System;
using System.Diagnostics;
using Mono.Posix;
using Gtk;

namespace Search.Pages {

	public delegate void DaemonStarted ();

	public class StartDaemon : Base {

		public DaemonStarted DaemonStarted;

		private Gtk.Button button;

		public StartDaemon ()
		{
			HeaderIconStock = Stock.DialogError;
			HeaderMarkup = "<big><b>" + Catalog.GetString ("Daemon not running") + "</b></big>";

			button = new Gtk.Button (Catalog.GetString ("Start the daemon"));
			button.Clicked += OnStartDaemon;
			button.Show ();
			Append (button);
		}

		private void OnStartDaemon (object o, EventArgs args)
		{
			string beagled_filename = "beagled";

			Process daemon = new Process ();
			daemon.StartInfo.FileName  = beagled_filename;
			daemon.StartInfo.UseShellExecute = false;

			try {
				daemon.Start ();
			} catch (System.ComponentModel.Win32Exception e) {
				Console.WriteLine ("Unable to start daemon: {0}", e.Message);
			}
			
			// Give the daemon some time to start
			if (DaemonStarted != null)
				GLib.Timeout.Add (5000, DaemonStartedTimeout);
		}

		private bool DaemonStartedTimeout ()
		{
			DaemonStarted ();
			return false;
		}
	}
}
