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

		FileSystemQueryable queryable;
		FileSystemModel model;
		FileSystemModel.Directory directory;
		IEnumerator files;
		bool done = false;

		public DirectoryIndexableGenerator (FileSystemQueryable queryable,
						    FileSystemModel.Directory directory)
		{
			this.queryable = queryable;
			this.directory = directory;

			if (this.directory == null)
				done = true;
			else {
				DirectoryInfo info = new DirectoryInfo (this.directory.FullName);
				files = info.GetFiles ().GetEnumerator ();
			}
		}

		// return null if we don't need to index that file
		private Indexable BuildIndexableForPath (string path)
		{
			FileSystemModel model = queryable.Model;

			FileSystemModel.RequiredAction action;
			string old_path;

			action = model.DetermineRequiredAction (path, out old_path);
			
			if (action == FileSystemModel.RequiredAction.None)
				return null;

			if (action == FileSystemModel.RequiredAction.Rename) {
				queryable.Rename (old_path, path, Scheduler.Priority.Delayed);
				return null;
			}
			
			Uri file_uri = UriFu.PathToFileUri (path);
			Uri internal_uri = model.ToInternalUri (file_uri);
			return FileSystemQueryable.FileToIndexable (file_uri, internal_uri, true);
		}
		
		public Indexable GetNextIndexable ()
		{
			if (done)
				return null;

			while (files.MoveNext ()) {
				FileInfo f = files.Current as FileInfo;
				Indexable indexable = null;
				try { 
					if (f.Exists)
						indexable = BuildIndexableForPath (f.FullName);
				} catch (Exception ex) {
					Logger.Log.Debug ("Caught exception calling BuildIndexableForPath on '{0}'", f.FullName);
					Logger.Log.Debug (ex);
				}
				if (indexable != null)
					return indexable;
			}

			done = true;

			// Finally, try to index the directory itself
			return BuildIndexableForPath (directory.FullName);
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
