//
// Crawler.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

using Dewey.Filters;
using Dewey;

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

		int totalCount = 0;
		int filterableCount = 0;
		Hashtable fileTable = new Hashtable ();

		ArrayList toBeIndexed = new ArrayList ();

		void CrawlFile (String path)
		{
			Flavor flavor = Flavor.FromPath (path);
			if (fileTable.Contains (flavor)) {
				int n = (int) fileTable [flavor];
				fileTable [flavor] = n+1;
			} else
				fileTable [flavor] = 1;

			++totalCount;
			if (! Filter.CanFilter (flavor))
				return;
			++filterableCount;

			Indexable indexable = new IndexableFile (path);
			toBeIndexed.Add (indexable);
		}

		void CrawlDirectory (String path)
		{
			FileMatcher noindex = new FileMatcher ();

			String noindexPath = Path.Combine (path, ".noindex");
			if (File.Exists (noindexPath)) {
				noindex.Load (noindexPath);
				// An empty .noindex file causes all files and subdirs
				// to be skipped.
				if (noindex.IsEmpty) {
					Console.WriteLine ("Skipping {0}", path);
					return;
				}
			}

			DirectoryInfo dir = new DirectoryInfo (path);

			foreach (FileSystemInfo info in dir.GetFileSystemInfos ()) {

				if (noindex.IsMatch (info.Name))
					continue;

				if ((int)(info.Attributes & FileAttributes.Directory) != 0)
					CrawlDirectory (info.FullName);
				else
					CrawlFile (info.FullName);
			}
		}

		public void Crawl (String path)
		{
			if (File.Exists (path))
				CrawlFile (path);
			else if (Directory.Exists (path))
				CrawlDirectory (path);
			else
				Console.WriteLine ("Can't crawl {0}", path);
		}

		public void Finish ()
		{
			if (toBeIndexed.Count > 0) {
				IndexDriver driver = new IndexDriver ();
				driver.Add (toBeIndexed);
			}

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
