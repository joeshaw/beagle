//
// GoogleDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Google is a trademark of Google.  But you already knew that.
//

using System;
using System.Collections;
using Dewey.Util;

namespace Dewey {

	public class GoogleDriver {

		int maxResults = 5;

		GoogleSearchService gss = new GoogleSearchService ();
		String googleKey;

		public GoogleDriver ()
		{
			googleKey = Environment.GetEnvironmentVariable ("GOOGLE_WEB_API_KEY");
		}

		Hit FromGoogleResultElement (ResultElement res, int rank)
		{
			Hit hit = new Hit ();

			hit.Uri      = res.URL;
			hit.Domain   = "Google";
			hit.MimeType = "text/html"; // FIXME
			hit.Source   = "Google";

			// FIXME: We can't really compare scores if the Hits
			// come from different sources.  This is a hack.
			hit.Score    = 0.2f / (1 + rank);

			hit ["summary"]                   = res.summary;
			hit ["snippet"]                   = res.snippet;
			hit ["title"]                     = res.title;
			hit ["cachedSize"]                = res.cachedSize;
			hit ["hostName"]                  = res.hostName;
			hit ["directoryTitle"]            = res.directoryTitle;

			hit.Lockdown ();

			return hit;
		}

		static bool showNoKeyMessage = true;
		
		// FIXME: Should be async, etc.
		public IEnumerable Query (Query query)
		{
			ArrayList hits = new ArrayList ();

			// If the google key isn't set, just return the empty array.
			if (googleKey == null) {
				if (showNoKeyMessage) {
					Console.WriteLine ("To query Google, set the GOOGLE_WEB_API_KEY environment variable.");
					showNoKeyMessage = false;
				}
				return hits;
			}

			GoogleSearchResult result = gss.doGoogleSearch (googleKey,
									query.AbusivePeekInsideQuery,
									0, maxResults,
									false, "", false, "", "", "");

			int rank = 0;
			foreach (ResultElement elt in result.resultElements) {
				Hit hit = FromGoogleResultElement (elt, rank);
				++rank;
				hits.Add (hit);
			}

			return hits;
		}

	}

}
