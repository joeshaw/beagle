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
	/// The interface for search implementations.
	/// <p>Implementations provide search over a single index, over multiple
	/// indices, and over indices on remote servers.</p>
	/// </summary>
	public interface Searchable 
	{
		/// <summary>
		/// Lower-level search API.
		/// <p>HitCollector.Collect(int,float) is called for every non-zero
		/// scoring document.</p>
		/// <p>Applications should only use this if they need <i>all</i> of the
		/// matching documents.  The high-level search API (Searcher.Search(Query)) 
		/// is usually more efficient, as it skips
		/// non-high-scoring hits.</p>
		/// </summary>
		/// <param name="query">to match documents</param>
		/// <param name="filter">if non-null, a bitset used to eliminate some documents</param>
		/// <param name="results">to receive hits</param>
		void Search(Query query, Filter filter, HitCollector results);

		/// <summary>
		/// Frees resources associated with this Searcher.
		/// </summary>
		void Close() ;

		/// <summary>
		/// Expert: Returns the number of documents containing <code>term</code>.
		/// Called by search code to compute term weights.
		/// <see cref="IndexReader.DocFreq(Term)"/>
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		int DocFreq(Term term);

		/// <summary>
		/// Expert: Returns one greater than the largest possible document number.
		/// Called by search code to compute term weights.
		/// <see cref="IndexReader.MaxDoc()"/>
		/// </summary>
		/// <returns></returns>
		int MaxDoc() ;

		/// <summary>
		/// Expert: Low-level search implementation.  Finds the top <code>n</code>
		/// hits for <code>query</code>, applying <code>filter</code> if non-null.
		/// <p>Called by Hits.</p>
		/// <p>Applications should usually call Searcher.Search(Query) or
		/// Searcher.Search(Query,Filter) instead.</p>
		/// </summary>
		TopDocs Search(Query query, Filter filter, int n);

		/// <summary>
		/// Expert: Returns the stored fields of document <code>i</code>.
		/// Called by HitCollector implementations.
		/// <see cref="IndexReader.Document(int)"/>
		/// </summary>
		Document Doc(int i);

		/// <summary>
		/// Expert: called to re-write queries into primitive queries. 
		/// </summary>
		Query Rewrite(Query query);

		/// <summary>
		/// Returns an Explanation that describes how <code>doc</code> scored against
		/// <code>query</code>.
		/// <p>This is intended to be used in developing Similarity implementations,
		/// and, for good performance, should not be displayed with every hit.
		/// Computing an explanation is as expensive as executing the query over the
		/// entire index.</p>
		/// </summary>
		Explanation Explain(Query query, int doc);
	}
}