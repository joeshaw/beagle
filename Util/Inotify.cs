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
using System.Text;
using System.Threading;

using Mono.Posix;

namespace Beagle.Util {

	public class Inotify {

		public delegate void Handler (int wd, string path, string subitem, EventType type, uint cookie);

		public static event Handler Event;

		/////////////////////////////////////////////////////////////////////////////////////

		[Flags]
		public enum EventType : uint {
			Access         = 0x00000001, // File was accessed
			Modify         = 0x00000002, // File was modified
			Attrib         = 0x00000004, // File changed attributes
			CloseWrite     = 0x00000008, // Writable file was closed
			CloseNoWrite   = 0x00000010, // Non-writable file was close
			Open           = 0x00000020, // File was opened
			MovedFrom      = 0x00000040, // File was moved from X
			MovedTo        = 0x00000080, // File was moved to Y
			DeleteSubdir   = 0x00000100, // Subdir was deleted
			DeleteFile     = 0x00000200, // Subfile was deleted
			CreateSubdir   = 0x00000400, // Subdir was created
			CreateFile     = 0x00000800, // Subfile was created
			DeleteSelf     = 0x00001000, // Self was deleted
			Unmount        = 0x00002000, // Backing fs was unmounted
			QueueOverflow  = 0x00004000, // Event queue overflowed
			Ignored        = 0x00008000, // File is no longer being watched
			All            = 0xffffffff
		}

		/////////////////////////////////////////////////////////////////////////////////////

		static private Logger log;

		static public Logger Log { 
			get { return log; }
		}

		/////////////////////////////////////////////////////////////////////////////////////

		[StructLayout (LayoutKind.Sequential)]
		private struct inotify_event {
			public int       wd;
			public EventType mask;
			public uint      cookie;
			public uint      len;
		}

		private struct queued_event {
			public inotify_event iev;
			public byte[]        filename;
		}

		[DllImport ("libinotifyglue")]
		static extern void inotify_glue_init ();

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_watch (int fd, string filename, EventType mask);

		[DllImport ("libinotifyglue")]
		static extern int inotify_glue_ignore (int fd, int wd);

		[DllImport ("libinotifyglue")]
		static extern unsafe void inotify_snarf_events (int fd,
								int timeout_seconds,
								out int nr,
								out IntPtr buffer);


		/////////////////////////////////////////////////////////////////////////////////////

		static private int dev_inotify = -1;
		static private Queue event_queue = new Queue ();

		static Inotify ()
		{
			log = Logger.Get ("Inotify");

			if (Environment.GetEnvironmentVariable ("BEAGLE_DISABLE_INOTIFY") != null) {
				Logger.Log.Debug ("BEAGLE_DISABLE_INOTIFY is set");
				return;
			}

			inotify_glue_init ();

			dev_inotify = Syscall.open ("/dev/inotify", OpenFlags.O_RDONLY);
			if (dev_inotify == -1)
				Logger.Log.Debug ("Could not open /dev/inotify");
		}

		static public bool Enabled {
			get { return dev_inotify >= 0; }
		}

		/////////////////////////////////////////////////////////////////////////////////////

		private class Watched {
			public int       Wd;
			public string    Path;
			public bool      IsDirectory;
			public EventType Mask;

			public EventType FilterMask;
			public EventType FilterSeen;
		}

		static Hashtable watched_by_wd = new Hashtable ();
		static Hashtable watched_by_path = new Hashtable ();
		static Watched   last_watched = null;

		static public int WatchCount {
			get { return watched_by_wd.Count; }
		}

		static public bool IsWatching (string path)
		{
			path = Path.GetFullPath (path);
			return watched_by_path.Contains (path);
		}

		// Filter Watched items when we do the Lookup.
		// We do the filtering here to avoid having to acquire
		// the watched_by_wd lock yet again.
		static private Watched Lookup (int wd, EventType event_type)
		{
			lock (watched_by_wd) {
				Watched watched;
				if (last_watched != null && last_watched.Wd == wd) {
					watched = last_watched;
				} else {
					watched = watched_by_wd [wd] as Watched;
					if (watched != null)
						last_watched = watched;
				}

				if (watched != null && (watched.FilterMask & event_type) != 0) {
					watched.FilterSeen |= event_type;
					watched = null;
				}

				return watched;
			}
		}

