using System;
using System.Collections;

using Lucene.Net.Util; 

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
	/// Describe class <code>MultipleTermPositions</code> here.
	/// </summary>
	/// <author>Anders Nielsen</author> 
	/// <version>1.0</version>
	public class MultipleTermPositions : TermPositions
	{
		internal sealed class TermPositionsQueue : PriorityQueue
		{
			internal TermPositionsQueue(IList termPositions)
	    
			{
				Initialize(termPositions.Count);

				foreach (TermPositions tp in termPositions)
				{
					if (tp.Next())
						Put(tp);
				}
			}

			internal TermPositions Peek()
			{
				return (TermPositions)Top();
			}

			override protected bool LessThan(Object a, Object b)
			{
				return ((TermPositions)a).Doc() < ((TermPositions)b).Doc();
			}
		}

		private sealed class IntQueue
		{
			private int _arraySize = 16;

			private int _index = 0;
			private int _lastIndex = 0;

			private int[] _array;

			internal IntQueue()
			{
				_array = new int[_arraySize];
			}

			internal void Add(int i)
			{
				if (_lastIndex == _arraySize)
					GrowArray();

				_array[_lastIndex++] = i;
			}

			internal int Next()
			{
				return _array[_index++];
			}

			internal void Sort()
			{
				Array.Sort(_array, _index, _lastIndex - _index);
			}

			internal void Clear()
			{
				_index = 0;
				_lastIndex = 0;
			}

			internal int Size()
			{
				return (_lastIndex-_index);
			}

			private void GrowArray()
			{
				int[] newArray = new int[_arraySize*2];
				Array.Copy(_array, 0, newArray, 0, _arraySize);
				_array = newArray;
				_arraySize *= 2;
			}
		}

		private int doc;
		private int freq;

		private TermPositionsQueue termPositionsQueue;
		private IntQueue posList;

		/// <summary>
		/// Creates a new <code>MultipleTermPositions</code> instance.
		/// </summary>
		/// <param name="indexReader">an <code>IndexReader</code> value</param>
		/// <param name="terms"><code>Term[]</code> value</param>
		/// <exception cref="System.IO.IOException">if an error occurs</exception>
		public MultipleTermPositions(IndexReader indexReader, Term[] terms)
	
		{
			IList termPositions = new ArrayList();

			for (int i=0; i< terms.Length; i++)
				termPositions.Add(indexReader.TermPositions(terms[i]));

			termPositionsQueue = new TermPositionsQueue(termPositions);
			posList = new IntQueue();
		}

		/// <summary>
		/// Describe <code>next</code> method here.
		/// <seealso cref="TermDocs.Next()"/>
		/// </summary>
		/// <returns>a <code>bool</code> value</returns>
		/// <exception cref="System.IO.IOException">if an error occurs</exception>
		public bool Next()
	
		{
			if (termPositionsQueue.Size() == 0)
				return false;

			posList.Clear();
			doc = termPositionsQueue.Peek().Doc();

			TermPositions tp;
			do
			{
				tp = termPositionsQueue.Peek();

				for (int i=0; i<tp.Freq(); i++)
					posList.Add(tp.NextPosition());

				if (tp.Next())
					termPositionsQueue.AdjustTop();
				else
				{
					termPositionsQueue.Pop();
					tp.Close();
				}
			}
			while (termPositionsQueue.Size() > 0 && termPositionsQueue.Peek().Doc() == doc);

			posList.Sort();
			freq = posList.Size();

			return true;
		}

		/// <summary>
		/// Describe <code>NextPosition</code> method here.
		/// <seealso cref="TermPositions.NextPosition()"/>
		/// </summary>
		/// <returns>an <code>int</code> value</returns>
		/// <exception cref="System.IO.IOException">if an error occurs</exception>  
		public int NextPosition()
	
		{
			return posList.Next();
		}

		/// <summary>
		/// Describe <code>SkipTo</code> method here.
		/// <see cref="TermDocs.SkipTo(int)"/>
		/// </summary>
		/// <param name="target">an <code>int</code> value</param>
		/// <returns>a <code>bool</code> value</returns>
		/// <exception>IOException if an error occurs</exception> 
		public bool SkipTo(int target)
	
		{
			while (target > termPositionsQueue.Peek().Doc())
			{
				TermPositions tp = (TermPositions)termPositionsQueue.Pop();

				if (tp.SkipTo(target))
					termPositionsQueue.Put(tp);
				else
					tp.Close();
			}

			return Next();
		}

		/// <summary>
		/// Describe <code>Doc</code> method here. 
		/// <see cref="TermDocs.Doc()"/>
		/// </summary>
		/// <returns>an <code>int</code> value</returns>
		public int Doc()
		{
			return doc;
		}

		/// <summary>
		/// Describe <code>Freq</code> method here.
		/// <see cref="TermDocs.Freq()"/>
		/// </summary>
		/// <returns>an <code>int</code> value</returns>
		public int Freq()
		{
			return freq;
		}

		/// <summary>
		/// Describe <code>Close</code> method here. 
		/// <see cref="TermDocs.Close()"/>
		/// </summary>
		/// <exception cref="System.IO.IOException">if an error occurs</exception> 
		public void Close()
		{
			while (termPositionsQueue.Size() > 0)
				((TermPositions)termPositionsQueue.Pop()).Close();
		}

		/// <summary>
		/// Describe <code>Seek</code> method here.
		/// <see cref="TermDocs.Seek(Term)"/>
		/// </summary>
		/// <param name="arg0">a <code>Term</code> value</param>
		public void Seek(Term arg0)
		{
			throw new InvalidOperationException();
		}

		public void Seek(TermEnum termEnum)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Describe <code>Read</code> method here.
		/// </summary>
		/// <param name="arg0">an <code>int[]</code> value</param>
		/// <param name="arg1">an <code>int[]</code> value</param>
		/// <returns>an <code>int</code> value</returns>
		public int Read(int[] arg0, int[] arg1)
		{
			throw new InvalidOperationException();
		}
	}
}