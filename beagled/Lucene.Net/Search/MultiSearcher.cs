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
	/// Implements search over a set of <code>Searchables</code>.
	/// <p>Applications usually need only call the inherited Search(Query)
	/// or Search(Query,Filter) methods.</p>
	/// </summary>
	public class MultiSearcher : Searcher 
	{
		private Searchable[] searchables;
		private int[] starts;
		private int maxDoc = 0;

		/// <summary>
		/// Creates a searcher which searches <i>searchables</i>.
		/// </summary>
		/// <param name="searchables"></param>
		public MultiSearcher(Searchable[] searchables)  
		{
			this.searchables = searchables;

			starts = new int[searchables.Length + 1];	  // build starts array
			for (int i = 0; i < searchables.Length; i++) 
			{
				starts[i] = maxDoc;
				maxDoc += searchables[i].MaxDoc();          // compute maxDocs
			}
			starts[searchables.Length] = maxDoc;
		}

		/// <summary>
		/// Frees resources associated with this <code>Searcher</code>.
		/// </summary>
		public override void Close()  
		{
			for (int i = 0; i < searchables.Length; i++)
				searchables[i].Close();
		}

		public override int DocFreq(Term term)  
		{
			int docFreq = 0;
			for (int i = 0; i < searchables.Length; i++)
				docFreq += searchables[i].DocFreq(term);
			return docFreq;
		}

		/// <summary>
		/// For use by HitCollector implementations.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public override Document Doc(int n)  
		{
			int i = SubSearcher(n);			  // find searcher index
			return searchables[i].Doc(n - starts[i]);	  // dispatch to searcher
		}

		/// <summary>
		/// Call SubSearcher instead.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		/// <remarks>deprecated</remarks>
		public int SearcherIndex(int n) 
		{
			return SubSearcher(n);
		}

		/// <summary>
		/// Returns index of the searcher for document <code>n</code> in the array
		/// used to construct this searcher.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public int SubSearcher(int n) 
		{                 
			// find searcher for doc n:
			int lo = 0;					  // search starts array
			int hi = searchables.Length - 1;		  // for first element less
			// than n, return its index
			while (hi >= lo) 
			{
				int mid = (lo + hi) >> 1;
				int midValue = starts[mid];
				if (n < midValue)
					hi = mid - 1;
				else if (n > midValue)
					lo = mid + 1;
				else 
				{                                      // found a match
					while (mid+1 < searchables.Length && starts[mid+1] == midValue) 
					{
						mid++;                                  // scan to last match
					}
					return mid;
				}
			}
			return hi;
		}

		/// <summary>
		/// Returns the document number of document <code>n</code> within its
		/// sub-index.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public int SubDoc(int n) 
		{
			return n - starts[SubSearcher(n)];
		}

		public override int MaxDoc()  
		{
			return maxDoc;
		}

		public override TopDocs Search(Query query, Filter filter, int nDocs)
		{
			HitQueue hq = new HitQueue(nDocs);
			int totalHits = 0;

			for (int i = 0; i < searchables.Length; i++) 
			{ // search each searcher
				TopDocs docs = searchables[i].Search(query, filter, nDocs);
				totalHits += docs.totalHits;		  // update totalHits
				ScoreDoc[] scoreDocs = docs.scoreDocs;
				for (int j = 0; j < scoreDocs.Length; j++) 
				{ // merge scoreDocs into hq
					ScoreDoc scoreDoc = scoreDocs[j];

					scoreDoc.doc += starts[i];		  // convert doc
					if(!hq.Insert(scoreDoc))		// no more scores > minScore
						break;
				}
			}

			ScoreDoc[] _scoreDocs = new ScoreDoc[hq.Size()];
			for (int i = hq.Size()-1; i >= 0; i--)	  // put docs in array
				_scoreDocs[i] = (ScoreDoc)hq.Pop();

			return new TopDocs(totalHits, _scoreDocs);
		}

		class MultiSearcherHitCollector : HitCollector 
		{
			int start;
			HitCollector results;
			internal MultiSearcherHitCollector(int start, HitCollector results)
			{
				this.start = start;
				this.results = results;
			}

			public override void Collect(int doc, float score) 
			{
				results.Collect(doc + start, score);
			}
		}

		/// <summary>
		/// Lower-level search API.
		///
		/// <p>HitCollector.Collect(int,float) is called for every non-zero
		/// scoring document.
		///
		/// <p>Applications should only use this if they need <i>all</i> of the
		/// matching documents.  The high-level search API (Searcher.Search(Query)) 
		/// is usually more efficient, as it skips
		/// non-high-scoring hits.</p>
		/// </p>
		/// </summary>
		/// <param name="query">to match documents</param>
		/// <param name="filter">if non-null, a bitset used to eliminate some documents</param>
		/// <param name="results">to receive hits</param>
		public override void Search(Query query, Filter filter, HitCollector results)
		{
			for (int i = 0; i < searchables.Length; i++) 
			{

				int start = starts[i];

				searchables[i].Search(
					query, filter, new MultiSearcherHitCollector(start, results)
				);
			}
		}
  
		public override Query Rewrite(Query original)  
		{
			Query[] queries = new Query[searchables.Length];
			for (int i = 0; i < searchables.Length; i++) 
			{
				queries[i] = searchables[i].Rewrite(original);
			}
			return original.Combine(queries);
		}

		public override Explanation Explain(Query query, int doc)  
		{
			int i = SubSearcher(doc);			  // find searcher index
			return searchables[i].Explain(query,doc-starts[i]); // dispatch to searcher
		}
	}
}