//
// IndexWebContent.cs
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

using Beagle;

class IndexWebContentTool {

	public class IndexableWeb : Indexable {
		
		Stream stream;
		ArrayList properties = new ArrayList ();
		Filter filter;
		
		public IndexableWeb (String uri,
				     String title,
				     Stream contentStream)
		{
			Uri = uri;
			Type = "WebHistory";
			MimeType = "text/html";
			Timestamp = DateTime.Now;

			stream = contentStream;

			Console.WriteLine (uri);
			Console.WriteLine (title);
			Console.WriteLine ();

			properties.Add (Property.New ("dc:title", title));

			filter = Filter.FilterFromMimeType ("text/html");
		}

		override public IEnumerable Properties {
			get { return properties; }
		}

		override protected void DoBuild ()
		{
			filter.Open (stream);
		}

		override public TextReader GetTextReader ()
		{
			return filter.GetTextReader ();
		}

		override public TextReader GetHotTextReader ()
		{
			return filter.GetHotTextReader ();
		}
	}

	static void Main (String[] args)
	{
		String uri = args[0];
		String title = args[1];

		// For security/privacy reasons, we don't index any
		// SSL-encrypted pages.
		if (uri.StartsWith ("https://"))
			return;

		Indexable indexable = new IndexableWeb (uri, title,
							Console.OpenStandardInput ());

		IndexDriver driver = new IndexDriver ();
		driver.Add (indexable);

	}
}
