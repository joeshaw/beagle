//
// Query.cs
//
// Copyright (C) 2004-2006 Novell, Inc.
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
using System.IO;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using GLib;

using Beagle;
using Beagle.Util;
using Beagle.Daemon;

// Assembly information
[assembly: AssemblyTitle ("beagle-query")]
[assembly: AssemblyDescription ("Command-line interface to the Beagle search system")]

public class QueryTool {

	private static int count = 0;
	private static Query query = null;
	private static DateTime queryStartTime;
	private static DateTime lastQueryTime = DateTime.Now;

	private static MainLoop main_loop = null;

	// CLI args
	private static bool keep_running = false;
	private static bool verbose = false;
	private static bool display_hits = true;
	private static bool flood = false;
	private static bool listener = false;
	private static DateTime start_date = DateTime.MinValue;
	private static DateTime end_date = DateTime.MinValue;

	private static void OnHitsAdded (HitsAddedResponse response)
	{
		lastQueryTime = DateTime.Now;

		if (count == 0 && verbose) {
			Console.WriteLine ("First hit returned in {0:0.000}s",
					   (lastQueryTime - queryStartTime).TotalSeconds);
		}

		if (verbose && response.NumMatches >= 0)
			Console.WriteLine ("Returned latest {0} results out of total {1} matches", response.Hits.Count, response.NumMatches);

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
				SnippetRequest sreq = new SnippetRequest (query, hit);
				SnippetResponse sresp = (SnippetResponse) sreq.Send ();
				Console.WriteLine ("PaUri: {0}", hit.ParentUri != null ? hit.ParentUri.ToString () : "(null)");
				Console.WriteLine (" Snip: {0}", sresp.Snippet != null ? sresp.Snippet : "(null)");
				Console.WriteLine (" Type: {0}", hit.Type);
				Console.WriteLine ("MimeT: {0}", hit.MimeType == null ? "(null)" : hit.MimeType);
				Console.WriteLine ("  Src: {0}", hit.Source);
				Console.WriteLine ("Score: {0}", hit.Score);
				if (hit.ValidTimestamp)
					Console.WriteLine (" Time: {0}", DateTimeUtil.ToString (hit.Timestamp));
				
				foreach (Property prop in hit.Properties)
					Console.WriteLine ("    {0} = '{1}'",
						prop.Key,
						(prop.Type != PropertyType.Date ? prop.Value : DateTimeUtil.ToString (StringFu.StringToDateTime (prop.Value))));
				
				Console.WriteLine ();
			}

