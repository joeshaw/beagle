//
// IndexableFile.cs
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

using Beagle.Filters;

namespace Beagle {

	public class IndexableFile : Indexable {

		Filter filter;
		String path;

		public IndexableFile (String _path)
		{
			path = Path.GetFullPath (_path);
			if (! File.Exists (path))
				throw new Exception ("No such file: " + path);
	    
			filter = Filter.FilterFromPath (path);
			if (filter == null)
				throw new Exception ("Can't find filter for file " + path);

			Uri = "file://" + path;
			Type = "File";
			MimeType = filter.Flavor.MimeType;		

			Timestamp = File.GetLastWriteTime (path);
		}

		override protected void DoBuild ()
		{
			Stream stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			filter.Open (stream);
			foreach (String key in filter.Keys)
				this [key] = filter [key];
			ContentReader = filter.Content;
			HotContentReader = filter.HotContent;

			FileInfo info = new FileInfo (path);
			this ["_Directory"] = info.DirectoryName;

			// FIXME: there is more information that we could attach
			// * Filesystem-level metadata
			// * Nautilus emblems
		}
	}

}
