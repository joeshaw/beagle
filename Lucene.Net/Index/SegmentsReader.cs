using System;
using System.Collections;
using System.Runtime.CompilerServices;

using Lucene.Net.Documents; 
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

	sealed class SegmentsReader : IndexReader
	{
		private SegmentReader[] readers;
		private int[] starts;			  // 1st docno for each segment
		private Hashtable normsCache = new Hashtable();
		private int maxDoc = 0;
		private int numDocs = -1;
		private bool hasDeletions = false;

		internal SegmentsReader(Directory directory, SegmentReader[] r) 
			: base(directory)
		{
			readers = r;
			starts = new int[readers.Length + 1];	  // build starts array
			for (int i = 0; i < readers.Length; i++) 
			{
				starts[i] = maxDoc;
				maxDoc += readers[i].MaxDoc();		  // compute maxDocs
				
				if (readers[i].HasDeletions())
					hasDeletions = true;
			}
			starts[readers.Length] = maxDoc;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		override public int NumDocs() 
		{
			if (numDocs == -1) 
			{			  // check cache
				int n = 0;				  // cache miss--recompute
				for (int i = 0; i < readers.Length; i++)
					n += readers[i].NumDocs();		  // sum from readers
				numDocs = n;
			}
			return numDocs;
		}

		override public int MaxDoc() 
		{
			return maxDoc;
		}

		override public Document Document(int n)  
		{
			int i = ReaderIndex(n);			  // find segment num
			return readers[i].Document(n - starts[i]);	  // dispatch to segment reader
		}

		override public bool IsDeleted(int n) 
		{
			int i = ReaderIndex(n);			  // find segment num
			return readers[i].IsDeleted(n - starts[i]);	  // dispatch to segment reader
		}

		public override bool HasDeletions()
		{
			return hasDeletions;
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		override protected internal void DoDelete(int n)  
		{
			numDocs = -1;				  // invalidate cache
			int i = ReaderIndex(n);			  // find segment num
			readers[i].DoDelete(n - starts[i]);		  // dispatch to segment reader
			hasDeletions = true;
		}

		public override void UndeleteAll()
		{
			for (int i = 0; i < readers.Length; i++)
				readers[i].UndeleteAll();
		}

		private int ReaderIndex(int n) 
		{	  // find reader for doc n:
			int lo = 0;					  // search starts array
			int hi = readers.Length - 1;                  // for first element less

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
					while (mid+1 < readers.Length && starts[mid+1] == midValue) 
					{
						mid++;                                  // scan to last match
					}
					return mid;
				}
			}
			return hi;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		override public byte[] Norms(String field)  
		{
			byte[] bytes = (byte[])normsCache[field];
			if (bytes != null)
				return bytes;				  // cache hit

			bytes = new byte[MaxDoc()];
			for (int i = 0; i < readers.Length; i++)
				readers[i].Norms(field, bytes, starts[i]);
			normsCache.Add(field, bytes);		  // update cache
			return bytes;
		}

		override public TermEnum Terms()  
		{
			return new SegmentsTermEnum(readers, starts, null);
		}

		override public TermEnum Terms(Term term)  
		{
			return new SegmentsTermEnum(readers, starts, term);
		}

		override public int DocFreq(Term t)  
		{
			int total = 0;				  // sum freqs in segments
			for (int i = 0; i < readers.Length; i++)
				total += readers[i].DocFreq(t);
			return total;
		}

		override public TermDocs TermDocs()  
		{
			return new SegmentsTermDocs(readers, starts);
		}

		override public TermPositions TermPositions()  
		{
			return new SegmentsTermPositions(readers, starts);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		override protected internal void DoClose()  
		{
			for (int i = 0; i < readers.Length; i++)
				readers[i].Close();
		}

		override public ICollection GetFieldNames()  
		{
			// maintain a unique set of field names
			Hashtable fieldSet = new Hashtable();
			for (int i = 0; i < readers.Length; i++) 
			{
				SegmentReader reader = readers[i];
				ICollection names = reader.GetFieldNames();
				// iterate through the field names and add them to the set
				foreach (string de in names) 
				{
					fieldSet[de] = "";
				}
			}
			return fieldSet.Keys;
		}
		
		public override ICollection GetFieldNames(bool indexed)
		{
			// maintain a unique set of field names
			Hashtable fieldSet = new Hashtable();
			for (int i = 0; i < readers.Length; i++) 
			{
				SegmentReader reader = readers[i];
				ICollection names = reader.GetFieldNames(indexed);
				foreach (string de in names) 
				{
					fieldSet[de] = "";
				}
			}
			return fieldSet.Keys;
		}
	}

	class SegmentsTermEnum : TermEnum 
	{
		private SegmentMergeQueue queue;

		private Term term;
		private int docFreq;

		internal SegmentsTermEnum(SegmentReader[] readers, int[] starts, Term t)
		{
			queue = new SegmentMergeQueue(readers.Length);
			for (int i = 0; i < readers.Length; i++) 
			{
				SegmentReader reader = readers[i];
				SegmentTermEnum termEnum;

				if (t != null) 
				{
					termEnum = (SegmentTermEnum)reader.Terms(t);
				} 
				else
					termEnum = (SegmentTermEnum)reader.Terms();

				SegmentMergeInfo smi = new SegmentMergeInfo(starts[i], termEnum, reader);
				if (t == null ? smi.Next() : termEnum.Term() != null)
					queue.Put(smi);				  // initialize queue
				else
					smi.Close();
			}

			if (t != null && queue.Size() > 0) 
			{
				Next();
			}
		}

		override public bool Next()  
		{
			SegmentMergeInfo top = (SegmentMergeInfo)queue.Top();
			if (top == null) 
			{
				term = null;
				return false;
			}

			term = top.term;
			docFreq = 0;

			while (top != null && term.CompareTo(top.term) == 0) 
			{
				queue.Pop();
				docFreq += top.termEnum.DocFreq();	  // increment freq
				if (top.Next())
					queue.Put(top);				  // restore queue
				else
					top.Close();				  // done with a segment
				top = (SegmentMergeInfo)queue.Top();
			}
			return true;
		}

		override public Term Term() 
		{
			return term;
		}

		override public int DocFreq() 
		{
			return docFreq;
		}

		override public void Close()  
		{
			queue.Close();
		}
	}

	class SegmentsTermDocs : TermDocs 
	{
		protected SegmentReader[] readers;
		protected int[] starts;
		protected Term term;

		protected int _base = 0;
		protected int pointer = 0;

		private SegmentTermDocs[] segTermDocs;
		protected SegmentTermDocs current;              // == segTermDocs[pointer]

		internal SegmentsTermDocs(SegmentReader[] r, int[] s) 
		{
			readers = r;
			starts = s;

			segTermDocs = new SegmentTermDocs[r.Length];
		}

		public int Doc() 
		{
			return _base + current.doc;
		}
		public int Freq() 
		{
			return current.freq;
		}

		public void Seek(Term term) 
		{
			this.term = term;
			this._base = 0;
			this.pointer = 0;
			this.current = null;
		}

		public void Seek(TermEnum termEnum)
		{
			Seek(termEnum.Term());
		}

		public bool Next()  
		{
			if (current != null && current.Next()) 
			{
				return true;
			} 
			else if (pointer < readers.Length) 
			{
				_base = starts[pointer];
				current = TermDocs(pointer++);
				return Next();
			} 
			else
				return false;
		}

		/// <summary>
		/// Optimized implementation.
		/// </summary>
		/// <param name="docs"></param>
		/// <param name="freqs"></param>
		/// <returns></returns>
		public int Read(int[] docs, int[] freqs)
		{
			while (true) 
			{
				while (current == null) 
				{
					if (pointer < readers.Length) 
					{		  // try next segment
						_base = starts[pointer];
						current = TermDocs(pointer++);
					} 
					else 
					{
						return 0;
					}
				}
				int end = current.Read(docs, freqs);
				if (end == 0) 
				{				  // none left in segment
					current = null;
				} 
				else 
				{					  // got some
					int b = _base;			  // adjust doc numbers
					for (int i = 0; i < end; i++)
						docs[i] += b;
					return end;
				}
			}
		}

		/// <summary>
		/// As yet unoptimized implementation.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public bool SkipTo(int target)  
		{
			do 
			{
				if (!Next())
					return false;
			} while (target > Doc());
			return true;
		}

		private SegmentTermDocs TermDocs(int i)  
		{
			if (term == null)
				return null;
			SegmentTermDocs result = segTermDocs[i];
			if (result == null)
				result = segTermDocs[i] = TermDocs(readers[i]);
			result.Seek(term);
			return result;
		}

		virtual protected SegmentTermDocs TermDocs(SegmentReader reader)
		{
			return (SegmentTermDocs)reader.TermDocs();
		}

		public void Close()  
		{
			for (int i = 0; i < segTermDocs.Length; i++) 
			{
				if (segTermDocs[i] != null)
					segTermDocs[i].Close();
			}
		}
	}

	class SegmentsTermPositions : SegmentsTermDocs, TermPositions 
	{
		internal SegmentsTermPositions(SegmentReader[] r, int[] s) : base(r,s)
		{
		}

		override protected SegmentTermDocs TermDocs(SegmentReader reader)
		{
			return (SegmentTermDocs)reader.TermPositions();
		}

		public int NextPosition()  
		{
			return ((SegmentTermPositions)current).NextPosition();
		}
	}
}