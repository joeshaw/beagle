//
// IndexWebContent.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;

using Beagle;
using Beagle.Filters;

class IndexWebContentTool {

	public class IndexableWeb : Indexable {
		
		String content, hotContent;
		
		public IndexableWeb (String uri,
				     String title,
				     Stream contentStream)
		{
			Uri = uri;
			Type = "WebLink";
			MimeType = "text/html";
			Timestamp = DateTime.Now;

			this ["title"] = title;
			
			Filter filter = Filter.FilterFromMimeType ("text/html");
			filter.Open (contentStream);
			Content = filter.Content;
			HotContent = filter.HotContent;
			filter.Close ();
		}
	}

	static void Main (String[] args)
	{
		String uri = args[0];
		String title = args[1];

		Indexable indexable = new IndexableWeb (uri, title,
							Console.OpenStandardInput ());

		IndexDriver driver = new IndexDriver ();
		driver.Add (indexable);

	}
}
