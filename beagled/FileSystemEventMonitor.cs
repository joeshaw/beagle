//
// FileSystemEventMonitor.cs
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

namespace Beagle.Daemon {

	public enum FileSystemEventType {
		Changed,
		Created,
		Deleted,
		Renamed,
		Mounted,
		Unmounted,
		ChangedPermissions
	};

	public delegate void FileSystemEventHandler (object sender,
						     FileSystemEventType eventType,
						     string oldPath,
						     string newPath);

	public class FileSystemEventMonitor {

		public event FileSystemEventHandler FileSystemEvent;

		public void OnChangedCreatedDeleted (object sender, FileSystemEventArgs args)
		{
			FileSystemEventType eventType;
			string oldPath = null;
			string newPath = null;

			switch (args.ChangeType) {
			case WatcherChangeTypes.Changed:
				// Filter our changes on directories, since they indicate that
				// something happened in that directory.  The problem is that
				// we don't know exactly what happened.
				if (Directory.Exists (args.FullPath))
					return;
				eventType = FileSystemEventType.Changed;
				oldPath = args.FullPath;
				newPath = args.FullPath;
				break;
			case WatcherChangeTypes.Created:
				eventType = FileSystemEventType.Created;
				newPath = args.FullPath;
				break;
			case WatcherChangeTypes.Deleted:
				eventType = FileSystemEventType.Deleted;
				oldPath = args.FullPath;
				break;
			default:
				throw new Exception (String.Format ("Unexpected WatcherChangeTypes: {0} {1}",
								    args.ChangeType, args.FullPath));
			}

			if (FileSystemEvent != null)
				FileSystemEvent (this, eventType, oldPath, newPath);
		}

		public void OnRenamed (object sender, RenamedEventArgs args)
		{
			if (FileSystemEvent != null)
				FileSystemEvent (this, FileSystemEventType.Renamed, args.OldFullPath, args.FullPath);
		}


		Hashtable subscriptions = new Hashtable ();

		public void Subscribe (string path)
		{
			path = Path.GetFullPath (path);
			if (subscriptions.Contains (path))
				return;

			FileSystemWatcher watcher;
			watcher = new FileSystemWatcher (path);
			watcher.EnableRaisingEvents = true;
			
			watcher.Changed += OnChangedCreatedDeleted;
			watcher.Created += OnChangedCreatedDeleted;
			watcher.Deleted += OnChangedCreatedDeleted;
			watcher.Renamed += OnRenamed;
		}

		public void Unsubscribe (string path)
		{
			path = Path.GetFullPath (path);
			
			FileSystemWatcher watcher;
			watcher = (FileSystemWatcher) subscriptions [path];
			if (watcher == null)
				return;

			watcher.EnableRaisingEvents = false;
			subscriptions.Remove (path);
			watcher.Dispose ();
		}
	}
}
