using Lucene.Net.Util;
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

	abstract class PhraseScorer : Scorer 
	{
		private Weight weight;
		protected byte[] norms;
		protected float value;

		protected PhraseQueue pq;
		protected PhrasePositions first, last;

		private float freq;

		internal PhraseScorer(Weight weight, TermPositions[] tps, Similarity similarity,
			byte[] norms) : base(similarity)
		{
			this.norms = norms;
			this.weight = weight;
			this.value = weight.GetValue();

			// use PQ to build a sorted list of PhrasePositions
			pq = new PhraseQueue(tps.Length);
			for (int i = 0; i < tps.Length; i++)
				pq.Put(new PhrasePositions(tps[i], i));
			PqToList();
		}

		public override void Score(HitCollector results, int end)  
		{
			Similarity similarity = GetSimilarity();
			while (last.doc < end) 
			{			  // find doc w/ all the terms
				while (first.doc < last.doc) 
				{		  // scan forward in first
					do 
					{
						first.Next();
					} while (first.doc < last.doc);
					FirstToLast();
					if (last.doc >= end)
						return;
				}

				// found doc with all terms
				freq = PhraseFreq();                        // check for phrase

				if (freq > 0.0) 
				{
					float score = similarity.Tf(freq)*value;  // compute score
					score *= Similarity.DecodeNorm(norms[first.doc]); // normalize
					results.Collect(first.doc, score);	  // add to results
				}
				last.Next();				  // resume scanning
			}
		}

		protected abstract float PhraseFreq() ;

		protected void PqToList() 
		{
			last = first = null;
			while (pq.Top() != null) 
			{
				PhrasePositions pp = (PhrasePositions)pq.Pop();
				if (last != null) 
				{			  // add next to end of list
					last.next = pp;
				} 
				else
					first = pp;
				last = pp;
				pp.next = null;
			}
		}

		protected void FirstToLast() 
		{
			last.next = first;			  // move first to end of list
			last = first;
			first = first.next;
			last.next = null;
		}

		class PhraseScorerHitCollector : HitCollector 
		{
			public override void Collect(int d, float score) {}
		}

		public override Explanation Explain(int doc)  
		{
			Explanation tfExplanation = new Explanation();

			Score(new PhraseScorerHitCollector(), doc+1);

			float phraseFreq = (first.doc == doc) ? freq : 0.0f;
			tfExplanation.SetValue(GetSimilarity().Tf(phraseFreq));
			tfExplanation.SetDescription("tf(phraseFreq=" + phraseFreq + ")");

			return tfExplanation;
		}
	}
}