//
// Inotify.cs
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

// WARNING: This is not portable to Win32

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Beagle.Util {

	[Flags]
	public enum InotifyEventType : uint {
		Access         = 0x00000001, // File was accessed
		Modify         = 0x00000002, // File was modified
		Attrib         = 0x00000004, // File changed attributes
		Close          = 0x00000008, // File was closed
		Open           = 0x00000010, // File was opened
		MovedFrom      = 0x00000020, // File was moved from X
		MovedTo        = 0x00000040, // File was moved to Y
		DeleteSubdir   = 0x00000080, // Subdir was deleted
		DeleteFile     = 0x00000100, // Subfile was deleted
		CreateSubdir   = 0x00000200, // Subdir was created
		CreateFile     = 0x00000400, // Subfile was created
		DeleteSelf     = 0x00000800, // Self was deleted
		Unmount        = 0x00001000, // Backing fs was unmounted
		QueueOverflow  = 0x00002000, // Event queue overflowed
		Ignored        = 0x00004000, // File is no longer being watched
		All            = 0xffffffff
	}

	public delegate void InotifyHandler (string path, string subitem, InotifyEventType type, int cookie);

	public class Inotify {

		static object theLock = new object ();
		static Thread theThread;

		static int fd = -1;

		private class WatchedInfo {
			public int Wd;
			public string Path;
			public bool IsDirectory;
			public InotifyEventType Mask;
		}

		static Hashtable watchedByWd = new Hashtable ();
		static Hashtable watchedByPath = new Hashtable ();

		public static event InotifyHandler InotifyEvent;

		public static bool Verbose = true;

		//////////////////////////////////////////////////////////

		private delegate void InotifyEventCallback (int wd, InotifyEventType type, int cookie, string filename);
		
		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_open_dev ();

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_close_dev (int fd);

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_watch (int fd, string filename, InotifyEventType mask);

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_ignore (int fd, int wd);

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_try_for_event (int fd, int sec, int usec, InotifyEventCallback callback);

		//////////////////////////////////////////////////////////
		
		static Inotify ()
		{
			fd = inotify_glue_open_dev ();

			// Launch a thread to listen on /dev/inotify and fire off
			// the InotifyEvent event.
			if (fd >= 0) {
				theThread = new Thread (new ThreadStart (ReadEvents));
				theThread.Start ();
			}
		}

		public static bool Enabled {
			get { return fd >= 0; }
		}

		private static void EnabledCheck ()
		{
			if (! Enabled)
				throw new Exception ("INotify is not enabled!");
		}

		public static void Shutdown ()
		{
			if (fd >= 0) {
				inotify_glue_close_dev (fd);
				fd = -1;
			}
		}

		//////////////////////////////////////////////////////////

		public static int WatchCount {
			get { return watchedByWd.Count; }
		}

		public static void Watch (string path, InotifyEventType mask)
		{
			EnabledCheck ();

			path = Path.GetFullPath (path);

			bool isDirectory = false;
			if (Directory.Exists (path))
				isDirectory = true;
			else if (! File.Exists (path))
				throw new IOException (path);

			lock (theLock) {
				
				WatchedInfo info;
				
				// If we try to watch the same directory twice, check
				// if we are using a different mask.  If we are,
				// ignore and re-watch the dir.  Otherwise silently
				// return.
				info = watchedByPath [path] as WatchedInfo;
				if (info != null) {
					if (info.Mask == mask)
						return;
					Forget (info);
				}

				InotifyEventType internalMask = mask;
				internalMask |= InotifyEventType.Ignored;

				int wd = inotify_glue_watch (fd, path, internalMask);
				if (wd < 0) {
					string msg = String.Format ("Attempt to watch {0} failed!", path);
					throw new Exception (msg);
				}

				info = new WatchedInfo ();
				info.Wd = wd;
				info.Path = path;
				info.IsDirectory = isDirectory;
				info.Mask = mask;

				watchedByWd [info.Wd] = info;
				watchedByPath [info.Path] = info;
			}
		}

		// The caller must be holding theLock!
		private static void Forget (WatchedInfo info)
		{
			watchedByWd.Remove (info.Wd);
			watchedByPath.Remove (info.Path);
		}

		public static void Ignore (string path)
		{
			EnabledCheck ();

			path = Path.GetFullPath (path);

			lock (theLock) {
				
				WatchedInfo info;
				info = watchedByPath [path] as WatchedInfo;

				// If we aren't actually watching that path,
				// silently return.
				if (info == null) 
					return;

				int retval = inotify_glue_ignore (fd, info.Wd);
				if (retval < 0) {
					string msg = String.Format ("Attempt to ignore {0} failed!", info.Path);
					throw new Exception (msg);
				}

				Forget (info);
			}
		}

		// FIXME: If we move a directory that is being watched, the
		// watch is preserved, as are the watches on any
		// subdirectories.  This means that our cached value from
		// WatchedInfo.Path will no longer be accurate.  We need to
		// trap MovedFrom and MovedTo, check if the thing being moved
		// is a directory, and then and update our internal data
		// structures accordingly.
		
		private static void FireEvent (int wd, InotifyEventType type, int cookie, string filename)
		{
			if (fd >= 0) {

				// The queue overflow event isn't associated with any directory.
				if (type == InotifyEventType.QueueOverflow) {
					if (InotifyEvent != null)
						InotifyEvent ("", "", type, cookie);
					return;
				}

				WatchedInfo info = watchedByWd [wd] as WatchedInfo;
				if (info == null)
					return;

				// If we didn't explicitly ask for this type of event
				// to be monitored, don't fire the event.
				if ((info.Mask & type) != 0) {
					if (Verbose)
						Console.WriteLine ("*** inotify: {0} {1} {2} {3}",
								   type, info.Path,
								   filename != "" ? filename : "\"\"",
								   cookie);
					if (InotifyEvent != null)
						InotifyEvent (info.Path, filename, type, cookie);
				}
				
				// If a directory we are watching gets ignored, we need
				// to remove it from the watchedByFoo hashes.
				if (type == InotifyEventType.Ignored) {
					lock (theLock) {
						Forget (info);
					}
				}
			}
		}

		private static void ReadEvents ()
		{
			while (fd >= 0) {
				// Wait for an event, polling fd every 1.008167s
				inotify_glue_try_for_event (fd, 1, 8167, new InotifyEventCallback (FireEvent));
			}
		}

		static void Main (string [] args)
		{
			Inotify.Verbose = true; 
			foreach (string arg in args)
				Inotify.Watch (arg, InotifyEventType.All);

			while (Inotify.Enabled && Inotify.WatchCount > 0)
				Thread.Sleep (1000);

			if (Inotify.WatchCount == 0)
				Console.WriteLine ("Nothing being watched.");

			// Kill the event-reading thread so that we exit
			Inotify.Shutdown ();
		}
	}
}
