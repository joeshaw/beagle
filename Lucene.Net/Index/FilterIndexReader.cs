using System;
using System.Collections;

using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2003 The Apache Software Foundation. All rights reserved.
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
	/// A <code>FilterIndexReader</code> contains another IndexReader, which it
	/// uses as its basic source of data, possibly transforming the data along the
	/// way or providing additional functionality. The class
	/// <code>FilterIndexReader</code> itself simply implements all abstract methods
	/// of <code>IndexReader</code> with versions that pass all requests to the
	/// contained index reader. Subclasses of <code>FilterIndexReader</code> may
	/// further override some of these methods and may also provide additional
	/// methods and fields.
	/// </summary>
	public class FilterIndexReader : IndexReader 
	{
		/// <summary>
		/// Base class for filtering {@link TermDocs} implementations.
		/// </summary>
		public class FilterTermDocs : TermDocs 
		{
			protected TermDocs _in;

			public FilterTermDocs(TermDocs _in) { this._in = _in; }

			public virtual void Seek(Term term){ _in.Seek(term); }
			public virtual void Seek(TermEnum _enum){ _in.Seek(_enum); }
			public virtual int Doc() { return _in.Doc(); }
			public virtual int Freq() { return _in.Freq(); }
			public virtual bool Next(){ return _in.Next(); }
			public virtual int Read(int[] docs, int[] freqs)
			{
				return _in.Read(docs, freqs);
			}
			public virtual bool SkipTo(int i){ return _in.SkipTo(i); }
			public virtual void Close(){ _in.Close(); } 
		}

		/// <summary>
		/// Base class for filtering {@link TermPositions} implementations.
		/// </summary>
		public class FilterTermPositions : FilterTermDocs, TermPositions
		{

			public FilterTermPositions(TermPositions _in) : base(_in) {}

			public virtual int NextPosition()
			{
				return ((TermPositions)_in).NextPosition();
			}
		}

		/// <summary>
		/// Base class for filtering {@link TermEnum} implementations.
		/// </summary>
		public class FilterTermEnum : TermEnum 
		{
			protected TermEnum _in;

			public FilterTermEnum(TermEnum _in) { this._in = _in; }

			public override bool Next(){ return _in.Next(); }
			public override Term Term() { return _in.Term(); }
			public override int DocFreq() { return _in.DocFreq(); }
			public override void Close(){ _in.Close(); }
		}

		protected IndexReader _in;

		public FilterIndexReader(IndexReader _in) : base(_in.Directory())
		{
			this._in = _in;
		}

		public override int NumDocs() { return _in.NumDocs(); }
		public override int MaxDoc() { return _in.MaxDoc(); }

		public override Document Document(int n){return _in.Document(n);}

		public override bool IsDeleted(int n) { return _in.IsDeleted(n); }
		public override bool HasDeletions() { return _in.HasDeletions(); }
		public override void UndeleteAll(){ _in.UndeleteAll(); }

		public override byte[] Norms(String f){ return _in.Norms(f); }

		public override TermEnum Terms(){ return _in.Terms(); }
		public override TermEnum Terms(Term t){ return _in.Terms(t); }

		public override int DocFreq(Term t){ return _in.DocFreq(t); }

		public override TermDocs TermDocs(){ return _in.TermDocs(); }
		public override TermPositions TermPositions()
		{
			return _in.TermPositions();
		}

		protected internal override void DoDelete(int n){ _in.DoDelete(n); }
		protected internal override void DoClose(){ _in.DoClose(); }

		public override ICollection GetFieldNames()
		{
			return _in.GetFieldNames();
		}
		public override ICollection GetFieldNames(bool indexed)
		{
			return _in.GetFieldNames(indexed);
		}
	}
}
