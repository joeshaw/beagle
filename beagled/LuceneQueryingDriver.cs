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

		static public bool Debug = false;

		public const string PrivateNamespace = "_private:";

		public delegate bool UriFilter (Uri uri);
		public delegate double RelevancyMultiplier (Hit hit);

		public LuceneQueryingDriver (string index_name, int minor_version, bool read_only) 
			: base (index_name, minor_version)
		{
			// FIXME: Maybe the LuceneQueryingDriver should never try to create the index?
			if (Exists ())
				Open (read_only);
			else if (!read_only)
				Create ();
			else {
				// We're in read-only mode, but we can't create an index.
				// Maybe a different exception would be better?  This one is caught
				// in QueryDriver.LoadStaticQueryable ()
				throw new InvalidOperationException ();
			}
		}

		////////////////////////////////////////////////////////////////


		////////////////////////////////////////////////////////////////

		public Uri[] PropertyQuery (Property prop)
		{
			// FIXME: Should we support scanning the secondary
			// index as well?

			IndexReader primary_reader;
			LNS.IndexSearcher primary_searcher;

			primary_reader = LuceneCommon.GetReader (PrimaryStore);
			primary_searcher = new LNS.IndexSearcher (primary_reader);

			Term term = new Term (PropertyToFieldName (prop.Type, prop.Key), prop.Value);
			LNS.TermQuery query = new LNS.TermQuery (term);
			LNS.Hits hits = primary_searcher.Search (query);

			Uri[] uri_list = new Uri [hits.Length ()];
			for (int i = 0; i < hits.Length (); i++) {
				Document doc;
				doc = hits.Doc (i);
				uri_list [i] = GetUriFromDocument (doc);
			}

			primary_searcher.Close ();

			return uri_list;
		}

		////////////////////////////////////////////////////////////////

		// Returns the lowest matching score before the results are
		// truncated.
		public void DoQuery (Query               query,
				     IQueryResult        result,
				     ICollection         search_subset_uris, // should be internal uris
				     UriFilter           uri_filter,
				     HitFilter           hit_filter)
		{
			Stopwatch sw;
			sw = new Stopwatch ();
			sw.Start ();

			// Assemble all of the parts into a bunch of Lucene queries

			ArrayList primary_required_part_queries = null;
			ArrayList secondary_required_part_queries = null;

			LNS.BooleanQuery primary_prohibited_part_query = null;
			LNS.BooleanQuery secondary_prohibited_part_query = null;

			AndHitFilter all_hit_filters;
			all_hit_filters = new AndHitFilter ();
			if (hit_filter != null)
				all_hit_filters.Add (hit_filter);

			ArrayList term_list = new ArrayList ();

			foreach (QueryPart part in query.Parts) {
				LNS.Query primary_part_query;
				LNS.Query secondary_part_query;
				HitFilter part_hit_filter;
				QueryPartToQuery (part,
						  false, // we want both primary and secondary queries
						  part.Logic == QueryPartLogic.Required ? term_list : null,
						  out primary_part_query,
						  out secondary_part_query,
						  out part_hit_filter);

				if (primary_part_query == null)
					continue;

				switch (part.Logic) {
					
				case QueryPartLogic.Required:
					if (primary_required_part_queries == null) {
						primary_required_part_queries = new ArrayList ();
						secondary_required_part_queries = new ArrayList ();
					}
					primary_required_part_queries.Add (primary_part_query);
					secondary_required_part_queries.Add (secondary_part_query);
					
					if (part_hit_filter != null)
						all_hit_filters.Add (part_hit_filter);
					
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

					if (part_hit_filter != null) {
						NotHitFilter nhf;
						nhf = new NotHitFilter (part_hit_filter);
						all_hit_filters.Add (new HitFilter (nhf.HitFilter));
					}

					break;
				}

				// We assume that QueryPartToQuery does the right thing when it returns
				// a hit filter associated with a Prohibited part, and that we don't
				// have to invert it or anything like that.
				if (part_hit_filter != null)
					all_hit_filters.Add (part_hit_filter);
			}

			// If we have no required parts, give up.
			if (primary_required_part_queries == null)
				return;
			
			//
			// Now that we have all of these nice queries, let's execute them!
			//

			// Create the searchers that we will need.

			IndexReader primary_reader;
			LNS.IndexSearcher primary_searcher;
			IndexReader secondary_reader = null;
			LNS.IndexSearcher secondary_searcher = null;

			primary_reader = LuceneCommon.GetReader (PrimaryStore);
			primary_searcher = new LNS.IndexSearcher (primary_reader);
			
			if (SecondaryStore != null) {
				secondary_reader = LuceneCommon.GetReader (SecondaryStore);
				if (secondary_reader.NumDocs () == 0) {
					secondary_reader.Close ();
					secondary_reader = null;
				}
			}

			if (secondary_reader != null)
				secondary_searcher = new LNS.IndexSearcher (secondary_reader);


			// Possibly create our whitelists from the search subset.
			
			LuceneBitArray primary_whitelist = null;
			LuceneBitArray secondary_whitelist = null;
			
			if (search_subset_uris != null && search_subset_uris.Count > 0) {
				primary_whitelist = new LuceneBitArray (primary_searcher);
				if (secondary_searcher != null)
					secondary_whitelist = new LuceneBitArray (secondary_searcher);

				foreach (Uri uri in search_subset_uris) {
					primary_whitelist.AddUri (uri);
					if (secondary_whitelist != null)
						secondary_whitelist.AddUri (uri);
				}
				primary_whitelist.FlushUris ();
				if (secondary_whitelist != null)
					secondary_whitelist.FlushUris ();
			}


			// Build blacklists from our prohibited parts.
			
			LuceneBitArray primary_blacklist = null;
			LuceneBitArray secondary_blacklist = null;

			if (primary_prohibited_part_query != null) {
				primary_blacklist = new LuceneBitArray (primary_searcher,
									primary_prohibited_part_query);
				
				if (secondary_searcher != null) {
					secondary_blacklist = new LuceneBitArray (secondary_searcher);
					if (secondary_prohibited_part_query != null)
						secondary_blacklist.Or (secondary_prohibited_part_query);
					primary_blacklist.Join (secondary_blacklist);
				}
			}

			
			// Combine our whitelist and blacklist into just a whitelist.
			
			if (primary_blacklist != null) {
				if (primary_whitelist == null) {
					primary_blacklist.Not ();
					primary_whitelist = primary_blacklist;
				} else {
					primary_whitelist.AndNot (primary_blacklist);
				}
			}

			if (secondary_blacklist != null) {
				if (secondary_whitelist == null) {
					secondary_blacklist.Not ();
					secondary_whitelist = secondary_blacklist;
				} else {
					secondary_whitelist.AndNot (secondary_blacklist);
				}
			}

			BetterBitArray primary_matches = null;

			if (primary_required_part_queries != null) {

				if (secondary_searcher != null)
					primary_matches = DoRequiredQueries_TwoIndex (primary_searcher,
										      secondary_searcher,
										      primary_required_part_queries,
										      secondary_required_part_queries,
										      primary_whitelist,
										      secondary_whitelist);
				else
					primary_matches = DoRequiredQueries (primary_searcher,
									     primary_required_part_queries,
									     primary_whitelist);

			} 

			sw.Stop ();
			if (Debug)
				Logger.Log.Debug ("###### Finished low-level queries in {0}", sw);
			sw.Reset ();
			sw.Start ();

			// Only generate results if we got some matches
			if (primary_matches != null && primary_matches.ContainsTrue ()) {
				GenerateQueryResults (primary_reader,
						      primary_searcher,
						      secondary_searcher,
						      primary_matches,
						      result,
						      term_list,
						      query.MaxHits,
						      uri_filter,
						      new HitFilter (all_hit_filters.HitFilter));
			}

			//
			// Finally, we clean up after ourselves.
			//
			
			primary_searcher.Close ();
			if (secondary_searcher != null)
				secondary_searcher.Close ();


			sw.Stop ();
			if (Debug)
				Logger.Log.Debug ("###### Processed query in {0}", sw);

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

			LuceneBitArray matches;
			matches = new LuceneBitArray (primary_searcher, combined_query);
			if (primary_whitelist != null)
				matches.And (primary_whitelist);

			return matches;
		}

		// This code attempts to execute N required queries in the
		// most efficient order to minimize the amount of time spent
		// joining between the two indexes.  It returns a joined bit
		// array of matches against the primary index.

		private class MatchInfo : IComparable {

			public LuceneBitArray PrimaryMatches = null;
			public LuceneBitArray SecondaryMatches = null;
			public int UpperBound = 0;

			public void Join ()
			{
				PrimaryMatches.Join (SecondaryMatches);
			}

			public void RestrictBy (MatchInfo joined)
			{
				if (joined != null) {
					this.PrimaryMatches.And (joined.PrimaryMatches);
					this.SecondaryMatches.And (joined.SecondaryMatches);
				}

				UpperBound = 0;
				UpperBound += PrimaryMatches.TrueCount;
				UpperBound += SecondaryMatches.TrueCount;
			}

			public int CompareTo (object obj)
			{
				MatchInfo other = (MatchInfo) obj;
				return this.UpperBound - other.UpperBound;
			}
		}

		// Any whitelists that are passed in must be fully joined, or
		// query results will be incorrect.
		private static BetterBitArray DoRequiredQueries_TwoIndex (LNS.IndexSearcher primary_searcher,
									  LNS.IndexSearcher secondary_searcher,
									  ArrayList primary_queries,
									  ArrayList secondary_queries,
									  BetterBitArray primary_whitelist,
									  BetterBitArray secondary_whitelist)
		{
			ArrayList match_info_list;
			match_info_list = new ArrayList ();

			// First, do all of the low-level queries
			// and store them in our MatchInfo 
			for (int i = 0; i < primary_queries.Count; ++i) {
				LNS.Query pq, sq;
				pq = primary_queries [i] as LNS.Query;
				sq = secondary_queries [i] as LNS.Query;

				LuceneBitArray p_matches = null, s_matches = null;
				p_matches = new LuceneBitArray (primary_searcher);
				if (pq != null) {
					p_matches.Or (pq);
					if (primary_whitelist != null)
						p_matches.And (primary_whitelist);
				}

				s_matches = new LuceneBitArray (secondary_searcher);
				if (sq != null) {
					s_matches.Or (sq);
					if (secondary_whitelist != null)
						s_matches.And (secondary_whitelist);
				}

				MatchInfo info;
				info = new MatchInfo ();
				info.PrimaryMatches = p_matches;
				info.SecondaryMatches = s_matches;
				info.RestrictBy (null); // a hack to initialize the UpperBound
				match_info_list.Add (info);
			}

			// We want to be smart about the order we do this in,
			// to minimize the expense of the Join.
			while (match_info_list.Count > 1) {

				// FIXME: We don't really need to sort here, it would
				// be sufficient to just find the minimal element.
				match_info_list.Sort ();
				MatchInfo smallest;
				smallest = match_info_list [0] as MatchInfo;
				match_info_list.RemoveAt (0);

				// We can short-circuit if our smallest set of
				// matches is empty.
				if (smallest.UpperBound == 0)
					return smallest.PrimaryMatches; // this must be an empty array.

				smallest.Join ();

				foreach (MatchInfo info in match_info_list)
					info.RestrictBy (smallest);
			}
			
			// For the final pair, we don't need to do a full join:
			// mapping the secondary onto the primary is sufficient
			MatchInfo last;
			last = match_info_list [0] as MatchInfo;
			last.SecondaryMatches.ProjectOnto (last.PrimaryMatches);

			return last.PrimaryMatches;
		}		

		////////////////////////////////////////////////////////////////

		static private void ScoreHits (Hashtable   hits_by_id,
					       IndexReader reader,
					       ICollection term_list)
		{
			Stopwatch sw;
			sw = new Stopwatch ();
			sw.Start ();

			LNS.Similarity similarity;
			similarity = LNS.Similarity.GetDefault ();

			foreach (Term term in term_list) {

				double idf;
				idf = similarity.Idf (reader.DocFreq (term), reader.MaxDoc ());

				int hit_count;
				hit_count = hits_by_id.Count;

				TermDocs term_docs;
				term_docs = reader.TermDocs (term);
				while (term_docs.Next () && hit_count > 0) {
					
					int id;
					id = term_docs.Doc ();

					Hit hit;
					hit = hits_by_id [id] as Hit;
					if (hit != null) {
						double tf;
						tf = similarity.Tf (term_docs.Freq ());
						hit.Score += tf * idf;
						--hit_count;
					}
				}
			}

			sw.Stop ();
		}

		////////////////////////////////////////////////////////////////

		private class DocAndId {
			public Document Doc;
			public int Id;
		}

		//
		// Given a set of hits, broadcast some set out as our query
		// results.
		//

		private static void GenerateQueryResults (IndexReader       primary_reader,
							  LNS.IndexSearcher primary_searcher,
							  LNS.IndexSearcher secondary_searcher,
							  BetterBitArray    primary_matches,
							  IQueryResult      result,
							  ICollection       query_term_list,
							  int               max_results,
							  UriFilter         uri_filter,
							  HitFilter         hit_filter)
		{
			TopScores top_docs = null;
			ArrayList all_docs = null;

			if (Debug)
				Logger.Log.Debug (">>> Initially handed {0} matches", primary_matches.TrueCount);

			if (primary_matches.TrueCount <= max_results) {
				if (Debug)
					Logger.Log.Debug (">>> Initial count is within our limit of {0}", max_results);
				all_docs = new ArrayList ();
			} else {
				if (Debug)
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

				// Check the timestamp --- if we have already reached our
				// limit, we might be able to reject it immediately.
				string timestamp_str;
				long timestamp_num = 0;

				timestamp_str = doc.Get ("Timestamp");
				if (timestamp_str == null) {
					Logger.Log.Warn ("No timestamp on {0}!", GetUriFromDocument (doc));
				} else {
					timestamp_num = Int64.Parse (doc.Get ("Timestamp"));
					if (top_docs != null && ! top_docs.WillAccept (timestamp_num))
						continue;
				}

				// If we have a UriFilter, apply it.
				if (uri_filter != null) {
					Uri uri;
					uri = GetUriFromDocument (doc);
					if (! uri_filter (uri)) 
						continue;
				}

				DocAndId doc_and_id = new DocAndId ();
				doc_and_id.Doc = doc;
				doc_and_id.Id = i;

				// Add the document to the appropriate data structure.
				// We use the timestamp_num as the score, so high
				// scores correspond to more-recent timestamps.
				if (all_docs != null)
					all_docs.Add (doc_and_id);
				else
					top_docs.Add (timestamp_num, doc_and_id);
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

			// This is used only for scoring
			Hashtable hits_by_id = null;
			hits_by_id = new Hashtable ();

			// If we aren't using the secondary index, the next step is
			// very straightforward.
			if (secondary_searcher == null) {

				foreach (DocAndId doc_and_id in final_list_of_docs) {
					Hit hit;
					hit = DocumentToHit (doc_and_id.Doc);
					hits_by_id [doc_and_id.Id] = hit;
					final_list_of_hits.Add (hit);
				}

			} else {

				if (Debug)
					Logger.Log.Debug (">>> Performing cross-index Hit reunification");

				Hashtable hits_by_uri;
				hits_by_uri = UriFu.NewHashtable ();

				LuceneBitArray secondary_matches;
				secondary_matches = new LuceneBitArray (secondary_searcher);

				foreach (DocAndId doc_and_id in final_list_of_docs) {
					Hit hit;
					hit = DocumentToHit (doc_and_id.Doc);
					hits_by_id [doc_and_id.Id] = hit;
					hits_by_uri [hit.Uri] = hit;
					secondary_matches.AddUri (hit.Uri);
				}

				secondary_matches.FlushUris ();
				
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

					final_list_of_hits.Add (hit);
				}
			}

			ScoreHits (hits_by_id, primary_reader, query_term_list);

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
					if (! hit_filter (hit)) {
						if (Debug)
							Logger.Log.Debug ("Filtered out {0}", hit.Uri);
						final_list_of_hits [i] = null;
					}
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

			if (Debug) {
				Logger.Log.Debug (">>> GenerateQueryResults time statistics:");
				Logger.Log.Debug (">>>   First pass {0} ({1:0.0}%)", a, 100 * a.ElapsedTime / total.ElapsedTime);
				Logger.Log.Debug (">>> Hit assembly {0} ({1:0.0}%)", b, 100 * b.ElapsedTime / total.ElapsedTime);
				Logger.Log.Debug (">>>   Final pass {0} ({1:0.0}%)", c, 100 * c.ElapsedTime / total.ElapsedTime);
				Logger.Log.Debug (">>>        TOTAL {0}", total);
			}
		}

	}
}
