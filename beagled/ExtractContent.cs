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

using Beagle.Daemon;

class ExtractContentTool {

	static void Main (String[] args)
	{
		bool firstArg = true;

		foreach (String arg in args) {
			
			FilteredIndexable indexable;

			string uri = arg;

			if (!uri.StartsWith ("file://")) {
				uri = Path.GetFullPath (uri);
				uri = "file://" + uri;
			}
				
			Console.WriteLine ("uri: {0}", uri);

			indexable = new FilteredIndexable (new Uri (uri, false));

			if (!firstArg) {
				Console.WriteLine ();
				Console.WriteLine ("-----------------------------------------");
				Console.WriteLine ();
			}
			firstArg = false;

			// FIX: We should call "Build" as it updates the 
			// "Flavor" and "Filter" members, failing which 
			// will result in a "No filter" situation. :)
			indexable.Build ();

			Console.WriteLine ("Filename: " + uri);
			Console.WriteLine ("  Flavor: " + indexable.Flavor);
			if (! indexable.HaveFilter)
				Console.WriteLine ("No filter!");

			Console.WriteLine ();

			TextReader reader;
			bool first;

			first = true;
			foreach (Beagle.Property prop in indexable.Properties) {
				if (first) {
					Console.WriteLine ("Properties:");
					first = false;
				}
				Console.WriteLine ("{0} = {1}", prop.Key, prop.Value);
			}
			if (! first)
				Console.WriteLine ();


			reader = indexable.GetTextReader ();
			if (reader != null) {
				string line;
				first = true;
				while ((line = reader.ReadLine ()) != null) {
					if (first) {
						Console.WriteLine ("Content:");
						first = false;
					}
					Console.WriteLine (line);
				}

				if (! first)
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
					Console.WriteLine (line);
				}

				if (! first)
					Console.WriteLine ();
			}
		}
	}
}
