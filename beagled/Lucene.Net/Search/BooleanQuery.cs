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
	/// A Query that matches documents matching bool combinations of other
	/// queries, typically TermQuery's or PhraseQuery's.
	/// </summary>
	[Serializable]
	public class BooleanQuery : Query 
	{
		private static int maxClauseCount = 1024;

		/// <summary>
		/// Thrown when an attempt is made to add more than 
		/// GetMaxClauseCount() clauses.
		/// </summary>
		public class TooManyClausesException : Exception {}

		/// <summary>
		/// Return the maximum number of clauses permitted, 1024 by default.
		/// Attempts to add more than the permitted number of clauses cause 
		/// {TooManyClauses} to be thrown.
		/// </summary>
		/// <returns></returns>
		public static int GetMaxClauseCount() { return maxClauseCount; }

		/// <summary>
		/// Set the maximum number of clauses permitted.
		/// </summary>
		/// <param name="maxClauseCount"></param>
		public static void SetMaxClauseCount(int maxClauseCount) 
		{
			BooleanQuery.maxClauseCount = maxClauseCount;
		}
  
		private ArrayList clauses = new ArrayList();

		/// <summary>
		/// Constructs an empty bool query.
		/// </summary>
		public BooleanQuery() {}

		/// <summary>
		/// Adds a clause to a bool query.  Clauses may be:
		/// <ul>
		/// <li><code>required</code> which means that documents which <i>do not</i>
		/// match this sub-query will <i>not</i> match the bool query;</li>
		/// <li><code>prohibited</code> which means that documents which <i>do</i>
		/// match this sub-query will <i>not</i> match the bool query; or</li>
		/// <li>neither, in which case matched documents are neither prohibited from
		/// nor required to match the sub-query.</li>
		/// </ul>
		/// It is an error to specify a clause as both <code>required</code> and
		/// <code>prohibited</code>.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="required"></param>
		/// <param name="prohibited"></param>
		public void Add(Query query, bool required, bool prohibited) 
		{
			Add(new BooleanClause(query, required, prohibited));
		}

		/// <summary>
		/// Adds a clause to a bool query.
		/// </summary>
		/// <param name="clause"></param>
		public void Add(BooleanClause clause) 
		{
			if (clauses.Count >= maxClauseCount)
				throw new TooManyClausesException();

			clauses.Add(clause);
		}

		/// <summary>
		/// Returns the set of clauses in this query.
		/// </summary>
		/// <returns></returns>
		public BooleanClause[] GetClauses() 
		{
			return (BooleanClause[])clauses.ToArray(typeof(BooleanClause));
		}

		[Serializable]
		private class BooleanWeight : Weight 
		{
			internal BooleanQuery booleanQuery;
			private Searcher searcher;
			private ArrayList weights = new ArrayList();

			public BooleanWeight(Searcher searcher, BooleanQuery booleanQuery) 
			{
				this.booleanQuery = booleanQuery;
				this.searcher = searcher;
				for (int i = 0 ; i < booleanQuery.clauses.Count; i++) 
				{
					BooleanClause c = (BooleanClause)booleanQuery.clauses[i];
					weights.Add(c.query.CreateWeight(searcher));
				}
			}

			public Query GetQuery() { return booleanQuery; }
			public float GetValue() { return booleanQuery.GetBoost(); }

			public float SumOfSquaredWeights()  
			{
				float sum = 0.0f;
				for (int i = 0 ; i < weights.Count; i++) 
				{
					BooleanClause c = (BooleanClause)booleanQuery.clauses[i];
					Weight w = (Weight)weights[i];
					if (!c.prohibited)
						sum += w.SumOfSquaredWeights();         // sum sub weights
				}
      
				sum *= booleanQuery.GetBoost() * booleanQuery.GetBoost();             // boost each sub-weight
				return sum;
			}

			public void Normalize(float norm) 
			{
				norm *= booleanQuery.GetBoost();                         // incorporate boost
				for (int i = 0 ; i < weights.Count; i++) 
				{
					BooleanClause c = (BooleanClause)booleanQuery.clauses[i];
					Weight w = (Weight)weights[i];
					if (!c.prohibited)
						w.Normalize(norm);
				}
			}

			public Scorer Scorer(IndexReader reader)  
			{
				BooleanScorer result = new BooleanScorer(searcher.GetSimilarity());

				for (int i = 0 ; i < weights.Count; i++) 
				{
					BooleanClause c = (BooleanClause)booleanQuery.clauses[i];
					Weight w = (Weight)weights[i];
					Scorer subScorer = w.Scorer(reader);
					if (subScorer != null)
						result.Add(subScorer, c.required, c.prohibited);
					else if (c.required)
						return null;
				}

				return result;
			}

			public Explanation Explain(IndexReader reader, int doc)
			{
				Explanation sumExpl = new Explanation();
				sumExpl.SetDescription("sum of:");
				int coord = 0;
				int maxCoord = 0;
				float sum = 0.0f;
				for (int i = 0 ; i < weights.Count; i++) 
				{
					BooleanClause c = (BooleanClause)booleanQuery.clauses[i];
					Weight w = (Weight)weights[i];
					Explanation e = w.Explain(reader, doc);
					if (!c.prohibited) maxCoord++;
					if (e.GetValue() > 0) 
					{
						if (!c.prohibited) 
						{
							sumExpl.AddDetail(e);
							sum += e.GetValue();
							coord++;
						} 
						else 
						{
							return new Explanation(0.0f, "match prohibited");
						}
					} 
					else if (c.required) 
					{
						return new Explanation(0.0f, "match required");
					}
				}
				sumExpl.SetValue(sum);

				if (coord == 1)                               // only one clause matched
					sumExpl = sumExpl.GetDetails()[0];          // eliminate wrapper

				float coordFactor = searcher.GetSimilarity().Coord(coord, maxCoord);
				if (coordFactor == 1.0f)                      // coord is no-op
					return sumExpl;                             // eliminate wrapper
				else 
				{
					Explanation result = new Explanation();
					result.SetDescription("product of:");
					result.AddDetail(sumExpl);
					result.AddDetail(new Explanation(coordFactor,
						"coord("+coord+"/"+maxCoord+")"));
					result.SetValue(sum*coordFactor);
					return result;
				}
			}
		}

		public override Weight CreateWeight(Searcher searcher) 
		{
			return new BooleanWeight(searcher, this);
		}

		public override Query Rewrite(IndexReader reader)  
		{
			if (clauses.Count == 1) 
			{                    // optimize 1-clause queries
				BooleanClause c = (BooleanClause)clauses[0];
				if (!c.prohibited) 
				{			  // just return clause
					Query query = c.query;
					if (GetBoost() != 1.0f) 
					{                 // have to clone to boost
						query = (Query)query.Clone();
						query.SetBoost(GetBoost() * query.GetBoost());
					}
					return query;
				}
			}

			BooleanQuery clone = null;                    // recursively rewrite
			for (int i = 0 ; i < clauses.Count; i++) 
			{
				BooleanClause c = (BooleanClause)clauses[i];
				Query query = c.query.Rewrite(reader);
				if (query != c.query) 
				{                     // clause rewrote: must clone
					if (clone == null)
						clone = (BooleanQuery)this.Clone();
					clone.clauses[i] = new BooleanClause(query, c.required, c.prohibited);
				}
			}
			if (clone != null) 
			{
				return clone;                               // some clauses rewrote
			} 
			else
				return this;                                // no clauses rewrote
		}


		public override Object Clone()
		{
			BooleanQuery clone = (BooleanQuery)base.Clone();
			clone.clauses = (ArrayList)this.clauses.Clone();
			return clone;
		}

		/// <summary>
		/// Prints a user-readable version of this query.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public override String ToString(String field) 
		{
			StringBuilder buffer = new StringBuilder();
			if (GetBoost() != 1.0) 
			{
				buffer.Append("(");
			}

			for (int i = 0 ; i < clauses.Count; i++) 
			{
				BooleanClause c = (BooleanClause)clauses[i];
				if (c.prohibited)
					buffer.Append("-");
				else if (c.required)
					buffer.Append("+");

				Query subQuery = c.query;
				if (subQuery is BooleanQuery) 
				{	  // wrap sub-bools in parens
					buffer.Append("(");
					buffer.Append(c.query.ToString(field));
					buffer.Append(")");
				} 
				else
					buffer.Append(c.query.ToString(field));

				if (i != clauses.Count-1)
					buffer.Append(" ");
			}

			if (GetBoost() != 1.0) 
			{
				buffer.Append(")^");
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
			if (!(o is BooleanQuery))
				return false;
			BooleanQuery other = (BooleanQuery)o;
			return (this.GetBoost() == other.GetBoost())
				&&  this.clauses.Equals(other.clauses);
		}

		/// <summary>
		/// Returns a hash code value for this object.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int boostInt = BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0);
			return boostInt ^ clauses.GetHashCode();
		}
	}
}