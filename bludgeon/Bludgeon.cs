
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Beagle.Util;

namespace Bludgeon {

	class BludgeonMain {

		private BludgeonMain () { }

		static void CreateTestHome ()
		{
			int i = 0;
			
			string home;
			do 
				home = Path.GetFullPath (String.Format ("./test-{0}", ++i));
			while (Directory.Exists (home));

			Directory.CreateDirectory (home);
			PathFinder.HomeDir = home;

			string dot;
			dot = Path.Combine (home, ".bludgeon");
			Directory.CreateDirectory (dot);

			string log;
			log = Path.Combine (dot, "Log");
			Log.Create (log);

			Log.Spew ("Test home directory is '{0}'", home);
		}

		static void Main (string [] args)
		{
			CreateTestHome ();

			ArrayList all_files;
			all_files = new ArrayList ();

			for (int i = 0; i < 10; ++i) {
				FileModel file;
				file = FileModel.Create ();
				all_files.Add (file);
			}
			
			Log.Spew ("Created {0} files", all_files.Count);

			Daemon.Start ();
			Daemon.WaitUntilIdle ();

			Log.Spew ("Waiting 5s");
			Thread.Sleep (5000);

			if (! SanityCheck.DoAll (all_files))
				Log.Info ("Sanity check failed");

			Daemon.Shutdown ();

			Log.Spew ("Test home directory was '{0}'", PathFinder.HomeDir);
		}
	}
}
