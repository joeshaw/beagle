//
// IndexWebContent.cs
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
using System.IO;

using Beagle;

class IndexWebContentTool {

	public class IndexableWeb : Indexable {
		public IndexableWeb (string uri,
				     string title,
				     Stream contentStream)
		{
			Uri = new Uri (uri, true);
			Type = "WebHistory";
			MimeType = "text/html";
			Timestamp = DateTime.Now;

			System.Console.WriteLine ("getting reader");
			SetTextReader (new StreamReader (contentStream));

			if (title != null) 
				AddProperty (Property.New ("dc:title", title));
		}

		public IndexableWeb (string uri,
				     string title,
				     string filename,
				     bool deleteSource)
		{
			Uri = new Uri (uri, true);
			Type = "WebHistory";
			MimeType = "text/html";
			Timestamp = DateTime.Now;
			
			ContentUri = new Uri ("file://" + Path.GetFullPath (filename), false);
			DeleteContent = deleteSource;
		}
	}

	static void PrintUsage () {
		Console.WriteLine ("IndexWebContent.exe: Index web page content using the Beagle Search Engine.");
		Console.WriteLine ("  --url URL\t\tURL for the web page being indexed.\n" +
				   "  --title TITLE\t\tTitle for the web page.\n" +
				   "  --sourcefile PATH\tFile containing content to index.\n" +
				   "\t\t\tIf not set, content is read from STDIN.\n" +
				   "  --deletesourcefile\tDelete file passed to --sourcefile after index.\n" +
				   "  --help\t\tPrint this usage message.\n");
	}

	static void Main (String[] args)
	{
		string uri = null;
		string title = null;
		string sourcefile = null;
		bool deletesourcefile = false;

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
				uri = args [++i];
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
			}
		}

		if (uri == null) {
			Console.WriteLine ("ERROR: URI not specified!\n");
			PrintUsage ();
			Environment.Exit (1);
		} else if (uri.StartsWith ("https://")) {
			// For security/privacy reasons, we don't index any
			// SSL-encrypted pages.
			Console.WriteLine ("ERROR: Indexing secure https:// URIs is not secure!");
			Environment.Exit (1);
		}

		Indexable indexable;

		if (sourcefile != null) {
			
			if (!File.Exists (sourcefile)) {
				Console.WriteLine ("ERROR: sourcefile '{0}' does not exist!",
						   sourcefile);
				Environment.Exit (1);
			}

			indexable = new IndexableWeb (uri, 
						      title, 
						      sourcefile,
						      deletesourcefile);
		} else {
			Stream stdin = Console.OpenStandardInput ();
			if (stdin == null) {
				Console.WriteLine ("ERROR: No sourcefile specified, and no standard input!\n");
				PrintUsage ();
				Environment.Exit (1);
			}

			indexable = new IndexableWeb (uri, title, stdin);
		}

		try {
			System.Console.WriteLine ("Indexing");
			Indexer.Index (indexable);
		} catch (Exception e) {
			Console.WriteLine ("ERROR: Indexing failed:");
			Console.Write (e);
			Environment.Exit (1);
		}
	}
}
