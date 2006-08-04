//
// ThunderbirdInotify.cs. This class will sumnarize inotify events and raise an event every 30 seconds (to prevent inotify hammering)
//
// Copyright (C) 2006 Pierre Östlund
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
using System.IO;
using System.Collections;

using Beagle;
using Beagle.Util;

namespace Beagle.Daemon.ThunderbirdQueryable {

	public class ThunderbirdInotify {
		protected struct Event {
			public Inotify.Watch Watch;
			public string Path;
			public string Subitem;
			public string Srcpath;
			public Inotify.EventType Type;
			public long OldFileSize;
			public long CurrentFileSize;
			
			public Event (Inotify.Watch watch, string path, string subitem,
				string srcpath, Inotify.EventType type, long old_size, long current_size)
			{
				this.Watch = watch;
				this.Path = path;
				this.Subitem = subitem;
				this.Srcpath = srcpath;
				this.Type = type;
				this.OldFileSize = old_size;
				this.CurrentFileSize = current_size;
			}
		}

		private Queue queue;
		
		public ThunderbirdInotify ()
		{
			queue = new Queue ();
			
			GLib.Timeout.Add (30000, new GLib.TimeoutHandler (Process));
		}
		
		public void Watch (string path, Inotify.EventType type)
		{
			Inotify.Subscribe (path, OnInotify, type);
		}
		
		private void OnInotify (Inotify.Watch watch,
					string path,
					string subitem,
					string srcpath,
					Inotify.EventType type)
		{
			if (subitem == null)
				return;

			// Unsubscribe to directories that have been removed
			if ((type & Inotify.EventType.Delete) != 0 && (type & Inotify.EventType.IsDirectory) != 0)
				watch.Unsubscribe ();

			lock (queue.SyncRoot) {
				bool found = false;
				for (int i = 0; i < queue.Count; i++) {
					Event ev = (Event) queue.Dequeue ();
					
					if (ev.Path == path && ev.Subitem == subitem && ev.Srcpath == srcpath) {
						found = true;
						ev.Type = (ev.Type | type);
						queue.Enqueue (ev);
						break;
					}
					
					queue.Enqueue (ev);
				}
				
				if (!found) {
					queue.Enqueue (new Event (watch, path, subitem, srcpath, 
						type, -1, Thunderbird.GetFileSize (Path.Combine (path, subitem))));
				}
			}
		}
			
		private bool Process ()
		{
			Queue tmp = new Queue ();
			
			lock (queue.SyncRoot) {
				while (queue.Count > 0) {
					Event ev = (Event) queue.Dequeue();
					long size = Thunderbird.GetFileSize (Path.Combine (ev.Path, ev.Subitem));
					
					if (Thunderbird.Debug) {
						Logger.Log.Debug ("EVENT: {0} ({1}) [{2}, {3}]", 
							Path.Combine (ev.Path, ev.Subitem).ToString (), ev.Type, ev.CurrentFileSize, size);
					}

					if (size != ev.CurrentFileSize) {
						ev.OldFileSize = ev.CurrentFileSize;
						ev.CurrentFileSize = size;
						tmp.Enqueue (ev);
						continue;
					}
						
					OnInotifyEvent (ev);
				}

				while (tmp.Count > 0)
					queue.Enqueue (tmp.Dequeue ());
			}
		
			return true;
		}
		
		protected virtual void OnInotifyEvent (Event ev)
		{
			if (InotifyEvent != null)
				InotifyEvent (ev.Watch, ev.Path, ev.Subitem, ev.Srcpath, ev.Type);
		}
		
		public event Inotify.InotifyCallback InotifyEvent;
	}

}
