using System;
using Lucene.Net.Index; 

namespace Lucene.Net.Search
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2003 The Apache Software Foundation.  All rights
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
	/// Expert: Calculate query weights and build query scorers.
	///
	/// <p>A Weight is constructed by a query, given a Searcher 
	/// (Query.CreateWeight(Searcher)).  The SumOfSquaredWeights() method
	/// is then called on the top-level query to compute the query normalization
	/// factor Similarity.QueryNorm(float).  This factor is then passed to
	/// Normalize(float). At this point the weighting is complete and a
	/// scorer may be constructed by calling Scorer(IndexReader).</p>
	/// </summary>
	public interface Weight 
	{
		/// <summary>
		/// The query that this concerns.
		/// </summary>
		Query GetQuery();

		/// <summary>
		/// The weight for this query.
		/// </summary>
		/// <returns></returns>
		float GetValue();

		/// <summary>
		/// The sum of squared weights of contained query clauses.
		/// </summary>
		/// <returns></returns>
		float SumOfSquaredWeights() ;

		/// <summary>
		/// Assigns the query normalization factor to this.
		/// </summary>
		/// <param name="norm"></param>
		void Normalize(float norm);

		/// <summary>
		/// Constructs a scorer for this.
		/// </summary>
		Scorer Scorer(IndexReader reader) ;

		/// <summary>
		/// An explanation of the score computation for the named document.
		/// </summary>
		Explanation Explain(IndexReader reader, int doc) ;
	}
}