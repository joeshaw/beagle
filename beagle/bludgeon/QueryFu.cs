
using System;
using System.Collections;
using System.Threading;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class QueryFu {

		static public Query NewTokenQuery (string token)
		{
			Query query;
			query = new Query ();

			QueryPart_Text part;
			part = new QueryPart_Text ();
			part.Text = token;
			query.AddPart (part);

			return query;
		}

		static public Query NewTokenQuery (int id)
		{
			return NewTokenQuery (Token.IdToString (id));
		}

		static Random random = new Random ();

		static public Query NewRandomQuery (int length,
						    bool allow_inexpensive)
		{
			return NewRandomQuery (length, allow_inexpensive, false);
		}

		static private Query NewRandomQuery (int length,
						     bool allow_inexpensive,
						     bool inside_an_or)
		{
			Query query;
			query = new Query ();

			// One in four queries will contain some OR terms.
			if (! inside_an_or && random.Next (4) == 0) {
				int N = random.Next (3) + 1;
				for (int i = 0; i < N; ++i) {
					QueryPart_Or part;
					part = new QueryPart_Or ();

					int sub_length;
					sub_length = random.Next (length) + 1;
					if (sub_length < 2)
						sub_length = 2;
					
					// We generate a new query at random, and stuff its QueryParts
					// into our Or QueryPart.
					Query or_query;
					or_query = NewRandomQuery (sub_length, allow_inexpensive, true);
					foreach (QueryPart sub_part in or_query.Parts)
						part.Add (sub_part);
					
					query.AddPart (part);
				}
			}

			if (allow_inexpensive && ! inside_an_or) {
				int mime_type;
				mime_type = random.Next (3);
				if (mime_type == 0)
					query.AddMimeType ("inode/directory");
				else if (mime_type == 1)
					query.AddMimeType ("text/plain");
			}

			// Every query must contain at least
			// one required part.
			bool contains_required;
			contains_required = false;

			for (int i = 0; i < length; ++i) {
				QueryPart_Text part;
				part = new QueryPart_Text ();
				part.Text = Token.GetRandom ();

				// Prohibited parts are not allowed inside an or
				if (contains_required && ! inside_an_or) {
					if (random.Next (2) == 0)
						part.Logic = QueryPartLogic.Prohibited;
				} else {
					// This part will be required.
					contains_required = true;
				}
				
				if (random.Next (2) == 0)
					part.SearchTextProperties = false;
				else if (allow_inexpensive && random.Next (2) == 0)
					part.SearchFullText = false;
				
				query.AddPart (part);
			}

			// Note the ! inside_an_or; date range queries don't
			// work right inside OR queries when being searched
			// within the resolution of one day.  See the FIXME
			// about hit filters in LuceneCommon.cs
			if (allow_inexpensive && ! inside_an_or && random.Next (3) == 0) {
				DateTime a, b;
				FileSystemObject.PickTimestampRange (out a, out b);

				QueryPart_DateRange part;
				part = new QueryPart_DateRange ();
				part.StartDate = a;
				part.EndDate = b;
				query.AddPart (part);
			}

			return query;
		}

		static public Query NewRandomQuery ()
		{
			return NewRandomQuery (2 + random.Next (4), true);
		}

		/////////////////////////////////////////////////////////////

		static public string QueryPartToString (QueryPart abstract_part)
		{
			string msg;
			msg = "????";
				
			if (abstract_part is QueryPart_Text) {
				QueryPart_Text part;
				part = (QueryPart_Text) abstract_part;
				
				msg = part.Text;
				if (! (part.SearchFullText && part.SearchTextProperties)) {
					if (part.SearchFullText)
						msg += " IN FULLTEXT";
					else if (part.SearchTextProperties)
						msg += " IN TEXT PROPERTIES";
				} else
					msg += " IN ANY TEXT";

			} else if (abstract_part is QueryPart_Property) {
				QueryPart_Property part;
				part = (QueryPart_Property) abstract_part;
				msg = String.Format ("PROPERTY {0} = {1}", part.Key, part.Value);
			} else if (abstract_part is QueryPart_DateRange) {
				QueryPart_DateRange part;
				part = (QueryPart_DateRange) abstract_part;
				msg = String.Format ("DATE RANGE {0} to {1}", part.StartDate, part.EndDate);
			}

			if (abstract_part.Logic == QueryPartLogic.Prohibited)
				msg = "NOT " + msg;

			
			return msg;
		}

		static public void SpewQuery (Query query)
		{
			int i = 0;

			foreach (QueryPart abstract_part in query.Parts) {

				++i;

				if (abstract_part is QueryPart_Or) {
					QueryPart_Or part = abstract_part as QueryPart_Or;
					int j = 0;
					Log.Spew ("{0}: OR", i);
					foreach (QueryPart sub_part in part.SubParts) {
						++j;
						Log.Spew ("    {0}.{1}: {2}", i, j, QueryPartToString (sub_part));
					}

				} else {
					Log.Spew ("{0}: {1}", i, QueryPartToString (abstract_part));
				}
			}
		}

		/////////////////////////////////////////////////////////////

		private class QueryClosure {
			
			public Hashtable Hits;
			private Query query;

			public QueryClosure (Query query)
			{
				this.Hits = UriFu.NewHashtable ();
				this.query = query;
			}

			public void OnHitsAdded (HitsAddedResponse response)
			{
				foreach (Hit hit in response.Hits)
					Hits [hit.Uri] = hit;
			}

			public void OnFinished ()
			{
				query.Close ();
				
			}
		}
		
		static public Hashtable GetHits (Query q)
		{
			QueryClosure qc;
			qc = new QueryClosure (q);
			q.HitsAddedEvent += qc.OnHitsAdded;
			q.FinishedEvent += qc.OnFinished;
			
			q.SendAsyncBlocking ();

			return qc.Hits;
		}

		static public ICollection GetUris (Query q)
		{
			return GetHits (q).Keys;
		}
	}
}
