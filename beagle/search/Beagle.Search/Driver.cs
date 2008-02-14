//
// Driver.cs
//
// Copyright (C) 2008 Lukas Lipka <lukaslipka@gmail.com>
//

using System;

using NDesk.DBus;
using Mono.Unix;

using Beagle;
using Beagle.Util;

namespace Beagle.Search {

	public class Driver {

		private static string ParseArgs (String[] args)
		{
			string query = String.Empty;
			int i = 0;

			while (i < args.Length) {
				switch (args [i]) {
				case "--help":
				case "--usage":
					PrintUsageAndExit ();
					return null;

				case "--version":
					VersionFu.PrintVersion ();
					Environment.Exit (0);
					break;

					//case "--icon":
					//IconEnabled = true;
					//break;

					//case "--search-docs":
					//search_docs = true;
					//break;

				// Ignore session management
				case "--sm-config-prefix":
				case "--sm-client-id":
				case "--screen":
					// These all take an argument, so
					// increment i
					i++;
					break;

				default:
					if (args [i].Length < 2 || args [i].Substring (0, 2) != "--") {
						if (query.Length != 0)
							query += " ";
						query += args [i];
					}
					break;
				}

				i++;
			}

			return query;
		}

		public static void PrintUsageAndExit ()
		{
			VersionFu.PrintHeader ();

			string usage =
				"Usage: beagle-search [OPTIONS] [<query string>]\n\n" +
				"Options:\n" +
				"  --icon\t\t\tAdd an icon to the notification area rather than opening a search window.\n" +
				"  --search-docs\t\t\tAlso search the system-wide documentation index.\n" +
				"  --help\t\t\tPrint this usage message.\n" +
				"  --version\t\t\tPrint version information.\n";

			Console.WriteLine (usage);
			System.Environment.Exit (0);
		}

		public static void Main (string[] args)
		{
			// Set our process name

			SystemInformation.SetProcessName ("beagle-search");

			// Initialize our translations catalog
			
			Catalog.Init ("beagle", ExternalStringsHack.LocaleDir);

			// Set up DBus for our GLib main loop
			
			BusG.Init ();

			// Parse arguments

			string query = ParseArgs (args);

			// Init Gnome program

			Gnome.Program program = new Gnome.Program ("search", "0.0", Gnome.Modules.UI, args);

			Search window = new Search (query);

			//if (query != null && query != "" && !IconEnabled) {
			//	window.entry.Text = query;
			//	window.Search (true);
			//}

			program.Run ();
		}
	}
}
