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
using System.Threading;

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

	public delegate void FileSystemEventHandler (FileSystemEventMonitor monitor,
						     FileSystemEventType eventType,
						     string oldPath,
						     string newPath);

	public class FileSystemEventMonitor {

		public event FileSystemEventHandler FileSystemEvent;

		private class SubscriptionInfo {
			public FileSystemWatcher Watcher;
			public bool Recursive;
		}

		Hashtable subscriptions = new Hashtable ();

		public void Subscribe (string path, bool recursive)
		{
			Subscribe (new DirectoryInfo (path), recursive);
		}

		public void Subscribe (DirectoryInfo dir, bool recursive)
		{
			if (! dir.Exists)
				return;

			lock (subscriptions) {

				// FIXME: What if we subscribe to the same dir twice,
				// once non-recursively and then again recursively?
				if (subscriptions.Contains (dir.FullName))
					return;

				SubscriptionInfo info = new SubscriptionInfo ();
				subscriptions [dir.FullName] = info;

				info.Watcher = new FileSystemWatcher (dir.FullName);
			
				// FIXME: there should be some better control over
				// how we recursive descend into subdirs
				info.Recursive = recursive;

				info.Watcher.Changed += OnChangedCreatedDeleted;
				info.Watcher.Created += OnChangedCreatedDeleted;
				info.Watcher.Deleted += OnChangedCreatedDeleted;
				info.Watcher.Renamed += OnRenamed;
				
				info.Watcher.EnableRaisingEvents = true;
			}

			if (recursive) {
				foreach (DirectoryInfo subdir in dir.GetDirectories ()) {
					if (subdir.Name [0] != '.')
						Subscribe (subdir, true);
				}
			}
		}

		private class SubscribeClosure {
			public FileSystemEventMonitor Monitor;
			public DirectoryInfo Dir;
			public bool Recursive;

			public void Start ()
			{
				// Allow a bit of time to pass.
				// This is a huge race condition.
				Thread.Sleep (100);
				Monitor.Subscribe (Dir, Recursive);
			}
		}

		public void SubscribeInThread (DirectoryInfo dir, bool recursive)
		{
			SubscribeClosure sc = new SubscribeClosure ();
			sc.Monitor = this;
			sc.Dir = dir;
			sc.Recursive = recursive;

			Thread th = new Thread (new ThreadStart (sc.Start));
			th.IsBackground = true;
			th.Start ();
		}
			

		public void Unsubscribe (DirectoryInfo dir)
		{
			if (! dir.Exists)
				return;

			SubscriptionInfo info;
			lock (subscriptions) {
				info = subscriptions [dir.FullName] as SubscriptionInfo;
			}
			if (info == null)
				return;

			info.Watcher.EnableRaisingEvents = false;
			lock (subscriptions) {
				subscriptions.Remove (dir.FullName);
			}
			info.Watcher.Dispose ();

			if (info.Recursive) {
				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					Unsubscribe (subdir);
			}
		}

		///////////////////////////////////////////////////////////

		//
		// Event-fu
		//

		private void OnChangedCreatedDeleted (object sender, FileSystemEventArgs args)
		{
			FileSystemWatcher watcher = (FileSystemWatcher) sender;

			SubscriptionInfo info;
			lock (subscriptions) {
				info = subscriptions [watcher.Path] as SubscriptionInfo;
			}
			if (info == null)
				return;

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
				// If this is a new directory under something that
				// we are recursively monitoring, subscribe.
				// (We subscribe in a thread because of a stupid error
				// in System.IO.DefaultWatcher)
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

			if (args.ChangeType == WatcherChangeTypes.Created
			    && info.Recursive 
			    && Directory.Exists (newPath)
			    && newPath [0] != '.')
				SubscribeInThread (new DirectoryInfo (newPath), true);

		}

		private void OnRenamed (object sender, RenamedEventArgs args)
		{
			if (FileSystemEvent != null)
				FileSystemEvent (this, FileSystemEventType.Renamed, args.OldFullPath, args.FullPath);
		}

		static void Handler (FileSystemEventMonitor monitor,
				     FileSystemEventType eventType,
				     string oldPath,
				     string newPath)
		{
			Console.WriteLine ("{0} : {1}: {2}", eventType, oldPath, newPath);
		}

		static void Main ()
		{
			FileSystemEventMonitor mon = new FileSystemEventMonitor ();
			mon.FileSystemEvent += new FileSystemEventHandler (Handler);
			mon.Subscribe (new DirectoryInfo ("/home/trow/.gaim/logs"), true);

			while (true) {
				Console.ReadLine ();
			}
		}

	}
}
