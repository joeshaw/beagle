using System;
using System.Collections;
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
	/// The abstract base class for queries.
	///	<p>Instantiable subclasses are:
	///	<ul>
	///	<li> TermQuery</li>
	///	<li> MultiTermQuery</li>
	///	<li> PhraseQuery</li>
	///	<li> BooleanQuery</li>
	///	<li> WildcardQuery</li>
	///	<li> PrefixQuery</li>
	///	<li> FuzzyQuery</li>
	///	<li> RangeQuery</li>
	///	</ul>
	///	<p>A parser for queries is contained in:
	///		<ul>
	///		<li>Lucene.Net.QueryParsers.QueryParser</li>
	///		</ul>
	///	</p>
	///	</p>
	/// </summary>
	[Serializable]
	public abstract class Query : ICloneable 
	{
		private float boost = 1.0f;                     // query boost factor

		/// <summary>
		/// Sets the boost for this query clause to <code>b</code>.  Documents
		/// matching this clause will (in addition to the normal weightings) have
		/// their score multiplied by <code>b</code>.
		/// </summary>
		/// <param name="b"></param>
		public void SetBoost(float b) 
		{ 
			boost = b; 
		}

		/// <summary>
		/// Gets the boost for this clause.  Documents matching
		/// this clause will (in addition to the normal weightings) have their score
		/// multiplied by <code>b</code>.   The boost is 1.0 by default.
		/// </summary>
		/// <returns></returns>
		public float GetBoost() { return boost; }

		/// <summary>
		/// Prints a query to a string, with <code>field</code> as the default field
		/// for terms.  <p>The representation used is one that is readable by 
		/// Lucene.Net.QueryParsers.QueryParser QueryParser (although, if the
		/// query was created by the parser, the printed representation may not be
		/// exactly what was parsed).</p>
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public abstract String ToString(String field);

		/// <summary>
		/// Prints a query to a string.
		/// </summary>
		/// <returns></returns>
		public override String ToString() 
		{
			return ToString("");
		}

		/// <summary>
		/// Expert: Constructs an appropriate Weight implementation for this query.
		/// <p>Only implemented by primitive queries, which re-write to themselves.</p>
		/// </summary>
		/// <param name="searcher"></param>
		/// <returns></returns>
		public virtual Weight CreateWeight(Searcher searcher) 
		{
			throw new InvalidOperationException();
		}
  
		/// <summary>
		/// Expert: Constructs an initializes a Weight for a top-level query.
		/// </summary>
		/// <param name="searcher"></param>
		/// <returns></returns>
		public virtual Weight Weight(Searcher searcher)
		{
			Query query = searcher.Rewrite(this);
			Weight weight = query.CreateWeight(searcher);
			float sum = weight.SumOfSquaredWeights();
			float norm = searcher.GetSimilarity().QueryNorm(sum);
			weight.Normalize(norm);
			return weight;
		}

		/// <summary>
		/// Expert: called to re-write queries into primitive queries.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public virtual Query Rewrite(IndexReader reader)  
		{
			return this;
		}

		/// <summary>
		/// Expert: called when re-writing queries under MultiSearcher.
		/// <p>Only implemented by derived queries, with no 
		/// CreateWeight(Searcher) implementatation.</p>
		/// </summary>
		/// <param name="queries"></param>
		/// <returns></returns>
		public virtual Query Combine(Query[] queries) 
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Expert: merges the clauses of a set of BooleanQuery's into a single
		/// BooleanQuery.
		/// <p>A utility for use by Combine(Query[]) implementations.</p>
		/// </summary>
		/// <param name="queries"></param>
		/// <returns></returns>
		public static Query MergeBooleanQueries(Query[] queries) 
		{
			Hashtable allClauses = new Hashtable();
			for (int i = 0; i < queries.Length; i++) 
			{
				BooleanClause[] clauses = ((BooleanQuery)queries[i]).GetClauses();
				for (int j = 0; j < clauses.Length; j++) 
				{
					if (allClauses[clauses[j]] == null)
					{
						allClauses.Add(clauses[j], null);
					}
				}
			}

			BooleanQuery result = new BooleanQuery();
			foreach (BooleanClause booleanClause in allClauses.Keys)
			{
				result.Add(booleanClause);
			}
			return result;
		}

		/// <summary>
		/// Returns a clone of this query.
		/// </summary>
		/// <returns></returns>
		public virtual Object Clone() 
		{
			try 
			{
				return (Query)this.MemberwiseClone();
			} 
			catch (Exception ex) 
			{
				throw new Exception("Clone not supported: " + ex.Message);
			}
		}
	}
}