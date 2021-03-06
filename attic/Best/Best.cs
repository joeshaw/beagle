//
// Best.cs
// Bleeding Edge Search Tool: Beagle search GUI
//
// Nat Friedman <nat@novell.com>
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

using Gtk;
using GtkSharp;
using Gnome;

using Mono.Unix;

using Beagle;
using Beagle.Util;

namespace Best {
	
	class Best {

		static public string DefaultWindowTitle = "Beagle Search (beta)";

		static void PrintUsageAndExit ()
		{
			string usage =
				"best: GUI interface to the Beagle search system.\n" +
				"Web page: http://www.gnome.org/projects/beagle\n" +
				"Copyright (C) 2004-2005 Novell, Inc.\n\n";

			usage +=
				"Usage: best [OPTIONS] [<query string>]\n\n" +
				"Options:\n" +
				"  --no-tray\t\t\tDo not create a notification area applet.\n" +
				"  --show-window\t\t\tDisplay a search window.\n" +
				"  --help\t\t\tPrint this usage message.\n";

			Console.WriteLine (usage);

			System.Environment.Exit (0);
		}

		static string query = "";
		static bool doTray = true;
		static bool showWindow = false;
		static bool autostarted = false;

		static void ParseArgs (String[] args)
		{
			int i = 0;
			while (i < args.Length) {
				switch (args [i]) {
				case "--no-tray":
					doTray = false;
					break;

				case "--show-window":
					showWindow = true;
					break;

				case "--help":
				case "--usage":
					PrintUsageAndExit ();
					return;

				// Ignore session management
				case "--sm-config-prefix":
				case "--sm-client-id":
				case "--screen":
					// These all take an argument, so
					// increment i
					i++;
					break;

				case "--autostarted":
					if (! Conf.Searching.Autostart){
						Console.WriteLine ("Autostarting is disabled, not starting");
                                                Environment.Exit (0);
					}
					autostarted = true;
					break;

				default:
					if (args [i].Length < 2 || args [i].Substring (0, 2) != "--") {
						if (query.Length != 0)
							query += " ";
						query += args [i];
					}
					break;
				}

				i ++;
			}
		}

		static void NoTrayWindowDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			BestWindow win = (BestWindow) o;
			win.StoreSettingsInConf (false);
			Application.Quit ();
		}

		static void Main (String[] args)
		{
			ParseArgs (args);

			Program best = new Program ("best", "0.0", Modules.UI, args);

			try {
				GeckoUtils.Init ();
			} catch (DllNotFoundException) {
				// We might get this exception if there are
				// missing symbols from the Mozilla runtime if
				// the user did a Firefox 1.0 -> 1.5 upgrade.
				// There's nothing we can do about this, it's
				// an ABI change, so tell the user this is
				// probably what's wrong.
				Console.WriteLine (Catalog.GetString ("Best cannot initialize Mozilla's Gecko runtime environment.\n" +
								      "Have you upgraded Mozilla or Firefox recently?  If so, you\n" +
								      "probably need to rebuild beagle against this new version.\n" +
								      "See http://bugzilla.gnome.org/show_bug.cgi?id=326503 for\n" + 
								      "more information."));
				Environment.Exit (1);
			}

			GeckoUtils.SetSystemFonts ();

			// I18N
			Catalog.Init ("beagle", ExternalStringsHack.LocaleDir);

			// Create the window.
			BestWindow win;
			if (query != "") {
				win = new BestWindow (query);
				win.Show ();
			} else {
				win = new BestWindow ();
				if (showWindow)
					win.Show ();
			}

			// Create the tray icon.
			BestTray icon;			
			if (doTray) {
				icon = new BestTray (win, autostarted);
				icon.Show ();
				Console.WriteLine (Catalog.GetString ("If you're wondering whether Best is working check " +
						   "your notification area (system tray)"));
			} else {
				win.Show ();
				win.Present ();
				win.FocusEntry ();
				win.DeleteEvent += new Gtk.DeleteEventHandler (NoTrayWindowDeleteEvent);
			}

			best.Run ();
		}
	}
}
