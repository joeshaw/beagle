//
// ExtractContent.cs
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
using System.Net;

using Beagle;
using Beagle.Util;
using Beagle.Daemon;

class ExtractContentTool {

	static bool tokenize = false;
	static bool show_children = false;
	static string mime_type = null;

	// FIXME: We don't display structural breaks
	static void DisplayContent (string line)
	{
		if (tokenize) {
			
			string [] parts = line.Split (' ');
			for (int i = 0; i < parts.Length; ++i) {
				string part = parts [i].Trim ();
				if (part != "")
					Console.WriteLine ("{0}", part);
			}

		} else {
			Console.WriteLine (line);
		}
	}

	static bool first_indexable = true;

	static void Display (Indexable indexable)
	{
		if (!first_indexable) {
			Console.WriteLine ();
			Console.WriteLine ("-----------------------------------------");
			Console.WriteLine ();
		}
		first_indexable = false;

		Console.WriteLine ("Filename: " + indexable.Uri);

		Filter filter;

		if (! FilterFactory.FilterIndexable (indexable, out filter))
			Console.WriteLine ("No filter!");

		if (filter != null) {
			Console.WriteLine ("Filter: {0}", filter);
			Console.WriteLine ("MimeType: {0}", filter.MimeType);
		}

		Console.WriteLine ();

		if (filter != null && filter.ChildIndexables != null && filter.ChildIndexables.Count > 0) {
			Console.WriteLine ("Child indexables:");

			foreach (Indexable i in filter.ChildIndexables)
				Console.WriteLine ("  {0}", i.Uri);

			Console.WriteLine ();
		}

		bool first;

		first = true;
		foreach (Beagle.Property prop in indexable.Properties) {
			if (first) {
				Console.WriteLine ("Properties:");
				first = false;
			}
			Console.WriteLine ("  {0} = {1}", prop.Key, prop.Value);
		}
		if (! first)
			Console.WriteLine ();

		if (indexable.NoContent)
			return;

		TextReader reader;

		reader = indexable.GetTextReader ();
		if (reader != null) {
			string line;
			first = true;
			while ((line = reader.ReadLine ()) != null) {
				if (first) {
					Console.WriteLine ("Content:");
					first = false;
				}
				DisplayContent (line);
			}

			if (first)
				Console.WriteLine ("(no content)");
			else
				Console.WriteLine ();
		}
			
		reader = indexable.GetHotTextReader ();
		if (reader != null) {
			string line;
			first = true;
			while ((line = reader.ReadLine ()) != null) {
				if (first) {
					Console.WriteLine ("HotContent:");
					first = false;
				}
				DisplayContent (line);
			}

			if (first)
				Console.WriteLine ("(no hot content)");
			else
				Console.WriteLine ();
		}

		if (show_children && filter != null && filter.ChildIndexables != null) {
			foreach (Indexable i in filter.ChildIndexables) {
				i.StoreStream ();
				i.DeleteContent = true;
				Display (i);
			}
		}

	}

	static void PrintUsage ()
	{
		Console.WriteLine ("beagle-extract-content: Extracts filtered data from a file.");
		Console.WriteLine ("Copyright (C) 2004-2005 Novell, Inc.");
		Console.WriteLine ();
		Console.WriteLine ("Usage: beagle-extract-content [OPTIONS] file [file ...]");
		Console.WriteLine ();
		Console.WriteLine ("Options:");
		Console.WriteLine ("  --tokenize\t\t\tTokenize the text before printing");
		Console.WriteLine ("  --show-children\t\tShow filtering information for items created by filters");
		Console.WriteLine ("  --mimetype=<mime_type>\tUse filter for mime_type");
		Console.WriteLine ("  --help\t\t\tShow this message");
		Console.WriteLine ();
	}

	static int Main (string[] args)
	{
		Logger.DefaultEcho = true;
		Logger.DefaultLevel = LogLevel.Debug;

		if (Array.IndexOf (args, "--help") != -1) {
			PrintUsage ();
			return 0;
		}

		if (Array.IndexOf (args, "--tokenize") != -1)
			tokenize = true;
		
		if (Array.IndexOf (args, "--show-children") != -1)
			show_children = true;

		foreach (string arg in args) {

			// mime-type option
			if (arg.StartsWith ("--mimetype=")) {
				mime_type = arg.Substring (11);    
				continue;
			} else if (arg.StartsWith ("--")) // option, skip it 
				continue;
			
			Uri uri = UriFu.PathToFileUri (arg);
			Indexable indexable = new Indexable (uri);
			if (mime_type != null)
				indexable.MimeType = mime_type;

			try {
				Display (indexable);
			} catch (Exception e) {
				Console.WriteLine ("Unable to filter {0}: {1}", uri, e.Message);
				return -1;
			}
		}

		return 0;
	}
}
