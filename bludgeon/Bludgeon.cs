
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

		///////////////////////////////////////////////////////////////////////////
		
		// Command-line arguments

		[Option (LongName="total-time", Description="Time to run all tests (in minutes)")]
		static private double total_time = -1;

		[Option (LongName="total-count", Description="Number of tests to run")]
		static private int total_count = -1;

		[Option (LongName="min_pause", Description="Minimum number of seconds between tests")]
		static private double min_pause = 0;

		[Option (LongName="max_pause", Description="Maximum number of seconds between tests")] 
		static private double max_pause = -1;

		[Option (LongName="pause", Description="Exact number of seconds between tests")] 
		static private double pause = -1;

                [Option (LongName="min-cycles", Description="Minimum number of cycles to run")]
                static private int min_cycles = 1; // Always at least one cycle

		[Option (LongName="max-cycles", Description="Maximum number of cycles to run")]
		static private int max_cycles = -1;

		[Option (LongName="cycles", Description="Exact number of cycles to run")]
		static private int cycles = -1;

		[Option (LongName="test-queries", Description="Generate random queries and check that they return the correct results")]
		static private bool test_queries = false;

		[Option (LongName="total-query-time", Description="Number of minutes to run test queries")]
		static private double total_query_time = 1;

		[Option (LongName="heap-buddy", Description="Profile daemon with heap-buddy")]
		static private bool heap_buddy = false;

		[Option (LongName="list-hammers", Description="Lists the available hammers and exits")]
		static private bool list_hammers = false;

		/////////////////////////////////////////////////////////////////

		static private DirectoryObject root;
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

			if (test_queries) {
				SanityCheck.TestRandomQueries (total_query_time, root);
				Shutdown ();
			}
				
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

			if (list_hammers) {
				foreach (string hammer in Toolbox.HammerNames)
					Console.WriteLine ("  - {0}", hammer);
				return;
			}

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

			abuse.TotalCount = total_count;
			abuse.TotalTime = total_time;

			abuse.Cycles = cycles;
			abuse.MinCycles = min_cycles;
			abuse.MaxCycles = max_cycles;

			abuse.Pause = pause;
			abuse.MinPause = min_pause;
			abuse.MaxPause = max_pause;

			GLib.Idle.Add (new GLib.IdleHandler (Startup));
			Gtk.Application.Run ();
		}
	}
}
