//
// Indexer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal// in the Software without restriction, including without limitation the rights
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
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE// SOFTWARE.
//

using DBus;
using System;
using System.IO;
using System.Collections;
using Beagle.Util;
using Beagle.Core;
using Beagle;
namespace BeagleDaemon
{
	public class Indexer : Beagle.Indexer 
	{
		struct IndexerDirectoryInfo {
			public bool hasNoIndex;
			public FileMatcher matcher;
		}

		Hashtable dirInfos = new Hashtable ();
		IndexDriver driver = new IndexDriver ();

		// Contains Indexables
		ArrayList toBeIndexed = new ArrayList ();

		// Contains Obsoleted or Deleted Hits
		ArrayList toBeRemoved = new ArrayList ();
		
		int sinceOptimize = 0;
		const int optimizeCount = 10;

		FileMatcher LoadNoIndex (string dirName)
		{
			IndexerDirectoryInfo info;
			if (dirInfos.Contains (dirName)) {
				info = (IndexerDirectoryInfo)dirInfos[dirName];
				return info.matcher;
			}
			
			string noIndexPath = Path.Combine (dirName, ".noindex");
			info = new IndexerDirectoryInfo ();
			if (File.Exists (noIndexPath)) {
				info.hasNoIndex = true;
				info.matcher = new FileMatcher ();
				info.matcher.Load (noIndexPath);
			} else {
				info.hasNoIndex = false;
				info.matcher = null;
			}

			dirInfos[dirName] = info;
			return info.matcher;
		}

		bool ShouldIndex (string path)
		{
			string dirName = Path.GetDirectoryName (path);
			string fileName = Path.GetFileName (path);

			while (dirName != null) {
				System.Console.WriteLine ("checking {0}", dirName);
				FileMatcher noIndex = LoadNoIndex (dirName);
				
				if ((noIndex != null) && (noIndex.IsEmpty || noIndex.IsMatch (fileName))) {
					return false;
				}

				fileName = Path.GetFileName (dirName);
				dirName = Path.GetDirectoryName (dirName);
			}

			return true;
		}
		
		void ScheduleAdd (Indexable indexable)
		{
			toBeIndexed.Add (indexable);
		}

		void ScheduleRemove (Hit hit)
		{
			if (hit == null)
				return;
			
			toBeRemoved.Add (hit);
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

		void Index (FileInfo file)
		{
			string uri;
			uri = "file://" + file.FullName;
			
			Hit hit = driver.FindByUri (uri);
			
			DateTime changeTime = file.LastWriteTime;
			DateTime nautilusTime = NautilusTools.GetMetaFileTime (file.FullName);
			
			if (nautilusTime > changeTime)
				changeTime = nautilusTime;

			// If the file isn't newer than the hit, don't
			// even bother

			if (hit != null && !hit.IsObsoletedBy (changeTime))
				return;
			
			Indexable indexable = new IndexableFile (file.FullName);
			ScheduleAdd (indexable);
			ScheduleRemove (hit);
		}

		void IndexPath (string path) 
		{
			if (path.StartsWith ("file://"))
				path = path.Substring ("file://".Length);

			path = Path.GetFullPath (path);
			
			if (path == null) {
				return;
			}

			if (ShouldIndex (path)) {
				FileInfo file = new FileInfo (path);
				System.Console.WriteLine ("Indexing " + file.FullName);
				Index (file);
				System.Console.WriteLine ("Indexed");
				System.Console.WriteLine ("Flushed");

			} else {
				System.Console.WriteLine ("Not Indexing");
			}				

		}

		public override void IndexFile (string path)
		{
			IndexPath (path);
			Flush ();
		}

		public override void IndexFiles (string[] paths)
		{
			foreach (String path in paths) {
				IndexPath (path);
			}
			
			Flush ();
		}
	}
}
