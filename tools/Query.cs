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
	static DateTime lastQueryTime = DateTime.Now;

	// CLI args
	static bool keepRunning = false;
	static bool verbose = false;

	static void OnHitAdded (Query source, Hit hit)
	{
		lastQueryTime = DateTime.Now;

		if (verbose)
			Console.WriteLine ("  Uri: {0}", hit.Uri);
		else
			Console.WriteLine (hit.Uri);

		if (verbose) {
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

	static void OnHitSubtracted (Query source, Uri uri)
	{
		lastQueryTime = DateTime.Now;

		Console.WriteLine ("Subtracted Uri '{0}'", uri);
		Console.WriteLine ();

		--count;
	}


	static bool QuitIfNoRecentResults ()
	{
		if ((DateTime.Now - lastQueryTime).Milliseconds > 1000) {
			Console.WriteLine ("No results in one second.  Quitting.");
			Gtk.Application.Quit ();
		}

		return true;
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

		query.HitAddedEvent += OnHitAdded;
		query.HitSubtractedEvent += OnHitSubtracted;

		int i = 0;
		while (i < args.Length) {
			if (args [i].StartsWith ("mimetype:")) {
				string mt = args [i].Substring ("mimetype:".Length);
				query.AddMimeType (mt);
			} else if (args [i].StartsWith ("source:")) {
				string ss = args [i].Substring ("source:".Length);
				query.AddSource (ss);
			} else if (args [i].StartsWith ("--keep-running") || args [i].StartsWith ("--keeprunning")) {
				keepRunning = true;
			} else if (args [i].StartsWith ("--verbose")) {
				verbose = true;
			} else {
				query.AddTextRaw (args [i]);
			}
			++i;
		}

		if (! keepRunning)
			GLib.Timeout.Add (50, new GLib.TimeoutHandler (QuitIfNoRecentResults));

		query.Start ();
		Gtk.Application.Run ();
	}


}