			++count;
		}
	}

	private static void OnHitsSubtracted (HitsSubtractedResponse response)
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

	private static void OnFinished (FinishedResponse response)
	{
		if (verbose) {
			Console.WriteLine ("Elapsed time: {0:0.000}s",
					   (DateTime.Now - queryStartTime).TotalSeconds);
			Console.WriteLine ("Total hits: {0}", count);
		}

		if (flood)
			SendQuery ();
		else
			main_loop.Quit ();
	}

	public static void PrintUsageAndExit () 
	{
		VersionFu.PrintHeader ();

		string usage =
			"Usage: beagle-query [OPTIONS] <query string>\n\n" +
			"Options:\n" +
			"  --verbose\t\t\tPrint detailed information about each hit.\n" +
			"  --mime <mime type>\t\t(DEPRECATED Use mimetype: property query.)\n" +
			"  --type <hit type>\t\t(DEPRECATED Use hittype: property query.)\n" +
			"  --source <source>\t\t(DEPRECATED Use source: property query.)\n" +
			"                   \t\tSources list available from beagle-info --status.\n" +
			"  --start <date>\t\t(DEPRECATED Use date range query syntax).\n" +
			"  --end <date>\t\t\t(DEPRECATED Use date range query syntax).\n" +
			"  --keywords\t\t\tLists the keywords allowed in 'query string'.\n" +
			"            \t\t\tKeyword queries can be specified as keywordname:value e.g. ext:jpg\n" +
			"  --live-query\t\t\tRun continuously, printing notifications if a\n" +
			"              \t\t\tquery changes.\n" +
			"  --stats-only\t\t\tOnly display statistics about the query, not\n" +
			"              \t\t\tthe actual results.\n" +
			"  --max-hits\t\t\tLimit number of search results per backend\n" +
			"            \t\t\t(default 100)\n" +
			"\n" +
			"  --local <yes|no>\t\tQuery local system (default yes)\n" +
			"  --network <yes|no>\t\tQuery other beagle systems in the neighbourhood domain specified in config (default no)\n" +
			"                    \t\tUse 'beagle-config networking AddNeighborhoodBeagleNode hostname:portnumber' to add a remote beagle system\n" +
			"                    \t\tThe service by default runs in port 4000\n" +
			"\n" +
			"  --flood\t\t\tExecute the query over and over again.  Don't do that.\n" +
			"  --listener\t\t\tExecute an index listener query.  Don't do that either.\n" +
			"  --help\t\t\tPrint this usage message.\n" +
			"  --version\t\t\tPrint version information.\n" +
			"\n" +
			"Query string supports an advanced query syntax.\n" +
			"For details of the query syntax, please see http://beagle-project.org/Searching_Data\n" +
			"Note: Quotes (\" or \') need to be shell escaped if used.\n";

		Console.WriteLine (usage);

		System.Environment.Exit (0);
	}

	private static void ReadBackendMappings ()
	{
		ArrayList assemblies = ReflectionFu.ScanEnvironmentForAssemblies ("BEAGLE_BACKEND_PATH", PathFinder.BackendDir);

		// Add BeagleDaemonLib if it hasn't already been added.
		bool found_daemon_lib = false;
		foreach (Assembly assembly in assemblies) {
			if (assembly.GetName ().Name == "BeagleDaemonLib") {
				found_daemon_lib = true;
				break;
			}
		}

		if (!found_daemon_lib) {
			try {
				assemblies.Add (Assembly.LoadFrom (Path.Combine (PathFinder.PkgLibDir, "BeagleDaemonLib.dll")));
			} catch (FileNotFoundException) {
				Console.WriteLine ("WARNING: Could not find backend list.");
				Environment.Exit (1);
			}
		}

		foreach (Assembly assembly in assemblies) {
			foreach (Type type in ReflectionFu.GetTypesFromAssemblyAttribute (assembly, typeof (IQueryableTypesAttribute))) {
				object[] attributes = type.GetCustomAttributes (false);
				foreach (object attribute in attributes) {
					PropertyKeywordMapping mapping = attribute as PropertyKeywordMapping;
					if (mapping == null)
						continue;
					//Logger.Log.Debug (mapping.Keyword + " => " 
					//		+ mapping.PropertyName + 
					//		+ " is-keyword=" + mapping.IsKeyword + " (" 
					//		+ mapping.Description + ") "
					//		+ "(" + type.FullName + ")");
					PropertyKeywordFu.RegisterMapping (mapping);
				}
			}
		}
	}

	private static void OnClosed ()
	{
		if (flood)
			SendQuery ();
		else
			main_loop.Quit ();
	}

	private static int query_counter = 0;
	private static void SendQuery ()
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
		} catch (Exception ex) {
			Console.WriteLine ("Could not connect to the Beagle daemon.  The daemon probably isn't running.");
			Console.WriteLine (ex);
			System.Environment.Exit (-1);
		}
	}
	
	[DllImport("libgobject-2.0.so.0")]
	static extern void g_type_init ();

	public static void Main (string[] args) 
	{
		// Initialize GObject type system
		g_type_init ();

		main_loop = new MainLoop ();

		if (args.Length == 0 || Array.IndexOf (args, "--help") > -1 || Array.IndexOf (args, "--usage") > -1)
			PrintUsageAndExit ();

		if (Array.IndexOf (args, "--version") > -1) {
			VersionFu.PrintVersion ();
			Environment.Exit (0);
		}

		StringBuilder query_str =  new StringBuilder ();

		string[] formats = {
			"yyyyMMdd",
			"yyyyMMddHHmmss"
		};

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
				keep_running = true;
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
			case "--listener":
				listener = true;
				keep_running = true;
				break;
			case "--start":
				if (++i >= args.Length) PrintUsageAndExit ();
				try {
					start_date = DateTime.ParseExact (args[i], formats,
									  CultureInfo.InvariantCulture,
									  DateTimeStyles.None);
				} catch (FormatException) {
					Console.WriteLine ("Invalid start date");
					System.Environment.Exit (-1);
				}
				start_date = start_date.ToUniversalTime ();
				break;

			case "--end":
				if (++i >= args.Length) PrintUsageAndExit ();
				try {
					end_date = DateTime.ParseExact (args[i], formats,
									CultureInfo.InvariantCulture,
									DateTimeStyles.None);
				} catch (FormatException) {
					Console.WriteLine ("Invalid end date");
					System.Environment.Exit (-1);
				}
				end_date = end_date.ToUniversalTime ();
				break;

			case "--keywords":
				ReadBackendMappings ();
				QueryDriver.ReadKeywordMappings ();

				Console.WriteLine ("Supported query keywords are:");

				foreach (string key in PropertyKeywordFu.Keys) {
					foreach (PropertyDetail prop in PropertyKeywordFu.Properties (key)) {
						// Dont print properties without description; they confuse people
						if (string.IsNullOrEmpty (prop.Description))
							continue;
						Console.WriteLine ("  {0,-20} for {1}", key, prop.Description);
					}
				}

				System.Environment.Exit (0);
				break;

			case "--network":
				if (++i >= args.Length) PrintUsageAndExit ();
				if (args [i].ToLower () == "yes")
					query.AddDomain (QueryDomain.Neighborhood);
				break;

			default:
				if (query_str.Length > 0)
					query_str.Append (' ');
				query_str.Append (args [i]);
				
				break;
			}

			++i;
		}

		if (listener) {
			query.IsIndexListener = true;
		} else {
		
			if (query_str.Length > 0)
				query.AddText (query_str.ToString ());

			if (start_date != DateTime.MinValue || end_date != DateTime.MinValue) {
				QueryPart_DateRange part = new QueryPart_DateRange ();
				
				if (start_date != DateTime.MinValue)
					part.StartDate = start_date;

				if (end_date != DateTime.MinValue)
					part.EndDate = end_date;
				
				query.AddPart (part);
			}
		}

		query.HitsAddedEvent += OnHitsAdded;
		query.HitsSubtractedEvent += OnHitsSubtracted;

		if (! keep_running)
			query.FinishedEvent += OnFinished;
		else
			query.ClosedEvent += OnClosed;

		SendQuery ();

		main_loop.Run ();
	}
}
	
