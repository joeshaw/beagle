/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using BooleanClause = Lucene.Net.Search.BooleanClause;
using BooleanQuery = Lucene.Net.Search.BooleanQuery;
using Query = Lucene.Net.Search.Query;
namespace Lucene.Net.QueryParsers
{
	
	/// <summary> A QueryParser which constructs queries to search multiple fields.
	/// 
	/// </summary>
	/// <author>  <a href="mailto:kelvin@relevanz.com">Kelvin Tan</a>
	/// </author>
	/// <version>  $Revision$
	/// </version>
	public class MultiFieldQueryParser : QueryParser
	{
		
        private System.String[] fields;
		
        /// <summary> Creates a MultiFieldQueryParser.
        /// 
        /// <p>It will, when parse(String query)
        /// is called, construct a query like this (assuming the query consists of
        /// two terms and you specify the two fields <code>title</code> and <code>body</code>):</p>
        /// 
        /// <code>
        /// (title:term1 body:term1) (title:term2 body:term2)
        /// </code>
        /// 
        /// <p>When setDefaultOperator(AND_OPERATOR) is set, the result will be:</p>
        /// 
        /// <code>
        /// +(title:term1 body:term1) +(title:term2 body:term2)
        /// </code>
        /// 
        /// <p>In other words, all the query's terms must appear, but it doesn't matter in
        /// what fields they appear.</p>
        /// </summary>
        public MultiFieldQueryParser(System.String[] fields, Analyzer analyzer) : base(null, analyzer)
        {
            this.fields = fields;
        }
		
        protected internal override Query GetFieldQuery(System.String field, System.String queryText)
        {
            if (field == null)
            {
                System.Collections.ArrayList clauses = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
                for (int i = 0; i < fields.Length; i++)
                    clauses.Add(new BooleanClause(base.GetFieldQuery(fields[i], queryText), BooleanClause.Occur.SHOULD));
                return GetBooleanQuery(clauses, true);
            }
            return base.GetFieldQuery(field, queryText);
        }
		
        protected internal override Query GetFieldQuery(System.String field, Analyzer analyzer, System.String queryText)
        {
            if (field == null)
            {
                System.Collections.ArrayList clauses = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
                for (int i = 0; i < fields.Length; i++)
                    clauses.Add(new BooleanClause(base.GetFieldQuery(fields[i], queryText), BooleanClause.Occur.SHOULD));
                return GetBooleanQuery(clauses, true);
            }
            return base.GetFieldQuery(field, queryText);
        }
		
        /// <deprecated> use {@link #GetFuzzyQuery(String, String, float)}
        /// </deprecated>
        protected internal override Query GetFuzzyQuery(System.String field, System.String termStr)
        {
            return GetFuzzyQuery(field, termStr, fuzzyMinSim);
        }
		
