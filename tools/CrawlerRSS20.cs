//
// CrawlerRSS20.cs
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
using System.Threading;
using System.Xml;

using Beagle;

class IndexableRSS20Item : Indexable {

	String content;

	public IndexableRSS20Item (XmlNode itemNode) {
		Type = "RssFeed";
		MimeType = "text/html";

		XmlNode linkNode = itemNode.SelectSingleNode ("link");
		if (linkNode == null
		    || linkNode.InnerText == null)
			throw new Exception ("No link node!");
		Uri = linkNode.InnerText;
	
		XmlNode titleNode = itemNode.SelectSingleNode ("title");
		if (titleNode != null && titleNode.InnerText != null)
			this ["title"] = titleNode.InnerText;

		XmlNode pubDateNode = itemNode.SelectSingleNode ("pubDate");
		if (pubDateNode == null || pubDateNode.InnerText == null)
			throw new Exception ("No pub date!");
		String pubDateStr = pubDateNode.InnerText;
		// Chop off the time zone
		int k = pubDateStr.LastIndexOf (" ");
		pubDateStr = pubDateStr.Remove (k, pubDateStr.Length - k);
		Timestamp = DateTime.Parse (pubDateStr);

		XmlNode descriptionNode = itemNode.SelectSingleNode ("description");
		if (descriptionNode == null || descriptionNode.InnerText == null)
			throw new Exception ("No description!");
		String description = descriptionNode.InnerText;
		// Strip out any tags.
		while (true) {
			int i = description.IndexOf ("<");
			if (i == -1)
				break;
			int j = description.IndexOf (">", i);
			description = description.Remove(i, j-i+1);
		}
		Content = description;

	}
}

class CrawlerRSS20Tool {

	class CrawlerRSS20 {

		String uri;
		ArrayList array;

		public CrawlerRSS20 (String _uri, ArrayList _array)
		{
			uri = _uri;
			array = _array;
		}

		public void Crawl ()
		{
			lock (array)
				Console.WriteLine ("Scanning {0}", uri);

			WebRequest req = WebRequest.Create (uri);
			WebResponse resp = req.GetResponse ();
	    
			Stream s = resp.GetResponseStream ();
			XmlDocument doc = new XmlDocument ();
			doc.Load (s);

			XmlNode titleNode = doc.SelectSingleNode ("/rss/channel/title");
			String title = "unknown";
			if (titleNode != null) 
				title = titleNode.InnerText;
			lock (array)
				Console.WriteLine ("Found {0}", title);

			int count = 0;
			XmlNodeList items = doc.SelectNodes ("/rss/channel/item");
			foreach (XmlNode item in items) {
				try { 
					Indexable indexable = new IndexableRSS20Item (item);
					++count;
					lock (array) {
						array.Add (indexable);
					}
				} catch (Exception e) {
					// There was some problem building the indexable, so
					// we just skip it.
				}
			}
			lock (array)
				Console.WriteLine ("Processed {0} items from {1}", count, title);
		}
	}

	static void Crawl (String [] sites, ArrayList array)
	{
		ArrayList threads = new ArrayList ();

		foreach (String uri in sites) {
			CrawlerRSS20 crawler = new CrawlerRSS20 (uri, array);
			Thread th = new Thread (new ThreadStart (crawler.Crawl));
			threads.Add (th);
		}

		foreach (Thread th in threads)
			th.Start ();

		foreach (Thread th in threads)
			th.Join ();
	}
    
	static void Main (String[] args)
	{
		ArrayList array = new ArrayList ();

		String[] defaultSites = new String[] { 
			"http://planet.gnome.org/rss20.xml",
			"http://www.go-mono.com/monologue/index.rss",
			"http://www.planetsuse.org/rss20.xml",
			"http://classpath.wildebeest.org/planet/rss20.xml",
			"http://fedora.linux.duke.edu/fedorapeople/rss20.xml",
			"http://planetsun.org/rss20.xml",
			"http://planet.debian.net/rss20.xml"
		};
	    
		if (args.Length > 0)
			Crawl (args, array);
		else
			Crawl (defaultSites, array);

		IndexDriver driver = new IndexDriver ();
		driver.Add (array);
	}
}
