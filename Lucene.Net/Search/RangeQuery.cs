using System;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

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
	/// A Query that matches documents within an exclusive range.
	/// </summary>
	[Serializable]
	public class RangeQuery : Query
	{
		private Term lowerTerm;
		private Term upperTerm;
		private bool inclusive;
    
		/// <summary>
		/// Constructs a query selecting all terms greater than 
		/// <code>lowerTerm</code> but less than <code>upperTerm</code>.
		/// There must be at least one term and either term may be null--
		/// in which case there is no bound on that side, but if there are 
		/// two term, both terms <b>must</b> be for the same field.
		/// </summary>
		/// <param name="lowerTerm"></param>
		/// <param name="upperTerm"></param>
		/// <param name="inclusive"></param>
		public RangeQuery(Term lowerTerm, Term upperTerm, bool inclusive)
		{
			if (lowerTerm == null && upperTerm == null)
			{
				throw new ArgumentException("At least one term must be non-null");
			}
			if (lowerTerm != null && upperTerm != null && lowerTerm.Field() != upperTerm.Field())
			{
				throw new ArgumentException("Both terms must be for the same field");
			}
			
			// if we have a lowerTerm, start there. otherwise, start at beginning
			if (lowerTerm != null) 
			{
				this.lowerTerm = lowerTerm;
			}
			else 
			{
				this.lowerTerm = new Term(upperTerm.Field(), "");
			}
			
			this.upperTerm = upperTerm;
			this.inclusive = inclusive;
		}

		public override Query Rewrite(IndexReader reader)  
		{
			BooleanQuery query = new BooleanQuery();
			TermEnum _enum = reader.Terms(lowerTerm);
			
			try 
			{
				bool checkLower = false;
				if (!inclusive) 
				{
					checkLower = true;
				}
				String testField = GetField();
				do 
				{
					Term term = _enum.Term();
					if (term != null && term.Field() == testField) 
					{
						if (!checkLower || String.CompareOrdinal(term.Text(), lowerTerm.Text()) > 0) 
						{
							checkLower = false;
							if (upperTerm != null) 
							{
								int compare = String.CompareOrdinal(upperTerm.Text(), term.Text());
								/* if beyond the upper term, or is exclusive and
								 * this is equal to the upper term, break out */
								if ((compare < 0) || (!inclusive && compare == 0)) break;
							}
							TermQuery tq = new TermQuery(term); // found a match
							tq.SetBoost(GetBoost());          // set the boost
							query.Add(tq, false, false); // add to query
						}
					} 
					else 
					{
						break;
					}
				}
				while (_enum.Next());
			} 
			finally 
			{
				_enum.Close();
			}
			return query;
		}
    
		public override Query Combine(Query[] queries) 
		{
			return Query.MergeBooleanQueries(queries);
		}

		private String GetField()
		{
			return (lowerTerm != null ? lowerTerm.Field() : upperTerm.Field());
		}
    
		/// <summary>
		/// Prints a user-readable version of this query.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public override String ToString(String field)
		{
			StringBuilder buffer = new StringBuilder();
			if (!GetField().Equals(field))
			{
				buffer.Append(GetField());
				buffer.Append(":");
			}
			buffer.Append(inclusive ? "[" : "{");
			buffer.Append(lowerTerm != null ? lowerTerm.Text() : "null");
			buffer.Append(" TO ");
			buffer.Append(upperTerm != null ? upperTerm.Text() : "null");
			buffer.Append(inclusive ? "]" : "}");
			if (GetBoost() != 1.0f)
			{
				buffer.Append("^");
				buffer.Append(Number.ToString(GetBoost()));
			}
			return buffer.ToString();
		}
	}
}