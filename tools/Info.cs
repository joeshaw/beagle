//
// Info.cs
//
// Copyright (C) 2005 Novell, Inc.
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

using Beagle;

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
			"  --help\t\t\tPrint this usage message.\n";

		Console.WriteLine (usage);

		System.Environment.Exit (0);
	}

	static int Main (string[] args)
	{
		if (args.Length == 0 || Array.IndexOf (args, "--help") > -1)
			PrintUsageAndExit ();

		DaemonInformationRequest request = new DaemonInformationRequest ();
		DaemonInformationResponse response;

		try {
			response = (DaemonInformationResponse) request.Send ();
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

		return 0;
	}
}
		
		
