//
// ImLogViewer.cs
//
// Lukas Lipka <lukas@pmad.net>
// Raphael  Slinckx <rslinckx@gmail.com>
//
// Copyright (C) 2005 Novell, Inc.
//

using System;
using Mono.Posix;

namespace ImLogViewer {

	public class ImLogViewer {
		
		private static string highlight;
		private static string search;
		private static string path;

		public static void Main (string[] args)
		{
			// I18N
			Catalog.Init ("beagle", Beagle.Util.ExternalStringsHack.LocaleDir);

			ParseArgs (args);
			ImLogWindow window = new ImLogWindow (path, search, highlight);
		}

		private static void PrintUsageAndExit ()
		{
			Console.WriteLine ("USAGE: beagle-imlogviewer [OPTIONS] <log file or directory>\n" +
					   "Options:\n" +
					   "  --highlight-search\t\tWords to highlight in the buffer.\n" +
					   "  --search\t\t\tSearch query to filter hits on.");

			Environment.Exit (0);
		}

		private static void ParseArgs (string [] args)
		{
			if (args.Length < 1)
				PrintUsageAndExit ();
			
			for (int i = 0; i < args.Length; i++) {
				switch (args [i]) {
				case "-h":
				case "--help":
					PrintUsageAndExit ();
					break;

				case "--highlight-search":
					highlight = args [i + 1];
					i++;
					break;

				case "--search":
					search = args [i + 1];
					i++;
					break;

				default:
					if (args [i].StartsWith ("--")) {
						Console.WriteLine ("WARN: Invalid option {0}", args [i]);
					} else {
						path = args [i];
					}
					break;
				}
			}

			if (path == null) {
				Console.WriteLine ("ERROR: Please specify a valid log path or log directory.");
				Environment.Exit (1);
			}
		}
	}
}
