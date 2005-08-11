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

			GenerateQueryResults (primary_searcher,
					      secondary_searcher,
					      primary_matches,
					      result,
					      query.MaxHits,
					      DateTime.MinValue,
					      DateTime.MaxValue,
					      uri_filter,
					      hit_filter);

#if false

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


					result.Add (hit); // broadcast the hit we just constructed
					++hit_count;

					if (hit_count > query.MaxHits)
						break;
				}

				++j;
			}
#endif

			//
			// Finally, we clean up after ourselves.
			//
			
			if (secondary_reader != null)
				secondary_reader.Close ();
			primary_searcher.Close ();
			if (secondary_searcher != null)
				secondary_searcher.Close ();


			sw.Stop ();
			Logger.Log.Debug ("###### Processed query in {0}", sw);

		}

		////////////////////////////////////////////////////////////////

		//
		// Given a set of hits, broadcast some set out as our query
		// results.
		//

		private static void GenerateQueryResults (LNS.IndexSearcher primary_searcher,
							  LNS.IndexSearcher secondary_searcher,
							  BetterBitArray    primary_matches,
							  IQueryResult      result,
							  int               max_results,
							  DateTime          min_date,
							  DateTime          max_date,
							  UriFilter         uri_filter,
							  HitFilter         hit_filter)
		{
			TopScores top_docs = null;
			ArrayList all_docs = null;

			long min_date_num, max_date_num;
			min_date_num = Int64.Parse (StringFu.DateTimeToString (min_date));
			max_date_num = Int64.Parse (StringFu.DateTimeToString (max_date));

			if (max_date_num < min_date_num)
				return;

			Logger.Log.Debug (">>> Initially handed {0} matches", primary_matches.TrueCount);

			if (primary_matches.TrueCount <= max_results) {
				Logger.Log.Debug (">>> Initial count is within our limit of {0}", max_results);
				all_docs = new ArrayList ();
			} else {
				Logger.Log.Debug (">>> Number of hits is capped at {0}", max_results);
				top_docs = new TopScores (max_results);
			}

			Stopwatch total, a, b, c;
			total = new Stopwatch ();
			a = new Stopwatch ();
			b = new Stopwatch ();
			c = new Stopwatch ();

			total.Start ();
			a.Start ();

			// Pull in the primary documents.
			// We walk across them backwards, since newer 
			// documents are more likely to be at the end of
			// the index.
			int j = primary_matches.Count;
			while (true) {
				int i;
				i = primary_matches.GetPreviousTrueIndex (j);
				if (i < 0)
					break;
				j = i-1; // This way we can't forget to adjust i

				Document doc;
				doc = primary_searcher.Doc (i);

				// Check the timestamp to make sure it is in range
				long timestamp_num;
				timestamp_num = Int64.Parse (doc.Get ("Timestamp"));
				if (timestamp_num < min_date_num || max_date_num < timestamp_num)
					continue;

				if (top_docs != null && ! top_docs.WillAccept (timestamp_num))
					continue;

				// If we have a UriFilter, apply it.
				if (uri_filter != null) {
					Uri uri;
					uri = GetUriFromDocument (doc);
					if (! uri_filter (uri)) 
						continue;
				}

				// Add the document to the appropriate data structure.
				// We use the timestamp_num as the score, so high
				// scores correspond to more-recent timestamps.
				if (all_docs != null)
					all_docs.Add (doc);
				else
					top_docs.Add (timestamp_num, doc);
			}

			a.Stop ();

			b.Start ();

			ICollection final_list_of_docs;
			if (all_docs != null)
				final_list_of_docs = all_docs;
			else
				final_list_of_docs = top_docs.TopScoringObjects;

			ArrayList final_list_of_hits;
			final_list_of_hits = new ArrayList (final_list_of_docs.Count);

			// If we aren't using the secondary index, the next step is
			// very straightforward.
			if (secondary_searcher == null) {

				foreach (Document doc in final_list_of_docs) {
					Hit hit;
					hit = DocumentToHit (doc);
					final_list_of_hits.Add (hit);
				}

			} else {

				Logger.Log.Debug (">>> Performing cross-index Hit reunification");

				Hashtable hits_by_uri;
				hits_by_uri = UriFu.NewHashtable ();

				// FIXME: Want to avoid too many clauses
				// in this query.  Normally our cap will
				// be set low enough that it won't
				// be a problem.
				LNS.BooleanQuery uri_query;
				uri_query = new LNS.BooleanQuery ();

				foreach (Document primary_doc in final_list_of_docs) {
				
					Hit hit;
					hit = DocumentToHit (primary_doc);

					final_list_of_hits.Add (hit);
					hits_by_uri [hit.Uri] = hit;

					uri_query.Add (UriQuery ("Uri", hit.Uri), false, false);
				}
				
				// Find the other halves of our matches
				BetterBitArray secondary_matches = null;
				DoLowLevelQuery (secondary_searcher,
						 uri_query,
						 ref secondary_matches,
						 null);
				
				// Attach all of our secondary properties
				// to the hits
				j = 0;
				while (true) {
					int i;
					i = secondary_matches.GetNextTrueIndex (j);
					if (i >= secondary_matches.Count)
						break;
					j = i+1;

					Document secondary_doc;
					secondary_doc = secondary_searcher.Doc (i);
					
					Uri uri;
					uri = GetUriFromDocument (secondary_doc);

					Hit hit;
					hit = hits_by_uri [uri] as Hit;

					AddPropertiesToHit (hit, secondary_doc, false);
				}
			}

			b.Stop ();

			// If we used the TopScores object, we got our original
			// list of documents sorted for us.  If not, sort the
			// final list.
			if (top_docs == null)
				final_list_of_hits.Sort ();

			c.Start ();

			// If we have a hit_filter, use it now.
			if (hit_filter != null) {
				for (int i = 0; i < final_list_of_hits.Count; ++i) {
					Hit hit;
					hit = final_list_of_hits [i] as Hit;
					if (! hit_filter (hit))
						final_list_of_hits [i] = null;
				}
			}

			// Before we broadcast a hit, we strip out any
			// properties in the PrivateNamespace.  We
			// manipulate the property ArrayList directory,
			// which is pretty gross... but this is safe,
			// since removing items will not change the sort
			// order.
			foreach (Hit hit in final_list_of_hits) {
				if (hit == null)
					continue;
				int i = 0;
				while (i < hit.Properties.Count) {
					Property prop = hit.Properties [i] as Property;
					if (prop.Key.StartsWith (PrivateNamespace))
						hit.Properties.RemoveAt (i);
					else
						++i;
				}
			}

			result.Add (final_list_of_hits);

			c.Stop ();
			total.Stop ();

			Logger.Log.Debug (">>> GenerateQueryResults time statistics:");
			Logger.Log.Debug (">>>   First pass {0} ({1:0.0}%)", a, 100 * a.ElapsedTime / total.ElapsedTime);
			Logger.Log.Debug (">>> Hit assembly {0} ({1:0.0}%)", b, 100 * b.ElapsedTime / total.ElapsedTime);
			Logger.Log.Debug (">>>   Final pass {0} ({1:0.0}%)", c, 100 * c.ElapsedTime / total.ElapsedTime);
			Logger.Log.Debug (">>>        TOTAL {0}", total);
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
			    || primary_matches.TrueCount < max_match_count)
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
			if (first_into_second == null || first_into_second.TrueCount == 0) 
				filtered_second_matches = second_matches;
			else {
				filtered_second_matches = new BetterBitArray (second_matches);
				filtered_second_matches.AndNot (first_into_second);
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
				info.UpperBound = info.PrimaryMatches.TrueCount + info.SecondaryMatches.TrueCount;

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
				    && info.PrimaryMatches.TrueCount >= max_match_count)
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
