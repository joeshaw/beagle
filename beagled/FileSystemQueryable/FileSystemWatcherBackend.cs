//
// FileSystemWatcherBackend.cs
//
// Copyright (C) 2005 Novell, Inc.
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

	public class FileSystemWatcherBackend : IFileEventBackend {
		
		Hashtable to_be_watched = new Hashtable ();
		Hashtable watching = new Hashtable ();
		FileSystemQueryable queryable;

		public FileSystemWatcherBackend ()
		{
			// FIXME: This list shouldn't be hard-wired
			to_be_watched [PathFinder.HomeDir] = true;
			to_be_watched [Path.Combine (PathFinder.HomeDir, "Desktop")] = true;
			to_be_watched [Path.Combine (PathFinder.HomeDir, "Documents")] = true;
		}

		public object WatchDirectories (string path)
		{
			if (! to_be_watched.Contains (path))
				return null;

			FileSystemWatcher fsw = new FileSystemWatcher (path);

			fsw.Changed += OnChangedEvent;
			fsw.Created += OnCreatedEvent;
			fsw.Deleted += OnDeletedEvent;
			fsw.Renamed += OnRenamedEvent;
			fsw.Error   += OnErrorEvent;
			
			fsw.EnableRaisingEvents = true;

			return fsw;
		}

		public object WatchFiles (string path, object old_handle)
		{
			return old_handle;
		}

		public void Start (FileSystemQueryable queryable)
		{
			this.queryable = queryable;
		}

		public void OnChangedEvent (object source, FileSystemEventArgs args)
		{
			queryable.Add (args.FullPath);
		}

		public void OnCreatedEvent (object source, FileSystemEventArgs args)
		{
			// When a new directory is created, add it to our model
			if (Directory.Exists (args.FullPath)) {
				string parent = Path.GetDirectoryName (args.FullPath);
				FileSystemModel.Directory dir;
				dir = queryable.Model.GetDirectoryByPath (parent);
				queryable.Model.AddChild (dir, Path.GetFileName (args.FullPath));
			}
			queryable.Add (args.FullPath);
		}

		public void OnDeletedEvent (object source, FileSystemEventArgs args)
		{
			FileSystemModel.Directory dir;
			dir = queryable.Model.GetDirectoryByPath (args.FullPath);
			if (dir != null)
				queryable.Model.Delete (dir);
			queryable.Remove (args.FullPath);
		}

		public void OnRenamedEvent (object source, RenamedEventArgs args)
		{
			queryable.Remove (args.OldFullPath);
			queryable.Add (args.FullPath);
		}

		public void OnErrorEvent (object source, ErrorEventArgs args)
		{
			// FIXME: handle the error event
		}
	}
}
