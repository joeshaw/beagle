//
// Crawler.cs
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


using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using Mono.Posix;

using BU = Beagle.Util;
using Beagle;

class CrawlerTool { 

	public class Crawler {
		// Contains uris
		ArrayList toBeIndexed = new ArrayList ();

		int flushCount = 100;

		int sinceOptimize = 0;
		int optimizeCount = 10;

		Hashtable fileTable = new Hashtable ();

		Indexer indexer;

		public Crawler () {
			indexer = Indexer.Get ();
		}

		void ScheduleAdd (string uri)
		{
			toBeIndexed.Add (uri);
			MaybeFlush ();
		}

		void MaybeFlush ()
		{
			if (toBeIndexed.Count > flushCount)
				Flush ();
		}

		public void Flush ()
		{
			System.Console.WriteLine ("Flushing");
			if (toBeIndexed.Count > 0) {
				foreach (string uri in toBeIndexed)
					indexer.IndexFile (uri);
				toBeIndexed.Clear ();
			}
			System.Console.WriteLine ("Done Flushing");
		}

		private bool IsSymLink (string path)
		{
			Stat stat = new Stat ();
			Syscall.lstat (path, out stat);
			int mode = (int) stat.Mode & (int)StatModeMasks.TypeMask;
			return mode == (int) StatMode.SymLink;
		}

		void CrawlFile (FileInfo info)
		{
			// Don't follow symlinks
			if (IsSymLink (info.FullName))
				return;

			ScheduleAdd ("file://" + info.FullName);
		}

		void CrawlDirectory (DirectoryInfo info, int maxRecursion)
		{
			// Don't follow symlinks
			if (IsSymLink (info.FullName))
				return;

			// Scan the .noindex file.
			BU.FileMatcher noindex = new BU.FileMatcher ();
			String noindexPath = Path.Combine (info.FullName, ".noindex");
			if (File.Exists (noindexPath)) {
				noindex.Load (noindexPath);
				// An empty .noindex file causes all files and subdirs
				// to be skipped.
				if (noindex.IsEmpty) {
					Console.WriteLine ("Skipping {0}", info.FullName);
					return;
				}
			}

			ScheduleAdd ("file://" + info.FullName);

#if false
			// Pull this directory's indexables and put them in a hashtable
			// by Uri.  Schedule obsolete duplicates for deletion.
			Hit[] hits = driver.FindByProperty ("_Directory", info.FullName);
			Hashtable hitHash = new Hashtable ();
			foreach (Hit hit in hits) {
				Hit prev = (Hit) hitHash [hit.Uri];
				if (prev != null) {
					if (prev.IsObsoletedBy (hit))
						ScheduleDelete (prev);
					else {
						ScheduleDelete (hit);
						continue;
					}
				}
				hitHash [hit.Uri] = hit;
			}
#endif
			
			Console.WriteLine ("Scanning files in {0}", info.FullName);
			
			foreach (FileInfo file in info.GetFiles ()) {
				if (! noindex.IsMatch (file.Name)) {
					//try {
						String uri = "file://" + file.FullName;
						CrawlFile (file);
						//} catch (Exception e) {
						//Console.WriteLine ("Caught exception while crawling file '" + file.Name + "':\n" + e.Message);
						//}
				}
			}
				
			if (maxRecursion > 0) {

				foreach (DirectoryInfo subdir in info.GetDirectories ()) {
					if (! noindex.IsMatch (subdir.Name)) {
						//try {
							String uri = "file://" + subdir.FullName;
							CrawlDirectory (subdir, maxRecursion - 1);
							//} catch (Exception e) {
							//Console.WriteLine ("Caught exception while crawling directory '" + subdir.Name + "':\n" + e.Message);
							//}
					}
				}
			}

#if false
			// If we didn't see some files that were previously indexed, they
			// must have been deleted.  Schedule the previously-retrieved hits
			// for deletion.
			foreach (Hit hit in hitHash.Values)
				ScheduleDelete (hit);
#endif
		}

		public void Crawl (String path)
		{
			bool quick = false;
			if (path.StartsWith ("-quick:")) {
				path = path.Substring ("-quick:".Length);
				quick = true;
			}

			if (path.StartsWith ("file://"))
				path = path.Substring ("file://".Length);

			if (File.Exists (path)) {
				CrawlFile (new FileInfo (path));
			} else if (Directory.Exists (path)) {
				DirectoryInfo dirinfo = new DirectoryInfo (path);
				int maxRecursion = 10000;
				if (quick)
					maxRecursion = 0;

				CrawlDirectory (dirinfo, maxRecursion);
			} else {
				Console.WriteLine ("Can't crawl {0}", path);
			}
		}

		public void CrawlRecentFiles ()
		{	
			string HomeDir = Environment.GetEnvironmentVariable ("HOME");
			string path = Path.Combine (HomeDir, ".recently-used");
			XmlDocument doc;

			//try {
				if (!File.Exists (path))
					return;
				doc = new XmlDocument ();
				doc.Load (path);
				//} catch {
				//Console.WriteLine ("No File: {0}", path);
				//return;
				//}
			XmlNodeList nodes = doc.SelectNodes ("/RecentFiles/RecentItem/URI");

			foreach (XmlNode node in nodes)
				Crawl (String.Concat ("-quick:", node.InnerText));
		}

		public void CrawlFast ()
		{
			CrawlRecentFiles ();
		}
	}

	static void Main (String[] args)
	{
		Gtk.Application.Init ();
		BU.FileMatcher.AddDefault (".*",
					"*~",
					"#*#",
					"*.cs", // FIXME: we skip other source code...
					"*.o",
					"*.a",
					"*.S",
					"*.la",
					"*.lo",
					"*.so",
					"*.exe",
					"*.dll",
					"*.com",
					"*.csproj",
					"*.dsp",
					"*.dsw",
					"*.m4",
					"*.pc",
					"*.pc.in",
					"*.in.in",
					"*.omf",
					"*.aux",
					"po",
					"aclocal",
					"Makefile",
					"Makefile.am",
					"Makefile.in",
					"CVS");

		Crawler crawler = new Crawler ();

		if (args.Length > 0) {
			foreach (String arg in args) {
				if (String.Compare (arg, "--fast") == 0)
					crawler.CrawlFast ();
				else
					crawler.Crawl (arg);
			}
		} else {
			// By default, crawl the user's home directory.
			crawler.Crawl (Environment.GetEnvironmentVariable ("HOME"));
		}

		crawler.Flush ();
	}
}
	
