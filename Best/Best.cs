//
// Best.c
//
// Nat Friedman <nat@novell.com>
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

using Gtk;
using GtkSharp;
using Gnome;

using Beagle;
using Beagle.Util;

namespace Best {
	
	class Best {

		static void PrintUsageAndExit ()
		{
			string usage =
				"best: GUI interface to the Beagle search system.\n" +
				"Web page: http://www.gnome.org/projects/beagle\n" +
				"Copyright (C) 2004 Novell, Inc.\n\n";

			usage +=
				"Usage: best [OPTIONS] [<query string>]\n\n" +
				"Options:\n" +
				"  --no-tray\t\t\tDo not create a notification area applet.\n" +
				"  --help\t\t\tPrint this usage message.\n";

			Console.WriteLine (usage);

			System.Environment.Exit (0);
		}

		static string query = "";
		static bool doTray = true;

		static void ParseArgs (String[] args)
		{
			int i = 0;
			while (i < args.Length) {
				switch (args [i]) {
				case "--no-tray":
					doTray = false;
					break;

				case "--help":
				case "--usage":
					PrintUsageAndExit ();
					return;
				default:
					query += args [i];
					break;
				}

				i ++;
			}
		}

		static void Main (String[] args)
		{
			Program best = new Program ("best", "0.0", Modules.UI, args);

			ParseArgs (args);

			GeckoUtils.Init ();
			GeckoUtils.SetSystemFonts ();

			// Create the window.
			BestWindow win;
			if (query != "")
				win = new BestWindow (query);
			else
				win = new BestWindow ();

			// Create the tray icon.
			if (doTray) {
				BestTray icon;
				icon = new BestTray (win);
			}

			best.Run ();
		}
	}
}
