using System;
using System.Text;
using System.Collections;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2003 The Apache Software Foundation.  All rights
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
	/// Expert: Describes the score computation for document and query.
	/// </summary>
	[Serializable]
	public class Explanation 
	{
		private float value;                            // the value of this node
		private String description;                     // what it represents
		private ArrayList details;                      // sub-explanations

		public Explanation() {}

		public Explanation(float value, String description) 
		{
			this.value = value;
			this.description = description;
		}

		/// <summary>
		/// The value assigned to this explanation node.
		/// </summary>
		/// <returns></returns>
		public float GetValue() { return value; }

		/// <summary>
		/// Sets the value assigned to this explanation node.
		/// </summary>
		/// <param name="value"></param>
		public void SetValue(float value) { this.value = value; }

		/// <summary>
		/// A description of this explanation node.
		/// </summary>
		/// <returns></returns>
		public String GetDescription() { return description; }

		/// <summary>
		/// Sets the description of this explanation node.
		/// </summary>
		/// <param name="description"></param>
		public void SetDescription(String description) 
		{
			this.description = description;
		}

		/// <summary>
		/// The sub-nodes of this explanation node.
		/// </summary>
		/// <returns></returns>
		public Explanation[] GetDetails() 
		{
			if (details == null)
				return null;
			return (Explanation[])details.ToArray(typeof(Explanation));
		}

		/// <summary>
		/// The sub-nodes of this explanation node. 
		/// </summary>
		/// <param name="detail"></param>
		public void AddDetail(Explanation detail) 
		{
			if (details == null)
				details = new ArrayList();
			details.Add(detail);
		}

		/// <summary>
		/// Render an explanation as HTML.
		/// </summary>
		/// <returns></returns>
		public override String ToString() 
		{
			return ToString(0);
		}

		private String ToString(int depth) 
		{
			StringBuilder buffer = new StringBuilder();
			for (int i = 0; i < depth; i++) 
			{
				buffer.Append("  ");
			}
			buffer.Append(Number.ToString(GetValue()));
			buffer.Append(" = ");
			buffer.Append(GetDescription());
			buffer.Append("\n");

			Explanation[] details = GetDetails();
			if (details != null) 
			{
				for (int i = 0 ; i < details.Length; i++) 
				{
					buffer.Append(details[i].ToString(depth+1));
				}
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Render an explanation as HTML.
		/// </summary>
		/// <returns></returns>
		public String ToHtml() 
		{
			StringBuilder buffer = new StringBuilder();
			buffer.Append("<ul>\n");

			buffer.Append("<li>");
			buffer.Append(Number.ToString(GetValue()));
			buffer.Append(" = ");
			buffer.Append(GetDescription());
			buffer.Append("</li>\n");

			Explanation[] details = GetDetails();
			if (details != null) 
			{
				for (int i = 0 ; i < details.Length; i++) 
				{
					buffer.Append(details[i].ToHtml());
				}
			}

			buffer.Append("</ul>\n");

			return buffer.ToString();
		}
	}
}