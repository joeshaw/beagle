using System;
using System.Collections;
using System.Runtime.CompilerServices;

using Lucene.Net.Documents; 
using Lucene.Net.Store; 
using Lucene.Net.Util; 

namespace Lucene.Net.Index
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001 The Apache Software Foundation.  All rights
	 * reserved.
	 *
	 * Redistribution and use _in source and binary forms, with or without
	 * modification, are permitted provided that the following conditions
	 * are met:
	 *
	 * 1. Redistributions of source code must retain the above copyright
	 *    notice, this list of conditions and the following disclaimer.
	 *
	 * 2. Redistributions _in binary form must reproduce the above copyright
	 *    notice, this list of conditions and the following disclaimer _in
	 *    the documentation and/or other materials provided with the
	 *    distribution.
	 *
	 * 3. The end-user documentation included with the redistribution,
	 *    if any, must include the following acknowledgment:
	 *       "This product includes software developed by the
	 *        Apache Software Foundation (http://www.apache.org/)."
	 *    Alternately, this acknowledgment may appear _in the software itself,
	 *    if and wherever such third-party acknowledgments normally appear.
	 *
	 * 4. The names "Apache" and "Apache Software Foundation" and
	 *    "Apache Lucene" must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    "Apache Lucene", nor may "Apache" appear _in their name, without
	 *    prior written permission of the Apache Software Foundation.
	 *
	 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
	 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
	 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	 * DISCLAIMED.  _in NO EVENT SHALL THE APACHE SOFTWARE FOUNDATION OR
	 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
	 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
	 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
	 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	 * ON ANY THEORY OF LIABILITY, WHETHER _in CONTRACT, STRICT LIABILITY,
	 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING _in ANY WAY OUT
	 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
	 * SUCH DAMAGE.
	 * ====================================================================
	 *
	 * This software consists of voluntary contributions made by many
	 * individuals on behalf of the Apache Software Foundation.  For more
	 * information on the Apache Software Foundation, please see
	 * <http://www.apache.org/>.
	 */

	public sealed class SegmentReader : IndexReader 
	{
		private bool closeDirectory = false;
		private String segment;

		internal FieldInfos fieldInfos;
		private FieldsReader fieldsReader;

		internal TermInfosReader tis;

		internal BitVector deletedDocs = null;
		private bool deletedDocsDirty = false;

		internal InputStream freqStream;
		internal InputStream proxStream;

		// Compound File Reader when based on a compound file segment
		CompoundFileReader cfsReader;
		
		private class Norm 
		{
			public Norm(InputStream _in) { this._in = _in; }
			public InputStream _in;
			public byte[] bytes;
		}
		private Hashtable norms = new Hashtable();

		public SegmentReader(SegmentInfo si, bool closeDir) : this(si)
		{
			closeDirectory = closeDir;
		}

		public SegmentReader(SegmentInfo si) : base(si.dir)
		{
			segment = si.name;

			// Use compound file directory for some files, if it exists
			Directory cfsDir = Directory();
			if (Directory().FileExists(segment + ".cfs")) 
			{
				cfsReader = new CompoundFileReader(Directory(), segment + ".cfs");
				cfsDir = cfsReader;
			}

			 // No compound file exists - use the multi-file format
			fieldInfos = new FieldInfos(cfsDir, segment + ".fnm");
			fieldsReader = new FieldsReader(cfsDir, segment, fieldInfos);

			tis = new TermInfosReader(cfsDir, segment, fieldInfos);

			// NOTE: the bitvector is stored using the regular directory, not cfs
			if (HasDeletions(si))
				deletedDocs = new BitVector(Directory(), segment + ".del");

			// make sure that all index files have been read or are kept open
			// so that if an index update removes them we'll still have them
			freqStream = cfsDir.OpenFile(segment + ".frq");
			proxStream = cfsDir.OpenFile(segment + ".prx");
			OpenNorms(cfsDir);
		}

		internal class SegmentReaderLockWith : Lock.With
		{
			SegmentReader segmentReader;

			internal SegmentReaderLockWith(Lock _lock, long lockTimeout, SegmentReader segmentReader) 
				: base(_lock, lockTimeout) 
			{
				this.segmentReader = segmentReader;
			}

			override public Object DoBody()  
			{
				segmentReader.deletedDocs.Write(segmentReader.Directory(), segmentReader.segment + ".tmp");
				segmentReader.Directory().RenameFile(segmentReader.segment + ".tmp", segmentReader.segment + ".del");
				segmentReader.Directory().TouchFile("segments");
				return null;
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		protected internal override void DoClose()  
		{
			if (deletedDocsDirty) 
			{
				lock (Directory()) 
				{		  // _in- & inter-process sync
					new SegmentReaderLockWith(Directory().MakeLock(IndexWriter.COMMIT_LOCK_NAME), IndexWriter.COMMIT_LOCK_TIMEOUT, this).Run();
				}
				deletedDocsDirty = false;
			}

			fieldsReader.Close();
			tis.Close();

			if (freqStream != null)
				freqStream.Close();
			if (proxStream != null)
				proxStream.Close();

			CloseNorms();
			
			if (cfsReader != null)
				cfsReader.Close();

			if (closeDirectory)
				Directory().Close();
		}

		internal static bool HasDeletions(SegmentInfo si)  
		{
			return si.dir.FileExists(si.name + ".del");
		}

		public override bool HasDeletions() 
		{
			return deletedDocs != null;
		}

		internal static bool UsesCompoundFile(SegmentInfo si)
		{
			return si.dir.FileExists(si.name + ".cfs");
		}
	
		[MethodImpl(MethodImplOptions.Synchronized)]
		protected internal override void DoDelete(int docNum)  
		{
			if (deletedDocs == null)
				deletedDocs = new BitVector(MaxDoc());
			deletedDocsDirty = true;
			deletedDocs.Set(docNum);
		}

		internal class SegmentReaderLockWith2 : Lock.With
		{
			SegmentReader segmentReader;
			string segment;

			internal SegmentReaderLockWith2(Lock _lock, long lockTimeout, SegmentReader segmentReader, string segment) 
				: base(_lock, lockTimeout) 
			{
				this.segmentReader = segmentReader;
				this.segment = segment;
			}

			override public Object DoBody()  
			{
				if (segmentReader.Directory().FileExists(segment + ".del")) 
				{
					segmentReader.Directory().DeleteFile(segment + ".del");
				}
				return null;
			}
		}
		
		public override void UndeleteAll()
		{
			lock(Directory()) 
			{		  // in- & inter-process sync
				new SegmentReaderLockWith2(Directory().MakeLock(IndexWriter.COMMIT_LOCK_NAME),
										   IndexWriter.COMMIT_LOCK_TIMEOUT,
										   this,
										   segment);
				deletedDocs = null;
				deletedDocsDirty = false;
			}
		}

		internal ArrayList Files()  
		{
			ArrayList files = new ArrayList(16);
			String[] ext = new String[] 
			{"cfs", "fnm", "fdx", "fdt", "tii", "tis", "frq", "prx", "del"};

			for (int i=0; i<ext.Length; i++) 
			{
				String name = segment + "." + ext[i];
				if (Directory().FileExists(name))
					files.Add(name);
			}
			
			for (int i = 0; i < fieldInfos.Size(); i++) 
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed)
					files.Add(segment + ".f" + i);
			}
			return files;
		}

		public override TermEnum Terms()  
		{
			return tis.Terms();
		}

		public override TermEnum Terms(Term t)  
		{
			return tis.Terms(t);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override Document Document(int n)  
		{
			if (IsDeleted(n))
				throw new ArgumentException
					("attempt to access a deleted document");
			return fieldsReader.Doc(n);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override bool IsDeleted(int n) 
		{
			return (deletedDocs != null && deletedDocs.Get(n));
		}

		public override TermDocs TermDocs()  
		{
			return new SegmentTermDocs(this);
		}

		public override TermPositions TermPositions()  
		{
			return new SegmentTermPositions(this);
		}

		public override int DocFreq(Term t)  
		{
			TermInfo ti = tis.Get(t);
			if (ti != null)
				return ti.docFreq;
			else
				return 0;
		}

		public override int NumDocs() 
		{
			int n = MaxDoc();
			if (deletedDocs != null)
				n -= deletedDocs.Count();
			return n;
		}

		public override int MaxDoc() 
		{
			return fieldsReader.Size();
		}

		public override ICollection GetFieldNames()
		{
			// maintain a unique set of field names
			Hashtable fieldSet = new Hashtable();
			for (int i = 0; i < fieldInfos.Size(); i++) 
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				fieldSet.Add(fi.name, fi.name);
			}
			return fieldSet.Keys;
		}

		public override ICollection GetFieldNames(bool indexed)
		{
			// maintain a unique set of field names
			Hashtable fieldSet = new Hashtable();
			for (int i = 0; i < fieldInfos.Size(); i++) 
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed == indexed)
					fieldSet.Add(fi.name, fi.name);
			}
			return fieldSet.Keys;
		}

		public override byte[] Norms(String field)  
		{
			Norm norm = (Norm)norms[field];
			if (norm == null)
				return null;
			if (norm.bytes == null) 
			{
				byte[] bytes = new byte[MaxDoc()];
				Norms(field, bytes, 0);
				norm.bytes = bytes;
			}
			return norm.bytes;
		}

		internal void Norms(String field, byte[] bytes, int offset)  
		{
			InputStream normStream = NormStream(field);
			if (normStream == null)
				return;					  // use zeros _in array

			try 
			{
				normStream.ReadBytes(bytes, offset, MaxDoc());
			} 
			finally 
			{
				normStream.Close();
			}
		}

		internal InputStream NormStream(String field)  
		{
			Norm norm = (Norm)norms[field];
			if (norm == null)
				return null;
			InputStream result = (InputStream)norm._in.Clone();
			result.Seek(0);
			return result;
		}

		private void OpenNorms(Directory useDir)  
		{
			for (int i = 0; i < fieldInfos.Size(); i++) 
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed) 
					norms.Add(
						fi.name, new Norm(useDir.OpenFile(segment + ".f" + fi.number))
					);
			}
		}

		private void CloseNorms()  
		{
			lock (norms) 
			{
				foreach (Norm norm in norms.Values)
				{
					norm._in.Close();
				}
			}
		}

//		public override ICollection GetFieldNames()  
//		{
//			// maintain a unique set of field names
//			Hashtable fieldSet = new Hashtable();
//			for (int i = 0; i < fieldInfos.Size(); i++) 
//			{
//				FieldInfo fi = fieldInfos.FieldInfo(i);
//
//				if (fieldSet[fi.name] == null)
//				{
//					fieldSet.Add(fi.name, null);
//				}
//			}
//			return fieldSet;
//		}
	}
}