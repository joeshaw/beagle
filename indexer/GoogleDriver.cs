//
// GoogleDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Google is a trademark of Google.  But you already knew that.
//

using System;
using System.Collections;
using Beagle.Util;

namespace Beagle {

	public class GoogleDriver : IQueryable {

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
			hit.Type     = "WebLink";
			hit.MimeType = "text/html"; // FIXME

			// FIXME: We can't really compare scores if the Hits
			// come from different sources.  This is a hack.
			hit.Score    = 0.2f / (1 + rank);

			hit ["Summary"]        = res.summary;
			hit ["Snippet"]        = res.snippet;
			hit ["Title"]          = res.title;
			hit ["CachedSize"]     = res.cachedSize;
			hit ["HostName"]       = res.hostName;
			hit ["DirectoryTitle"] = res.directoryTitle;

			return hit;
		}

		static bool showNoKeyMessage = true;

		public String Name {
			get { return "Google"; }
		}

		public bool AcceptQuery (Query query)
		{
			// Reject queries if the key isn't set.
			if (googleKey == null) {
				if (showNoKeyMessage) {
					Console.WriteLine ("To query Google, set the GOOGLE_WEB_API_KEY environment variable.");
					showNoKeyMessage = false;
				}
				return false;
			}
			return true;
		}


		public void Query (Query query, HitCollector collector)
		{
			GoogleSearchResult result = gss.doGoogleSearch (googleKey,
									query.AbusivePeekInsideQuery,
									0, maxResults,
									false, "", false, "", "", "");

			ArrayList hits = new ArrayList ();
			int rank = 0;
			foreach (ResultElement elt in result.resultElements) {
				Hit hit = FromGoogleResultElement (elt, rank);
				++rank;
				hits.Add (hit);
			}

			collector (hits);
		}

	}

}
