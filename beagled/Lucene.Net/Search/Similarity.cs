using System;
using System.Collections; 

using Lucene.Net.Index; 
using Lucene.Net.Documents; 

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
	/// Expert: Scoring API.
	/// <p>Subclasses implement search scoring.</p>
	/// <p>The score of query <code>q</code> for document <code>d</code> is defined
	/// in terms of these methods as follows:</p>
	/// <table cellpadding="0" cellspacing="0" border="0">
	///  <tr>
	///    <td valign="middle" align="right" rowspan="2">score(q,d) =<br/></td>
	///    <td valign="middle" align="center">
	///    <big><big><big><big><big>Sigma;</big></big></big></big></big></td>
	///    <td valign="middle"><small>
	///		Tf(int) tf}(t in d) 
	///     Idf(Term,Searcher) idf}(t) 
	///		Field.GetBoost GetBoost(t.field in d) 
	///     LengthNorm(String,int) lengthNorm(t.field in d)
	///    </small></td>
	///    <td valign="middle" rowspan="2"> 
	///     Coord(int,int) Coord(q,d) 
	///     QueryNorm(float) QueryNorm(q)
	///    </td>
	///  </tr>
	///  <tr> 
	///   <td valign="top" align="right">
	///    <small>t in q</small>
	///    </td>
	///  </tr>
	/// </table>
	/// 
	/// <see cref="SetDefault(Similarity)"/>
	/// <see cref="IndexWriter.SetSimilarity(Similarity)"/>
	/// <see cref="Searcher.SetSimilarity(Similarity)"/>
	/// </summary>
	public abstract class Similarity 
	{
		/// <summary>
		/// The Similarity implementation used by default.
		/// </summary>
		private static Similarity defaultImpl = new DefaultSimilarity();

		/// <summary>
		/// Set the default Similarity implementation used by indexing and search
		/// code.
		/// <see cref="Searcher.SetSimilarity(Similarity)"/>
		/// <see cref="IndexWriter.SetSimilarity(Similarity)"/>
		/// </summary>
		/// <param name="similarity"></param>
		public static void SetDefault(Similarity similarity) 
		{
			Similarity.defaultImpl = similarity;
		}

		/// <summary>
		/// Return the default Similarity implementation used by indexing and search
		/// code.
		/// <p>This is initially an instance of DefaultSimilarity.</p>
		/// <see cref="Searcher.SetSimilarity(Similarity)"/>
		/// <see cref="IndexWriter.SetSimilarity(Similarity)"/>
		/// </summary>
		/// <returns></returns>
		public static Similarity GetDefault() 
		{
			return Similarity.defaultImpl;
		}

		/// <summary>
		/// Cache of decoded bytes.
		/// </summary>
		private static readonly float[] NORM_TABLE = new float[256];

		static Similarity() 
		{
			for (int i = 0; i < 256; i++)
				NORM_TABLE[i] = ByteToFloat((byte)i);
		}

		/// <summary>
		/// Decodes a normalization factor stored in an index.
		/// <see cref="EncodeNorm(float)"/>
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float DecodeNorm(byte b) 
		{
			return NORM_TABLE[b & 0xFF];
		}

		/// <summary>
		/// Computes the normalization value for a field given the total number of
		/// terms contained in a field.  These values, together with field boosts, are
		/// stored in an index and multipled into scores for hits on each field by the
		/// search code.
		///
		/// <p>Matches in longer fields are less precise, so implemenations of this
		/// method usually return smaller values when <code>numTokens</code> is large,
		/// and larger values when <code>numTokens</code> is small.</p>
		/// 
		/// <p>That these values are computed under 
		/// IndexWriter.AddDocument(Document) and stored then using
		/// EncodeNorm(float).  Thus they have limited precision, and documents
		/// must be re-indexed if this method is altered.
		/// </p>
		/// 
		/// <see cref="Field.SetBoost(float)"/>
		/// </summary>
		/// <param name="fieldName">the name of the field</param>
		/// <param name="numTokens">
		///		the total number of tokens contained in fields named
		///		<i>fieldName</i> of <i>doc</i>.
		/// </param>
		/// <returns>a normalization factor for hits on this field of this document</returns>
		public abstract float LengthNorm(String fieldName, int numTokens);

		/// <summary>
		/// Computes the normalization value for a query given the sum of the squared
		/// weights of each of the query terms.  This value is then multipled into the
		/// weight of each query term.
		///
		/// <p>This does not affect ranking, but rather just attempts to make scores
		/// from different queries comparable.
		/// </p>
		/// </summary>
		/// <param name="sumOfSquaredWeights">the sum of the squares of query term weights</param>
		/// <returns>a normalization factor for query weights</returns>
		public abstract float QueryNorm(float sumOfSquaredWeights);

		/// <summary>
		/// Encodes a normalization factor for storage in an index.  
		///
		/// <p>The encoding uses a five-bit exponent and three-bit mantissa, thus
		/// representing values from around 7x10^9 to 2x10^-9 with about one
		/// significant decimal digit of accuracy.  Zero is also represented.
		/// Negative numbers are rounded up to zero.  Values too large to represent
		/// are rounded down to the largest representable value.  Positive values too
		/// small to represent are rounded up to the smallest positive representable
		/// value.
		/// </p>
		/// 
		/// <see cref="Field.SetBoost(float)"/>
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		public static byte EncodeNorm(float f) 
		{
			return FloatToByte(f);
		}

		private static float ByteToFloat(byte b) 
		{
			if (b == 0)                                   // zero is a special case
				return 0.0f;
			int mantissa = b & 7;
			int exponent = (b >> 3) & 31;
			int bits = ((exponent+(63-15)) << 24) | (mantissa << 21);

			
			return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
		}
   
		private static byte FloatToByte(float f) 
		{
			if (f < 0.0f)                                 // round negatives up to zero
				f = 0.0f;

			if (f == 0.0f)                                // zero is a special case
				return 0;

			int bits = BitConverter.ToInt32(BitConverter.GetBytes(f), 0);           // parse float into parts
			int mantissa = (bits & 0xffffff) >> 21;
			int exponent = (((bits >> 24) & 0x7f) - 63) + 15;

			if (exponent > 31) 
			{                          // overflow: use max value
				exponent = 31;
				mantissa = 7;
			}

			if (exponent < 0) 
			{                           // underflow: use min value
				exponent = 0;
				mantissa = 1;
			}

			return (byte)((exponent << 3) | mantissa);    // pack into a byte
		}

		/// <summary>
		/// Computes a score factor based on a term or phrase's frequency in a
		/// document.  This value is multiplied by the Idf(Term, Searcher)
		/// factor for each term in the query and these products are then summed to
		/// form the initial score for a document.
		///
		/// <p>Terms and phrases repeated in a document indicate the topic of the
		/// document, so implemenations of this method usually return larger values
		/// when <code>freq</code> is large, and smaller values when <code>freq</code>
		/// is small.
		/// </p>
		///
		/// <p>The default implementation calls Tf(float).</p>
		/// </summary>
		/// <param name="freq">the frequency of a term within a document</param>
		/// <returns>a score factor based on a term's within-document frequency</returns>
		virtual public float Tf(int freq) 
		{
			return Tf((float)freq);
		}

		/// <summary>
		/// Computes the amount of a sloppy phrase match, based on an edit distance.
		/// This value is summed for each sloppy phrase match in a document to form
		/// the frequency that is passed to Tf(float).
		///
		/// <p>A phrase match with a small edit distance to a document passage more
		/// closely matches the document, so implemenations of this method usually
		/// return larger values when the edit distance is small and smaller values
		/// when it is large.
		/// </p>
		///
		/// <see cref="PhraseQuery.SetSlop(int)"/>
		/// </summary>
		/// <param name="distance">the edit distance of this sloppy phrase match</param>
		/// <returns>the frequency increment for this match</returns>
		public abstract float SloppyFreq(int distance);

		/// <summary>
		/// Computes a score factor based on a term or phrase's frequency in a
		/// document.  This value is multiplied by the Idf(Term, Searcher)
		/// factor for each term in the query and these products are then summed to
		/// form the initial score for a document.
		///
		/// <p>Terms and phrases repeated in a document indicate the topic of the
		/// document, so implemenations of this method usually return larger values
		/// when <code>freq</code> is large, and smaller values when <code>freq</code>
		/// is small.
		/// </p>
		///
		/// </summary>
		/// <param name="freq">the frequency of a term within a document</param>
		/// <returns>a score factor based on a term's within-document frequency</returns>
		public abstract float Tf(float freq);
    
		/// <summary>
		/// Computes a score factor for a simple term.
		///
		/// <p>The default implementation is:
		/// <pre>
		///   return Idf(searcher.DocFreq(term), searcher.MaxDoc());
		/// </pre>
		///
		/// Note that Searcher.MaxDoc() is used instead of 
		/// IndexReader.NumDocs() because it is proportional to 
		/// Searcher.DocFreq(Term), i.e., when one is inaccurate, so is the other,
		/// and in the same direction.
		/// </p>
		///
		/// </summary>
		/// <param name="term">the term in question</param>
		/// <param name="searcher">the document collection being searched</param>
		/// <returns>a score factor for the term</returns>
		virtual public float Idf(Term term, Searcher searcher)  
		{
			return Idf(searcher.DocFreq(term), searcher.MaxDoc());
		}

		/// <summary>
		/// Computes a score factor for a phrase.
		/// <p>The default implementation sums the Idf(Term,Searcher) factor
		/// for each term in the phrase.
		/// </p>
		/// </summary>
		/// <param name="terms">the vector of terms in the phrase</param>
		/// <param name="searcher">the document collection being searched</param>
		/// <returns>a score factor for the phrase</returns>
		virtual public float Idf(ArrayList terms, Searcher searcher)  
		{
			float _idf = 0.0f;
			for (int i = 0; i < terms.Count; i++) 
			{
				_idf += Idf((Term)terms[i], searcher);
			}
			return _idf;
		}

		/// <summary>
		/// Computes a score factor based on a term's document frequency (the number
		/// of documents which contain the term).  This value is multiplied by the
		/// Tf(int) factor for each term in the query and these products are
		/// then summed to form the initial score for a document.
		///
		/// <p>Terms that occur in fewer documents are better indicators of topic, so
		/// implemenations of this method usually return larger values for rare terms,
		/// and smaller values for common terms.
		/// </p>
		/// </summary>
		/// <param name="docFreq">the number of documents which contain the term</param>
		/// <param name="numDocs">the total number of documents in the collection</param>
		/// <returns>a score factor based on the term's document frequency</returns>
		public abstract float Idf(int docFreq, int numDocs);
    
		/// <summary>
		/// Computes a score factor based on the fraction of all query terms that a
		/// document contains.  This value is multiplied into scores.
		///
		/// <p>The presence of a large portion of the query terms indicates a better
		/// match with the query, so implemenations of this method usually return
		/// larger values when the ratio between these parameters is large and smaller
		/// values when the ratio between them is small.
		/// </p>
		/// </summary>
		/// <param name="overlap">the number of query terms matched in the document</param>
		/// <param name="maxOverlap">the total number of terms in the query</param>
		/// <returns>a score factor based on term overlap with the query</returns>
		public abstract float Coord(int overlap, int maxOverlap);
	}
}