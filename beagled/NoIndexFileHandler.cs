//
// NoIndexFileCallout.cs
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

using Beagle.Core;
using Beagle.Util;
using System.Collections;
using System.IO;

namespace Beagle.Daemon {
	class NoIndexFileHandler : PreIndexHandler {
		struct IndexerDirectoryInfo {
			public bool hasNoIndex;
			public FileMatcher matcher;
		}

		Hashtable dirInfos = new Hashtable ();

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
				FileMatcher noIndex = LoadNoIndex (dirName);
				
				if ((noIndex != null) && (noIndex.IsEmpty || noIndex.IsMatch (fileName))) {
					return false;
				}

				fileName = Path.GetFileName (dirName);
				dirName = Path.GetDirectoryName (dirName);
			}

			return true;
		}

		public override void Run (PreIndexHandlerArgs args)
		{
			IndexableFile file = args.indexable as IndexableFile;

			if (file == null) 
				return;

			string path = file.Uri;
			if (path.StartsWith ("file://"))
				path = path.Substring ("file://".Length);

			path = Path.GetFullPath (path);
			
			if (path == null)
				return;

			if (!ShouldIndex (path)) {
				args.shouldIndex = false;
			}
		}
	}
}
