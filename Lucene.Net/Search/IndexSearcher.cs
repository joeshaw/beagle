using System;
using System.Collections;
using Lucene.Net.Store;
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
	/// Implements search over a single IndexReader.
	/// <p>Applications usually need only call the inherited Search(Query)
	/// or Search(Query,Filter) methods.</p>
	/// </summary>
	public class IndexSearcher : Searcher 
	{
		internal IndexReader reader;

		/// <summary>
		/// Creates a searcher searching the index in the named directory.
		/// </summary>
		/// <param name="path"></param>
		public IndexSearcher(String path) : this(IndexReader.Open(path))
		{
		}
    
		/// <summary>
		/// Creates a searcher searching the index in the provided directory.
		/// </summary>
		/// <param name="directory"></param>
		public IndexSearcher(Directory directory) : 
			this(IndexReader.Open(directory))
		{
		}
    
		/// <summary>
		/// Creates a searcher searching the provided index. 
		/// </summary>
		/// <param name="r"></param>
		public IndexSearcher(IndexReader r) 
		{
			reader = r;
		}
    
		/// <summary>
		/// Frees resources associated with this Searcher.
		/// </summary>
		public override void Close()  
		{
			reader.Close();
		}

		/// <summary>
		/// Expert: Returns the number of documents containing <code>term</code>.
		/// Called by search code to compute term weights.
		/// <see cref="IndexReader.DocFreq(Term)"/>
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		public override int DocFreq(Term term)  
		{
			return reader.DocFreq(term);
		}

		/// <summary>
		/// For use by HitCollector implementations.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public override Document Doc(int i)  
		{
			return reader.Document(i);
		}

		/// <summary>
		/// Expert: Returns one greater than the largest possible document number.
		/// Called by search code to compute term weights.
		/// <see cref="IndexReader.MaxDoc()"/>
		/// </summary>
		/// <returns></returns>
		public override int MaxDoc()  
		{
			return reader.MaxDoc();
		}

		class IndexSearcherHitCollector : HitCollector 
		{
			int nDocs;
			HitQueue hq;
			int[] totalHits;
			BitArray bits;

			internal IndexSearcherHitCollector(BitArray bits, int[] totalHits, HitQueue hq, int nDocs)
			{
				this.totalHits = totalHits;
				this.bits = bits;
				this.hq = hq;
				this.nDocs = nDocs;
			}

			public override void Collect(int doc, float score) 
			{
				if (score > 0.0f &&			  // ignore zeroed buckets
					(bits == null || bits.Get(doc))) 
				{	  // skip docs not in bits
					totalHits[0]++;
					hq.Insert(new ScoreDoc(doc, score));
				}
			}
		}

		/// <summary>
		/// Expert: Low-level search implementation.  Finds the top <code>n</code>
		/// hits for <code>query</code>, applying <code>filter</code> if non-null.
		/// <p>Called by Hits.</p>
		/// <p>Applications should usually call Search(Query) or 
		/// Search(Query,Filter) instead.</p>
		/// </summary>
		/// <param name="query"></param>
		/// <param name="filter"></param>
		/// <param name="nDocs"></param>
		/// <returns></returns>
		public override TopDocs Search(Query query, Filter filter, int nDocs)
		{
			Scorer scorer = query.Weight(this).Scorer(reader);
			if (scorer == null)
				return new TopDocs(0, new ScoreDoc[0]);

			BitArray bits = filter != null ? filter.Bits(reader) : null;
			HitQueue hq = new HitQueue(nDocs);
			int[] totalHits = new int[1];
			scorer.Score(new IndexSearcherHitCollector(bits, totalHits, hq, nDocs), reader.MaxDoc());

			ScoreDoc[] scoreDocs = new ScoreDoc[hq.Size()];
			for (int i = hq.Size()-1; i >= 0; i--)	  // put docs in array
				scoreDocs[i] = (ScoreDoc)hq.Pop();
    
			return new TopDocs(totalHits[0], scoreDocs);
		}

		class IndexSearcherHitCollector2 : HitCollector 
		{
			BitArray bits;
			HitCollector results;

			internal IndexSearcherHitCollector2(
				BitArray bits, HitCollector results)
			{
				this.bits = bits;
				this.results = results;
			}

			public override void Collect(int doc, float score) 
			{
				if (bits.Get(doc)) 
				{		  // skip docs not in bits
					results.Collect(doc, score);
				}
			}
		}


		/// <summary>
		/// Lower-level search API.
		/// <p>HitCollector.Collect(int,float) is called for every non-zero
		/// scoring document.
		/// </p>
		/// <p>Applications should only use this if they need <i>all</i> of the
		/// matching documents.  The high-level search API (Searcher.Search(Query)) 
		/// is usually more efficient, as it skips
		/// non-high-scoring hits.
		/// </p>
		/// </summary>
		/// <param name="query">to match documents</param>
		/// <param name="filter">if non-null, a bitset used to eliminate some documents</param>
		/// <param name="results">to receive hits</param>
		public override void Search(Query query, Filter filter,
			HitCollector results)  
		{
			HitCollector collector = results;
			if (filter != null) 
			{
				BitArray bits = filter.Bits(reader);
				collector = new IndexSearcherHitCollector2(bits, results);
			}

			Scorer scorer = query.Weight(this).Scorer(reader);
			if (scorer == null)
				return;
			scorer.Score(collector, reader.MaxDoc());
		}

		public override Query Rewrite(Query original)  
		{
			Query query = original;
			for (Query rewrittenQuery = query.Rewrite(reader); rewrittenQuery != query;
				rewrittenQuery = query.Rewrite(reader)) 
			{
				query = rewrittenQuery;
			}
			return query;
		}

		public override Explanation Explain(Query query, int doc)  
		{
			return query.Weight(this).Explain(reader, doc);
		}
	}
}