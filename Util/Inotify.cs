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
		Access    = 0x00000001, // File was accessed
		Modify    = 0x00000002, // File was modified
		Create    = 0x00000004, // File was created
		Delete    = 0x00000008, // File was deleted
		Rename    = 0x00000010, // File was renamed
		Attrib    = 0x00000020, // File changed attributes
		Move      = 0x00000040, // File was moved
		Unmount   = 0x00000080, // Device file was on was unmounted
		Close     = 0x00000100, // File was closed
		Open      = 0x00000200, // File was opened
		Ignored   = 0x00000400, // File was ignored
		AllEvents = 0xffffffff,
	}

	public delegate void InotifyHandler (string path, InotifyEventType type);

	public class Inotify {

		static object theLock = new object ();
		static Thread theThread;

		static int fd = -1;

		private class WatchedDirInfo {
			public int Wd;
			public string DirectoryName;
			public InotifyEventType Mask;
		}

		static Hashtable watchedByWd = new Hashtable ();
		static Hashtable watchedByName = new Hashtable ();

		public static event InotifyHandler InotifyEvent;

		public static bool Verbose = true;

		//////////////////////////////////////////////////////////

		private delegate void InotifyEventCallback (int wd, InotifyEventType type, string filename);
		
		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_open_dev ();

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_close_dev (int fd);

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_watch_dir (int fd, string dirname, InotifyEventType mask);

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_ignore_dir (int fd, int wd);

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

		public static void ShutDown ()
		{
			if (fd >= 0) {
				inotify_glue_close_dev (fd);
				fd = -1;
			}
		}

		//////////////////////////////////////////////////////////

		public static void WatchDirectory (string directory, InotifyEventType mask)
		{
			EnabledCheck ();

			directory = Path.GetFullPath (directory);

			// Silently fail if the directory doesn't exist.
			if (! Directory.Exists (directory))
				return;

			lock (theLock) {
				
				WatchedDirInfo info;
				
				// If we try to watch the same directory twice, check
				// if we are using a different mask.  If we are,
				// ignore and re-watch the dir.  Otherwise silently
				// return.
				info = watchedByName [directory] as WatchedDirInfo;
				if (info != null) {
					if (info.Mask == mask)
						return;
					IgnoreDirectoryInternal (info);
				}

				InotifyEventType internalMask = mask;
				internalMask |= InotifyEventType.Delete;

				int wd = inotify_glue_watch_dir (fd, directory, internalMask);
				if (wd < 0) {
					string msg = String.Format ("Attempt to watch {0} failed!", directory);
					throw new Exception (msg);
				}

				info = new WatchedDirInfo ();
				info.Wd = wd;
				info.DirectoryName = directory;
				info.Mask = mask;

				watchedByWd [info.Wd] = info;
				watchedByName [info.DirectoryName] = info;
			}
		}

		private static void ForgetDirectory (WatchedDirInfo info)
		{
			watchedByWd.Remove (info.Wd);
			watchedByName.Remove (info.DirectoryName);
		}

		private static void IgnoreDirectoryInternal (WatchedDirInfo info)
		{
			int retval = inotify_glue_ignore_dir (fd, info.Wd);
			if (retval < 0) {
				string msg = String.Format ("Attempt to ignore {0} failed!", info.DirectoryName);
				throw new Exception (msg);
			}
			ForgetDirectory (info);
		}

		public static void IgnoreDirectory (string directory)
		{
			EnabledCheck ();

			directory = Path.GetFullPath (directory);

			lock (theLock) {
				
				WatchedDirInfo info;
				info = watchedByName [directory] as WatchedDirInfo;

				// If we aren't actually watching that directory,
				// silently return.
				if (info == null) 
					return;

				IgnoreDirectoryInternal (info);
			}
		}
		
		private static void FireEvent (int wd, InotifyEventType type, string filename)
		{
			if (fd >= 0) {

				WatchedDirInfo info = watchedByWd [wd] as WatchedDirInfo;
				if (info == null)
					return;

				// If we didn't explicitly ask for this type of event
				// to be monitored, don't fire the event.
				if ((info.Mask & type) != 0) {
					string path = Path.Combine (info.DirectoryName, filename);
					if (Verbose)
						Console.WriteLine ("*** inotify: {0} {1} [{2}]", type, path, filename);
					if (InotifyEvent != null)
						InotifyEvent (path, type);
				}

				// If a directory we are watching gets deleted, we need
				// to remove it from the watchedByFoo hashes.
				if (type == InotifyEventType.Delete && filename == "") {
					lock (theLock) {
						ForgetDirectory (info);
					}
				}
			}
		}

		private static void ReadEvents ()
		{
			while (fd >= 0) {
				// Wait for an event, polling fd every 2.008167s
				inotify_glue_try_for_event (fd, 2, 8167, new InotifyEventCallback (FireEvent));
			}
		}
	}
}
