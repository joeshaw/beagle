using System;
using System.Text;
using System.Collections;

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
	/// Documents are the unit of indexing and search.
	///
	/// A Document is a set of fields.  Each field has a name and a textual value.
	/// A field may be stored with the document, in which case it is returned with
	/// search hits on the document.  Thus each document should typically contain
	/// stored fields which uniquely identify it.
	/// </summary>
	[Serializable]
	public sealed class Document 
	{
		internal DocumentFieldList fieldList = null;
		private float boost = 1.0f;

		/// <summary>
		/// Constructs a new document with no fields.
		/// </summary>
		public Document() {}

		/// <summary>
		/// Sets a boost factor for hits on any field of this document.  This value
		/// will be multiplied into the score of all hits on this document.
		///
		/// <p>
		/// Values are multiplied into the value of Field.GetBoost() of
		/// each field in this document.  Thus, this method in effect sets a default
		/// boost for the fields of this document.
		/// </p>
		///
		/// <see cref="Field.SetBoost(float)"/>
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
		/// <p>Note: This value is not stored directly with the document in the index.
		/// Documents returned from IndexReader.Document(int) and 
		/// Hits.Doc(int) may thus not have the same value present as when this
		/// document was indexed.
		/// </p>
		///
		/// <see cref="SetBoost(float)"/>
		/// </summary>
		/// <returns></returns>
		public float GetBoost() 
		{
			return boost;
		}

		/// <summary>
		/// Adds a field to a document.  Several fields may be added with
		/// the same name.  In this case, if the fields are indexed, their text is
		/// treated as though appended for the purposes of search. 
		/// </summary>
		/// <param name="field"></param>
		public void Add(Field field) 
		{
			fieldList = new DocumentFieldList(field, fieldList);
		}

		/// <summary>
		/// Returns a field with the given name if any exist in this document, or
		/// null. If multiple fields exists with this name, this method returns the
		/// last field value added. 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Field GetField(String name) 
		{
			for (DocumentFieldList list = fieldList; list != null; list = list.next)
				if (list.field.Name().Equals(name))
					return list.field;
			return null;
		}

		/// <summary>
		/// Returns the string value of the field with the given name if any exist in
		/// this document, or null. If multiple fields exist with this name, this
		/// method returns the last value added. 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public String Get(String name) 
		{
			Field field = GetField(name);
			if (field != null)
				return field.StringValue();
			else
				return null;
		}

		/// <summary>
		/// Returns an Enumeration of all the fields in a document.
		/// </summary>
		/// <returns></returns>
		public IEnumerable Fields() 
		{
			return new DocumentFieldEnumeration(this);
		}

		/// <summary>
		/// Returns an array of Field's with the given name.
		/// This method can return <code>null</code>.
		/// </summary>
		/// <param name="name">the name of the field</param>
		/// <returns>a <code>Field[]</code> array</returns>
		public Field[] GetFields(String name) 
		{
			ArrayList tempFieldList = new ArrayList();
			for (DocumentFieldList list = fieldList; list != null; list = list.next) 
			{
				if (list.field.Name().Equals(name)) 
				{
					tempFieldList.Add(list.field);
				}
			}
			int fieldCount = tempFieldList.Count;
			if (fieldCount == 0) 
			{
				return null;
			}
			else 
			{
				return (Field[])tempFieldList.ToArray(typeof(Field));
			}
		}

		/// <summary>
		/// Returns an array of values of the field specified as the method parameter.
		/// This method can return <code>null</code>.
		/// UnStored fields' values cannot be returned by this method.
		/// </summary>
		/// <param name="name">the name of the field</param>
		/// <returns>a <code>String[]</code> of field values</returns>
		public String[] GetValues(String name) 
		{
			Field[] namedFields = GetFields(name);
			if (namedFields == null)
				return null;
			String[] values = new String[namedFields.Length];
			for (int i = 0; i < namedFields.Length; i++) 
			{
				values[i] = namedFields[i].StringValue();
			}
			return values;
		}

		/// <summary>
		/// Prints the fields of a document for human consumption.
		/// </summary>
		/// <returns></returns>
		override public String ToString() 
		{
			StringBuilder buffer = new StringBuilder();
			buffer.Append("Document<");
			for (DocumentFieldList list = fieldList; list != null; list = list.next) 
			{
				buffer.Append(list.field.ToString());
				if (list.next != null)
					buffer.Append(" ");
			}
			buffer.Append(">");
			return buffer.ToString();
		}
	}


	[Serializable]
	sealed class DocumentFieldList 
	{
		internal DocumentFieldList(Field f, DocumentFieldList n) 
		{
			field = f;
			next = n;
		}
		internal Field field;
		internal DocumentFieldList next;
	}

	sealed class DocumentFieldEnumeration : IEnumerable
	{
		internal Document doc;
		internal DocumentFieldEnumeration(Document doc) 
		{
			this.doc = doc;
		}

		/// <summary>
		/// IEnumerable Interface Implementation:
		///   Declaration of the GetEnumerator() method 
		///   required by IEnumerable
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return new DocumentFieldEnumerator(doc);
		}

		class DocumentFieldEnumerator : IEnumerator
		{
			bool before;
			DocumentFieldList fields;
			Document doc;

			internal DocumentFieldEnumerator(Document doc) 
			{
				this.doc = doc;
				Reset();
			}

			/// <summary>
			/// Declare the Reset method required by IEnumerator
			/// </summary>
			public void Reset()
			{
				before = true;
				fields = doc.fieldList;
			}

			/// <summary>
			/// Declare the Current property required by IEnumerator
			/// </summary>
			public object Current
			{
				get
				{
					return fields.field;
				}
			}

			/// <summary>
			/// Declare the MoveNext method required by IEnumerator
			/// </summary>
			/// <returns></returns>
			public bool MoveNext() 
			{
				if (before)
				{
					before = false;
				}
				else if (fields != null)
				{
					fields = fields.next;
				}
				return fields == null ? false : true;
			}
		}
	}
}