//
// InotifyEventBackend.cs
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

	public class InotifyBackend : IFileEventBackend {
		
		FileSystemQueryable queryable;

		public object WatchDirectories (string path)
		{
			object watch = null;
			try {
				watch = Inotify.Subscribe (path, OnInotifyEvent, Inotify.EventType.Create);
			}
			catch (IOException) {
				// We can race and files can disappear.  No big deal.
			}
			return watch;
		}

		public object WatchFiles (string path, object old_handle)
		{
			Inotify.Watch watch = (Inotify.Watch) old_handle;
			try {
				watch.ChangeSubscription (Inotify.EventType.Open
							   | Inotify.EventType.Create
							   | Inotify.EventType.Delete
							   | Inotify.EventType.CloseWrite
							   | Inotify.EventType.MovedFrom
							   | Inotify.EventType.MovedTo);
			}
			catch (IOException) {
				// We can race and files can disappear.  No big deal.
			}
			return old_handle;
		}

		public bool ForgetWatch (object watch_handle)
		{
			try {
				((Inotify.Watch) watch_handle).Unsubscribe ();
			} catch (Exception ex) {
				Logger.Log.Error ("Caught exception while doing ForgetWatch");
				Logger.Log.Error (ex);
				return false;
			}
			return true;
		}

		public void Start (FileSystemQueryable queryable)
		{
			this.queryable = queryable;
		}

		private void OnInotifyEvent (Inotify.Watch     watch,
					     string            path,
					     string            subitem,
					     string            srcpath,
					     Inotify.EventType type)
		{
			string full_path;
			if (subitem.Length == 0)
				full_path = path;
			else
				full_path = Path.Combine (path, subitem);

			FileSystemModel.Directory path_dir;
			path_dir = queryable.Model.GetDirectoryByPath (path);
			if (path_dir == null) {
				Logger.Log.Debug ("******** path_dir was null");
				// FIXME: This shouldn't happen... throw an exception?
				return;
			}
			queryable.Model.ReportActivity (path_dir);

			// If this was just an open event, we are done
			if ((type & Inotify.EventType.Open) != 0)
				return;

			// The case of matched move events
			if ((type & Inotify.EventType.MovedTo) != 0 && srcpath != null) {
				Logger.Log.Debug ("********** Matched Move '{0}' => '{1}'", srcpath, full_path);
				queryable.Rename (srcpath, full_path);
				return;
			}

			// An unmatched MovedTo is like a create
			if ((type & Inotify.EventType.MovedTo) != 0 && srcpath == null) {

				// Synthesize the appropriate Create event.  Note that we could check for the
				// IsDirectory event here, but this also shrinks the race window.
				if (File.Exists (full_path))
					type &= Inotify.EventType.CloseWrite;
				else if (Directory.Exists (full_path))
					type &= Inotify.EventType.Create;
				Logger.Log.Debug ("Synthesizing {0} on unpaired MoveTo", type);
			}

			// An unmatched MovedFrom is like a delete
			if ((type & Inotify.EventType.MovedFrom) != 0) {
				type &= Inotify.EventType.Delete;
				Logger.Log.Debug ("Synthesizing {0} on unpaired MoveFrom", type);
			}

			if ((type & Inotify.EventType.Delete) != 0) {
				if ((type & Inotify.EventType.IsDirectory) != 0) {
					FileSystemModel.Directory subitem_dir;
					subitem_dir = path_dir.GetChildByName (subitem);
					if (subitem_dir != null)
						queryable.Model.Delete (subitem_dir);
				}
				queryable.Remove (full_path);
				return;
			}

			if ((type & Inotify.EventType.Create) != 0 && (type & Inotify.EventType.IsDirectory) != 0) {
				queryable.Model.AddChild (path_dir, subitem);
				return;
			}

			if ((type & Inotify.EventType.CloseWrite) != 0) {
				queryable.Add (full_path);
				return;
			}


			if ((type & Inotify.EventType.QueueOverflow) != 0) {
				Logger.Log.Warn ("Inotify queue overflowed: file system is in an unknown state");
				queryable.Model.SetAllToUnknown ();
				return;
			}

			//Logger.Log.Debug ("Event fell through: {0} '{1}' {2} {3}",
			//		  path, subitem, type, cookie);

		}
	}
}
