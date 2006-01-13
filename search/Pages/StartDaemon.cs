using System;
using System.Diagnostics;
using Gtk;

namespace Search.Pages {

	public delegate void DaemonStarted ();

	public class StartDaemon : Base {

		public DaemonStarted DaemonStarted;

		public StartDaemon ()
		{
			HeaderIconStock = Stock.DialogError;
			HeaderMarkup = "<big><b>Daemon not running</b></big>";

			Gtk.Button button = new Gtk.Button ("Start the daemon");
			button.Clicked += OnStartDaemon;
			button.Show ();
			Append (button);
		}

		private void OnStartDaemon (object o, EventArgs args)
		{
			// If we're running uninstalled (in the source tree), then run
			// the uninstalled daemon.  Otherwise run the installed daemon.
			
			// FIXME: Uncomment the stuff below when we move to the beagle tree.
			//string bestpath = System.Environment.GetCommandLineArgs () [0];
			//string bestdir = System.IO.Path.GetDirectoryName (bestpath);
			string beagled_filename = "beagled";

			//if (bestdir.EndsWith ("lib/search") || bestdir.EndsWith ("lib64/search")) {
			//	Console.WriteLine ("Running installed daemon...");
			//	beagled_filename = "beagled"; // Running installed
			//} else {
			//	Console.WriteLine ("Running uninstalled daemon...");
			//	beagled_filename = System.IO.Path.Combine (bestdir, "../beagled/beagled");
			//}
				
			
			Process daemon = new Process ();
			daemon.StartInfo.FileName  = beagled_filename;
			daemon.StartInfo.UseShellExecute = false;

			try {
				daemon.Start ();
			} catch (System.ComponentModel.Win32Exception e) {
				Console.WriteLine ("Unable to start daemon: {0}", e.Message);
			}
			
			if (DaemonStarted != null)
				DaemonStarted ();
		}
	}
}
