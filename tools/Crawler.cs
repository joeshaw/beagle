//
// Crawler.cs
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
using System.Text.RegularExpressions;

using Beagle.Filters;
using Beagle;

class CrawlerTool { 

	public class FileMatcher {
		
		static ArrayList defaultPatterns = new ArrayList ();
		ArrayList patterns = new ArrayList ();

		static Regex PatternToRegex (String pattern)
		{
			pattern = "^" + Regex.Escape (pattern) + "$";
			pattern = pattern.Replace ("\\?", ".");
			pattern = pattern.Replace ("\\*", ".*");
			return new Regex (pattern);
		}

		public void Add (String pattern)
		{
			patterns.Add (PatternToRegex (pattern));
		}

		public void Add (params String[] patterns)
		{
			foreach (String pattern in patterns)
				Add (pattern);
		}

		static public void AddDefault (String pattern)
		{
			defaultPatterns.Add (PatternToRegex (pattern));
		}

		static public void AddDefault (params String[] patterns)
		{
			foreach (String pattern in patterns)
				AddDefault (pattern);
		}

		public void Load (String path)
		{
			StreamReader sr = new StreamReader (path);
			String line;
			while ((line = sr.ReadLine ()) != null) {
				line = line.Trim ();
				if (line.Length > 0)
					Add (line);
			}
		}

		public bool IsEmpty {
			get { return patterns.Count == 0; }
		}

		public bool IsMatch (String path)
		{
			String fileName = Path.GetFileName (path);
			foreach (Regex regex in defaultPatterns)
				if (regex.IsMatch (fileName))
					return true;
			foreach (Regex regex in patterns)
				if (regex.IsMatch (fileName))
					return true;
			return false;
		}
	}


	public class Crawler {

		int flushCount = 1000;
		int flushSize =  50 * 1048576; // = 50mb

		int totalCount = 0;
		int filterableCount = 0;
		long pendingSize = 0;

		Hashtable fileTable = new Hashtable ();

		ArrayList toBeIndexed = new ArrayList ();

		void Schedule (FileInfo info, Indexable indexable)
		{
			pendingSize += info.Length;
			toBeIndexed.Add (indexable);
			if (toBeIndexed.Count > flushCount || pendingSize > flushSize)
				Flush (false);
				
		}

		void Flush (bool isLast)
		{
			if (toBeIndexed.Count > 0 || isLast) {
				IndexDriver driver = new IndexDriver ();
				if (toBeIndexed.Count > 0) {
					driver.Add (toBeIndexed, false);
					toBeIndexed.Clear ();
					pendingSize = 0;
				}
				
				//Console.WriteLine ("Optimize begin");
				driver.Optimize ();
				//Console.WriteLine ("Optimize end");

				if (! isLast) {
					//Console.WriteLine ("GC begin");
					GC.Collect ();
					//Console.WriteLine ("GC end");
				}
			}
		}

		void CrawlFile (FileInfo info)
		{
			Flavor flavor = Flavor.FromPath (info.FullName);
			if (fileTable.Contains (flavor)) {
				int n = (int) fileTable [flavor];
				fileTable [flavor] = n+1;
			} else
				fileTable [flavor] = 1;

			++totalCount;
			if (! Filter.CanFilter (flavor))
				return;
			++filterableCount;

			if (info.Length > 50 * 1048576) { // =50mb FIXME: shouldn't just be a hard-wired constant
				Console.WriteLine ("To big: {0}", info.FullName);
				return;
			}

			Indexable indexable = new IndexableFile (info.FullName);
			Schedule (info, indexable);
		}

		void CrawlDirectory (DirectoryInfo info)
		{
			FileMatcher noindex = new FileMatcher ();

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

			Console.WriteLine ("Scanning {0}", info.FullName);

			foreach (FileInfo file in info.GetFiles ()) {
				if (! noindex.IsMatch (file.Name)) {
					try {
						CrawlFile (file);
					} catch (Exception e) {
						Console.WriteLine ("Caught exception while crawling file '" + file.Name + "':\n" + e.Message);
					}
				}
			}

			foreach (DirectoryInfo subdir in info.GetDirectories ()) {
				if (! noindex.IsMatch (subdir.Name)) {
					try {
						CrawlDirectory (subdir);
					} catch (Exception e) {
						Console.WriteLine ("Caught exception while crawling directory '" + subdir.Name + "':\n" + e.Message);
					}
				}
			}
		}

		public void Crawl (String path)
		{
			if (File.Exists (path))
				CrawlFile (new FileInfo (path));
			else if (Directory.Exists (path))
				CrawlDirectory (new DirectoryInfo (path));
			else
				Console.WriteLine ("Can't crawl {0}", path);
		}

		public void Finish ()
		{
			Flush (true);

			Console.WriteLine ("\n**** FILE STATS ****\n");
			foreach (Flavor flavor in fileTable.Keys)
				Console.WriteLine ("{0} {1}", (int) fileTable [flavor], flavor);
			Console.WriteLine ();
			Console.WriteLine ("     Total files: {0}", totalCount);
			if (totalCount > 0) {
				Console.WriteLine ("Filterable files: {0} ({1:f1}%)",
						   filterableCount,
						   100.0 * filterableCount / totalCount);
			}
		}
	}

	static void Main (String[] args)
	{
		FileMatcher.AddDefault (".*",
					"*~",
					"#*#",
					"*.o",
					"*.a",
					"*.la",
					"*.so",
					"*.exe",
					"*.dll",
					"*.com",
					"CVS");

		Crawler crawler = new Crawler ();

		if (args.Length > 0) {
			foreach (String arg in args)
				crawler.Crawl (arg);
		} else {
			// By default, crawl the user's home directory.
			crawler.Crawl (Environment.GetEnvironmentVariable ("HOME"));
		}

		crawler.Finish ();
	}
}
