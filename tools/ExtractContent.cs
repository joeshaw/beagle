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
using System.IO;
using System.Net;

using Beagle.Filters;

class ExtractContentTool {

	static void Main (String[] args)
	{
		foreach (String arg in args) {
			
			Filter filter;
			Stream stream;

			if (arg.StartsWith ("http://")) {
				HttpWebRequest req = (HttpWebRequest) WebRequest.Create (arg);
				req.UserAgent = "Beagle.ExtractContent";
				HttpWebResponse resp = (HttpWebResponse) req.GetResponse ();
				if (resp.StatusCode != HttpStatusCode.OK)
					throw new Exception (String.Format ("{0} returned {1}: {2}",
									    resp.StatusCode,
									    resp.StatusDescription));
				String mimeType = resp.ContentType;
				int i = mimeType.IndexOf (";");
				if (i != -1)
					mimeType = mimeType.Substring (0, i);
				filter = Filter.FilterFromMimeType (mimeType);

				if (filter == null) {
					Console.WriteLine ("\nNo filter for mime type '{0}'\n", mimeType);
					continue;
				}

				stream = resp.GetResponseStream ();
			} else {
				filter = Filter.FilterFromPath (arg);
				
				if (filter == null) {
					Flavor flavor = Flavor.FromPath (arg);
					Console.WriteLine ("{0}: No filter for {1}", arg, flavor);
					continue;
				}
				stream = new FileStream (arg, FileMode.Open, FileAccess.Read);
			}

			filter.Open (stream);
	    
			Console.WriteLine ("");

			Console.WriteLine ("Filename: " + arg);
			Console.WriteLine ("  Flavor: " + filter.Flavor);

			Console.WriteLine ("");

			if (filter.Keys.Count == 0)
				Console.WriteLine ("No metadata.");
			else
				foreach (String key in filter.Keys)
					Console.WriteLine (key + " = " + filter [key]);

			Console.WriteLine ("");

			if (filter.Content == null)
				Console.WriteLine ("No Content.");
			else
				Console.WriteLine ("Content: " + filter.Content);

			Console.WriteLine ("");

			if (filter.HotContent == null)
				Console.WriteLine ("No HotContent.");
			else
				Console.WriteLine ("HotContent: " + filter.HotContent);

			Console.WriteLine ("");

			filter.Close ();
		}
	}
}
