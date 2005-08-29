
using System;
using System.Collections;
using System.Threading;

using Beagle.Util;

namespace Bludgeon {

	public class SanityCheck {

		private SanityCheck () { }

		static public bool CheckQuery (Beagle.Query query)
		{
			Hashtable matching_path_hash;
			matching_path_hash = new Hashtable ();
			
			foreach (Uri uri in QueryFu.GetUris (query))
				matching_path_hash [uri.LocalPath] = null;

			bool success;
			success = true;

			foreach (FileModel root in FileModel.Roots) {
				foreach (FileModel file in root.GetMatchingDescendants (query)) {
					if (matching_path_hash.Contains (file.FullName)) {
						matching_path_hash.Remove (file.FullName);
					} else {
						Log.Failure ("Missing match {0}", file.FullName);
						success = false;
					}
				}
			}

			foreach (string path in matching_path_hash.Keys) {
				Log.Failure ("Unexpected match {0}", path);
				success = false;
			}

			return success;
		}

		static public bool VerifyIndex ()
		{
			Thread.Sleep (1000); // wait one second

			Log.Info ("Starting sanity check");

			Daemon.WaitUntilIdle ();
			
			bool success;
			success = true;

			foreach (FileModel root in FileModel.Roots) {

				Log.Info ("Verifying index for root {0}", root.FullName);

				for (int i = 0; i < Token.Count; ++i) {
					Beagle.Query query;
					query = QueryFu.NewTokenQuery (i);

					if (! CheckQuery (query)) {
						Log.Spew ("Failed query is:");
						QueryFu.SpewQuery (query);
						success = false;
					}
				}
			}

			if (success)
				Log.Info ("Index successfully verified");
			else
				Log.Info ("Verification failed");

			return success;
		}
		
		static public bool TestRandomQueries (double minutes_to_run)
		{
			Log.Info ("Running random queries for {1:0.0} minutes", minutes_to_run);

			bool success;
			success = true;

			Stopwatch sw;
			sw = new Stopwatch ();
			sw.Start ();

			int count = 0;
			while (true) {
				if ((count % 100 == 0) && sw.ElapsedTime > minutes_to_run * 60)
					break;
				++count;

				Beagle.Query query;
				query = QueryFu.NewRandomQuery ();

				if (! CheckQuery (query)) {
					Log.Spew ("Failed query is:");
					QueryFu.SpewQuery (query);
					success = false;
					break;
				}
			}

			// In case we ended early
			minutes_to_run = sw.ElapsedTime / 60;

			Log.Spew ("Ran {0} queries in {1:0.0} minutes ({2:0.0} queries/s)",
				  count, minutes_to_run, count / (minutes_to_run * 60));

			return success;
		}

	}
}
