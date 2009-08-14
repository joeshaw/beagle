using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Mono.Unix;
using Mono.Unix.Native;

using Gtk;

namespace Beagle.Search.Pages {

	public delegate void DaemonStarted ();

	public class StartDaemon : Base {

		public event DaemonStarted DaemonStarted;
		private Gtk.ToggleButton autostart_toggle;

		public StartDaemon ()
		{
			HeaderIconFromStock = Stock.DialogError;
			Header = Catalog.GetString ("Search service not running");

			Append (Catalog.GetString ("The search service does not appear to be running. " +
						   "You can start it by clicking the button below."));

			Gtk.Button button = new Gtk.Button (Catalog.GetString ("Start search service"));
			button.Clicked += OnStartDaemon;
			button.Show ();

			Append (button);

			autostart_toggle = new Gtk.CheckButton (Catalog.GetString ("Automatically start service on login"));
			autostart_toggle.Active = true;
			autostart_toggle.Show ();

			Append (autostart_toggle);
		}

		private void OnStartDaemon (object o, EventArgs args)
		{
			if (autostart_toggle.Active)
				EnableAutostart ();

			DoStartDaemon (DaemonStarted);
		}

		private void EnableAutostart ()
		{
			string local_autostart_dir = System.IO.Path.Combine (System.IO.Path.Combine (Environment.GetEnvironmentVariable ("HOME"), ".config"), "autostart");

			if (! Directory.Exists (local_autostart_dir)) {
				Directory.CreateDirectory (local_autostart_dir);
				Syscall.chmod (local_autostart_dir, (FilePermissions) 448); // 448 == 0700
			}

			string beagled_file = System.IO.Path.Combine (local_autostart_dir, "beagled-autostart.desktop");

			Assembly assembly = Assembly.GetExecutingAssembly ();

			StreamReader reader = new StreamReader (assembly.GetManifestResourceStream ("beagled-autostart.desktop"));
			StreamWriter writer = new StreamWriter (beagled_file);

			string l;
			while ((l = reader.ReadLine ()) != null)
				writer.WriteLine (l);
			reader.Close ();
			writer.Close ();
		}

		internal static void DoStartDaemon (DaemonStarted DaemonStarted)
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
				GLib.Timeout.Add (5000, delegate () {
								if (DaemonStarted != null)
									DaemonStarted ();
								return false;
							});
		}
	}
}
