using System;

using Lucene.Net.Util; 
using Lucene.Net.Store; 

namespace Lucene.Net.Index
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

	class SegmentTermDocs : TermDocs 
	{
		protected SegmentReader parent;
		private InputStream freqStream;
		private int freqCount;
		private BitVector deletedDocs;
		internal int doc = 0;
		internal int freq;

		internal SegmentTermDocs(SegmentReader parent)
		{
			this.parent = parent;
			this.freqStream = (InputStream)parent.freqStream.Clone();
			this.deletedDocs = parent.deletedDocs;
		}
  
		virtual public void Seek(Term term)  
		{
			TermInfo ti = parent.tis.Get(term);
			Seek(ti);
		}
  
		virtual public void Seek(TermEnum _enum)
		{
			TermInfo ti;
			
			if (typeof(SegmentTermEnum).IsInstanceOfType(_enum))          // optimized case
				ti = ((SegmentTermEnum)_enum).TermInfo();
			else                                          // punt case
				ti = parent.tis.Get(_enum.Term());
			
			Seek(ti);
		}
		
		virtual internal void Seek(TermInfo ti)  
		{
			if (ti == null) 
			{
				freqCount = 0;
			} 
			else 
			{
				freqCount = ti.docFreq;
				doc = 0;
				freqStream.Seek(ti.freqPointer);
			}
		}
  
		virtual public void Close()  
		{
			freqStream.Close();
		}

		virtual public int Doc() { return doc; }
		virtual public int Freq() { return freq; }

		virtual protected void SkippingDoc()  
		{
		}

		virtual public bool Next()
		{
			while (true) 
			{
				if (freqCount == 0)
					return false;

				int docCode = freqStream.ReadVInt();
				doc += (int)(((uint)docCode) >> 1);			  // shift off low bit
				if ((docCode & 1) != 0)			  // if low bit is set
					freq = 1;				  // freq is one
				else
					freq = freqStream.ReadVInt();		  // else read freq
 
				freqCount--;
    
				if (deletedDocs == null || !deletedDocs.Get(doc))
					break;
				SkippingDoc();
			}
			return true;
		}

		/// <summary>
		/// Optimized implementation.
		/// </summary>
		/// <param name="docs"></param>
		/// <param name="freqs"></param>
		/// <returns></returns>
		virtual public int Read(int[] docs, int[] freqs)
		{
			int end = docs.Length;
			int i = 0;
			while (i < end && freqCount > 0) 
			{

				// manually inlined call to next() for speed
				int docCode = freqStream.ReadVInt();
				doc += (int)(((uint)docCode) >> 1);			  // shift off low bit
				if ((docCode & 1) != 0)			  // if low bit is set
					freq = 1;				  // freq is one
				else
					freq = freqStream.ReadVInt();		  // else read freq
				freqCount--;
   
				if (deletedDocs == null || !deletedDocs.Get(doc)) 
				{
					docs[i] = doc;
					freqs[i] = freq;
					++i;
				}
			}
			return i;
		}

		/// <summary>
		/// As yet unoptimized implementation.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		virtual public bool SkipTo(int target)  
		{
			do 
			{
				if (!Next())
					return false;
			} while (target > doc);
			return true;
		}
	}
}