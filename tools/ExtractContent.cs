//
// ExtractContent.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;
using System.Net;

using Dewey.Filters;

class ExtractContentTool {

	static void Main (String[] args)
	{
		foreach (String arg in args) {
			
			Filter filter;
			Stream stream;

			if (arg.StartsWith ("http://")) {
				HttpWebRequest req = (HttpWebRequest) WebRequest.Create (arg);
				req.UserAgent = "Dewey.ExtractContent";
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
				stream = resp.GetResponseStream ();
			} else {
				filter = Filter.FilterFromPath (arg);
				stream = new FileStream (arg, FileMode.Open, FileAccess.Read);
			}

			filter.Open (stream);
	    
			Console.WriteLine ("");

			Console.WriteLine ("Filename: " + arg);
			Console.WriteLine ("MimeType: " + filter.MimeType);

			Console.WriteLine ("");

			if (filter.MetadataKeys.Count == 0)
				Console.WriteLine ("No metadata.");
			else
				foreach (String key in filter.MetadataKeys)
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
