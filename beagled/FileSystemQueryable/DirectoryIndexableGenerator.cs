//
// DirectoryIndexableGenerator.cs
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

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Daemon.FileSystemQueryable {

	public class DirectoryIndexableGenerator : IIndexableGenerator {

		FileSystemModel model;
		FileSystemModel.Directory directory;
		IEnumerator files;
		bool done = false;

		public DirectoryIndexableGenerator (FileSystemModel model,
						    FileSystemModel.Directory directory)
		{
			this.model = model;
			this.directory = directory;

			if (this.directory == null)
				done = true;
			else {
				DirectoryInfo info = new DirectoryInfo (this.directory.FullName);
				files = info.GetFiles ().GetEnumerator ();
			}
		}

		public Indexable GetNextIndexable ()
		{
			if (done)
				return null;

			while (files.MoveNext ()) {
				FileInfo f = files.Current as FileInfo;
				if (! model.Ignore (f.FullName)
				    && ! model.IsUpToDate (f.FullName))
					return FileSystemQueryable.FileToIndexable (f.FullName, true);
			}

			done = true;
			return null;
		}

		public bool HasNextIndexable ()
		{
			return ! done;
		}

		public string StatusName {
			get { 
				if (this.directory == null)
					return "Crawling the null directory?";
				return "Crawling " + this.directory.FullName;
			}
		}
	}
}
