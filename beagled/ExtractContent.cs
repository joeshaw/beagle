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

		if (! FilterFactory.FilterIndexable (indexable, out filter)) {
			Console.WriteLine ("No filter for {0}", indexable.MimeType);
			indexable.Cleanup ();
			return;
		}

		Console.WriteLine ("Filter: {0}", filter);
		Console.WriteLine ("MimeType: {0}", filter.MimeType);
		Console.WriteLine ();

		if (filter.ChildIndexables != null && filter.ChildIndexables.Count > 0) {
			Console.WriteLine ("Child indexables:");

			foreach (Indexable i in filter.ChildIndexables)
				Console.WriteLine ("  {0}", i.Uri);

			Console.WriteLine ();
		}

		// Make sure that the properties are sorted.
		ArrayList prop_array = new ArrayList (indexable.Properties);
		prop_array.Sort ();

		bool first = true;

		Console.WriteLine ("Properties:");
		Console.WriteLine ("  Timestamp = {0}", indexable.Timestamp);
		foreach (Beagle.Property prop in prop_array) {
			Console.WriteLine ("  {0} = {1}", prop.Key, prop.Value);
		}
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
			reader.Close ();

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
			reader.Close ();

			if (first)
				Console.WriteLine ("(no hot content)");
			else
				Console.WriteLine ();
		}

		Stream stream = indexable.GetBinaryStream ();
		if (stream != null)
			stream.Close ();

		if (show_children && filter.ChildIndexables != null) {
			foreach (Indexable i in filter.ChildIndexables) {
				i.StoreStream ();
				Display (i);
			}
		}
		
		indexable.Cleanup ();

	}

	static void PrintUsage ()
	{
		Console.WriteLine ("beagle-extract-content: Extracts filtered data from a file.");
		Console.WriteLine ("Copyright (C) 2004-2005 Novell, Inc.");
		Console.WriteLine ();
		Console.WriteLine ("Usage: beagle-extract-content [OPTIONS] file [file ...]");
		Console.WriteLine ();
		Console.WriteLine ("Options:");
		Console.WriteLine ("  --debug\t\t\tPrint debug info to the console");
		Console.WriteLine ("  --tokenize\t\t\tTokenize the text before printing");
		Console.WriteLine ("  --show-children\t\tShow filtering information for items created by filters");
		Console.WriteLine ("  --mimetype=<mime_type>\tUse filter for mime_type");
		Console.WriteLine ("  --outfile=<filename>\t\tOutput file name");
		Console.WriteLine ("  --help\t\t\tShow this message");
		Console.WriteLine ();
	}

	static int Main (string[] args)
	{
		if (Array.IndexOf (args, "--debug") == -1)
			Log.Disable ();

		if (Array.IndexOf (args, "--help") != -1) {
			PrintUsage ();
			return 0;
		}

		if (Array.IndexOf (args, "--tokenize") != -1)
			tokenize = true;
		
		if (Array.IndexOf (args, "--show-children") != -1)
			show_children = true;

		StreamWriter writer = null;
		string outfile = null;
		foreach (string arg in args) {

			// mime-type option
			if (arg.StartsWith ("--mimetype=")) {
				mime_type = arg.Substring (11);    
				continue;
			// output file option
			// we need this in case the output contains different encoding
			// printing to Console might not always display properly
			} else if (arg.StartsWith ("--outfile=")) {
				outfile = arg.Substring (10);    
				Console.WriteLine ("Redirecting output to " + outfile);
				FileStream f = new FileStream (outfile, FileMode.Create);
				writer = new StreamWriter (f, System.Text.Encoding.UTF8);
				continue;
			} else if (arg.StartsWith ("--")) // option, skip it 
				continue;
			
			Uri uri = UriFu.PathToFileUri (arg);
			Indexable indexable = new Indexable (uri);
			if (mime_type != null)
				indexable.MimeType = mime_type;

			try {
				if (writer != null) {
					Console.SetOut (writer);
				}

				Display (indexable);
				if (writer != null) {
					writer.Flush ();
				}
				
				if (outfile != null) {
					StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
					standardOutput.AutoFlush = true;
					Console.SetOut(standardOutput);
				}
				
			} catch (Exception e) {
				Console.WriteLine ("Unable to filter {0}: {1}", uri, e.Message);
				return -1;
			}
		}
		if (writer != null)
			writer.Close ();

		GLib.MainLoop main_loop = new GLib.MainLoop ();

		if (Environment.GetEnvironmentVariable ("BEAGLE_TEST_MEMORY") != null) {
			GC.Collect ();
			GLib.Timeout.Add (1000, delegate() { main_loop.Quit (); return false; });
			main_loop.Run ();
		}

		return 0;
	}
}
