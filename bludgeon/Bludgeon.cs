
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Beagle.Util;
using CommandLineFu;

namespace Bludgeon {

	static class BludgeonMain {

		static DirectoryObject CreateTestRoot ()
		{

			string parent;
			parent = Environment.GetEnvironmentVariable ("BLUDGEON_TEST_DIRECTORY");
			if (parent == null)
				parent = ".";
			
			if (! Directory.Exists (parent))
				Directory.CreateDirectory (parent);
			
			int i = 0;
			string home;
			do {
				string name;
				name = String.Format ("test-{0}", ++i);
				home = Path.GetFullPath (Path.Combine (parent, name));
			} while (Directory.Exists (home));

			Directory.CreateDirectory (home);
			PathFinder.HomeDir = home;

			string dot;
			dot = Path.Combine (home, ".bludgeon");
			Directory.CreateDirectory (dot);

			string log;
			log = Path.Combine (dot, "Log");
			Log.Create (log);

			Log.Spew ("Test home directory is '{0}'", home);

			return new DirectoryObject (home);
		}

#if false
		static FileModel InitialTree ()
		{
			FileModel root;
			root = FileModel.NewRoot ();
			root.Grow (2);

			Log.Info ("Initial tree contains {0} files", root.Size);

			return root;
		}
#endif

		///////////////////////////////////////////////////////////////////////////
		
		// Command-line arguments

		[Option (LongName="min-cycles")]
		static private int min_cycles = -1;

		[Option (LongName="max-cycles")]
		static private int max_cycles = -1;

		[Option (LongName="cycles")]
		static private int cycles = -1;

		[Option (LongName="disable-verify")]
		static private bool disable_verify = false;

		[Option (LongName="total-time")]
		static private double total_time = -1; // in minutes

		[Option (LongName="total-count")]
		static private int total_count = 1;

		[Option (LongName="slowdown", Description="Time between cycles (in seconds)")]
		static private double slowdown = -1; // time between cycles, in seconds

		[Option (LongName="pause", Description="Time between tests (in seconds)")] 
		static private double pause = -1; // time between tests, in seconds

		[Option (LongName="heap-buddy", Description="Profile daemon with heap-buddy")]
		static private bool heap_buddy = false;

		[Option (LongName="test-queries", Description="Generate random queries and check that they return the correct results")]
		static private bool test_queries = false;

		/////////////////////////////////////////////////////////////////

		static private Abuse abuse;

		static bool Startup ()
		{
			Daemon.UseHeapBuddy = heap_buddy;
			Daemon.Start (new Daemon.StartedHandler (OnDaemonStarted));
			return false;
		}

		static void OnDaemonStarted (string version)
		{
			if (version == null) {
				Log.Info ("Could not contact daemon -- giving up!");
				Gtk.Application.Quit ();
			}

			Daemon.WaitUntilIdle (OnDaemonIdle);
		}

		static void OnDaemonIdle ()
		{
			Log.Info ("Daemon is idle!");
			abuse.Run ();
		}

		static public void Shutdown ()
		{
			Daemon.Shutdown ();
			Log.Spew ("Test home directory was '{0}'", PathFinder.HomeDir);
			Gtk.Application.Quit ();
		}

		/////////////////////////////////////////////////////////////////

		static void Main (string [] args)
		{
			Gtk.Application.InitCheck ("bludgeon", ref args);
			
			args = CommandLine.Process (typeof (BludgeonMain), args);

			// BU.CommandLine.Process returns null if --help was passed
			if (args == null)
				return;

			ArrayList hammers_to_use;
			hammers_to_use = new ArrayList ();
			foreach (string name in args) {
				IHammer hammer;
				hammer = Toolbox.GetHammer (name);
				if (hammer != null)
					hammers_to_use.Add (hammer);
				else
					Log.Failure ("Unknown hammer '{0}'", name);
			}

			DirectoryObject root;
			root = CreateTestRoot ();
			TreeBuilder.Build (root,
					   30,    // three directories
					   100,   // ten files
					   0.1,   // no archives
					   0.5,   // archive decay, which does nothing here
					   false, // build all directories first, not in random order
					   null); // no need to track events
			if (! root.VerifyOnDisk ())
				throw new Exception ("VerifyOnDisk failed for " + root.FullName);
			
			EventTracker tracker;
			tracker = new EventTracker ();

			abuse = new Abuse (root, tracker, hammers_to_use);

			GLib.Idle.Add (new GLib.IdleHandler (Startup));
			Gtk.Application.Run ();
#if false


			CreateTestHome ();

			FileModel root;
			root = InitialTree ();
			FileModel.AddRoot (root);
			
			Daemon.Start ();
			if (! SanityCheck.VerifyIndex ()) {
				Log.Failure ("Initial index verify failed --- shutting down");
				Daemon.Shutdown ();
				return;
			}

			if (test_queries) {
				if (total_time < 0)
					total_time = 1.0;
				SanityCheck.TestRandomQueries (total_time);
				Daemon.Shutdown ();
				return;
			}

			if (hammers_to_use.Count == 0) {
				Log.Info ("No hammers specified --- shutting down");
				Daemon.Shutdown ();
				return;
			}
				
			Random random;
			random = new Random ();

			int test_count = 0;

			Stopwatch sw = null;
			if (total_time > 0) {
				sw = new Stopwatch ();
				sw.Start ();
			}

			bool failed;
			failed = false;

			while (true) {

				if (sw != null) {
					if (sw.ElapsedTime > total_time * 60)
						break;
					Log.Info ("Elapsed time: {0} ({1:0.0}%",
						  sw, 100 * sw.ElapsedTime / (total_time * 60));
				} else {
					if (test_count >= total_count)
						break;
				}

				IHammer hammer;
				hammer = hammers_to_use [test_count % hammers_to_use.Count] as IHammer;

				++test_count;

				Log.Info ("Starting test #{0}", test_count);

				int test_cycles;
				if (min_cycles != -1 && max_cycles != -1) {
					test_cycles = min_cycles;
					if (min_cycles < max_cycles)
						test_cycles = min_cycles + random.Next (max_cycles - min_cycles);
				} else if (cycles != -1)
					test_cycles = cycles;
				else if (max_cycles != -1)
					test_cycles = max_cycles;
				else if (min_cycles != -1)
					test_cycles = min_cycles;
				else
					test_cycles = 1;

				for (int i = 0; i < test_cycles; ++i) {
					hammer.HammerOnce ();
					if (slowdown > 0)
						Thread.Sleep ((int) (slowdown * 1000));
				}


				if (! disable_verify && ! SanityCheck.VerifyIndex ()) {
					failed = true;
					break;
				}

				if (pause > 0)
					Thread.Sleep ((int) (pause * 1000));
			}

			if (failed)
				Log.Failure ("Testing aborted");
#endif
		}
	}
}
