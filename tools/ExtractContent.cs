//
// ExtractContent.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;

using Dewey.Filters;

class ExtractContentTool {

	static void Main (String[] args)
	{
		foreach (String arg in args) {

			Filter filter = Filter.FilterFromPath (arg);

			Stream stream = new FileStream (arg,
							FileMode.Open,
							FileAccess.Read);

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