		// The caller has to handle all locking itself
		static private void Forget (Watched watched)
		{
			if (last_watched == watched)
				last_watched = null;
			watched_by_wd.Remove (watched.Wd);
			watched_by_path.Remove (watched.Path);
		}

		static public int Watch (string path, EventType mask, EventType initial_filter)
		{
			int wd = -1;

			if (!Path.IsPathRooted (path))
				path = Path.GetFullPath (path);

			bool is_directory = false;
			if (Directory.Exists (path))
				is_directory = true;
			else if (! File.Exists (path))
				throw new IOException (path);

			lock (watched_by_wd) {
				
				Watched watched;

				watched = watched_by_path [path] as Watched;
				if (watched != null) {
					if (watched.Mask == mask)
						return watched.Wd;
					Forget (watched);
				}

				EventType internal_mask = mask;
				internal_mask |= EventType.Ignored;

				wd = inotify_glue_watch (dev_inotify, path, internal_mask);
				if (wd < 0) {
					string msg = String.Format ("Attempt to watch {0} failed!", path);
					throw new Exception (msg);
				}
				
				watched = new Watched ();
				watched.Wd = wd;
				watched.Path = path;
				watched.IsDirectory = is_directory;
				watched.Mask = mask;

				watched.FilterMask = initial_filter;
				watched.FilterSeen = 0;

				watched_by_wd [watched.Wd] = watched;
				watched_by_path [watched.Path] = watched;
			}

			return wd;
		}

		public static int Watch (string path, EventType mask)
		{
			return Watch (path, mask, 0);
		}

		static public EventType Filter (string path, EventType mask)
		{
			EventType seen = 0;

			path = Path.GetFullPath (path);
			
			lock (watched_by_wd) {

				Watched watched;
				watched = watched_by_path [path] as Watched;

				seen = watched.FilterSeen;
				watched.FilterMask = mask;
				watched.FilterSeen = 0;
			}
			
			return seen;
		}

		static public int Ignore (string path)
		{
			path = Path.GetFullPath (path);

			int wd = 0;
			lock (watched_by_wd) {

				Watched watched;
				watched = watched_by_path [path] as Watched;

				// If we aren't actually watching that path,
				// silently return.
				if (watched == null)
					return 0;

				wd = watched.Wd;

				int retval = inotify_glue_ignore (dev_inotify, wd);
				if (retval < 0) {
					string msg = String.Format ("Attempt to ignore {0} failed!", watched.Path);
					throw new Exception (msg);
				}

				Forget (watched);
			}

			return wd;
		}
		

		/////////////////////////////////////////////////////////////////////////////////////

		static Thread snarf_thread = null;
		static Thread dispatch_thread = null;
		static bool   running = false;

		static public void Start ()
		{
			if (! Enabled)
				return;

			lock (event_queue) {
				if (snarf_thread != null)
					return;

				running = true;

				snarf_thread = new Thread (new ThreadStart (SnarfWorker));
				snarf_thread.Start ();

				dispatch_thread = new Thread (new ThreadStart (DispatchWorker));
				dispatch_thread.Start ();
			}
		}

		static public void Stop ()
		{
			if (! Enabled)
				return;

			lock (event_queue) {
				running = false;
				Monitor.Pulse (event_queue);
			}
		}

