using System;
using System.IO;
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
	/// Constrains search results to only match those which also match a provided
	/// query.  Results are cached, so that searches after the first on the same
	/// index using this filter are much faster.
	/// <p> This could be used, for example, with a RangeQuery on a suitably
	/// formatted date field to implement date filtering.  One could re-use a single
	/// QueryFilter that matches, e.g., only documents modified within the last
	/// week.  The QueryFilter and RangeQuery would only need to be reconstructed
	/// once per day.</p>
	/// </summary>
	[Serializable]
	public class QueryFilter : Filter 
	{
		private Query query;
		[NonSerialized]
		private Hashtable cache = null;

		class QueryFilterHitCollector : HitCollector 
		{
			BitArray bits;
			internal QueryFilterHitCollector(BitArray bits)
			{
				this.bits = bits;
			}
			public override void Collect(int doc, float score) 
			{
				bits.Set(doc, true);                          // set bit for hit
			}
		}

		/// <summary>
		/// Constructs a filter which only matches documents matching
		/// <code>query</code>.
		/// </summary>
		/// <param name="query"></param>
		public QueryFilter(Query query) 
		{
			this.query = query;
		}

		public override BitArray Bits(IndexReader reader) 
		{
			if (cache == null)
			{
				cache = new Hashtable();
			}
			
			lock (cache) 
			{                        // check cache
				BitArray cached = (BitArray)cache[reader];
				if (cached != null)
					return cached;
			}

			BitArray bits = new BitArray(reader.MaxDoc());

			new IndexSearcher(reader).Search(query, new QueryFilterHitCollector(bits));
                                     

			lock (cache) 
			{                        
				// update cache
				cache.Add(reader, bits);
			}

			return bits;
		}
	}
}