        protected internal override Query GetFuzzyQuery(System.String field, System.String termStr, float minSimilarity)
        {
            if (field == null)
            {
                System.Collections.ArrayList clauses = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(base.GetFuzzyQuery(fields[i], termStr, minSimilarity), BooleanClause.Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetFuzzyQuery(field, termStr, minSimilarity);
        }
		
        protected internal override Query GetPrefixQuery(System.String field, System.String termStr)
        {
            if (field == null)
            {
                System.Collections.ArrayList clauses = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(base.GetPrefixQuery(fields[i], termStr), BooleanClause.Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetPrefixQuery(field, termStr);
        }
		
        /// <throws>  ParseException </throws>
        /// <deprecated> use {@link #GetRangeQuery(String, String, String, boolean)}
        /// </deprecated>
        protected internal override Query GetRangeQuery(System.String field, Analyzer analyzer, System.String part1, System.String part2, bool inclusive)
        {
            return GetRangeQuery(field, part1, part2, inclusive);
        }
		
        protected internal override Query GetRangeQuery(System.String field, System.String part1, System.String part2, bool inclusive)
        {
            if (field == null)
            {
                System.Collections.ArrayList clauses = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(base.GetRangeQuery(fields[i], part1, part2, inclusive), BooleanClause.Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetRangeQuery(field, part1, part2, inclusive);
        }
		
		
        public const int NORMAL_FIELD = 0;
        public const int REQUIRED_FIELD = 1;
        public const int PROHIBITED_FIELD = 2;
		
        /// <deprecated> use {@link #MultiFieldQueryParser(String[], Analyzer)} instead
        /// </deprecated>
        public MultiFieldQueryParser(QueryParserTokenManager tm):base(tm)
        {
        }
		
        /// <deprecated> use {@link #MultiFieldQueryParser(String[], Analyzer)} instead
        /// </deprecated>
        public MultiFieldQueryParser(CharStream stream):base(stream)
        {
        }
		
        /// <deprecated> use {@link #MultiFieldQueryParser(String[], Analyzer)} instead
        /// </deprecated>
        public MultiFieldQueryParser(System.String f, Analyzer a):base(f, a)
        {
        }
		
        /// <summary> <p>Parses a query which searches on the fields specified.
        /// If x fields are specified, this effectively constructs:</p>
        /// 
        /// <code>
        /// (field1:query) (field2:query) (field3:query)...(fieldx:query)
        /// </code>
        /// 
        /// </summary>
        /// <param name="query">Query string to parse
        /// </param>
        /// <param name="fields">Fields to search on
        /// </param>
        /// <param name="analyzer">Analyzer to use
        /// </param>
        /// <throws>  ParseException if query parsing fails </throws>
        /// <throws>  TokenMgrError if query parsing fails </throws>
        /// <deprecated> use {@link #Parse(String)} instead but note that it
        /// returns a different query for queries where all terms are required:
        /// its query excepts all terms, no matter in what field they occur whereas
        /// the query built by this (deprecated) method expected all terms in all fields 
        /// at the same time.
        /// </deprecated>
        public static Query Parse(System.String query, System.String[] fields, Analyzer analyzer)
        {
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                Query q = Parse(query, fields[i], analyzer);
                bQuery.Add(q, BooleanClause.Occur.SHOULD);
            }
            return bQuery;
        }
		
        /// <summary> <p>
        /// Parses a query which searches on the fields specified.
        /// <p>
        /// If x fields are specified, this effectively constructs:
        /// <pre>
        /// <code>
        /// (field1:query1) (field2:query2) (field3:query3)...(fieldx:queryx)
        /// </code>
        /// </pre>
        /// </summary>
        /// <param name="queries">Queries strings to parse
        /// </param>
        /// <param name="fields">Fields to search on
        /// </param>
        /// <param name="analyzer">Analyzer to use
        /// </param>
        /// <throws>  ParseException if query parsing fails </throws>
        /// <throws>  TokenMgrError if query parsing fails </throws>
        /// <throws>  IllegalArgumentException if the length of the queries array differs </throws>
        /// <summary>  from the length of the fields array
        /// </summary>
        public static Query Parse(System.String[] queries, System.String[] fields, Analyzer analyzer)
        {
            if (queries.Length != fields.Length)
                throw new System.ArgumentException("queries.length != fields.length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                Query q = Parse(queries[i], fields[i], analyzer);
                bQuery.Add(q, BooleanClause.Occur.SHOULD);
            }
            return bQuery;
        }
		
        /// <summary> <p>
        /// Parses a query, searching on the fields specified.
        /// Use this if you need to specify certain fields as required,
        /// and others as prohibited.
        /// <p><pre>
        /// Usage:
        /// <code>
        /// String[] fields = {"filename", "contents", "description"};
        /// int[] flags = {MultiFieldQueryParser.NORMAL_FIELD,
        /// MultiFieldQueryParser.REQUIRED_FIELD,
        /// MultiFieldQueryParser.PROHIBITED_FIELD,};
        /// parse(query, fields, flags, analyzer);
        /// </code>
        /// </pre>
        /// <p>
        /// The code above would construct a query:
        /// <pre>
        /// <code>
        /// (filename:query) +(contents:query) -(description:query)
        /// </code>
        /// </pre>
        /// 
        /// </summary>
        /// <param name="query">Query string to parse
        /// </param>
        /// <param name="fields">Fields to search on
        /// </param>
        /// <param name="flags">Flags describing the fields
        /// </param>
        /// <param name="analyzer">Analyzer to use
        /// </param>
        /// <throws>  ParseException if query parsing fails </throws>
        /// <throws>  TokenMgrError if query parsing fails </throws>
        /// <throws>  IllegalArgumentException if the length of the fields array differs </throws>
        /// <summary>  from the length of the flags array
        /// </summary>
        public static Query Parse(System.String query, System.String[] fields, int[] flags, Analyzer analyzer)
        {
            if (fields.Length != flags.Length)
                throw new System.ArgumentException("fields.length != flags.length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                Query q = Parse(query, fields[i], analyzer);
                int flag = flags[i];
                switch (flag)
                {
					
                    case REQUIRED_FIELD: 
                        bQuery.Add(q, BooleanClause.Occur.MUST);
                        break;
					
                    case PROHIBITED_FIELD: 
                        bQuery.Add(q, BooleanClause.Occur.MUST_NOT);
                        break;
					
                    default: 
                        bQuery.Add(q, BooleanClause.Occur.SHOULD);
                        break;
					
                }
            }
            return bQuery;
        }
		
        /// <summary> <p>
        /// Parses a query, searching on the fields specified.
        /// Use this if you need to specify certain fields as required,
        /// and others as prohibited.
        /// <p><pre>
        /// Usage:
        /// <code>
        /// String[] fields = {"filename", "contents", "description"};
        /// int[] flags = {MultiFieldQueryParser.NORMAL_FIELD,
        /// MultiFieldQueryParser.REQUIRED_FIELD,
        /// MultiFieldQueryParser.PROHIBITED_FIELD,};
        /// parse(query, fields, flags, analyzer);
        /// </code>
        /// </pre>
        /// <p>
        /// The code above would construct a query:
        /// <pre>
        /// <code>
        /// (filename:query1) +(contents:query2) -(description:query3)
        /// </code>
        /// </pre>
        /// 
        /// </summary>
        /// <param name="queries">Queries string to parse
        /// </param>
        /// <param name="fields">Fields to search on
        /// </param>
        /// <param name="flags">Flags describing the fields
        /// </param>
        /// <param name="analyzer">Analyzer to use
        /// </param>
        /// <throws>  ParseException if query parsing fails </throws>
        /// <throws>  TokenMgrError if query parsing fails </throws>
        /// <throws>  IllegalArgumentException if the length of the queries, fields, </throws>
        /// <summary>  and flags array differ
        /// </summary>
        public static Query Parse(System.String[] queries, System.String[] fields, int[] flags, Analyzer analyzer)
        {
            if (!(queries.Length == fields.Length && queries.Length == flags.Length))
                throw new System.ArgumentException("queries, fields, and flags array have have different length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                Query q = Parse(queries[i], fields[i], analyzer);
                int flag = flags[i];
                switch (flag)
                {
					
                    case REQUIRED_FIELD: 
                        bQuery.Add(q, BooleanClause.Occur.MUST);
                        break;
					
                    case PROHIBITED_FIELD: 
                        bQuery.Add(q, BooleanClause.Occur.MUST_NOT);
                        break;
					
                    default: 
                        bQuery.Add(q, BooleanClause.Occur.SHOULD);
                        break;
					
                }
            }
            return bQuery;
        }
    }
}