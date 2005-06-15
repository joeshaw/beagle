//
// Query.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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
	static bool flood = false;

	static void OnHitsAdded (HitsAddedResponse response)
	{
		lastQueryTime = DateTime.Now;

		if (count == 0 && verbose) {
			Console.WriteLine ("First hit returned in {0:0.000}s",
					   (lastQueryTime - queryStartTime).TotalSeconds);
		}

		if (! display_hits) {
			count += response.Hits.Count;
			return;
		}

		foreach (Hit hit in response.Hits) {
			if (verbose)
				Console.WriteLine ("  Uri: {0}", hit.Uri);
			else
				Console.WriteLine (hit.Uri);

			if (verbose) {
				SnippetRequest sreq = new SnippetRequest (query.Text, hit);
				SnippetResponse sresp = (SnippetResponse) sreq.Send ();
				Console.WriteLine ("PaUri: {0}", hit.ParentUri != null ? hit.ParentUri.ToString () : "(null)");
				Console.WriteLine (" Snip: {0}", sresp.Snippet != null ? sresp.Snippet : "(null)");
				Console.WriteLine (" Type: {0}", hit.Type);
				Console.WriteLine ("MimeT: {0}", hit.MimeType == null ? "(null)" : hit.MimeType);
				Console.WriteLine ("  Src: {0}", hit.Source);
				Console.WriteLine ("Score: {0}", hit.Score);
				if (hit.ValidTimestamp)
					Console.WriteLine (" Time: {0}", hit.Timestamp);
				if (hit.ValidRevision)
					Console.WriteLine ("  Rev: {0}", hit.Revision);
				
				foreach (Property prop in hit.Properties)
					Console.WriteLine ("    {0} = {1}", prop.Key, prop.Value);
				
				Console.WriteLine ();
			}

			++count;
		}
	}

	static void OnHitsSubtracted (HitsSubtractedResponse response)
	{
		lastQueryTime = DateTime.Now;

		if (! display_hits)
			return;

		foreach (Uri uri in response.Uris) {
			Console.WriteLine ("Subtracted Uri '{0}'", uri);
			Console.WriteLine ();

			--count;
		}
	}

	static void OnFinished (FinishedResponse response)
	{
		if (verbose) {
			Console.WriteLine ("Elapsed time: {0:0.000}s",
					   (DateTime.Now - queryStartTime).TotalSeconds);
			Console.WriteLine ("Total hits: {0}", count);
		}

		if (flood)
			SendQuery ();
		else
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
			"  --max-hits\t\t\tLimit number of search results per backend\n" +
			"            \t\t\t(default = 100, max = 100)\n" +
			"  --flood\t\t\tExecute the query over and over again.  Don't do that.\n" +
			"  --help\t\t\tPrint this usage message.\n";

		Console.WriteLine (usage);

		System.Environment.Exit (0);
	}

	static void OnClosed ()
	{
		if (flood)
			SendQuery ();
		else
			Gtk.Application.Quit ();
	}

	static int query_counter = 0;
	static void SendQuery ()
	{
		++query_counter;
		if (flood) {
			if (query_counter > 1)
				Console.WriteLine ();
			Console.WriteLine ("Sending query #{0}", query_counter);
		}

		queryStartTime = DateTime.Now;
		try {
			query.SendAsync ();
		} catch (System.Net.Sockets.SocketException e) {
			Console.WriteLine ("Could not connect to the Beagle daemon.  The daemon probably isn't running.");
			System.Environment.Exit (-1);
		}
	}
	
	static void Main (string[] args) 
	{
		Gtk.Application.InitCheck ("beagle-query", ref args);

		if (args.Length == 0 || Array.IndexOf (args, "--help") > -1 || Array.IndexOf (args, "--usage") > -1)
			PrintUsageAndExit ();

		query = new Query ();

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
			case "--max-hits":
			    if (++i >= args.Length) PrintUsageAndExit ();
				query.MaxHits = Int32.Parse (args[i]);
				break;
			case "--flood":
				flood = true;
				break;

			default:
				int j = args [i].IndexOf ('=');
				if (j == -1) {
					query.AddText (args [i]);
				} else {
					QueryPart part = new QueryPart ();
					part.Target = args [i].Substring (0, j);

					// This is very obscure notation.
					if (args [i] [j+1] == '~') {
						part.Text = args [i].Substring (j+2);
						part.IsKeyword = false;
					} else {
						part.Text = args [i].Substring (j+1);
						part.IsKeyword = true;
					}

					Console.WriteLine ("*** '{0}' '{1}' '{2}'",
							   part.Target, part.Text, part.IsKeyword);

					query.AddPart (part);
				}

				break;
			}

			++i;
		}

		query.HitsAddedEvent += OnHitsAdded;
		query.HitsSubtractedEvent += OnHitsSubtracted;


		if (! keepRunning)
			query.FinishedEvent += OnFinished;
		else
			query.ClosedEvent += OnClosed;

		SendQuery ();

		Gtk.Application.Run ();
	}


}
	
