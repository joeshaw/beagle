using System;
using System.Text;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001 The Apache Software Foundation.  All rights
	 * reserved.
	 *
	 * Redistribution and use in source and binary forms, with or without
	 * modification, are permitted provided that the following conditions
	 * are met:
	 *
	 * 1. Redistributions of source code must retain the above copyright
	 *    notice, this list of conditions and the following disclaimer.
	 *
	 * 2. Redistributions in binary form must reproduce the above copyright
	 *    notice, this list of conditions and the following disclaimer in
	 *    the documentation and/or other materials provided with the
	 *    distribution.
	 *
	 * 3. The end-user documentation included with the redistribution,
	 *    if any, must include the following acknowledgment:
	 *       "This product includes software developed by the
	 *        Apache Software Foundation (http://www.apache.org/)."
	 *    Alternately, this acknowledgment may appear in the software itself,
	 *    if and wherever such third-party acknowledgments normally appear.
	 *
	 * 4. The names "Apache" and "Apache Software Foundation" and
	 *    "Apache Lucene" must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    "Apache Lucene", nor may "Apache" appear in their name, without
	 *    prior written permission of the Apache Software Foundation.
	 *
	 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
	 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
	 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	 * DISCLAIMED.  IN NO EVENT SHALL THE APACHE SOFTWARE FOUNDATION OR
	 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
	 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
	 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
	 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
	 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
	 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
	 * SUCH DAMAGE.
	 * ====================================================================
	 *
	 * This software consists of voluntary contributions made by many
	 * individuals on behalf of the Apache Software Foundation.  For more
	 * information on the Apache Software Foundation, please see
	 * <http://www.apache.org/>.
	 */

	/// <summary>
	/// A Query that matches documents containing a particular sequence of terms.
	/// This may be combined with other terms with a {@link BooleanQuery}.
	/// </summary>
	[Serializable]
	public class PhraseQuery : Query 
	{
		private String field;
		private ArrayList terms = new ArrayList();
		private int slop = 0;

		/// <summary>
		/// Constructs an empty phrase query.
		/// </summary>
		public PhraseQuery() {}

		/// <summary>
		/// Sets the number of other words permitted between words in query phrase.
		/// If zero, then this is an exact phrase search.  For larger values this works
		/// like a <code>WITHIN</code> or <code>NEAR</code> operator.
		/// <p>The slop is in fact an edit-distance, where the units correspond to
		/// moves of terms in the query phrase out of position.  For example, to switch
		/// the order of two words requires two moves (the first move places the words
		/// atop one another), so to permit re-orderings of phrases, the slop must be
		/// at least two.
		/// </p>
		/// <p>More exact matches are scored higher than sloppier matches, thus search
		/// results are sorted by exactness.
		/// </p>
		/// <p>The slop is zero by default, requiring exact matches.</p>
		/// </summary>
		/// <param name="s"></param>
		public void SetSlop(int s) { slop = s; }

		/// <summary>
		/// Returns the slop. See SetSlop().
		/// </summary>
		/// <returns></returns>
		public int GetSlop() { return slop; }

		/// <summary>
		/// Adds a term to the end of the query phrase.
		/// </summary>
		/// <param name="term"></param>
		public void Add(Term term) 
		{
			if (terms.Count == 0)
				field = term.Field();
			else if (term.Field() != field)
				throw new ArgumentException
					("All phrase terms must be in the same field: " + term);

			terms.Add(term);
		}

		/// <summary>
		/// Returns the set of terms in this phrase.
		/// </summary>
		/// <returns></returns>
		public Term[] GetTerms() 
		{
			return (Term[])terms.ToArray(typeof(Term));
		}

		[Serializable]
		private class PhraseWeight : Weight 
		{
			private Searcher searcher;
			private float value;
			private float idf;
			private float queryNorm;
			private float queryWeight;
			private PhraseQuery phraseQuery;

			public PhraseWeight(Searcher searcher, PhraseQuery phraseQuery) 
			{
				this.searcher = searcher;
				this.phraseQuery = phraseQuery;
			}

			public Query GetQuery() { return phraseQuery; }
			public float GetValue() { return value; }

			public float SumOfSquaredWeights()  
			{
				idf = searcher.GetSimilarity().Idf(phraseQuery.terms, searcher);
				queryWeight = idf * phraseQuery.GetBoost();             // compute query weight
				return queryWeight * queryWeight;           // square it
			}

			public void Normalize(float queryNorm) 
			{
				this.queryNorm = queryNorm;
				queryWeight *= queryNorm;                   // normalize query weight
				value = queryWeight * idf;                  // idf for document 
			}

			public Scorer Scorer(IndexReader reader)  
			{
				if (phraseQuery.terms.Count == 0)			  // optimize zero-term case
					return null;

				TermPositions[] tps = new TermPositions[phraseQuery.terms.Count];
				for (int i = 0; i < phraseQuery.terms.Count; i++) 
				{
					TermPositions p = reader.TermPositions((Term)phraseQuery.terms[i]);
					if (p == null)
						return null;
					tps[i] = p;
				}

				if (phraseQuery.slop == 0)				  // optimize exact case
					return new ExactPhraseScorer(this, tps, searcher.GetSimilarity(),
						reader.Norms(phraseQuery.field));
				else
					return
						new SloppyPhraseScorer(this, tps, searcher.GetSimilarity(), 
						phraseQuery.slop,
						reader.Norms(phraseQuery.field));
      
			}

			public Explanation Explain(IndexReader reader, int doc)
			{

				Explanation result = new Explanation();
				result.SetDescription("weight("+GetQuery()+" in "+doc+"), product of:");

				StringBuilder docFreqs = new StringBuilder();
				StringBuilder query = new StringBuilder();
				query.Append('\"');
				for (int i = 0; i < phraseQuery.terms.Count; i++) 
				{
					if (i != 0) 
					{
						docFreqs.Append(" ");
						query.Append(" ");
					}

					Term term = (Term)phraseQuery.terms[i];

					docFreqs.Append(term.Text());
					docFreqs.Append("=");
					docFreqs.Append(searcher.DocFreq(term));

					query.Append(term.Text());
				}
				query.Append('\"');

				Explanation idfExpl =
					new Explanation(idf, "idf(" + phraseQuery.field + ": " + docFreqs + ")");
      
				// explain query weight
				Explanation queryExpl = new Explanation();
				queryExpl.SetDescription("queryWeight(" + GetQuery() + "), product of:");

				Explanation boostExpl = new Explanation(phraseQuery.GetBoost(), "boost");
				if (phraseQuery.GetBoost() != 1.0f)
					queryExpl.AddDetail(boostExpl);
				queryExpl.AddDetail(idfExpl);
      
				Explanation queryNormExpl = new Explanation(queryNorm,"queryNorm");
				queryExpl.AddDetail(queryNormExpl);
      
				queryExpl.SetValue(boostExpl.GetValue() *
					idfExpl.GetValue() *
					queryNormExpl.GetValue());

				result.AddDetail(queryExpl);
     
				// explain field weight
				Explanation fieldExpl = new Explanation();
				fieldExpl.SetDescription("fieldWeight("+phraseQuery.field+":"+query+" in "+doc+
					"), product of:");

				Explanation tfExpl = Scorer(reader).Explain(doc);
				fieldExpl.AddDetail(tfExpl);
				fieldExpl.AddDetail(idfExpl);

				Explanation fieldNormExpl = new Explanation();
				fieldNormExpl.SetValue(Similarity.DecodeNorm(reader.Norms(phraseQuery.field)[doc]));
				fieldNormExpl.SetDescription("fieldNorm(field="+phraseQuery.field+", doc="+doc+")");
				fieldExpl.AddDetail(fieldNormExpl);

				fieldExpl.SetValue(tfExpl.GetValue() *
					idfExpl.GetValue() *
					fieldNormExpl.GetValue());
      
				result.AddDetail(fieldExpl);

				// combine them
				result.SetValue(queryExpl.GetValue() * fieldExpl.GetValue());

				if (queryExpl.GetValue() == 1.0f)
					return fieldExpl;

				return result;
			}
		}

		public override Weight CreateWeight(Searcher searcher) 
		{
			if (terms.Count == 1) 
			{			  // optimize one-term case
				Term term = (Term)terms[0];
				Query termQuery = new TermQuery(term);
				termQuery.SetBoost(GetBoost());
				return termQuery.CreateWeight(searcher);
			}
			return new PhraseWeight(searcher, this);
		}

		/// <summary>
		/// Prints a user-readable version of this query.
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		public override String ToString(String f) 
		{
			StringBuilder buffer = new StringBuilder();
			if (!field.Equals(f)) 
			{
				buffer.Append(field);
				buffer.Append(":");
			}

			buffer.Append("\"");
			for (int i = 0; i < terms.Count; i++) 
			{
				buffer.Append(((Term)terms[i]).Text());
				if (i != terms.Count-1)
					buffer.Append(" ");
			}
			buffer.Append("\"");

			if (slop != 0) 
			{
				buffer.Append("~");
				buffer.Append(slop);
			}

			if (GetBoost() != 1.0f) 
			{
				buffer.Append("^");
				buffer.Append(Number.ToString(GetBoost()));
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Returns true iff <code>o</code> is equal to this.
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public override bool Equals(Object o) 
		{
			if (!(o is PhraseQuery))
				return false;
			PhraseQuery other = (PhraseQuery)o;
			return (this.GetBoost() == other.GetBoost())
				&& (this.slop == other.slop)
				&&  this.terms.Equals(other.terms);
		}

		/// <summary>
		/// Returns a hash code value for this object.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() 
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0)
				^ BitConverter.ToInt32(BitConverter.GetBytes(slop), 0)
				^ terms.GetHashCode();
		}
	}
}