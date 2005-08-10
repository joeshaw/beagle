//
// LuceneQueryingDriver.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using Beagle.Util;

namespace Beagle.Daemon {

	public class LuceneQueryingDriver : LuceneCommon {

		public const string PrivateNamespace = "_private:";

		public delegate bool UriFilter (Uri uri);
		public delegate bool HitFilter (Hit hit);
		public delegate double RelevancyMultiplier (Hit hit);

		public LuceneQueryingDriver (string index_name, int minor_version, bool read_only) 
			: base (index_name, minor_version)
		{
			// FIXME: Maybe the LuceneQueryingDriver should never try to create the index?
			if (Exists ())
				Open (read_only);
			// FIXME: Do something sane if we're in read-only mode and want to create an index.
			else if (!read_only)
				Create ();
		}

		////////////////////////////////////////////////////////////////

		// Returns the lowest matching score before the results are
		// truncated.
		public void DoQuery (Query               query,
				     IQueryResult        result,
				     ICollection         search_subset, // should be internal uris
				     UriFilter           uri_filter,
				     HitFilter           hit_filter,    // post-processing for hits
				     RelevancyMultiplier relevancy_multiplier)
		{

			Stopwatch sw;
			sw = new Stopwatch ();
			sw.Start ();

			//
			// First, we assemble a bunch of queries
			//

			LNS.Query non_part_query = null;
			LNS.Query search_subset_query = null;

			ArrayList primary_required_part_queries = null;
			ArrayList secondary_required_part_queries = null;

			LNS.BooleanQuery primary_optional_part_query = null;
			LNS.BooleanQuery secondary_optional_part_query = null;

			LNS.BooleanQuery primary_prohibited_part_query = null;
			LNS.BooleanQuery secondary_prohibited_part_query = null;

			if (search_subset != null && search_subset.Count > 0)
				search_subset_query = UriQuery ("Uri", search_subset, non_part_query);

			non_part_query = NonQueryPartQuery (query, search_subset_query);

			// Now assemble all of the parts into a bunch of Lucene queries
			foreach (QueryPart part in query.Parts) {
				LNS.Query primary_part_query;
				LNS.Query secondary_part_query;
				QueryPartToQuery (part,
						  false, // we want both primary and secondary queries
						  non_part_query, // add the non-part stuff to every part's query
						  out primary_part_query,
						  out secondary_part_query);

				switch (part.Logic) {
					
				case QueryPartLogic.Required:
					if (primary_required_part_queries == null) {
						primary_required_part_queries = new ArrayList ();
						secondary_required_part_queries = new ArrayList ();
					}
					primary_required_part_queries.Add (primary_part_query);
					secondary_required_part_queries.Add (secondary_part_query);
					break;

				case QueryPartLogic.Optional:
					if (primary_optional_part_query == null)
						primary_optional_part_query = new LNS.BooleanQuery ();
					primary_optional_part_query.Add (primary_part_query, false, false);

					if (secondary_part_query != null) {
						if (secondary_optional_part_query == null)
							secondary_optional_part_query = new LNS.BooleanQuery ();
						secondary_optional_part_query.Add (secondary_part_query, false, false);
					}
					break;

				case QueryPartLogic.Prohibited:
					if (primary_prohibited_part_query == null)
						primary_prohibited_part_query = new LNS.BooleanQuery ();
					primary_prohibited_part_query.Add (primary_part_query, false, false);

					if (secondary_part_query != null) {
						if (secondary_prohibited_part_query == null)
							secondary_prohibited_part_query = new LNS.BooleanQuery ();
						secondary_prohibited_part_query.Add (secondary_part_query, false, false);
					}

					break;
				}
			}

			// If we have no required or optional parts, give up.
			if (primary_required_part_queries == null && primary_optional_part_query == null)
				return;

			//
			// Now that we have all of these nice queries, let's execute them!
			//

			LNS.IndexSearcher primary_searcher;
			IndexReader secondary_reader = null;
			LNS.IndexSearcher secondary_searcher = null;

			primary_searcher = new LNS.IndexSearcher (PrimaryStore);
			
			if (SecondaryStore != null) {
				secondary_reader = IndexReader.Open (SecondaryStore);
				if (secondary_reader.NumDocs () == 0) {
					secondary_reader.Close ();
					secondary_reader = null;
				}
			}

			if (secondary_reader != null)
				secondary_searcher = new LNS.IndexSearcher (secondary_reader);

			// Sometimes you just need a convenient integer...
			int N;

			// Create an initial whitelist from the search subset query.
			BetterBitArray primary_whitelist = null;
			BetterBitArray secondary_whitelist = null;

			N = DoLowLevelQuery (primary_searcher,
					     search_subset_query,
					     ref primary_whitelist,
					     null); // no whitelist

			// If we didn't match anything (and we know the query was non-null,
			// or -1 would have been returned), just give up -- nothing
			// is going to match from here on out.
			if (N == 0)
				return;

			DoLowLevelQuery (secondary_searcher,
					 search_subset_query,
					 ref secondary_whitelist,
					 null); // no whitelist

			// Next we build up our blacklists from the query's prohibited parts.
			BetterBitArray primary_blacklist = null;
			BetterBitArray secondary_blacklist = null;
			
			// Don't worry... DoLowLevelQuery is smart about null arguments.
			DoLowLevelQuery_TwoIndex (primary_searcher,
						  secondary_searcher,
						  primary_prohibited_part_query,
						  secondary_prohibited_part_query,
						  ref primary_blacklist,
						  ref secondary_blacklist,
						  primary_whitelist,
						  secondary_whitelist,
						  -1);

			// Merge our blacklists into our whitelists.
			if (primary_blacklist != null) {
				primary_blacklist.Not ();
				if (secondary_blacklist != null)
					secondary_blacklist.Not ();
				
				if (primary_whitelist == null)
					primary_whitelist = primary_blacklist;
				else
					primary_whitelist.And (primary_blacklist);
				
				if (secondary_whitelist == null)
					secondary_whitelist = secondary_blacklist;
				else
					secondary_whitelist.And (secondary_blacklist);
			}

			// If either of our whitelists are empty, give up.
			if (primary_whitelist != null && ! primary_whitelist.ContainsTrue ())
				return;
			if (secondary_whitelist != null && ! secondary_whitelist.ContainsTrue ())
				return;

			//
			// Now we execute the core queries: Either the
			// required parts, or the optional parts if there are
			// no required ones.  //

			BetterBitArray primary_matches = null;

			if (primary_required_part_queries != null) {

				if (secondary_searcher != null)
					primary_matches = DoRequiredQueries_TwoIndex (primary_searcher,
										      secondary_searcher,
										      primary_required_part_queries,
										      secondary_required_part_queries,
										      primary_whitelist,
										      secondary_whitelist,
										      query.MaxHits);
				else
					primary_matches = DoRequiredQueries (primary_searcher,
									     primary_required_part_queries,
									     primary_whitelist);

			} else {
				
				// This does the optional parts of the query.

				BetterBitArray dummy_secondary_matches = null;
				
				DoLowLevelQuery_TwoIndex (primary_searcher,
							  secondary_searcher,
							  primary_optional_part_query,
							  secondary_optional_part_query,
							  ref primary_matches,
							  ref dummy_secondary_matches,
							  primary_whitelist,
							  secondary_whitelist,
							  query.MaxHits);
			}

			sw.Stop ();
			Logger.Log.Debug ("###### Finished low-level queries in {0}", sw);
			sw.Reset ();
			sw.Start ();

			// If we didn't get any matches, give up.

			if (primary_matches == null || ! primary_matches.ContainsTrue ())
				return;

			// Now we need to assemble the hits.
			// FIXME: For the moment, no relevancy or caps on Hits.  Scary.
			//

			int j = 0;
			int hit_count = 0;
			while (j < primary_matches.Count) {
				
				j = primary_matches.GetNextTrueIndex (j);
				if (j >= primary_matches.Count)
					break;

				Document primary_doc;
				primary_doc = primary_searcher.Doc (j);

				Uri uri;
				uri = GetUriFromDocument (primary_doc);
				if (uri_filter != null && ! uri_filter (uri))
					continue;

				float score;
				score = 1.0f; // FIXME!

				Hit hit;
				hit = DocumentToHit (primary_doc, uri, j, score);
				AddPropertiesToHit (hit, primary_doc, true);

				// Find the associated secondary document
				// and pull in it's properties.
				if (secondary_searcher != null) {
					LNS.Hits hits;
					hits = secondary_searcher.Search (UriQuery ("Uri", uri));
					if (hits.Length () != 0) {
						Document secondary_doc;
						secondary_doc = hits.Doc (0);
						AddPropertiesToHit (hit, secondary_doc, false);
					}
				}
				
				if (hit_filter == null || hit_filter (hit)) {

					// Before we broadcast a hit, we strip out any
					// properties in the PrivateNamespace.  We
					// manipulate the property ArrayList directory,
					// which is pretty gross... but this is safe,
					// since removing items will not change the sort
					// order.
					int i = 0;
					while (i < hit.Properties.Count) {
						Property prop = hit.Properties [i] as Property;
						if (prop.Key.StartsWith (PrivateNamespace))
							hit.Properties.RemoveAt (i);
						else
							++i;
					}

					result.Add (hit); // broadcast the hit we just constructed
					++hit_count;

					if (hit_count > query.MaxHits)
						break;
				}

				++j;
			}

			//
			// Finally, we clean up after ourselves.
			//
			
			if (secondary_reader != null)
				secondary_reader.Close ();
			primary_searcher.Close ();
			if (secondary_searcher != null)
				secondary_searcher.Close ();


			sw.Stop ();
			Logger.Log.Debug ("###### Processed {0} hits in {1}", hit_count, sw);

		}

