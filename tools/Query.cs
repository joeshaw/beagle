//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Collections;
using System.Threading;

using Gtk;

using Beagle;
using BU = Beagle.Util;

class QueryTool {

	static int count = 0;
	static Query query = null;
	static DateTime queryStartTime;
	static DateTime lastQueryTime = DateTime.Now;

	// CLI args
	static bool keepRunning = false;
	static bool verbose = false;
	static bool display_hits = true;

	static void OnHitsAdded (Query source, ICollection hits)
	{
		lastQueryTime = DateTime.Now;

		if (count == 0 && verbose) {
			Console.WriteLine ("First hit returned in {0:0.000}s",
					   (lastQueryTime - queryStartTime).TotalSeconds);
		}

		if (! display_hits) {
			count += hits.Count;
			return;
		}

		foreach (Hit hit in hits) {
			if (verbose)
				Console.WriteLine ("  Uri: {0}", hit.Uri);
			else
				Console.WriteLine (hit.Uri);

			if (verbose) {
				string snippet = query.GetSnippet (hit.Uri);
				Console.WriteLine (" Snip: {0}", snippet != null ? snippet : "(null)");
				Console.WriteLine (" Type: {0}", hit.Type);
				Console.WriteLine ("MimeT: {0}", hit.MimeType == null ? "(null)" : hit.MimeType);
				Console.WriteLine ("  Src: {0}", hit.Source);
				Console.WriteLine ("Score: {0}", hit.Score);
				if (hit.ValidTimestamp)
					Console.WriteLine (" Time: {0}", hit.Timestamp);
				if (hit.ValidRevision)
					Console.WriteLine ("  Rev: {0}", hit.Revision);
				
				foreach (String key in hit.Keys)
					Console.WriteLine ("    {0} = {1}", key, hit [key]);
				
				Console.WriteLine ();
			}

			++count;
		}
	}

	static void OnHitsSubtracted (Query source, ICollection uris)
	{
		lastQueryTime = DateTime.Now;

		if (! display_hits)
			return;

		foreach (Uri uri in uris) {
			Console.WriteLine ("Subtracted Uri '{0}'", uri);
			Console.WriteLine ();

			--count;
		}
	}

	static void OnFinished (QueryProxy query)
	{
		if (verbose) {
			Console.WriteLine ("Elapsed time: {0:0.000}s",
					   (DateTime.Now - queryStartTime).TotalSeconds);
			Console.WriteLine ("Total hits: {0}", count);
		}

		Gtk.Application.Quit ();
	}

	public static void PrintUsageAndExit () 
	{
		string usage =
			"beagle-query: Command-line interface to the Beagle search system.\n" +
			"Web page: http://www.gnome.org/projects/beagle\n" +
			"Copyright (C) 2004 Novell, Inc.\n\n";
		usage +=
			"Usage: beagle-query [OPTIONS] <query string>\n\n" +
			"Options:\n" +
			"  --verbose\t\t\tPrint detailed information about each hit.\n" +
			"  --mime <mime type>\t\tConstrain search results to the specified mime\n" +
			"                    \t\ttype. Can be used multiply.\n" +
			"  --type <hit type>\t\tConstrain search results to the specified hit\n" +
			"                    \t\ttype. Can be used multiply.\n" +
			"  --source <source>\t\tConstrain query to the specified source.\n" +
			"                   \t\tSources list available from beagle-status.\n" +
			"  --live-query\t\t\tRun continuously, printing notifications if a\n" +
			"              \t\t\tquery changes.\n" +
			"  --stats-only\t\t\tOnly display statistics about the query, not\n" +
			"              \t\t\tthe actual results.\n" +
			"  --help\t\t\tPrint this usage message.\n";

		Console.WriteLine (usage);

		System.Environment.Exit (0);
	}

	
	static void Main (string[] args) 
	{
		Gtk.Application.Init ();

		try {
			query = Beagle.Factory.NewQuery ();
		} catch (Exception e) {
			if (e.ToString ().IndexOf ("com.novell.Beagle") != -1) {
				Console.WriteLine ("Could not query.  The Beagle daemon is probably not running, or maybe you\ndon't have D-BUS set up properly.");
				System.Environment.Exit (-1);
			} else {
				Console.WriteLine ("The query failed with error:\n\n" + e);
				System.Environment.Exit (-1);
			}
		}

		query.HitsAddedEvent += OnHitsAdded;
		query.HitsSubtractedEvent += OnHitsSubtracted;

		// Parse args
		int i = 0;
		while (i < args.Length) {
			switch (args [i]) {

			case "--mime":
			        if (++i >= args.Length) PrintUsageAndExit ();
				query.AddMimeType (args [i]);
				break;
			case "--type":
			        if (++i >= args.Length) PrintUsageAndExit ();
				query.AddHitType (args [i]);
				break;
			case "--source":
			        if (++i >= args.Length) PrintUsageAndExit ();
				query.AddSource (args [i]);
				break;
			case "--live-query":
				keepRunning = true;
				break;
			case "--verbose":
				verbose = true;
				break;
			case "--stats-only":
				verbose = true;
				display_hits = false;
				break;

			case "--help":
			case "--usage":
				PrintUsageAndExit ();
				return;

			default:
				query.AddTextRaw (args [i]);
				break;
			}

			++i;
		}

		if (! keepRunning)
			query.FinishedEvent += OnFinished;

		queryStartTime = DateTime.Now;
		query.Start ();

		Gtk.Application.Run ();
	}


}
