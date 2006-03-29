//
// Info.cs
//
// Copyright (C) 2005 Novell, Inc.
// Copyright (C) 2006 Debajyoti Bera <dbera.web@gmail.com>
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Reflection;

using Beagle;
using Beagle.Daemon;
using Beagle.Util;

class InfoTool {

	public static void PrintUsageAndExit () 
	{
		string usage =
			"beagle-info: Statistics from the Beagle daemon.\n" +
			"Web page: http://www.gnome.org/projects/beagle\n" +
			"Copyright (C) 2004-2005 Novell, Inc.\n\n";
		usage +=
			"Usage: beagle-info <OPTIONS>\n\n" +
			"Options:\n" +
			"  --daemon-version\t\tPrint the version of the running daemon.\n" +
			"  --status\t\t\tDisplay status of the running daemon.\n" +
			"  --index-info\t\t\tDisplay statistics of the Beagle indexes.\n" +
			"  --list-filters\t\tList the currently available filters.\n" +
			"  --help\t\t\tPrint this usage message.\n";

		Console.WriteLine (usage);

		System.Environment.Exit (0);
	}

	static int Main (string[] args)
	{
		if (args.Length == 0 || Array.IndexOf (args, "--help") > -1)
			PrintUsageAndExit ();

		if (Array.IndexOf (args, "--list-filters") > -1)
			PrintFilterInformation ();
		else
			return PrintDaemonInformation (args);

		return 0;
	}
	
	private static int PrintDaemonInformation (string[] args)
	{
		DaemonInformationRequest request = new DaemonInformationRequest ();
		DaemonInformationResponse response;

		try {
			Console.WriteLine ("Sending begins at:" + DateTime.Now);
			response = (DaemonInformationResponse) request.Send ();
			Console.WriteLine ("Sending done at:" + DateTime.Now);
		} catch (Beagle.ResponseMessageException) {
			Console.WriteLine ("Could not connect to the daemon.");
			return 1;
		}

		if (Array.IndexOf (args, "--daemon-version") > -1)
			Console.WriteLine ("Daemon version: {0}", response.Version);

		if (Array.IndexOf (args, "--status") > -1)
			Console.Write (response.HumanReadableStatus);

		if (Array.IndexOf (args, "--index-info") > -1) {
			Console.WriteLine ("Index information:");
			Console.WriteLine (response.IndexInformation);
		}

		if (Array.IndexOf (args, "--is-indexing") > -1)
			Console.WriteLine ("Daemon indexing: {0}", response.IsIndexing);

		return 0;
	}
	
	private static void PrintFilterInformation ()
	{
		ReflectionFu.ScanEnvironmentForAssemblies ("BEAGLE_FILTER_PATH", PathFinder.FilterDir, PrintFilterDetails);
	}

	static void PrintFilterDetails (Assembly assembly)
	{
		foreach (Type t in ReflectionFu.ScanAssemblyForClass (assembly, typeof (Filter))) {
			Filter filter = null;

			try {
				filter = (Filter) Activator.CreateInstance (t);
			} catch (Exception ex) {
				Logger.Log.Error ("Caught exception while instantiating {0}", t);
				Logger.Log.Error (ex);
			}

			if (filter == null)
				continue;

			Console.WriteLine (t.ToString () + " Version-" + filter.Version + " (" + assembly.Location + ")");

			foreach (FilterFlavor flavor in filter.SupportedFlavors) {
				if (flavor.MimeType != null)
					Console.WriteLine ("\t- " + flavor.MimeType);
				if (flavor.Extension != null)
					Console.WriteLine ("\t- *" + flavor.Extension);
			}
		}
	}
}
		
		
