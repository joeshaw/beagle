using System;
using System.IO;

using Lucene.Net.Index;       
using Lucene.Net.Search;       

namespace Lucene.Net.Documents
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
	 *    "Apache Lucene", nor may "Apache" appear in their _name, without
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
	/// A field is a section of a Document.  Each field has two parts, a name and a
	/// value.  Values may be free text, provided as a String or as a TextReader, or they
	/// may be atomic keywords, which are not further processed.  Such keywords may
	/// be used to represent dates, urls, etc.  Fields are optionally stored in the
	/// index, so that they may be returned with hits on the document.
	/// </summary>
	[Serializable]
	public sealed class Field 
	{
		private String name = "body";
		private String stringValue = null;
		private TextReader readerValue = null;
		private bool isStored = false;
		private bool isIndexed = true;
		private bool isTokenized = true;

		private float boost = 1.0f;

		/// <summary>
		/// Sets the boost factor hits on this field.  This value will be
		/// multiplied into the score of all hits on this this field of this
		/// document.
		///
		/// <p>The boost is multiplied by Document.GetBoost() of the document
		/// containing this field.  If a document has multiple fields with the same
		/// name, all such values are multiplied together.  This product is then
		/// multipled by the value Similarity.LengthNorm(String,int), and
		/// rounded by Similarity.EncodeNorm(float) before it is stored in the
		/// index. One should attempt to ensure that this product does not overflow
		/// the range of that encoding.
		/// </p>
		///
		/// <seealso cref="Document.SetBoost(float)"/>
		/// <seealso cref="Similarity.LengthNorm(String, int)"/>
		/// <seealso cref="Similarity.EncodeNorm(float)"/>
		/// </summary>
		/// <param name="boost"></param>
		public void SetBoost(float boost) 
		{
			this.boost = boost;
		}

		/// <summary>
		/// Returns the boost factor for hits on any field of this document.
		///
		/// <p>The default value is 1.0.</p>
		///
		/// <p>Note: this value is not stored directly with the document in the index.
		/// Documents returned from IndexReader.Document(int) and 
		/// Hits.Doc(int) may thus not have the same value present as when this field
		/// was indexed.
		/// </p>
		///
		/// <seealso cref="SetBoost(float)"/>
		/// </summary>
		/// <returns></returns>
		public float GetBoost() 
		{
			return boost;
		}

		/// <summary>
		/// Constructs a String-valued Field that is not tokenized, but is indexed
		/// and stored.  Useful for non-text fields, e.g. date or url.
		/// </summary>
		/// <param name="_name"></param>
		/// <param name="_value"></param>
		/// <returns></returns>
		public static Field Keyword(String _name, String _value) 
		{
			return new Field(_name, _value, true, true, false);
		}

		/// <summary>
		/// Constructs a String-valued Field that is not tokenized nor indexed,
		/// but is stored in the index, for return with hits.
		/// </summary>
		/// <param name="_name"></param>
		/// <param name="_value"></param>
		/// <returns></returns>
		public static Field UnIndexed(String _name, String _value) 
		{
			return new Field(_name, _value, true, false, false);
		}

		/// <summary>
		/// Constructs a String-valued Field that is tokenized and indexed,
		/// and is stored in the index, for return with hits.  Useful for short text
		/// fields, like "title" or "subject".
		/// </summary>
		/// <param name="_name"></param>
		/// <param name="_value"></param>
		/// <returns></returns>
		public static Field Text(String _name, String _value) 
		{
			return new Field(_name, _value, true, true, true);
		}

		/// <summary>
		/// Constructs a Date-valued Field that is not tokenized and is indexed,
		/// and stored in the index, for return with hits.
		/// </summary>
		/// <param name="_name"></param>
		/// <param name="_value"></param>
		/// <returns></returns>
		public static Field Keyword(String _name, DateTime _value) 
		{
			return new Field(_name, DateField.DateToString(_value), true, true, false);
		}

		/// <summary>
		/// Constructs a String-valued Field that is tokenized and indexed,
		/// but that is not stored in the index.
		/// </summary>
		/// <param name="_name"></param>
		/// <param name="_value"></param>
		/// <returns></returns>
		public static Field UnStored(String _name, String _value) 
		{
			return new Field(_name, _value, false, true, true);
		}

		/// <summary>
		/// Constructs a TextReader-valued Field that is tokenized and indexed, but is
		/// not stored in the index verbatim.  Useful for longer text fields, like
		/// "body".
		/// </summary>
		/// <param name="_name"></param>
		/// <param name="_value"></param>
		/// <returns></returns>
		public static Field Text(String _name, TextReader _value) 
		{
			return new Field(_name, _value);
		}

		/// <summary>
		/// The name of the field (e.g., "date", "subject", "title", or "body")
		/// as an interned string.
		/// </summary>
		/// <returns></returns>
		public String Name() 		{ return name; }

		/// <summary>
		/// The value of the field as a String, or null.  If null, the TextReader value
		/// is used. Exactly one of stringValue and readerValue must be set.
		/// </summary>
		/// <returns></returns>
		public String StringValue()		{ return stringValue; }

		/// <summary>
		/// The value of the field as a TextReader, or null.  If null, the String value
		/// is used. Exactly one of stringValue and readerValue must be set.
		/// </summary>
		/// <returns></returns>
		public TextReader ReaderValue()	{ return readerValue; }

		public Field(String _name, String _string, bool store, bool index, bool token) 
		{
			if (_name == null)
				throw new ArgumentException("_name cannot be null");
			if (_string == null)
				throw new ArgumentException("_value cannot be null");

			this.name = String.Intern(_name);			  // field names are interned
			this.stringValue = _string;
			this.isStored = store;
			this.isIndexed = index;
			this.isTokenized = token;
		}

		Field(String _name, TextReader reader) 
		{
			if (_name == null)
				throw new ArgumentException("_name cannot be null");
			if (reader == null)
				throw new ArgumentException("_value cannot be null");

			this.name = String.Intern(_name);		  // field names are interned
			this.readerValue = reader;
		}

		/// <summary>
		/// True iff the value of the field is to be stored in the index for return
		/// with search hits.  It is an error for this to be true if a field is
		/// TextReader-valued.
		/// </summary>
		/// <returns></returns>
		public bool	IsStored() 	{ return isStored; }

		/// <summary>
		/// True iff the value of the field is to be indexed, so that it may be
		/// searched on.
		/// </summary>
		/// <returns></returns>
		public bool 	IsIndexed() 	{ return isIndexed; }

		/// <summary>
		/// True iff the value of the field should be tokenized as text prior to
		/// indexing. Un-tokenized fields are indexed as a single word and may not be
		///	TextReader-valued.
		/// </summary>
		/// <returns></returns>
		public bool 	IsTokenized() 	{ return isTokenized; }

		/// <summary>
		/// Prints a Field for human consumption.
		/// </summary>
		/// <returns></returns>
		public override String ToString() 
		{
			if (isStored && isIndexed && !isTokenized)
				return "Keyword<" + name + ":" + stringValue + ">";
			else if (isStored && !isIndexed && !isTokenized)
				return "Unindexed<" + name + ":" + stringValue + ">";
			else if (isStored && isIndexed && isTokenized && stringValue!=null)
				return "Text<" + name + ":" + stringValue + ">";
			else if (!isStored && isIndexed && isTokenized && readerValue!=null)
				return "Text<" + name + ":" + readerValue + ">";
			else
				return base.ToString();
		}
	}
}