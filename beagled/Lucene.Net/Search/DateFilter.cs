using System;
using System.Text;
using System.IO;
using System.Collections;
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
	/// A Filter that restricts search results to a range of time.
	/// <p>For this to work, documents must have been indexed with a DateField.</p>
	/// </summary>
	[Serializable]
	public class DateFilter : Filter 
	{
		internal String field;

		internal String start = DateField.MIN_DATE_STRING();
		internal String end = DateField.MAX_DATE_STRING();

		private DateFilter(String f) 
		{
			field = f;
		}

		/// <summary>
		/// Constructs a filter for field <code>f</code> matching dates between
		/// <code>from</code> and <code>to</code>.
		/// </summary>
		/// <param name="f"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public DateFilter(String f, DateTime from, DateTime to) 
		{
			field = f;
			start = DateField.DateToString(from);
			end = DateField.DateToString(to);
		}
		
		/// <summary>
		/// Constructs a filter for field <code>f</code> matching times between
		/// <code>from</code> and <code>to</code>.
		/// </summary>
		/// <param name="f"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public DateFilter(String f, long from, long to) 
		{
			field = f;
			start = DateField.TimeToString(from);
			end = DateField.TimeToString(to);
		}

		/// <summary>
		/// Constructs a filter for field <code>f</code> matching dates before
		/// <code>date</code>.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="date"></param>
		/// <returns></returns>
		public static DateFilter Before(String field, DateTime date) 
		{
			DateFilter result = new DateFilter(field);
			result.end = DateField.DateToString(date);
			return result;
		}
		
		/// <summary>
		/// Constructs a filter for field <code>f</code> matching times before
		/// <code>time</code>.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="time"></param>
		/// <returns></returns>
		public static DateFilter Before(String field, long time) 
		{
			DateFilter result = new DateFilter(field);
			result.end = DateField.TimeToString(time);
			return result;
		}

		/// <summary>
		/// Constructs a filter for field <code>f</code> matching dates after
		/// <code>date</code>.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="date"></param>
		/// <returns></returns>
		public static DateFilter After(String field, DateTime date) 
		{
			DateFilter result = new DateFilter(field);
			result.start = DateField.DateToString(date);
			return result;
		}
		
		/// <summary>
		/// Constructs a filter for field <code>f</code> matching times after
		/// <code>time</code>.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="time"></param>
		/// <returns></returns>
		public static DateFilter After(String field, long time) 
		{
			DateFilter result = new DateFilter(field);
			result.start = DateField.TimeToString(time);
			return result;
		}

		/// <summary>
		/// Returns a BitSet with true for documents which should be permitted in
		/// search results, and false for those that should not.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public override BitArray Bits(IndexReader reader)  
		{
			BitArray bits = new BitArray(reader.MaxDoc());
			TermEnum _enum = reader.Terms(new Term(field, start));
			TermDocs termDocs = reader.TermDocs();
			if (_enum.Term() == null)
				return bits;

			try 
			{
				Term stop = new Term(field, end);
				while (_enum.Term().CompareTo(stop) <= 0) 
				{
					termDocs.Seek(_enum.Term());

					while (termDocs.Next())
						bits.Set(termDocs.Doc(), true);

					if (!_enum.Next()) 
					{
						break;
					}
				}
			} 
			finally 
			{
				_enum.Close();
				termDocs.Close();
			}
			return bits;
		}

		public override String ToString() 
		{
			StringBuilder buffer = new StringBuilder();
			buffer.Append(field);
			buffer.Append(":");
			buffer.Append(DateField.StringToDate(start).ToString());
			buffer.Append("-");
			buffer.Append(DateField.StringToDate(end).ToString());
			return buffer.ToString();
		}
	}
}