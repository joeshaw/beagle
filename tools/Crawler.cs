//
// Crawler.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

using Dewey;

class CrawlerTool { 

	public class ExclusionList {

		static Regex GlobToRegex (String glob)
		{
			String pattern = "^" + Regex.Escape (glob) + "$";
			pattern = pattern.Replace ("\\?", ".");
			pattern = pattern.Replace ("\\*", ".*");
			return new Regex (pattern);
		}

		
		static ArrayList excludedStock;
		ArrayList excluded = new ArrayList ();

		public ExclusionList (String filename) 
		{
			StreamReader sr = new StreamReader (filename);
			String line;
			while ((line = sr.ReadLine ()) != null) {
				line = line.Trim ();
				if (line.Length > 0)
					excluded.Add (GlobToRegex (line));
			}
		}

		static public bool ExcludeStock (String filename)
		{
			if (excludedStock == null) {
				excludedStock = new ArrayList ();
				String[] patterns = new String[] { "*~",
								   "#*#",
								   ".*",
								   "*.o",
								   "*.a",
								   "*.la",
								   "*.so",
								   "*.exe",
								   "*.com",
								   "CVS" };
				foreach (String pattern in patterns)
					excludedStock.Add (GlobToRegex (pattern));
			}

			foreach (Regex regex in excludedStock)
				if (regex.IsMatch (filename))
					return true;

			return false;
		}

		public bool ExcludeAll {
			get { return excluded.Count == 0; }
		}

		public bool Exclude (String filename)
		{
			// Empty exclusion list == exclude everything
			if (ExcludeAll)
				return true;

			filename = Path.GetFileName (filename);

			foreach (Regex regex in excluded)
				if (regex.IsMatch (filename))
					return true;

			return false;
		}
	}

	static void ScheduleFileIndexing (String path, ArrayList toBeIndexed)
	{
		try {
			Indexable indexable = new IndexableFile (path);
			toBeIndexed.Add (indexable);
		} catch {
			// If we get an exception, it means that we couldn't
			// filter the file.  In that case, just do nothing.
		}
	}

	static void ScheduleHttpIndexing (String uri, ArrayList toBeIndexed)
	{
		try {
			Indexable indexable = new IndexableHttp (uri);
			toBeIndexed.Add (indexable);
		} catch {
			// If we get an exception, it probably means the uri
			// was malformed.  In that case, just do nothing.
		}
	}

	// path must be a file, or badness will ensue.
	static void CrawlFile (String path, ArrayList toBeIndexed)

	{
		FileInfo info = new FileInfo (path);

		// Check the .noindex in the same directory as the file.
		String noIndex = Path.Combine (info.DirectoryName, ".noindex");
		if (File.Exists (noIndex) && ! ExclusionList.ExcludeStock (info.Name)) {
			ExclusionList exlist = new ExclusionList (noIndex);
			if (exlist.Exclude (info.FullName)) {
				Console.WriteLine ("Excluding {0}", info.FullName);
				return;
			}
		}

		ScheduleFileIndexing (info.FullName, toBeIndexed);
	}

		
	// path must be a directory, or badness will ensue.
	static void CrawlDirectory (String path, ArrayList toBeIndexed)
	{
		Console.WriteLine (path);

		ExclusionList exlist = null;
		String noIndex = Path.Combine (path, ".noindex");
		if (File.Exists (noIndex)) {
			exlist = new ExclusionList (noIndex);
			if (exlist.ExcludeAll)
				return;
		}

		DirectoryInfo dir = new DirectoryInfo (path);
		foreach (FileSystemInfo info in dir.GetFileSystemInfos ()) {
			if (ExclusionList.ExcludeStock (info.Name))
				continue;
			if (exlist != null && exlist.Exclude (info.Name)) {
				if (info.Name != ".noindex")
					Console.WriteLine ("Excluding {0}", info.FullName);
				continue;
			}
			if ((int)(info.Attributes & FileAttributes.Directory) != 0)
				CrawlDirectory (info.FullName, toBeIndexed);
			else
				ScheduleFileIndexing (info.FullName, toBeIndexed);
		}
	}

	static void Crawl (String path, ArrayList toBeIndexed)
	{
		if (path.StartsWith ("http://")) {
			ScheduleHttpIndexing (path, toBeIndexed);
			return;
		}

		if (Directory.Exists (path)) {
			CrawlDirectory (path, toBeIndexed);
			return;
		}
		
		if (File.Exists (path)) {
			CrawlFile (path, toBeIndexed);
			return;
		}

		Console.WriteLine ("Bad crawl request: {0}", path);
	}

	static void Main (String[] args)
	{
		ArrayList toBeIndexed = new ArrayList ();

		if (args.Length > 0) {
			foreach (String arg in args)
				Crawl (arg, toBeIndexed);
		} else {
			// Default crawls
			CrawlDirectory (Environment.GetEnvironmentVariable ("HOME"), 
					toBeIndexed);
		}

		if (toBeIndexed.Count > 0) {
			IndexDriver driver = new IndexDriver ();
			driver.Add (toBeIndexed);
		}
	}
}
