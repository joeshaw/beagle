using Lucene.Net.Documents; 
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
	/// An abstract base class for search implementations.
	/// Implements some common utility methods.
	/// </summary>
	public abstract class Searcher : Searchable 
	{
		/** Returns the documents matching <code>query</code>. */
		public Hits Search(Query query)  
		{
			return Search(query, (Filter)null);
		}

		/// <summary>
		/// Returns the documents matching <code>query</code> and
		/// <code>filter</code>.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		public Hits Search(Query query, Filter filter)  
		{
			return new Hits(this, query, filter);
		}

		/// <summary>
		/// Lower-level search API.
		/// <p>HitCollector.Collect(int,float)} is called for every non-zero
		/// scoring document.</p>
		/// <p>Applications should only use this if they need <i>all</i> of the
		/// matching documents.  The high-level search API (Searcher.Search(Query)) 
		/// is usually more efficient, as it skips
		/// non-high-scoring hits.</p>
	    /// <p/>Note: The <code>score</code> passed to this method is a raw score.
		/// In other words, the score will not necessarily be a float whose value is
		/// between 0 and 1.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="results"></param>
		public void Search(Query query, HitCollector results)
	 	{
			Search(query, (Filter)null, results);
		}    

		/// <summary>
		/// The Similarity implementation used by this searcher.
		/// </summary>
		private Similarity similarity = Similarity.GetDefault();

		/// <summary>
		/// Expert: Set the Similarity implementation used by this Searcher.
		/// <see cref="Similarity.SetDefault(Similarity)"/>
		/// </summary>
		/// <param name="similarity"></param>
		public void SetSimilarity(Similarity similarity) 
		{
			this.similarity = similarity;
		}

		/// <summary>
		/// Expert: Return the Similarity implementation used by this Searcher.
		/// <p>This defaults to the current value of Similarity.GetDefault().</p>
		/// </summary>
		/// <returns></returns>
		public Similarity GetSimilarity() 
		{
			return this.similarity;
		}

		public abstract void Search(Query query, Filter filter, HitCollector results);
		public abstract void Close();
		public abstract int DocFreq(Term term);
		public abstract int MaxDoc();
		public abstract TopDocs Search(Query query, Filter filter, int n );
		public abstract Document Doc(int i);
		public abstract Query Rewrite(Query query);
		public abstract Explanation Explain(Query query, int doc);
	}
}