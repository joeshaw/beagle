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
	/// A Query that matches documents containing a subset of terms provided
	/// by a FilteredTermEnum enumeration.
	/// <P>
	/// <code>MultiTermQuery</code> is not designed to be used by itself.
	/// <BR/>
	/// The reason being that it is not intialized with a FilteredTermEnum
	/// enumeration. A FilteredTermEnum enumeration needs to be provided.</P>
	/// <P>
	/// For example, WildcardQuery and FuzzyQuery extend
	/// <code>MultiTermQuery</code> to provide WildcardTermEnum and
	/// FuzzyTermEnum, respectively.
	/// </P>
	/// </summary>
	[Serializable]
	public abstract class MultiTermQuery : Query 
	{
		private Term term;
    
		/// <summary>
		/// Constructs a query for terms matching <code>term</code>.
		/// </summary>
		/// <param name="term"></param>
		public MultiTermQuery(Term term) 
		{
			this.term = term;
		}
    
		/// <summary>
		/// Returns the pattern term.
		/// </summary>
		/// <returns></returns>
		public Term GetTerm() { return term; }

		/// <summary>
		/// Construct the enumeration to be used, expanding the pattern term.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		protected abstract FilteredTermEnum GetEnum(IndexReader reader);
    
		public override Query Rewrite(IndexReader reader) 
		{
			FilteredTermEnum _enum = GetEnum(reader);
			BooleanQuery query = new BooleanQuery();
			try 
			{
				do 
				{
					Term t = _enum.Term();
					if (t != null) 
					{
						TermQuery tq = new TermQuery(t);      // found a match
						tq.SetBoost(GetBoost() * _enum.Difference()); // set the boost
						query.Add(tq, false, false);          // add to query
					}
				} while (_enum.Next());
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

		/// <summary>
		/// Prints a user-readable version of this query.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public override String ToString(String field) 
		{
			StringBuilder buffer = new StringBuilder();
			if (!term.Field().Equals(field)) 
			{
				buffer.Append(term.Field());
				buffer.Append(":");
			}
			buffer.Append(term.Text());
			if (GetBoost() != 1.0f) 
			{
				buffer.Append("^");
				buffer.Append(Number.ToString(GetBoost()));
			}
			return buffer.ToString();
		}
	}
}