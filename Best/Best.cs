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

using Mono.Posix;

using Beagle;
using Beagle.Util;

namespace Best {
	
	class Best {

		static public string DefaultWindowTitle = "Beagle Search (alpha)";

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

				default:
					if (query.Length != 0)
						query += " ";
					query += args [i];
					break;
				}

				i ++;
			}
		}

		static void NoTrayWindowDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			Application.Quit ();
		}

		static void Main (String[] args)
		{
			ParseArgs (args);

			Program best = new Program ("best", "0.0", Modules.UI, args);

			GeckoUtils.Init ();
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
				icon = new BestTray (win);
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
