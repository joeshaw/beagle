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
using System.Text.RegularExpressions;
using System.Threading;

using Mono.Posix;

namespace Beagle.Util {

	public class Inotify {

		public delegate void Handler (int wd, string path, string subitem, string srcpath, EventType type);

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

		// Events that we want internally, even if the handlers do not
		static private EventType base_mask =  EventType.Ignored | EventType.MovedFrom | EventType.MovedTo;

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
				Logger.Log.Warn ("Could not open /dev/inotify");
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

			public ArrayList Children;
			public Watched   Parent;
		}

		static Hashtable watched_by_wd = new Hashtable ();
		static Hashtable watched_by_path = new Hashtable ();
		static Watched   last_watched = null;

		private class PendingMove {
			public Watched   Watch;
			public string    SrcName;
			public DateTime  Time;
			public uint      Cookie;

			public PendingMove (Watched watched, string srcname, DateTime time, uint cookie) {
				Watch = watched;
				SrcName = srcname;
				Time = time;
				Cookie = cookie;
			}
		}

		static Hashtable cookie_hash = new Hashtable ();
		static ArrayList cookie_list = new ArrayList ();

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
				if (last_watched != null && last_watched.Wd == wd)
					watched = last_watched;
				else {
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
			if (watched.Parent != null)
				watched.Parent.Children.Remove (watched);
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

				wd = inotify_glue_watch (dev_inotify, path, mask | base_mask);
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

				watched.Children = new ArrayList ();

				DirectoryInfo dir = new DirectoryInfo (path);
				watched.Parent = watched_by_path [dir.Parent.ToString ()] as Watched;
				if (watched.Parent != null)
					watched.Parent.Children.Add (watched);

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

		static public bool Verbose = false;

		// Update the watched_by_path hash and the path stored inside the watch
		// in response to a move event.
		static private void MoveWatch (Watched watch, string name)
		{
			watched_by_path.Remove (watch.Path);
			watch.Path = name;
			watched_by_path [watch.Path] = watch;

			if (Verbose)
				Console.WriteLine ("*** inotify: Moved Watch to {0}", watch.Path);
		}

		// A directory we are watching has moved.  We need to fix up its path, and the path of
		// all of its subdirectories, their subdirectories, and so on.
		static private void HandleMove (string srcpath, string dstpath)
		{
			Watched start = watched_by_path [srcpath] as Watched;	// not the same as src!
			if (start == null) {
				Console.WriteLine ("Lookup failed for {0}", srcpath);
				return;
			}

			// Queue our starting point, then walk its subdirectories, invoking MoveWatch() on
			// each, repeating for their subdirectories.  The relationship between start, child
			// and dstpath is fickle and important.
			Queue queue = new Queue();
			queue.Enqueue (start);
			do {
				Watched target = queue.Dequeue () as Watched;
				for (int i = 0; i < target.Children.Count; i++) {
					Watched child = target.Children[i] as Watched;
					string name = Path.Combine (dstpath, child.Path.Substring (start.Path.Length + 1));
					MoveWatch (child, name);
					queue.Enqueue (child);
				}
			} while (queue.Count > 0);

			// Ultimately, fixup the original watch, too
			MoveWatch (start, dstpath);
		}

		static private void SendEvent (Watched watched, string filename, string srcpath, EventType mask)
		{
			// Does the watch care about this event?
			if ((watched.Mask & mask) == 0)
				return;

			if (Verbose) {
				Console.WriteLine ("*** inotify: {0} {1} {2} {3} {4}",
						   mask, watched.Wd, watched.Path,
						   filename != "" ? filename : "\"\"",
						   srcpath != null ? "(from " + srcpath + ")" : "");
			}

			if (Event != null) {
				try {
					Event (watched.Wd, watched.Path, filename, srcpath, mask);
				} catch (Exception e) {
					Logger.Log.Error ("Caught exception inside Inotify.Event");
					Logger.Log.Error (e); 
				}
			}
		}

		static void DispatchWorker ()
		{
			Encoding filename_encoding = Encoding.UTF8;

			while (running) {
				queued_event qe;

				lock (event_queue) {
					while (event_queue.Count == 0 && running) {
						// Find all unmatched MovedFrom events and process them.  We expire in
						// five seconds.  This is quite conservative, but what is the rush?
						// Would be nice to not do this here and not have to wake up every 2 secs
						for (int i = 0; i < cookie_list.Count; i++) {
							PendingMove pending = cookie_list[i] as PendingMove;
							if (pending.Time.AddSeconds (2) < DateTime.Now) {
								SendEvent (pending.Watch, pending.SrcName, null,
									   EventType.MovedFrom);
								cookie_hash.Remove (pending);
								cookie_list.Remove (pending);
							}
						}

						Monitor.Wait (event_queue, 2);			
					}
					if (!running)
						break;
					qe = (queued_event) event_queue.Dequeue ();
				}

				Watched watched;
				watched = Lookup (qe.iev.wd, qe.iev.mask);
				if (watched == null)
					continue;

				// Get the filename payload, if any
				int n_chars = 0;
				while (n_chars < qe.filename.Length && qe.filename [n_chars] != 0)
					++n_chars;
				string filename = "";
				if (n_chars > 0)
					filename = filename_encoding.GetString (qe.filename, 0, n_chars);

				// If this is a directory, we need to handle MovedTo and MovedFrom events
				// (regardless of the actual watched.Mask) and fix up watched.Path and the
				// path of all subdirectories.
				//
				// We also have to handle unmatched events.  As we presume MovedTo always
				// follows MovedFrom, we only need to handle unmatched MoveFrom's
				// explicitly (unmatched MovedFrom's have srcpath=null in Inotify.Event).
				// We handle the expiration process above, in the idle loop.
				//
				// Unfortunately, we have to handle the Directory.Exists() test a bit
				// roundabout, because watched.Path during MovedFrom no longer exists.
				if (qe.iev.mask == EventType.MovedFrom) {
					PendingMove pending = new PendingMove (watched, filename, DateTime.Now,
									       qe.iev.cookie);
					cookie_hash [pending.Cookie] = pending;
					cookie_list.Add (pending);
					continue; // Wait for a possible matching MovedTo
				}
				string srcpath = null;
				if (qe.iev.mask == EventType.MovedTo) {
					PendingMove pending = cookie_hash [qe.iev.cookie] as PendingMove;
					if (pending != null) {
						string dstpath = Path.Combine (watched.Path, filename);
						srcpath = Path.Combine (pending.Watch.Path, pending.SrcName);
						if (Directory.Exists (dstpath))
							HandleMove (srcpath, dstpath);
						cookie_hash.Remove (pending);
						cookie_list.Remove (pending);
					}

					// This gets prettier and prettier.  Like my Ex-Wife.  Since we delay
					// unmatched MovedFrom events until they expire, a MovedFrom+MovedTo
					// combination on the same file out and back into a watched directory
					// can race and arrive as MovedTo+MovedFrom.  That is bad.
					for (int i = 0; i < cookie_list.Count; i++) {
						PendingMove race = cookie_list[i] as PendingMove;
						if (race.Watch.Wd == watched.Wd && race.SrcName == filename) {
							// Expire this MovedFrom and send it out right immediately
							SendEvent (race.Watch, race.SrcName, null, EventType.MovedFrom);
							cookie_hash.Remove (race);
							cookie_list.Remove (race);
						}
					}
				}

				SendEvent (watched, filename, srcpath, qe.iev.mask);

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
				else {
					// Our hashes work without a trailing path delimiter
					string path = arg.TrimEnd ('/');
					to_watch.Enqueue (path);
				}
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