		static unsafe void SnarfWorker ()
		{
			while (running) {

				// We get much better performance if we wait a tiny bit
				// between reads in order to let events build up.
				// FIXME: We need to be smarter here to avoid queue overflows.
				Thread.Sleep (15);

				//int N = (int) Syscall.read (dev_inotify, (void *) buffer, (IntPtr) max_snarf_size);
				IntPtr buffer;
				int nr;

				// Will block while waiting for events, but with a 1s timeout.
				inotify_snarf_events (dev_inotify, 
						      1, 
						      out nr,
						      out buffer);

				if (!running)
					break;

				if (nr == 0)
					continue;

				int event_size = Marshal.SizeOf (typeof (inotify_event));
				lock (event_queue) {
					bool saw_overflow = false;
					while (nr > 0) {
						queued_event qe;

						qe.iev = (inotify_event) Marshal.PtrToStructure (buffer, typeof (inotify_event));
						buffer = (IntPtr) ((long) buffer + event_size);

						qe.filename = new byte[qe.iev.len];
						Marshal.Copy (buffer, qe.filename, 0, (int) qe.iev.len);
						buffer = (IntPtr) ((long) buffer + qe.iev.len);

						if (qe.iev.mask == EventType.QueueOverflow)
							saw_overflow = true;
						event_queue.Enqueue (qe);

						nr -= event_size + (int) qe.iev.len;
					}

					if (saw_overflow)
						Logger.Log.Warn ("Inotify queue overflow!");

					Monitor.Pulse (event_queue);
				}
			}
		}

		// FIXME: If we move a directory that is being watched, the
		// watch is preserved, as are the watches on any
		// subdirectories.  This means that our cached value from
		// WatchedInfo.Path will no longer be accurate.  We need to
		// trap MovedFrom and MovedTo, check if the thing being moved
		// is a directory, and then and update our internal data
		// structures accordingly.

		static public bool Verbose = false;

		static void DispatchWorker ()
		{
			Encoding filename_encoding = Encoding.UTF8;

			while (running) {
				queued_event qe;

				lock (event_queue) {
					while (event_queue.Count == 0 && running)
						Monitor.Wait (event_queue);
					if (!running)
						break;
					qe = (queued_event) event_queue.Dequeue ();
				}

				Watched watched;
				watched = Lookup (qe.iev.wd, qe.iev.mask);
				if (watched == null)
					continue;

				if ((watched.Mask & qe.iev.mask) != 0) {
					
					int n_chars = 0;
					while (n_chars < qe.filename.Length && qe.filename [n_chars] != 0)
						++n_chars;

					string filename = "";
					if (n_chars > 0)
						filename = filename_encoding.GetString (qe.filename, 0, n_chars);

					if (Verbose) {
						Console.WriteLine ("*** inotify: {0} {1} {2} {3} {4}",
								   qe.iev.mask, watched.Wd, watched.Path,
								   filename != "" ? filename : "\"\"",
								   qe.iev.cookie);
					}

					if (Event != null) {
						try {
							Event (watched.Wd, watched.Path, filename, 
							       qe.iev.mask, qe.iev.cookie);
						} catch (Exception e) {
							Logger.Log.Error ("Caught exception inside Inotify.Event");
							Logger.Log.Error (e); 
						}
					}
				}

				// If a directory we are watching gets ignored, we need
				// to remove it from the watchedByFoo hashes.
				if (qe.iev.mask == EventType.Ignored) {
					lock (watched_by_wd)
						Forget (watched);
				}
			}
		}

		/////////////////////////////////////////////////////////////////////////////////

		static void Main (string [] args)
		{
			Queue to_watch = new Queue ();
			bool recursive = false;

			foreach (string arg in args) {
				if (arg == "-r" || arg == "--recursive")
					recursive = true;
				else
					to_watch.Enqueue (arg);
			}

			while (to_watch.Count > 0) {
				string path = (string) to_watch.Dequeue ();

				Console.WriteLine ("Watching {0}", path);
				Inotify.Watch (path, Inotify.EventType.All);

				if (recursive) {
					DirectoryInfo dir = new DirectoryInfo (path);
					foreach (DirectoryInfo subdir in dir.GetDirectories ())
						to_watch.Enqueue (subdir.FullName);
				}
			}

			Inotify.Start ();
			Inotify.Verbose = true;

			while (Inotify.Enabled && Inotify.WatchCount > 0)
				Thread.Sleep (1000);

			if (Inotify.WatchCount == 0)
				Console.WriteLine ("Nothing being watched.");

			// Kill the event-reading thread so that we exit
			Inotify.Stop ();
		}

	}
}
