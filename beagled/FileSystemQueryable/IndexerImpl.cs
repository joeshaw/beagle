//
// IndexerImpl.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using DBus;
using System;
using System.IO;
using System.Collections;
using System.Threading;
using Beagle.Daemon;
using BU = Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public class IndexerImpl : Beagle.IndexerProxy
	{
		LuceneDriver driver;
		

		public IndexerImpl (LuceneDriver _driver)
		{
			driver = _driver;
		}

		public override void Index (string xml)
		{
			FilteredIndexable indexable = FilteredIndexable.NewFromXml (xml);
			driver.ScheduleAdd (indexable);
		}

		public override void Delete (string uriStr)
		{
			Uri uri = new Uri (uriStr, true);
			driver.ScheduleDelete (uri);
		}

		private class PostIndexClosure {
			Crawler crawler;
			FileSystemInfo info;

			public PostIndexClosure (Crawler _crawler, FileSystemInfo _info)
			{
				crawler = _crawler;
				info = _info;
			}

			public void Hook (LuceneDriver driver, Uri uri)
			{
				crawler.MarkAsIndexed (info);
			}
		}

		private class CrawlClosure {

			LuceneDriver driver;
			string startPath;
			int startDepth;

			public CrawlClosure (LuceneDriver _driver, string path, int maxDepth)
			{
				driver = _driver;
				startPath = path;
				startDepth = maxDepth;
			}

			private void DoCrawl (string path, int maxDepth)
			{
				Console.WriteLine ("Crawling {0}", path);
			
				FileSystemInfo root = null;

				if (Directory.Exists (path))
					root = new DirectoryInfo (path);
				else if (File.Exists (path))
					root = new FileInfo (path);
				else
					return;

				Crawler crawler = new Crawler (driver, root);

				foreach (FileSystemInfo info in crawler.FilesToIndex) {
					Uri uri = new Uri (BU.StringFu.PathToQuotedFileUri (info.FullName), true);
					Indexable indexable = new FilteredIndexable (uri);
					PostIndexClosure pic = new PostIndexClosure (crawler, info);
					driver.ScheduleAdd (indexable, new PostIndexHook (pic.Hook));
				}

				if (maxDepth != 0) {
					foreach (DirectoryInfo dir in crawler.DirectoriesToCrawl) 
						DoCrawl (dir.FullName, maxDepth-1);
				}
			}

			public void Start ()
			{
				DoCrawl (startPath, startDepth);
			}
		}

		public override void Crawl (string path, int maxDepth)
		{
			CrawlClosure cc = new CrawlClosure (driver, path, maxDepth);

			Thread th = new Thread (new ThreadStart (cc.Start));
			th.Start ();
		}
	}
}
