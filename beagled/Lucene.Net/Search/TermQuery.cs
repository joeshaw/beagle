using System;
using System.Text;
using Lucene.Net.Util;
using Lucene.Net.Index;

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
	/// A Query that matches documents containing a term.
	/// This may be combined with other terms with a BooleanQuery.
	/// </summary>
	[Serializable]
	public class TermQuery : Query 
	{
		private Term term;

		[Serializable]
		private class TermWeight : Weight 
		{
			private Searcher searcher;
			private float value;
			private float idf;
			private float queryNorm;
			private float queryWeight;
			private TermQuery termQuery;

			public TermWeight(Searcher searcher, TermQuery termQuery) 
			{
				this.searcher = searcher;
				this.termQuery = termQuery;
			}

			public Query GetQuery() { return termQuery; }
			public float GetValue() { return value; }

			public float SumOfSquaredWeights()  
			{
				idf = searcher.GetSimilarity().Idf(termQuery.term, searcher); // compute idf
				queryWeight = idf * termQuery.GetBoost();             // compute query weight
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
				TermDocs termDocs = reader.TermDocs(termQuery.term);
      
				if (termDocs == null)
					return null;
      
				return new TermScorer(this, termDocs, searcher.GetSimilarity(),
					reader.Norms(termQuery.term.Field()));
			}

			public Explanation Explain(IndexReader reader, int doc)
			{

				Explanation result = new Explanation();
				result.SetDescription("weight("+GetQuery()+" in "+doc+"), product of:");

				Explanation idfExpl =
					new Explanation(idf, "idf(docFreq=" + searcher.DocFreq(termQuery.term) + ")");

				// explain query weight
				Explanation queryExpl = new Explanation();
				queryExpl.SetDescription("queryWeight(" + GetQuery() + "), product of:");

				Explanation boostExpl = new Explanation(termQuery.GetBoost(), "boost");
				if (termQuery.GetBoost() != 1.0f)
					queryExpl.AddDetail(boostExpl);
				queryExpl.AddDetail(idfExpl);
      
				Explanation queryNormExpl = new Explanation(queryNorm,"queryNorm");
				queryExpl.AddDetail(queryNormExpl);
      
				queryExpl.SetValue(boostExpl.GetValue() *
					idfExpl.GetValue() *
					queryNormExpl.GetValue());

				result.AddDetail(queryExpl);
     
				// explain field weight
				String field = termQuery.term.Field();
				Explanation fieldExpl = new Explanation();
				fieldExpl.SetDescription("fieldWeight("+termQuery.term+" in "+doc+
					"), product of:");

				Explanation tfExpl = Scorer(reader).Explain(doc);
				fieldExpl.AddDetail(tfExpl);
				fieldExpl.AddDetail(idfExpl);

				Explanation fieldNormExpl = new Explanation();
				fieldNormExpl.SetValue(Similarity.DecodeNorm(reader.Norms(field)[doc]));
				fieldNormExpl.SetDescription("fieldNorm(field="+field+", doc="+doc+")");
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

		/// <summary>
		/// Constructs a query for the term <code>t</code>.
		/// </summary>
		/// <param name="t"></param>
		public TermQuery(Term t) 
		{
			term = t;
		}

		/// <summary>
		/// Returns the term of this query.
		/// </summary>
		/// <returns></returns>
		public Term GetTerm() { return term; }

		public override Weight CreateWeight(Searcher searcher) 
		{
			return new TermWeight(searcher, this);
		}

		/// <summary>
		/// Prints a user-readable version of this query.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public override String ToString(String field) 
		{
			StringBuilder buffer = new StringBuilder();
			if (!term.Field().Equals(field)) 
			{
				buffer.Append(term.Field());
				buffer.Append(":");
			}
			buffer.Append(term.Text());
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
			if (!(o is TermQuery))
				return false;
			TermQuery other = (TermQuery)o;
			return (this.GetBoost() == other.GetBoost())
				&& this.term.Equals(other.term);
		}

		/// <summary>
		/// Returns a hash code value for this object.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() 
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0)
				^ term.GetHashCode();
		}
	}
}