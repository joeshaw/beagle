using System;
using System.Text;
using System.Collections;

using Lucene.Net.Index;
using Lucene.Net.Search;
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
	/// PhrasePrefixQuery is a generalized version of PhraseQuery, with an added
	/// method Add(Term[]).
	/// To use this class, to search for the phrase "Microsoft app*" first use
	/// Add(Term) on the term "Microsoft", then find all terms that has "app" as
	/// prefix using IndexReader.Terms(Term), and use PhrasePrefixQuery.Add(Term[]
	/// terms) to add them to the query.
	/// <author>Anders Nielsen</author>
	/// <versoin>1.0</versoin>
	/// </summary>
	[Serializable]
	public class PhrasePrefixQuery : Query 
	{
		private String field;
		private ArrayList termArrays = new ArrayList();

		//private float idf = 0.0f;
		//private float weight = 0.0f;

		private int slop = 0;

		/// <summary>
		/// Sets the phrase slop for this query.
		/// <see cref="PhraseQuery.SetSlop(int)"/>
		/// </summary>
		/// <param name="s"></param>
		public void SetSlop(int s) { slop = s; }

		/// <summary>
		/// Sets the phrase slop for this query.
		/// <see cref="PhraseQuery.GetSlop()"/>
		/// </summary>
		/// <returns></returns>
		public int GetSlop() { return slop; }

		/// <summary>
		/// Add a single term at the next position in the phrase.
		/// <see cref="PhraseQuery.Add(Term)"/>
		/// </summary>
		/// <param name="term"></param>
		public void Add(Term term) { Add(new Term[]{term}); }

		/// <summary>
		/// Add multiple terms at the next position in the phrase.  Any of the terms
		/// may match.
		/// <see cref="PhraseQuery.Add(Term)"/>
		/// </summary>
		/// <param name="terms"></param>
		public void Add(Term[] terms) 
		{
			if (termArrays.Count == 0)
				field = terms[0].Field();
    
			for (int i=0; i<terms.Length; i++) 
			{
				if (terms[i].Field() != field) 
				{
					throw new ArgumentException
						("All phrase terms must be in the same field (" + field + "): "
						+ terms[i]);
				}
			}

			termArrays.Add(terms);
		}

		[Serializable]
		private class PhrasePrefixWeight : Weight 
		{
			private Searcher searcher;
			private float value;
			private float idf;
			private float queryNorm;
			private float queryWeight;
			private PhrasePrefixQuery phrasePrefixQuery;

			public PhrasePrefixWeight(Searcher searcher, PhrasePrefixQuery phrasePrefixQuery) 
			{
				this.searcher = searcher;
				this.phrasePrefixQuery = phrasePrefixQuery;
			}

			public Query GetQuery() { return phrasePrefixQuery; }
			public float GetValue() { return value; }

			public float SumOfSquaredWeights()  
			{
				foreach (Term[] terms in phrasePrefixQuery.termArrays)
				{
					for (int j=0; j< terms.Length; j++)
						idf += searcher.GetSimilarity().Idf(terms[j], searcher);
				}

				queryWeight = idf * phrasePrefixQuery.GetBoost();             // compute query weight
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
				if (phrasePrefixQuery.termArrays.Count == 0)                  // optimize zero-term case
					return null;
    
				TermPositions[] tps = new TermPositions[phrasePrefixQuery.termArrays.Count];
				for (int i=0; i< tps.Length; i++) 
				{
					Term[] terms = (Term[])phrasePrefixQuery.termArrays[i];
      
					TermPositions p;
					if (terms.Length > 1)
						p = new MultipleTermPositions(reader, terms);
					else
						p = reader.TermPositions(terms[0]);
      
					if (p == null)
						return null;
      
					tps[i] = p;
				}
    
				if (phrasePrefixQuery.slop == 0)
					return new ExactPhraseScorer(this, tps, searcher.GetSimilarity(),
						reader.Norms(phrasePrefixQuery.field));
				else
					return new SloppyPhraseScorer(this, tps, searcher.GetSimilarity(),
						phrasePrefixQuery.slop, reader.Norms(phrasePrefixQuery.field));
			}
    
			public Explanation Explain(IndexReader reader, int doc)
			{
				Explanation result = new Explanation();
				result.SetDescription("weight("+GetQuery()+" in "+doc+"), product of:");

				Explanation idfExpl = new Explanation(idf, "idf("+GetQuery()+")");
      
				// explain query weight
				Explanation queryExpl = new Explanation();
				queryExpl.SetDescription("queryWeight(" + GetQuery() + "), product of:");

				Explanation boostExpl = new Explanation(phrasePrefixQuery.GetBoost(), "boost");
				if (phrasePrefixQuery.GetBoost() != 1.0f)
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
				fieldExpl.SetDescription("fieldWeight("+GetQuery()+" in "+doc+
					"), product of:");

				Explanation tfExpl = Scorer(reader).Explain(doc);
				fieldExpl.AddDetail(tfExpl);
				fieldExpl.AddDetail(idfExpl);

				Explanation fieldNormExpl = new Explanation();
				fieldNormExpl.SetValue(Similarity.DecodeNorm(reader.Norms(phrasePrefixQuery.field)[doc]));
				fieldNormExpl.SetDescription("fieldNorm(field="+phrasePrefixQuery.field+", doc="+doc+")");
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
			if (termArrays.Count == 1) 
			{                 // optimize one-term case
				Term[] terms = (Term[])termArrays[0];
				BooleanQuery boq = new BooleanQuery();
				for (int i=0; i<terms.Length; i++) 
				{
					boq.Add(new TermQuery(terms[i]), false, false);
				}
				boq.SetBoost(GetBoost());
				return boq.CreateWeight(searcher);
			}
			return new PhrasePrefixWeight(searcher, this);
		}

		/** Prints a user-readable version of this query. */
		public override String ToString(String f) 
		{
			StringBuilder buffer = new StringBuilder();
			if (!field.Equals(f)) 
			{
				buffer.Append(field);
				buffer.Append(":");
			}

			buffer.Append("\"");
			foreach (Term[] terms in termArrays)
			{
				buffer.Append(terms[0].Text() + (terms.Length > 0 ? "*" : ""));
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
	}
}