		////////////////////////////////////////////////////////////////
		
		//
		// Some extremely low-level operations
		//

		private class BitArrayHitCollector : LNS.HitCollector {

			private BetterBitArray matches;
			private BetterBitArray whitelist;

			private int hit_count = 0;

			public BitArrayHitCollector (BetterBitArray matches,
						     BetterBitArray whitelist)
			{
				this.matches = matches;
				this.whitelist = whitelist;
			}

			public override void Collect (int id, float score)
			{
				if (whitelist == null || whitelist [id]) {
					matches [id] = true;
					++hit_count;
				}
			}

			public int HitCount { get { return hit_count; } }
		}

		private static int DoLowLevelQuery (LNS.IndexSearcher  searcher,
						    LNS.Query          query,
						    ref BetterBitArray matches,
						    BetterBitArray     whitelist)
		{
			// Do nothing on a null searcher or query.
			if (searcher == null
			    || query == null)
				return -1;

			// If we have been handed a totally empty whitelist,
			// we have no hope of being able to match anything.
			if (whitelist != null && ! whitelist.ContainsTrue ())
				return 0;

			if (matches == null) 
				matches = new BetterBitArray (searcher.MaxDoc ());

			BitArrayHitCollector collector;
			collector = new BitArrayHitCollector (matches, whitelist);
			
			searcher.Search (query, null, collector);
			
			return collector.HitCount;
		}

