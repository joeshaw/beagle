
using System;
using System.Collections;
using System.Threading;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class QueryFu {

		static public void AddToken (Query query, int id)
		{
			if (id < 0 || id >= Token.Count)
				return;

			QueryPart_Text part;
			part = new QueryPart_Text ();
			part.Logic = QueryPartLogic.Required;
			part.Text = Token.GetString (id);

			query.AddPart (part);
		}

		static public void AddName (Query query, FileModel file)
		{
			QueryPart_Property part;
			part = new QueryPart_Property ();
			part.Logic = QueryPartLogic.Required;
			part.Type = PropertyType.Keyword;
			part.Key = "_private:ExactFilename";
			part.Value = file.Name;

			query.AddPart (part);
		}

		static public void AddBody (Query query, FileModel file)
		{
			for (int i = 0; i < file.Body.Length; ++i)
				AddToken (query, file.Body [i]);
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
