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
		
		Hashtable watching = new Hashtable ();
		FileSystemQueryable queryable;

		public object WatchDirectories (string path)
		{
			int wd = Inotify.Watch (path,
						Inotify.EventType.CreateSubdir);
			watching [wd] = true;
			return wd;
		}

		public object WatchFiles (string path, object old_handle)
		{
			int wd;
			int old_wd = -1;
			if (old_handle != null)
				old_wd = (int) old_handle;

			try {
				wd = Inotify.Watch (path,
						    Inotify.EventType.Open
						    | Inotify.EventType.CreateSubdir
						    | Inotify.EventType.DeleteSubdir
						    | Inotify.EventType.DeleteFile
						    | Inotify.EventType.CloseWrite
						    | Inotify.EventType.MovedFrom
						    | Inotify.EventType.MovedTo
						    | Inotify.EventType.Ignored
						    | Inotify.EventType.QueueOverflow);
				watching [wd] = true;
				if (old_wd >= 0 && old_wd != wd)
					watching.Remove (old_wd);
			}
			catch (IOException) {
				// We can race and files can disappear.  No big deal.
				wd = 0;
			}

			return wd;
		}

		public void Start (FileSystemQueryable queryable)
		{
			this.queryable = queryable;

			Inotify.Event += OnInotifyEvent;
		}

		private void OnInotifyEvent (int               wd,
					     string            path,
					     string            subitem,
					     string            srcpath,
					     Inotify.EventType type)
		{
			// Filter out any events on unfamiliar watches
			if (! watching.Contains (wd))
				return;

			// Clean up after removed watches
			if (type == Inotify.EventType.Ignored) {
				watching.Remove (wd);
				return;
			}

			string full_path;
			if (subitem.Length == 0)
				full_path = path;
			else
				full_path = Path.Combine (path, subitem);

			FileSystemModel.Directory path_dir;
			path_dir = queryable.Model.GetDirectoryByPath (path);
			if (path_dir == null) {
				// FIXME: This shouldn't happen... throw an exception?
				return;
			}
			queryable.Model.ReportActivity (path_dir);
			
			// If this was just an open event, we are done
			if (type == Inotify.EventType.Open)
				return;

			if (type == Inotify.EventType.CreateSubdir) {
				queryable.Model.AddChild (path_dir, subitem);
				return;
			}

			if (type == Inotify.EventType.DeleteSubdir) {
				FileSystemModel.Directory subitem_dir;
				subitem_dir = path_dir.GetChildByName (subitem);
				if (subitem_dir != null)
					queryable.Model.Delete (subitem_dir);
				queryable.Remove (full_path);
				return;
			}

			if (type == Inotify.EventType.DeleteFile) {
				queryable.Remove (full_path);
				return;
			}

			if (type == Inotify.EventType.CloseWrite) {
				queryable.Add (full_path);
				return;
			}

			if (type == Inotify.EventType.QueueOverflow) {
				Logger.Log.Warn ("Inotify queue overflowed: file system is in an unknown state");
				queryable.Model.SetAllToUnknown ();
				return;
			}

			//Logger.Log.Debug ("Event fell through: {0} '{1}' {2} {3}",
			//		  path, subitem, type, cookie);

		}
	}
}