		// Given a set of matches in one index, project them into a set of
		// matches in the other index.
		private static void ProjectMatches (LNS.IndexSearcher  source_searcher,
						    BetterBitArray     source_matches,
						    LNS.IndexSearcher  target_searcher,
						    ref BetterBitArray target_matches,
						    BetterBitArray     target_whitelist)
		{
			// If there are no source matches, there is nothing
			// to do.
			if (source_searcher == null
			    || source_matches == null
			    || ! source_matches.ContainsTrue ())
				return;

			LNS.BooleanQuery pending_uri_query = null;
			int pending_uri_count = 0;
			int pending_uri_max = LNS.BooleanQuery.GetMaxClauseCount ();

			int i = 0;

			while (true) {
				i = source_matches.GetNextTrueIndex (i);
				
				if (i < source_matches.Count) {

					Document doc;
					doc = source_searcher.Doc (i);

					Uri uri;
					uri = GetUriFromDocument (doc);

					LNS.Query query;
					query = UriQuery ("Uri", uri);

					if (pending_uri_query == null) {
						pending_uri_query = new LNS.BooleanQuery ();
						pending_uri_count = 0;
					}

					pending_uri_query.Add (query, false, false);

					++pending_uri_count;
				}

				++i;

				if (pending_uri_query != null
				    && (pending_uri_count >= pending_uri_max || i >= source_matches.Count)) {
					
					DoLowLevelQuery (target_searcher,
							 pending_uri_query,
							 ref target_matches,
							 target_whitelist);

					pending_uri_query = null;
				}

				if (i >= source_matches.Count)
					break;
			}
		}

		private static void DoLowLevelQuery_TwoIndex (LNS.IndexSearcher  primary_searcher,
							      LNS.IndexSearcher  secondary_searcher,
							      LNS.Query          primary_query,
							      LNS.Query          secondary_query,
							      ref BetterBitArray primary_matches,
							      ref BetterBitArray secondary_matches,
							      BetterBitArray     primary_whitelist,
							      BetterBitArray     secondary_whitelist,
							      int                max_match_count)
		{
			DoLowLevelQuery (primary_searcher,
					 primary_query,
					 ref primary_matches,
					 primary_whitelist);

			DoLowLevelQuery (secondary_searcher,
					 secondary_query,
					 ref secondary_matches,
					 secondary_whitelist);

			// Project the matches from the secondary searcher
			// onto the first.  But only if we don't already have
			// enough matches.
			if (max_match_count < 0
			    || primary_matches == null 
			    || primary_matches.GetTrueCount () < max_match_count)
				ProjectMatches (secondary_searcher,
						secondary_matches,
						primary_searcher,
						ref primary_matches,
						primary_whitelist);
		}

