
using System;
using System.Collections;
using System.Threading;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class SanityCheck {

		const double default_minutes_to_run = 1.0;

		private SanityCheck () { }

		static public bool CheckQuery (Query query, FileSystemObject root)
		{
			// Find the set of objects that we expect to match the query,
			// based on our knowledge of the current state of the tree.
			ICollection matching_fsos;
			matching_fsos = root.RecursiveQuery (query);

			// Query the daemon and get the actual list of hits.
			Hashtable matching_hits;
			matching_hits = QueryFu.GetHits (query);

			bool success;
			success = true;

			foreach (FileSystemObject fso in matching_fsos) {
				string uri = UriFu.UriToEscapedString (fso.Uri);
				if (matching_hits.Contains (uri))
					matching_hits.Remove (uri);
				else {
					Log.Failure ("Hit missing from beagled query results: {0}", uri);
					success = false;
				}
			}

			foreach (Hit hit in matching_hits.Values) {
				Log.Failure ("Unexpected extra hit in beagled query results: {0}", hit.Uri);
				Log.Failure ("  Properties:");
				foreach (Property prop in hit.Properties)
					Log.Failure ("    {0} = {1}", prop.Key, prop.Value);
				success = false;
			}

			return success;
		}

		static public bool VerifyIndex (FileSystemObject root)
		{
			bool success;
			success = true;

			Log.Info ("Verifying index for root {0}", root.FullName);

			for (int i = 0; i < Token.Count; ++i) {
				Query query;
				query = QueryFu.NewTokenQuery (i);

				if (! CheckQuery (query, root)) {
					Log.Spew ("Failed query is:");
					QueryFu.SpewQuery (query);
					success = false;
				}
			}

			if (success)
				Log.Info ("Index successfully verified");
			else
				Log.Info ("Verification failed");

			return success;
		}
		
		static public bool TestRandomQueries (double minutes_to_run, FileSystemObject root)
		{
			if (minutes_to_run < 0)
				minutes_to_run = default_minutes_to_run;

			Log.Info ("Running random queries for {0:0.0} minutes", minutes_to_run);

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

				if (count % 1000 == 0)
					Log.Spew ("{0} queries run", count);
				
				Beagle.Query query;
				query = QueryFu.NewRandomQuery ();

				if (! CheckQuery (query, root)) {
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
