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

using Beagle;
using BU = Beagle.Util;

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

		IndexDriver driver = new IndexDriver ();

		// Contains Indexables
		ArrayList toBeIndexed = new ArrayList ();

		// Contains Obsoleted or Deleted Hits
		ArrayList toBeRemoved = new ArrayList ();

		int flushCount = 100;

		int skippedCount = 0;
		int totalCount = 0;
		int filterableCount = 0;
		int dirCount = 0;

		int sinceOptimize = 0;
		int optimizeCount = 10;
		

		Hashtable fileTable = new Hashtable ();


		void ScheduleAdd (Indexable indexable)
		{
			toBeIndexed.Add (indexable);
			MaybeFlush ();
		}

		void ScheduleDelete (Hit hit)
		{
			if (hit == null)
				return;
			toBeRemoved.Add (hit);
			MaybeFlush ();
		}

		void MaybeFlush ()
		{
			if (toBeIndexed.Count + toBeRemoved.Count > flushCount)
				Flush ();
		}

		void Flush ()
		{
			bool didSomething = false;
			
			if (toBeIndexed.Count > 0) {
				driver.QuickAdd (toBeIndexed);
				toBeIndexed.Clear ();
				didSomething = true;
			}

			if (toBeRemoved.Count > 0) {
				driver.Remove (toBeRemoved);
				toBeRemoved.Clear ();
				didSomething = true;
			}

			if (didSomething) {
				++sinceOptimize;
				if (sinceOptimize > optimizeCount) {
					driver.Optimize ();
					sinceOptimize = 0;
				}
			}
		}

		private bool IsSymLink (string path)
		{
			Stat stat = new Stat ();
			Syscall.lstat (path, out stat);
			int mode = (int) stat.Mode & (int)StatModeMasks.TypeMask;
			return mode == (int) StatMode.SymLink;
		}

		void CrawlFile (FileInfo info, Hit hit)
		{
			// Don't follow symlinks
			if (IsSymLink (info.FullName))
				return;

			DateTime changeTime = info.LastWriteTime;
			DateTime nautilusTime = BU.NautilusTools.GetMetaFileTime (info.FullName);
			if (nautilusTime > changeTime)
				changeTime = nautilusTime;

			// If the file isn't newer that the hit, don't even bother...
			if (hit != null && ! hit.IsObsoletedBy (changeTime)) {
				++skippedCount;
				return;
			}

			Flavor flavor = Flavor.FromPath (info.FullName);
			if (fileTable.Contains (flavor)) {
				int n = (int) fileTable [flavor];
				fileTable [flavor] = n+1;
			} else
				fileTable [flavor] = 1;

			++totalCount;
			if (Filter.CanFilter (flavor))
				++filterableCount;

			Indexable indexable = new IndexableFile (info.FullName);

			ScheduleAdd (indexable);
			ScheduleDelete (hit);
		}

		void CrawlDirectory (DirectoryInfo info, Hit dirHit, int maxRecursion)
		{
			// Don't follow symlinks
			if (IsSymLink (info.FullName))
				return;

			++dirCount;

			// Scan the .noindex file.
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

			// Create an indexable for the directory.
			Indexable dir = new IndexableFile (info.FullName);
			bool dirChanged = false;
			if (dirHit == null || dirHit.IsObsoletedBy (dir)) {
				ScheduleAdd (dir);
				ScheduleDelete (dirHit);
				dirChanged = true;
			}

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

			
			Console.WriteLine ("Scanning files in {0}", info.FullName);
			
			foreach (FileInfo file in info.GetFiles ()) {
				if (! noindex.IsMatch (file.Name)) {
					//try {
						String uri = "file://" + file.FullName;
						CrawlFile (file, (Hit) hitHash [uri]);
						hitHash.Remove (uri);
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
							CrawlDirectory (subdir, (Hit) hitHash [uri], maxRecursion - 1);
							hitHash.Remove (uri);
							//} catch (Exception e) {
							//Console.WriteLine ("Caught exception while crawling directory '" + subdir.Name + "':\n" + e.Message);
							//}
					}
				}
			}

			// If we didn't see some files that were previously indexed, they
			// must have been deleted.  Schedule the previously-retrieved hits
			// for deletion.
			foreach (Hit hit in hitHash.Values)
				ScheduleDelete (hit);
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
				CrawlFile (new FileInfo (path), null);
			} else if (Directory.Exists (path)) {
				DirectoryInfo dirinfo = new DirectoryInfo (path);
				int maxRecursion = 10000;
				if (quick)
					maxRecursion = 0;

				Hit dirHit = null;
				if (dirinfo.Parent != null)
					dirHit = driver.FindByUri ("file://" + dirinfo.FullName);
				CrawlDirectory (dirinfo, dirHit, maxRecursion);
			} else {
				Console.WriteLine ("Can't crawl {0}", path);
			}
		}

		public void Finish ()
		{
			Flush ();
			if (sinceOptimize != 0)
				driver.Optimize ();

			Console.WriteLine ("\n**** FILE STATS ****\n");
			foreach (Flavor flavor in fileTable.Keys)
				Console.WriteLine ("{0} {1}", (int) fileTable [flavor], flavor);
			Console.WriteLine ();
			Console.WriteLine ("    Total directories: {0}", dirCount);
			Console.WriteLine ();
			Console.WriteLine ("        Skipped files: {0}", skippedCount);
			Console.WriteLine ("          Total files: {0}", totalCount);

			if (totalCount > 0) {
				Console.WriteLine ("     Filterable files: {0} ({1:f1}%)",
						   filterableCount,
						   100.0 * filterableCount / totalCount);
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
		FileMatcher.AddDefault (".*",
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

		crawler.Finish ();
	}
}
	
