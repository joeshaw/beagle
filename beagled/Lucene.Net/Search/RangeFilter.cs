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
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using TermDocs = Lucene.Net.Index.TermDocs;
using TermEnum = Lucene.Net.Index.TermEnum;
namespace Lucene.Net.Search
{
	
    /// <summary> A Filter that restricts search results to a range of values in a given
    /// field.
    /// 
    /// <p>
    /// This code borrows heavily from {@link RangeQuery}, but is implemented as a Filter
    /// (much like {@link DateFilter}).
    /// </p>
    /// </summary>
    [Serializable]
    public class RangeFilter:Filter
    {
		
        private System.String fieldName;
        private System.String lowerTerm;
        private System.String upperTerm;
        private bool includeLower;
        private bool includeUpper;
		
        /// <param name="fieldName">The field this range applies to
        /// </param>
        /// <param name="lowerTerm">The lower bound on this range
        /// </param>
        /// <param name="upperTerm">The upper bound on this range
        /// </param>
        /// <param name="includeLower">Does this range include the lower bound?
        /// </param>
        /// <param name="includeUpper">Does this range include the upper bound?
        /// </param>
        /// <throws>  IllegalArgumentException if both terms are null or if </throws>
        /// <summary>  lowerTerm is null and includeLower is true (similar for upperTerm
        /// and includeUpper)
        /// </summary>
        public RangeFilter(System.String fieldName, System.String lowerTerm, System.String upperTerm, bool includeLower, bool includeUpper)
        {
            this.fieldName = fieldName;
            this.lowerTerm = lowerTerm;
            this.upperTerm = upperTerm;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
			
            if (null == lowerTerm && null == upperTerm)
            {
                throw new System.ArgumentException("At least one value must be non-null");
            }
            if (includeLower && null == lowerTerm)
            {
                throw new System.ArgumentException("The lower bound must be non-null to be inclusive");
            }
            if (includeUpper && null == upperTerm)
            {
                throw new System.ArgumentException("The upper bound must be non-null to be inclusive");
            }
        }
		
        /// <summary> Constructs a filter for field <code>fieldName</code> matching
        /// less than or equal to <code>upperTerm</code>.
        /// </summary>
        public static RangeFilter Less(System.String fieldName, System.String upperTerm)
        {
            return new RangeFilter(fieldName, null, upperTerm, false, true);
        }
		
        /// <summary> Constructs a filter for field <code>fieldName</code> matching
        /// greater than or equal to <code>lowerTerm</code>.
        /// </summary>
        public static RangeFilter More(System.String fieldName, System.String lowerTerm)
        {
            return new RangeFilter(fieldName, lowerTerm, null, true, false);
        }
		
        /// <summary> Returns a BitSet with true for documents which should be
        /// permitted in search results, and false for those that should
        /// not.
        /// </summary>
        public override System.Collections.BitArray Bits(IndexReader reader)
        {
            System.Collections.BitArray bits = new System.Collections.BitArray((reader.MaxDoc() % 64 == 0?reader.MaxDoc() / 64:reader.MaxDoc() / 64 + 1) * 64);
            TermEnum enumerator = (null != lowerTerm?reader.Terms(new Term(fieldName, lowerTerm)):reader.Terms(new Term(fieldName, "")));
			
            try
            {
				
                if (enumerator.Term() == null)
                {
                    return bits;
                }
				
                bool checkLower = false;
                if (!includeLower)
                    // make adjustments to set to exclusive
                    checkLower = true;
				
                TermDocs termDocs = reader.TermDocs();
                try
                {
					
                    do 
                    {
                        Term term = enumerator.Term();
                        if (term != null && term.Field().Equals(fieldName))
                        {
                            if (!checkLower || null == lowerTerm || String.CompareOrdinal(term.Text(), lowerTerm) > 0)
                            {
                                checkLower = false;
                                if (upperTerm != null)
                                {
                                    int compare = String.CompareOrdinal(upperTerm, term.Text());
                                    /* if beyond the upper term, or is exclusive and
                                    * this is equal to the upper term, break out */
                                    if ((compare < 0) || (!includeUpper && compare == 0))
                                    {
                                        break;
                                    }
                                }
                                /* we have a good term, find the docs */
								
                                termDocs.Seek(enumerator.Term());
                                while (termDocs.Next())
                                {
                                    bits.Set(termDocs.Doc(), true);
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (enumerator.Next());
                }
                finally
                {
                    termDocs.Close();
                }
            }
            finally
            {
                enumerator.Close();
            }
			
            return bits;
        }
		
        public override System.String ToString()
        {
            System.Text.StringBuilder buffer = new System.Text.StringBuilder();
            buffer.Append(fieldName);
            buffer.Append(":");
            buffer.Append(includeLower?"[":"{");
            if (null != lowerTerm)
            {
                buffer.Append(lowerTerm);
            }
            buffer.Append("-");
            if (null != upperTerm)
            {
                buffer.Append(upperTerm);
            }
            buffer.Append(includeUpper?"]":"}");
            return buffer.ToString();
        }
    }
}