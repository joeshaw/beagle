//
// IndexWebContent.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;

using Dewey;
using Dewey.Filters;

class IndexWebContentTool {

	public class IndexableWeb : Indexable {
		
		String content, hotContent;
		
		public IndexableWeb (String _uri,
				     String _title,
				     Stream contentStream)
		{
			uri = _uri;
			domain = "web";
			mimeType = "text/html";
			timestamp = DateTime.Now;
			needPreload = false;

			SetMetadata ("title", _title);
			
			Filter filter = Filter.FilterFromMimeType ("text/html");
			filter.Open (contentStream);
			content = filter.Content;
			hotContent = filter.HotContent;
			filter.Close ();
		}

		override public String Content {
			get { return content; }
		}

		override public String HotContent {
			get { return hotContent; }
		}
	}

	static void Main (String[] args)
	{
		String uri = args[0];
		String title = args[1];

		Indexable indexable = new IndexableWeb (uri, title, Console.OpenStandardInput ());

		IndexDriver driver = new IndexDriver ();
		driver.Add (indexable);

	}
}
