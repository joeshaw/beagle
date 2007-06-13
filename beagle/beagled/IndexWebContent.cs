//
// IndexWebContent.cs
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
using System.IO;
using System.Reflection;
using System.Collections;

using Beagle;
using Beagle.Util;
using Beagle.Daemon;

[assembly: AssemblyTitle ("beagle-index-url")]
[assembly: AssemblyDescription ("Index web page content using the Beagle Search Engine")]

class IndexWebContentTool {

	static void PrintUsage ()
	{
		VersionFu.PrintHeader ();

		string usage =
			"Usage: beagle-index-url <OPTIONS>\n\n" +
			"Options:\n" +
			"  --url URL\t\tURL for the web page being indexed.\n" +
			"  --title TITLE\t\tTitle for the web page.\n" +
			"  --sourcefile PATH\tFile containing content to index.\n" +
			"\t\t\tIf not set, content is read from STDIN.\n" +
			"  --deletesourcefile\tDelete file passed to --sourcefile after index.\n" +
			"  --help\t\tPrint this usage message.\n" +
			"  --version\t\tPrint version information.\n";

		Console.WriteLine (usage);
	}

	static void Main (String[] args)
	{
		string uriStr = null;
		string title = null;
		string sourcefile = null;
		bool deletesourcefile = false;

		if (args.Length == 0 || Array.IndexOf (args, "--help") > -1) {
			PrintUsage ();
			Environment.Exit (1);
		}

		for (int i = 0; i < args.Length; i++) {
			switch (args [i]) {
			case "--url":
			case "--title":
			case "--sourcefile":
				if (i + 1 >= args.Length ||
				    args [i + 1].StartsWith ("--")) {
					PrintUsage ();
					Environment.Exit (1);
				}
				break;
			}

			switch (args [i]) {
			case "--url":
				uriStr = args [++i];
				break;
			case "--title":
				title = args [++i];
				break;
			case "--sourcefile":
				sourcefile = args [++i];
				break;
			case "--deletesourcefile":
				deletesourcefile = true;
				break;
			case "--help":
				PrintUsage ();
				return;
			case "--version":
				VersionFu.PrintVersion ();
				return;
			}
		}

		if (uriStr == null) {
			Logger.Log.Error ("URI not specified!\n");
			PrintUsage ();
			Environment.Exit (1);
		}

		Uri uri = new Uri (uriStr, true);
		if (uri.Scheme == Uri.UriSchemeHttps) {
			// For security/privacy reasons, we don't index any
			// SSL-encrypted pages.
			Logger.Log.Error ("Indexing secure https:// URIs is not secure!");
			Environment.Exit (1);
		}

		// We don't index file: Uris.  Silently exit.
		if (uri.IsFile)
			return;

		// We *definitely* don't index mailto: Uris.  Silently exit.
		if (uri.Scheme == Uri.UriSchemeMailto)
			return;

		Indexable indexable;
		
		indexable = new Indexable (uri);
		indexable.HitType = "WebHistory";
		indexable.MimeType = "text/html";
		indexable.Timestamp = DateTime.Now;

		if (title != null)
			indexable.AddProperty (Property.New ("dc:title", title));

		if (sourcefile != null) {
			
			if (!File.Exists (sourcefile)) {
				Logger.Log.Error ("sourcefile '{0}' does not exist!", sourcefile);
				Environment.Exit (1);
			}

			indexable.ContentUri = UriFu.PathToFileUri (sourcefile);
			indexable.DeleteContent = deletesourcefile;

		} else {
			Stream stdin = Console.OpenStandardInput ();
			if (stdin == null) {
				Logger.Log.Error ("No sourcefile specified, and no standard input!\n");
				PrintUsage ();
				Environment.Exit (1);
			}

			indexable.SetTextReader (new StreamReader (stdin));
		}

		IndexingServiceRequest req = new IndexingServiceRequest ();
		req.Add (indexable);

		try {
			Logger.Log.Info ("Indexing");
			Logger.Log.Debug ("SendAsync");
			req.SendAsync ();
			Logger.Log.Debug ("Close");
			req.Close ();
			Logger.Log.Debug ("Done");
		} catch (Exception e) {
			Logger.Log.Error ("Indexing failed: {0}", e);

			// Still clean up after ourselves, even if we couldn't
			// index the content.
			if (deletesourcefile)
				File.Delete (sourcefile);

			Environment.Exit (1);
		}
	}
}
