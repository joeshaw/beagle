using System;
using Lucene.Net.Analysis;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers
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
	 * 4. The names "Apache" and "Apache Software Foundation"
	 *    must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    nor may "Apache" appear in their name, without
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
	/// A QueryParser which constructs queries to search multiple fields.
	/// </summary>
	/// <author><a href="mailto:kelvin@relevanz.com">Kelvin Tan</a></author>
	/// <version>$Revision$</version>
	public class MultiFieldQueryParser : QueryParser
	{
		public const int NORMAL_FIELD     = 0;
		public const int REQUIRED_FIELD   = 1;
		public const int PROHIBITED_FIELD = 2;

		public MultiFieldQueryParser(QueryParserTokenManager tm) : base(tm)
		{
		}

		public MultiFieldQueryParser(CharStream stream) : base(stream)
		{
		}

		public MultiFieldQueryParser(String f, Analyzer a) : base(f, a)
		{
		}

		/// <summary>
		/// <p>Parses a query which searches on the fields specified.</p>
		/// <p>If x fields are specified, this effectively constructs:</p>
		/// <pre>
		/// <code>
		/// (field1:query) (field2:query) (field3:query)...(fieldx:query)
		/// </code>
		/// </pre>
		/// </summary>
		/// <param name="query">Query string to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <returns></returns>
		/// <throws>ParserException if query parsing fails</throws> 
		/// <throws>TokenMgrError if query parsing fails</throws>
		public static Query Parse(String query, String[] fields, Analyzer analyzer)
		{
			BooleanQuery bQuery = new BooleanQuery();
			for (int i = 0; i < fields.Length; i++)
			{
				Query q = Parse(query, fields[i], analyzer);
				bQuery.Add(q, false, false);
			}
			return bQuery;
		}

		/// <summary>
		/// <p>
		/// Parses a query, searching on the fields specified.
		/// Use this if you need to specify certain fields as required,
		/// and others as prohibited.</p>
		/// <p><pre>
		/// Usage:
		/// <code>
		/// String[] fields = {"filename", "contents", "description"};
		/// int[] flags = {MultiFieldQueryParser.NORMAL FIELD,
		///                MultiFieldQueryParser.REQUIRED FIELD,
		///                MultiFieldQueryParser.PROHIBITED FIELD,};
		/// Parse(query, fields, flags, analyzer);
		/// </code>
		/// </pre></p>
		///<p>
		/// The code above would construct a query:
		/// <pre>
		/// <code>
		/// (filename:query) +(contents:query) -(description:query)
		/// </code>
		/// </pre></p>
		/// </summary>
		/// <param name="query">Query string to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="flags">Flags describing the fields</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <returns></returns>
		/// <throws>ParserException if query parsing fails</throws>
		/// <throws>TokenMgrError if query parsing fails</throws> 
		public static Query Parse(String query, String[] fields, int[] flags,
			Analyzer analyzer)
		{
			BooleanQuery bQuery = new BooleanQuery();
			for (int i = 0; i < fields.Length; i++)
			{
				Query q = Parse(query, fields[i], analyzer);
				int flag = flags[i];
				switch (flag)
				{
					case REQUIRED_FIELD:
						bQuery.Add(q, true, false);
						break;
					case PROHIBITED_FIELD:
						bQuery.Add(q, false, true);
						break;
					default:
						bQuery.Add(q, false, false);
						break;
				}
			}
			return bQuery;
		}
	}
}