using System;
using System.Runtime.CompilerServices;
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

	/// <summary>
	/// This stores a monotonically increasing set of (Term, TermInfo) pairs in a
	/// Directory. Pairs are accessed either by Term or by ordinal position the
	/// set. 
	/// </summary>
	public sealed class TermInfosReader 
	{
		private Directory directory;
		private String segment;
		private FieldInfos fieldInfos;

		private SegmentTermEnum _enum;
		private int size;

		public TermInfosReader(Directory dir, String seg, FieldInfos fis)
		{
			directory = dir;
			segment = seg;
			fieldInfos = fis;

			_enum = new SegmentTermEnum(
				directory.OpenFile(segment + ".tis"),
				fieldInfos, false
			);
			size = _enum.size;
			ReadIndex();
		}

		public void Close()
		{
			if (_enum != null)
				_enum.Close();
		}

		/// <summary>
		/// Returns the number of term/value pairs in the set.
		/// </summary>
		/// <returns></returns>
		public int Size()
		{
			return size;
		}

		internal Term[] indexTerms = null;
		internal TermInfo[] indexInfos;
		internal long[] indexPointers;

		private void ReadIndex()  
		{
			SegmentTermEnum indexEnum =
				new SegmentTermEnum(directory.OpenFile(segment + ".tii"),
				fieldInfos, true);
			try 
			{
				int indexSize = indexEnum.size;

				indexTerms = new Term[indexSize];
				indexInfos = new TermInfo[indexSize];
				indexPointers = new long[indexSize];

				for (int i = 0; indexEnum.Next(); i++) 
				{
					indexTerms[i] = indexEnum.Term();
					indexInfos[i] = indexEnum.TermInfo();
					indexPointers[i] = indexEnum.indexPointer;
				}
			} 
			finally 
			{
				indexEnum.Close();
			}
		}

		/// <summary>
		/// Returns the offset of the greatest index entry which is less than term.
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		private int GetIndexOffset(Term term)  
		{
			int lo = 0;					  // binary search indexTerms[]
			int hi = indexTerms.Length - 1;

			while (hi >= lo) 
			{
				int mid = (lo + hi) >> 1;
				int delta = term.CompareTo(indexTerms[mid]);
				if (delta < 0)
					hi = mid - 1;
				else if (delta > 0)
					lo = mid + 1;
				else
					return mid;
			}
			return hi;
		}

		private void SeekEnum(int indexOffset)  
		{
			_enum.Seek(
				indexPointers[indexOffset],
				(indexOffset * TermInfosWriter.INDEX_INTERVAL) - 1,
				indexTerms[indexOffset], indexInfos[indexOffset]
			);
		}

		/// <summary>
		/// Returns the TermInfo for a Term in the set, or null.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public TermInfo Get(Term term)  
		{
			if (size == 0) return null;
    
			// optimize sequential access: first try scanning cached _enum w/o seeking
			if (_enum.Term() != null			  // term is at or past current
				&& ((_enum.prev != null && term.CompareTo(_enum.prev) > 0)
				|| term.CompareTo(_enum.Term()) >= 0)) 
			{ 
				int enumOffset = (_enum.position/TermInfosWriter.INDEX_INTERVAL)+1;
				if (indexTerms.Length == enumOffset	  // but before end of block
					|| term.CompareTo(indexTerms[enumOffset]) < 0)
					return ScanEnum(term);			  // no need to seek
			}
    
			// random-access: must seek
			SeekEnum(GetIndexOffset(term));
			return ScanEnum(term);
		}
  
		/// <summary>
		/// Scans within block for matching term.
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		private TermInfo ScanEnum(Term term)  
		{
			while (term.CompareTo(_enum.Term()) > 0 && _enum.Next()) {}
			if (_enum.Term() != null && term.CompareTo(_enum.Term()) == 0)
				return _enum.TermInfo();
			else
				return null;
		}

		/// <summary>
		/// Returns the nth term in the set.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public Term Get(int position)  
		{
			if (size == 0) return null;

			if (_enum != null && _enum.Term() != null && position >= _enum.position &&
				position < (_enum.position + TermInfosWriter.INDEX_INTERVAL))
				return ScanEnum(position);		  // can avoid seek

			SeekEnum(position / TermInfosWriter.INDEX_INTERVAL); // must seek
			return ScanEnum(position);
		}

		private Term ScanEnum(int position)  
		{
			while(_enum.position < position)
				if (!_enum.Next())
					return null;

			return _enum.Term();
		}

		/// <summary>
		/// Returns the position of a Term in the set or -1.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		internal int GetPosition(Term term)  
		{
			if (size == 0) return -1;

			int indexOffset = GetIndexOffset(term);
			SeekEnum(indexOffset);

			while(term.CompareTo(_enum.Term()) > 0 && _enum.Next()) {}

			if (term.CompareTo(_enum.Term()) == 0)
				return _enum.position;
			else
				return -1;
		}

		/// <summary>
		/// Returns an enumeration of all the Terms and TermInfos in the set.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public SegmentTermEnum Terms()  
		{
			if (_enum.position != -1)			  // if not at start
				SeekEnum(0);				  // reset to start
			return (SegmentTermEnum)_enum.Clone();
		}

		/// <summary>
		/// Returns an enumeration of terms starting at or after the named term.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public SegmentTermEnum Terms(Term term)  
		{
			Get(term);					  // seek _enum to term
			return (SegmentTermEnum)_enum.Clone();
		}
	}
}