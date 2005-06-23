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

	static void Main (String[] args)
	{
		bool firstArg = true;

		Logger.DefaultEcho = true;
		Logger.DefaultLevel = LogLevel.Debug;

		foreach (String arg in args) {

			if (arg == "--tokenize") {
				tokenize = true;
				continue;
			}
			
			Indexable indexable;

			Uri uri = UriFu.PathToFileUri (arg);
			Console.WriteLine ("uri: {0}", uri);

			indexable = new Indexable (uri);

			if (!firstArg) {
				Console.WriteLine ();
				Console.WriteLine ("-----------------------------------------");
				Console.WriteLine ();
			}
			firstArg = false;

			Console.WriteLine ("Filename: " + uri);

			if (! FilterFactory.FilterIndexable (indexable))
				Console.WriteLine ("No filter!");

			Console.WriteLine ();

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

			if (indexable.NoContent)
				return;

			Stream stream = indexable.GetBinaryStream ();
			if (stream != null)
				Console.WriteLine ("Contains binary data.");

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
			
		}
	}
}
