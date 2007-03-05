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
	static bool continue_last = false;

	// FIXME: We don't display structural breaks
	static void DisplayContent (char[] buffer, int length)
	{
		if (tokenize) {
			if (continue_last && buffer [0] == ' ')
				Console.WriteLine ();

			char last_char = buffer [length - 1];
			continue_last = (last_char != '\n' &&
					      last_char != '\t' &&
					      last_char != ' ');

			string line = new string (buffer, 0, length);
			string [] parts = line.Split (' ');
			for (int i = 0; i < parts.Length - 1; ++i) {
				string part = parts [i].Trim ();
				if (part != String.Empty)
					Console.WriteLine ("{0}", part);
			}

			string last = parts [parts.Length - 1];
			last = last.Trim ();
			if (last != String.Empty)
				Console.Write ("{0}{1}", last, (continue_last ? "" : "\n"));

		} else {
			Console.Write (buffer, 0, length);
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

		Stopwatch watch = new Stopwatch ();

		Filter filter;

		watch.Start ();
		if (! FilterFactory.FilterIndexable (indexable, out filter)) {
			Console.WriteLine ("No filter for {0}", indexable.MimeType);
			indexable.Cleanup ();
			return;
		}
		watch.Stop ();

		Console.WriteLine ("Filter: {0} (determined in {1})", filter, watch);
		Console.WriteLine ("MimeType: {0}", indexable.MimeType);
		Console.WriteLine ();

		if (filter.ChildIndexables != null && filter.ChildIndexables.Count > 0) {
			Console.WriteLine ("Child indexables ({0}):", filter.ChildIndexables.Count);

			foreach (Indexable i in filter.ChildIndexables)
				Console.WriteLine ("  {0}", i.Uri);

			Console.WriteLine ();
		}

		// Make sure that the properties are sorted.
		ArrayList prop_array = new ArrayList (indexable.Properties);
		prop_array.Sort ();

		bool first = true;

		Console.WriteLine ("Properties:");

		if (indexable.ValidTimestamp)
			Console.WriteLine ("  Timestamp = {0}", DateTimeUtil.ToString (indexable.Timestamp));

		foreach (Beagle.Property prop in prop_array) {
			Console.WriteLine ("  {0} = {1}", prop.Key, prop.Value);
		}

		Console.WriteLine ();

		if (indexable.NoContent)
			return;

		watch.Reset ();
		watch.Start ();

		TextReader reader;

		char[] buffer = new char [2048];
		reader = indexable.GetTextReader ();
		if (reader != null) {
			Console.WriteLine ("Content:");
			while (true) {
				int l = reader.Read (buffer, 0, 2048);
				if (l <= 0)
					break;
				if (first)
					first = false;
				DisplayContent (buffer, l);
			}
			reader.Close ();

			if (first)
				Console.WriteLine ("(no content)");
			else
				Console.WriteLine ('\n');
		}
			
		reader = indexable.GetHotTextReader ();
		if (reader != null) {
			Console.WriteLine ("HotContent:");
			first = true;
			while (true) {
				int l = reader.Read (buffer, 0, 2048);
				if (l <= 0)
					break;
				if (first)
					first = false;
				DisplayContent (buffer, l);
			}
			reader.Close ();

			if (first)
				Console.WriteLine ("(no hot content)");
			else
				Console.WriteLine ('\n');
		}

		watch.Stop ();

		Console.WriteLine ();
		Console.WriteLine ("Text extracted in {0}", watch);

		Stream stream = indexable.GetBinaryStream ();
		if (stream != null)
			stream.Close ();

		// Clean up any temporary files associated with filtering this indexable.
		indexable.Cleanup ();

		if (filter.ChildIndexables != null) {
			foreach (Indexable i in filter.ChildIndexables) {
				if (! show_children) {
					i.Cleanup ();
					continue;
				}

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
		SystemInformation.SetProcessName ("beagle-extract-content");

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
