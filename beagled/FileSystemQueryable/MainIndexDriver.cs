//
// MainIndexDriver.cs
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


//
// This should be the only piece of source code that knows anything
// about Lucene.
//

using System;
using System.Collections;
using System.IO;
using System.Globalization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using BU = Beagle.Util;
using Beagle.Daemon;


namespace Beagle.Daemon.FileSystemQueryable {

	public class MainIndexDriver : IndexDriver {

		// 1: Original
		// 2: Changed format of timestamp strings
		// 3: Schema changed to be more Dashboard-Match-like
		// 4: Schema changed for files to include _Directory property
		// 5: Changed analyzer to support stemming.  Bumped version # to
		//    force everyone to re-index.
		// 6: lots of schema changes as part of general post-guadec
		//    refactoring
		private const int VERSION = 5;

		//////////////////////////

		public MainIndexDriver () : base (Lucene.Net.Store.FSDirectory.GetDirectory (Path.Combine (PathFinder.RootDir, "Index"), false))
		{
			String dir = Path.Combine (PathFinder.RootDir, "Index");
			if (! Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			BootstrapIndex (dir);
			Lucene.Net.Store.FSDirectory.Logger = Log;
			//Lucene.Net.Store.FSDirectory.TempDirectoryName = LockDir;

		}

		private String LockDir {
			get {
				String dir = Path.Combine (PathFinder.RootDir, "Locks");
				if (! Directory.Exists (dir))
					Directory.CreateDirectory (dir);

				return dir;
			}
		}

		private void BootstrapIndex (String path) 
		{
			// Look to see if there are any signs of an existing index
			// with the correct version tag.  If everything looks OK,
			// just return.

			String indexTestFile = Path.Combine (path, "segments");
			String versionFile = Path.Combine (PathFinder.RootDir, "indexVersion");

			bool indexExists = File.Exists (indexTestFile);
			bool versionExists = PathFinder.HaveAppData ("__Index", "version");
			
			if (indexExists && versionExists) {
				String line = PathFinder.ReadAppDataLine ("__Index", "version");
				if (line == Convert.ToString (VERSION))
					return;
			}

			if (! indexExists)
				Log.Log ("Creating index.");
			else if (! versionExists)
				Log.Log ("No version information.  Purging index.");
			else
				Log.Log ("Index format is obsolete.  Purging index.");

			// If this looks like an old-style (pre-.beagle/Index) set-up,
			// blow away everything in sight.
			if (File.Exists (Path.Combine (PathFinder.RootDir, "segments")))
				Directory.Delete (PathFinder.RootDir, true);
			else {
				// Purge exist index-related directories.
				Directory.Delete (path, true);
				Directory.Delete (LockDir, true);
			}

			if (! Directory.Exists (path))
				Directory.CreateDirectory (path);

			// Initialize a new index.
			IndexWriter writer = new IndexWriter (IndexStore, null, true);
			writer.Close ();

			// Write out the correct version information.
			PathFinder.WriteAppDataLine ("__Index", "version", Convert.ToString (VERSION));
		}

	}

}