		private static void ProjectMatches_BiDirectional (LNS.IndexSearcher first_searcher,
								  BetterBitArray    first_matches,
								  BetterBitArray    first_whitelist,
								  LNS.IndexSearcher second_searcher,
								  BetterBitArray    second_matches,
								  BetterBitArray    second_whitelist)
		{
			Stopwatch sw = new Stopwatch ();
			sw.Start ();

			BetterBitArray first_into_second = null;
			BetterBitArray second_into_first = null;

			// Map the first set of matches into the second
			ProjectMatches (first_searcher,
					first_matches,
					second_searcher,
					ref first_into_second,
					second_whitelist);

			// Now map the second set of matches back to the
			// first, excluding items that we know came out of the
			// first set.
			BetterBitArray filtered_second_matches;
			if (first_into_second == null) 
				filtered_second_matches = second_matches;
			else {
				filtered_second_matches = new BetterBitArray (first_into_second);
				filtered_second_matches.Not ();
				filtered_second_matches.And (second_matches);
			}

			ProjectMatches (second_searcher,
					filtered_second_matches,
					first_searcher,
					ref second_into_first,
					first_whitelist);

			// Or our projections onto the original sets of matches.
			if (second_into_first != null)
				first_matches.Or (second_into_first);
			if (first_into_second != null)
				second_matches.Or (first_into_second);

			sw.Stop ();
		}

		////////////////////////////////////////////////////////////////

		//
		// Special logic for handling our set of required queries
		//

		// This is the easy case: we just combine all of the queries
		// into one big BooleanQuery.
		private static BetterBitArray DoRequiredQueries (LNS.IndexSearcher primary_searcher,
								 ArrayList primary_queries,
								 BetterBitArray primary_whitelist)
		{
			LNS.BooleanQuery combined_query;
			combined_query = new LNS.BooleanQuery ();
			foreach (LNS.Query query in primary_queries)
				combined_query.Add (query, true, false);
			
			BetterBitArray matches = null;
			DoLowLevelQuery (primary_searcher,
					 combined_query,
					 ref matches,
					 primary_whitelist);

			return matches;
		}

		// This code attempts to execute N required queries in the
		// most efficient order to minimize the amount of time spent
		// joining between the two indexes.  It returns a joined bit
		// array of matches against the primary index.

		private class MatchInfo : IComparable {
			public BetterBitArray PrimaryMatches;
			public BetterBitArray SecondaryMatches;
			public int UpperBound;

			public int CompareTo (object obj)
			{
				MatchInfo other = (MatchInfo) obj;
				return this.UpperBound - other.UpperBound;
			}
		}

		private static BetterBitArray DoRequiredQueries_TwoIndex (LNS.IndexSearcher primary_searcher,
									  LNS.IndexSearcher secondary_searcher,
									  ArrayList primary_queries,
									  ArrayList secondary_queries,
									  BetterBitArray primary_whitelist,
									  BetterBitArray secondary_whitelist,
									  int max_match_count)
		{
			ArrayList match_info_list;
			match_info_list = new ArrayList ();

			// First, do all of the low-level queries
			for (int i = 0; i < primary_queries.Count; ++i) {
				LNS.Query pq, sq;
				pq = primary_queries [i] as LNS.Query;
				sq = secondary_queries [i] as LNS.Query;

				MatchInfo info;
				info = new MatchInfo ();
				DoLowLevelQuery (primary_searcher, pq, ref info.PrimaryMatches, primary_whitelist);
				DoLowLevelQuery (secondary_searcher, sq, ref info.SecondaryMatches, secondary_whitelist);
				info.UpperBound = info.PrimaryMatches.GetTrueCount () + info.SecondaryMatches.GetTrueCount ();

				match_info_list.Add (info);
			}

			// Now sort
			match_info_list.Sort ();

			BetterBitArray primary_matches = null;
			BetterBitArray secondary_matches = null;

			for (int i = 0; i < match_info_list.Count; ++i) {
				
				MatchInfo info;
				info = match_info_list [i] as MatchInfo;

				if (primary_matches != null) {
					info.PrimaryMatches.And (primary_matches);
					info.SecondaryMatches.And (secondary_matches);
				}

				// An optimization: If this is the last query
				// and we are already at or over our query
				// limit, skip the final projection.  We don't
				// need any more matches.
				if (max_match_count >= 0
				    && i == match_info_list.Count-1
				    && info.PrimaryMatches.GetTrueCount () >= max_match_count)
					return info.PrimaryMatches;
				
				ProjectMatches_BiDirectional (primary_searcher,
							      info.PrimaryMatches,
							      primary_matches,
							      secondary_searcher,
							      info.SecondaryMatches,
							      secondary_whitelist);

				primary_matches = info.PrimaryMatches;
				secondary_matches = info.SecondaryMatches;
			}

			return primary_matches;
		}		
	}
}
