
using System;
using System.Collections;

using Beagle.Util;

namespace Bludgeon {

	public class SanityCheck {

		private SanityCheck () { }
		
		static IComparer uri_cmp = new UriFu.Comparer ();

		static public bool QueryWithExpectations (Beagle.Query q, ArrayList expected)
		{
			bool success;
			success = true;

			ArrayList actual;
			actual = QueryFu.GetUris (q);
			actual.Sort (uri_cmp);

			expected.Sort (uri_cmp);

			if (actual.Count != expected.Count)
				Log.Failure ("Expected {0} matches, got {1}",
					     expected.Count, actual.Count);

			int i_a = 0, i_e = 0;
			while (i_a < actual.Count || i_e < expected.Count) {

				Uri a = null, e = null;
				if (i_a < expected.Count)
					a = expected [i_a] as Uri;
				if (i_e < expected.Count)
					e = expected [i_e] as Uri;

				if (a == null) {
					Log.Failure ("Missing {0}", e);
					++i_e;
					success = false;
				} else if (e == null) {
					Log.Failure ("Extra {0}", a);
					++i_a;
					success = false;
				} else if (UriFu.Equals (a, e)) {
					++i_a;
					++i_e;
				} else if (UriFu.Compare (a, e) < 0) {
					Log.Failure ("Extra {0}", a);
					++i_a;
					success = false;
				} else {
					Log.Failure ("Missing {0}", e);
					++i_e;
					success = false;
				}
			}

			return success;
		}

		static public bool QueryWithExpectations (Beagle.Query q, Uri uri)
		{
			ArrayList array = new ArrayList (1);
			array.Add (uri);
			return QueryWithExpectations (q, array);
		}

		static public bool QueryAllFiles (ICollection files)
		{
			Log.Info ("querying all files (count={0})", files.Count);
			
			int count = 0, failures = 0;
			foreach (FileModel file in files) {
				Log.Spew ("Searching for {0}", file.FullName);
				
				Beagle.Query query;
				query = new Beagle.Query ();
				QueryFu.AddName (query, file);
				QueryFu.AddBody (query, file);

				++count;
				if (! QueryWithExpectations (query, file.Uri))
					++failures;
			}

			Log.Info ("Results: {0} attempts, {1} failures", count, failures);

			return failures == 0;
		}

		static public bool QueryAllTokens (ICollection files)
		{
			Log.Info ("querying all tokens (file count={0})", files.Count);

			int failures = 0;
			for (int id = 0; id < Token.Count; ++id) {

				Log.Spew ("Searching for token {0}", id);

				Beagle.Query query;
				query = new Beagle.Query ();
				QueryFu.AddToken (query, id);

				ArrayList expected = new ArrayList ();
				foreach (FileModel file in files)
					if (file.Contains (id))
						expected.Add (file.Uri);

				if (! QueryWithExpectations (query, expected))
					++failures;
			}

			Log.Info ("Results: {0} tokens, {1} failures", Token.Count, failures);

			return failures == 0;
		}

		static public bool DoAll (ICollection files)
		{
			return QueryAllTokens (files)
				&& QueryAllTokens (files);
		}
	}
}
