using System;
using System.Collections;

using  Lucene.Net.Documents;
using  Lucene.Net.Store;

namespace Lucene.Net.Index
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

	public sealed class FieldInfos 
	{
		private ArrayList byNumber = new ArrayList();
		private Hashtable byName = new Hashtable();

		public FieldInfos() 
		{
			Add("", false);
		}

		public FieldInfos(Directory d, String name) 
		{
			InputStream input = d.OpenFile(name);
			try 
			{
				Read(input);
			} 
			finally 
			{
				input.Close();
			}
		}

		/// <summary>
		/// Adds field info for a Document.
		/// </summary>
		/// <param name="doc"></param>
		public void Add(Document doc) 
		{
			foreach (Field field in doc.Fields()) 
			{
				Add(field.Name(), field.IsIndexed());
			}
		}

		public void Add(ICollection names, bool isIndexed) 
		{
			foreach(string name in names)
			{
				Add(name, isIndexed);
			}
		}

		public void Add(String name, bool isIndexed) 
		{
			FieldInfo fi = FieldInfo(name);
			if (fi == null)
				AddInternal(name, isIndexed);
			else if (fi.isIndexed != isIndexed)
				fi.isIndexed = true;
		}

		private void AddInternal(String name, bool isIndexed) 
		{
			FieldInfo fi = new FieldInfo(name, isIndexed, byNumber.Count);
			byNumber.Add(fi);
			byName.Add(name, fi);
		}

		internal int FieldNumber(String fieldName) 
		{
			FieldInfo fi = FieldInfo(fieldName);
			if (fi != null)
				return fi.number;
			else
				return -1;
		}

		internal FieldInfo FieldInfo(String fieldName) 
		{
			return (FieldInfo)byName[fieldName];
		}

		internal String FieldName(int fieldNumber) 
		{
			return FieldInfo(fieldNumber).name;
		}

		internal FieldInfo FieldInfo(int fieldNumber) 
		{
			return (FieldInfo)byNumber[fieldNumber];
		}

		internal int Size() 
		{
			return byNumber.Count;
		}

		internal void Write(Directory d, String name) 
		{
			OutputStream output = d.CreateFile(name);
			try 
			{
				Write(output);
			} 
			finally 
			{
				output.Close();
			}
		}

		internal void Write(OutputStream output) 
		{
			output.WriteVInt(Size());
			for (int i = 0; i < Size(); i++) 
			{
				FieldInfo fi = FieldInfo(i);
				output.WriteString(fi.name);
				output.WriteByte((byte)(fi.isIndexed ? 1 : 0));
			}
		}

		private void Read(InputStream input) 
		{
			int size = input.ReadVInt();
			for (int i = 0; i < size; i++)
				AddInternal(String.Intern(input.ReadString()),
					input.ReadByte() != 0);
		}
	}
}