using System;

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

	sealed class BooleanScorer : Scorer 
	{
		private int currentDoc;

		private SubScorer scorers = null;
		private BucketTable bucketTable = null;

		private int maxCoord = 1;
		private float[] coordFactors = null;

		private int requiredMask = 0;
		private int prohibitedMask = 0;
		private int nextMask = 1;

		internal BooleanScorer(Similarity similarity) : base(similarity)
		{
			bucketTable = new BucketTable(this);
		}

		sealed class SubScorer 
		{
			public Scorer scorer;
			public bool required = false;
			public bool prohibited = false;
			public HitCollector collector;
			public SubScorer next;

			public SubScorer(Scorer scorer, bool required, bool prohibited,
				HitCollector collector, SubScorer next) 
			{
				this.scorer = scorer;
				this.required = required;
				this.prohibited = prohibited;
				this.collector = collector;
				this.next = next;
			}
		}

		internal void Add(Scorer scorer, bool required, bool prohibited) 
		{
			int mask = 0;
			if (required || prohibited) 
			{
				if (nextMask == 0)
					throw new IndexOutOfRangeException("More than 32 required/prohibited clauses in query.");
				mask = nextMask;
				nextMask = nextMask << 1;
			} 
			else
				mask = 0;

			if (!prohibited)
				maxCoord++;

			if (prohibited)
				prohibitedMask |= mask;			  // update prohibited mask
			else if (required)
				requiredMask |= mask;			  // update required mask

			scorers = new SubScorer(scorer, required, prohibited,
				bucketTable.NewCollector(mask), scorers);
		}

		private void ComputeCoordFactors()  
		{
			coordFactors = new float[maxCoord];
			for (int i = 0; i < maxCoord; i++)
				coordFactors[i] = GetSimilarity().Coord(i, maxCoord-1);
		}

		public override void Score(HitCollector results, int maxDoc)
		{
			if (coordFactors == null)
				ComputeCoordFactors();

			while (currentDoc < maxDoc) 
			{
				currentDoc = Math.Min(currentDoc+BucketTable.SIZE, maxDoc);
				for (SubScorer t = scorers; t != null; t = t.next)
					t.scorer.Score(t.collector, currentDoc);
				bucketTable.CollectHits(results);
			}
		}

		sealed class Bucket 
		{
			internal int	doc = -1;				  // tells if bucket is valid
			internal float	score;				  // incremental score
			internal int	bits;					  // used for bool constraints
			internal int	coord;					  // count of terms in score
			internal Bucket 	next;				  // next valid bucket
		}

		/// <summary>
		/// A simple hash table of document scores within a range.
		/// </summary>
		sealed class BucketTable 
		{
			public const int SIZE = 1 << 10;
			public const int MASK = SIZE - 1;

			internal readonly Bucket[] buckets = new Bucket[SIZE];
			internal Bucket first = null;			  // head of valid list
  
			private BooleanScorer scorer;

			public BucketTable(BooleanScorer scorer) 
			{
				this.scorer = scorer;
			}

			public void CollectHits(HitCollector results) 
			{
				int required = scorer.requiredMask;
				int prohibited = scorer.prohibitedMask;
				float[] coord = scorer.coordFactors;

				for (Bucket bucket = first; bucket!=null; bucket = bucket.next) 
				{
					if ((bucket.bits & prohibited) == 0 &&	  // check prohibited
						(bucket.bits & required) == required)
					{// check required
						results.Collect(bucket.doc,		  // add to results
							bucket.score * coord[bucket.coord]);
					}
				}
				first = null;				  // reset for next round
			}

			public int Size() { return SIZE; }

			public HitCollector NewCollector(int mask) 
			{
				return new Collector(mask, this);
			}
		}

		sealed class Collector : HitCollector 
		{
			private BucketTable bucketTable;
			private int mask;
			public Collector(int mask, BucketTable bucketTable) 
			{
				this.mask = mask;
				this.bucketTable = bucketTable;
			}

			public override void Collect(int doc, float score) 
			{
				BucketTable table = bucketTable;
				int i = doc & BucketTable.MASK;
				Bucket bucket = table.buckets[i];
				if (bucket == null)
					table.buckets[i] = bucket = new Bucket();
      
				if (bucket.doc != doc) 
				{			  // invalid bucket
					bucket.doc = doc;			  // set doc
					bucket.score = score;			  // initialize score
					bucket.bits = mask;			  // initialize mask
					bucket.coord = 1;			  // initialize coord
	
					bucket.next = table.first;		  // push onto valid list
					table.first = bucket;
				} 
				else 
				{					  // valid bucket
					bucket.score += score;			  // increment score
					bucket.bits |= mask;			  // add bits in mask
					bucket.coord++;				  // increment coord
				}
			}
		}

		public override Explanation Explain(int doc)  
		{
			throw new InvalidOperationException();
		}
	}
}