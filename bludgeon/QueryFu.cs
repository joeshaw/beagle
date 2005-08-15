
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

		static public Query NewRandomQuery (int  length,
						    bool allow_inexpensive)
		{
			Query query;
			query = new Query ();

			if (allow_inexpensive) {
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

				if (contains_required) {
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

			return query;
		}

		static public Query NewRandomQuery ()
		{
			return NewRandomQuery (2 + random.Next (4), true);
		}

		/////////////////////////////////////////////////////////////

		static public void SpewQuery (Query query)
		{
			int i = 0;

			foreach (QueryPart abstract_part in query.Parts) {

				++i;

				string msg;
				msg = "????";
				
				if (abstract_part is QueryPart_Text) {
					QueryPart_Text part;
					part = (QueryPart_Text) abstract_part;

					msg = "";
					if (part.Logic == QueryPartLogic.Prohibited)
						msg = "NOT ";
					msg += part.Text;

					if (! (part.SearchFullText && part.SearchTextProperties)) {
						if (part.SearchFullText)
							msg += " IN FULLTEXT";
						else if (part.SearchTextProperties)
							msg += " IN TEXT PROPERTIES";
					}
				} else if (abstract_part is QueryPart_Property) {
					QueryPart_Property part;
					part = (QueryPart_Property) abstract_part;
					msg = String.Format ("PROPERTY {0} = {1}", part.Key, part.Value);
				}
				
				Log.Spew ("{0}: {1}", i, msg);
			}
		}

		/////////////////////////////////////////////////////////////

		private class QueryClosure {
			
			public ArrayList Hits = new ArrayList ();

			private Query query;

			public QueryClosure (Query query)
			{
				this.query = query;
			}

			public void OnHitsAdded (HitsAddedResponse response)
			{
				Hits.AddRange (response.Hits);
			}

			public void OnFinished (FinishedResponse response)
			{
				query.Close ();
				
			}
		}
		
		static public ArrayList GetHits (Query q)
		{
			QueryClosure qc;
			qc = new QueryClosure (q);
			q.HitsAddedEvent += qc.OnHitsAdded;
			q.FinishedEvent += qc.OnFinished;
			
			q.SendAsyncBlocking ();

			return qc.Hits;
		}

		static public ArrayList GetUris (Query q)
		{
			ArrayList array;
			array = GetHits (q);
			for (int i = 0; i < array.Count; ++i)
				array [i] = ((Beagle.Hit) array [i]).Uri;
			return array;
		}
	}
}
