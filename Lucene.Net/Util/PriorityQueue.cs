using System;

namespace Lucene.Net.Util
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
	/// A PriorityQueue maintains a partial ordering of its elements such that the
	/// least element can always be found in constant time.  Put()'s and Pop()'s
	/// require Log(size) time.
	/// </summary>
	public abstract class PriorityQueue 
	{
		private Object[] heap;
		private int size;
		private int maxSize;

		/// <summary>
		/// Determines the ordering of objects in this priority queue.  Subclasses
		/// must define this one method.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		protected abstract bool LessThan(Object a, Object b);

		/// <summary>
		/// Subclass constructors must call this.
		/// </summary>
		/// <param name="max_size"></param>
		protected void Initialize(int max_size) 
		{
			size = 0;
			int heap_size = max_size + 1;
			heap = new Object[heap_size];
			this.maxSize = max_size;
		}

		/// <summary>
		/// Adds an Object to a PriorityQueue in Log(size) time.
		/// </summary>
		/// <param name="element"></param>
		public void Put(Object element) 
		{
			size++;
			heap[size] = element;
			UpHeap();
		}

		/// <summary>
		/// Adds element to the PriorityQueue in log(size) time if either
		/// the PriorityQueue is not full, or not lessThan(element, top()).
		/// </summary>
		/// <param name="element">element</param>
		/// <returns>true if element is added, false otherwise.</returns>
		public bool Insert(Object element)
		{
			if(size < maxSize)
			{
				Put(element);
				return true;
			}
			else if(size > 0 && !LessThan(element, Top()))
			{
				heap[1] = element;
				AdjustTop();
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Returns the least element of the PriorityQueue in constant time.
		/// </summary>
		/// <returns></returns>
		public Object Top() 
		{
			if (size > 0)
				return heap[1];
			else
				return null;
		}

		/// <summary>
		/// Removes and returns the least element of the PriorityQueue in Log(size)
		///	time.
		/// </summary>
		/// <returns></returns>
		public Object Pop() 
		{
			if (size > 0) 
			{
				Object result = heap[1];			  // save first value
				heap[1] = heap[size];			  // move last to first
				heap[size] = null;			  // permit GC of objects
				size--;
				DownHeap();				  // adjust heap
				return result;
			} 
			else
				return null;
		}

		/// <summary>
		/// Should be called when the Object at top changes values.  Still Log(n)
		/// worst case, but it's at least twice as fast to <pre>
		/// { pq.Top().Change(); pq.AdjustTop(); }
		/// </pre> instead of <pre>
		/// { o = pq.Pop(); o.Change(); pq.Push(o); }
		/// </pre>
		/// </summary>
		public void AdjustTop() 
		{
			DownHeap();
		}

		/// <summary>
		/// Returns the number of elements currently stored in the PriorityQueue.
		/// </summary>
		/// <returns></returns>
		public int Size() 
		{
			return size;
		}

		/// <summary>
		/// Removes all entries from the PriorityQueue.
		/// </summary>
		public void Clear() 
		{
			for (int i = 0; i <= size; i++)
				heap[i] = null;
			size = 0;
		}

		private void UpHeap() 
		{
			int i = size;
			Object node = heap[i];			  // save bottom node
			int j = (int)(((uint)i) >> 1);
			while (j > 0 && LessThan(node, heap[j])) 
			{
				heap[i] = heap[j];			  // shift parents down
				i = j;
				j = (int)(((uint)j) >> 1);
			}
			heap[i] = node;				  // install saved node
		}

		private void DownHeap() 
		{
			int i = 1;
			Object node = heap[i];			  // save top node
			int j = i << 1;				  // find smaller child
			int k = j + 1;
			if (k <= size && LessThan(heap[k], heap[j])) 
			{
				j = k;
			}
			while (j <= size && LessThan(heap[j], node)) 
			{
				heap[i] = heap[j];			  // shift up child
				i = j;
				j = i << 1;
				k = j + 1;
				if (k <= size && LessThan(heap[k], heap[j])) 
				{
					j = k;
				}
			}
			heap[i] = node;				  // install saved node
		}
	}
}