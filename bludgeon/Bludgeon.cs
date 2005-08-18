
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

		static FileModel InitialTree ()
		{
			FileModel root;
			root = FileModel.NewRoot ();
			root.Grow (3);

			Log.Info ("Initial tree contains {0} files", root.Size);

			return root;
		}

		static void ManipulateTree (FileModel root)
		{
			FileModel grow_at;
			
			// Another burst of growth
			for (int i = 0; i < 10; ++i) {
				grow_at = root.PickDirectoryDescendant ();
				if (grow_at == null)
					grow_at = root;
				grow_at.Grow (0);
			}
			

			grow_at = root.PickDirectoryDescendant ();
			if (grow_at == null)
				grow_at = root;
			
			grow_at.Grow (1);
				
			// Delete some stuff
			for (int i = 0; i < 10; ++i) {
				FileModel file;
				file = root.PickDescendant ();
				if (file != null)
					file.Delete ();
			}
			
			
			// Another burst of growth
			for (int i = 0; i < 10; ++i) {
				grow_at = root.PickDirectoryDescendant ();
				if (grow_at == null)
					grow_at = root;
				grow_at.Grow (0);
			}

			Log.Info ("Perturbed tree contains {0} files", root.Size);
		}

		static bool DoStaticVerify (FileModel root)
		{
			Log.Info ("Starting sanity check");

			Daemon.WaitUntilIdle ();

			Daemon.OptimizeIndexes ();

			Daemon.WaitUntilIdle ();

			return SanityCheck.VerifyIndex (root);
		}

		static void Main (string [] args)
		{
			CreateTestHome ();

			FileModel root;
			root = InitialTree ();
			root.Grow (5);
			//ManipulateTree (root);

#if true
			Daemon.Start ();
			DoStaticVerify (root);
			SanityCheck.TestRandomQueries (root, 10);
			Daemon.Shutdown ();
#else
			int count = 0;
			while (true) {
				++count;
				Log.Info ("Test {0}", count);

				FileModel change_me;
				change_me = root.PickFileDescendant ();
				change_me.Touch ();

				Daemon.Start ();
				Daemon.WaitUntilIdle ();
				Daemon.Shutdown ();
			}
#endif

			Log.Spew ("Test home directory was '{0}'", PathFinder.HomeDir);
		}
	}
}
