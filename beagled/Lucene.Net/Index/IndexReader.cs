using System;
//using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;
using Lucene.Net.Store;
using Lucene.Net.Documents;

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

	/// <summary>
	/// IndexReader is an abstract class, providing an interface for accessing an
	/// index.  Search of an index is done entirely through this abstract interface,
	/// so that any subclass which implements it is searchable.
	/// <p> Concrete subclasses of IndexReader are usually constructed with a call to
	/// the static method Open.
	/// </p>
	/// <p> For efficiency, in this API documents are often referred to via
	/// <i>document numbers</i>, non-negative integers which each name a unique
	/// document in the index.  These document numbers are ephemeral--they may change
	/// as documents are added to and deleted from an index.  Clients should thus not
	/// rely on a given document having the same number between sessions. 
	/// </p>
	/// </summary>
	public abstract class IndexReader 
	{
		protected IndexReader(Lucene.Net.Store.Directory directory) 
		{
			this.directory = directory;
			segmentInfosAge = Int64.MaxValue;
		}

		/// <summary>
		/// Release the write lock, if needed.
		/// </summary>
		~IndexReader()  
		{
			if (writeLock != null) 
			{
				writeLock.Release();                        // release write lock
				writeLock = null;
			}
		}

		private Lucene.Net.Store.Directory directory;
		private Lock writeLock;
		
		//used to determine whether index has chaged since reader was opened
		private Int64 segmentInfosAge;

		/// <summary>
		/// Returns an IndexReader reading the index in an FSDirectory in the named path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static IndexReader Open(String path)  
		{
			return Open(FSDirectory.GetDirectory(path, false));
		}

		/// <summary>
		/// Returns an IndexReader reading the index in an FSDirectory in the named path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static IndexReader Open(System.IO.FileInfo path)  
		{
			return Open(FSDirectory.GetDirectory(path.FullName, false));
		}

		internal class IndexReaderLockWith : Lock.With
		{
			Lucene.Net.Store.Directory directory;
			internal IndexReaderLockWith(Lock _lock, long lockTimeOut, Lucene.Net.Store.Directory directory) : base(_lock, lockTimeOut)
			{
				this.directory = directory;
			}

			override public Object DoBody()  
			{
				IndexReader result = null;
				SegmentInfos infos = new SegmentInfos();
				
				infos.Read(directory);
				if (infos.Count == 1)		  // index is optimized
					result = new SegmentReader(infos.Info(0), true);
				else
				{
					SegmentReader[] readers = new SegmentReader[infos.Count];
					for (int i = 0; i < infos.Count; i++)
						readers[i] = new SegmentReader(infos.Info(i), i==infos.Count-1);
					result = new SegmentsReader(directory, readers);
				}
				
				result.segmentInfosAge = LastModified(directory);
				return result;
			}
		}

		/// <summary>
		/// Returns the directory this index resides in.
		/// </summary>
		/// <returns></returns>
		public Directory Directory() { return directory; }

		/// <summary>
		/// Returns an IndexReader reading the index in the given Lucene.Net.Store.Directory.
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		public static IndexReader Open(Lucene.Net.Store.Directory directory) 
		{
			lock (directory) 
			{			  
				// in- & inter-process sync
				return (IndexReader)new IndexReaderLockWith(
					directory.MakeLock(IndexWriter.COMMIT_LOCK_NAME), IndexWriter.COMMIT_LOCK_TIMEOUT, directory
				).Run();
			}
		}

		/// <summary>
		/// Returns the time the index in the named directory was last modified.
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		public static long LastModified(String directory)  
		{
			return LastModified(new System.IO.DirectoryInfo(directory));
		}

		/// <summary>
		/// Returns the time the index in the named directory was last modified.
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		public static long LastModified(System.IO.DirectoryInfo directory)
		{
			return FSDirectory.FileModified(directory, "segments");
		}

		/// <summary>
		/// Returns the time the index in this directory was last modified.
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		public static long LastModified(Lucene.Net.Store.Directory directory)  
		{
			return directory.FileModified("segments");
		}

		/// <summary>
		/// Returns <code>true</code> if an index exists at the specified directory.
		/// If the directory does not exist or if there is no index in it.
		/// <code>false</code> is returned.
		/// </summary>
		/// <param name="directory">the directory to check for an index</param>
		/// <returns>
		///		<code>true</code> if an index exists; <code>false</code> otherwise 
		/// </returns>
		public static bool IndexExists(String directory) 
		{
			return (new System.IO.FileInfo(directory + "/" + "segments")).Exists;
		}

		/// <summary>
		/// Returns <code>true</code> if an index exists at the specified directory.
		/// If the directory does not exist or if there is no index in it.
		/// </summary>
		/// <param name="directory">the directory to check for an index</param>
		/// <returns>
		///		<code>true</code> if an index exists; <code>false</code> otherwise
		///	</returns>
		public static bool IndexExists(System.IO.FileInfo directory) 
		{
			return (new System.IO.FileInfo(directory.FullName + "/" + "segments")).Exists;
		}

		/// <summary>
		/// Returns <code>true</code> if an index exists at the specified directory.
		/// If the directory does not exist or if there is no index in it.
		/// </summary>
		/// <param name="directory">the directory to check for an index</param>
		/// <returns>
		///		<code>true</code> if an index exists; 
		///		<code>false</code> otherwise if there is a problem with accessing the index
		/// </returns>
		public static bool IndexExists(Lucene.Net.Store.Directory directory)  
		{
			return directory.FileExists("segments");
		}

		/// <summary>
		/// Returns the number of documents in this index.
		/// </summary>
		/// <returns></returns>
		public abstract int NumDocs();

		/// <summary>
		/// Returns one greater than the largest possible document number.
		/// This may be used to, e.g., determine how big to allocate an array which
		/// will have an element for every document number in an index.
		/// </summary>
		/// <returns></returns>
		public abstract int MaxDoc();

		/// <summary>
		/// Returns the stored fields of the <code>n</code><sup>th</sup>
		/// <code>Document</code> in this index. 
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public abstract Document Document(int n) ;

		
		/// <summary>
		/// Returns true if document <i>n</i> has been deleted
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public abstract bool IsDeleted(int n);

		/// <summary>
		/// Returns true if any documents have been deleted
		/// </summary>
		/// <returns></returns>
		public abstract bool HasDeletions();
		
		/// <summary>
		/// Returns the byte-encoded normalization factor for the named field of
		/// every document.  This is used by the search code to score documents.
		/// <seealso cref="Field.SetBoost(float)"/>
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public abstract byte[] Norms(String field);

		/// <summary>
		/// Returns an enumeration of all the terms in the index.
		///	The enumeration is ordered by Term.CompareTo().  Each term
		///	is greater than all that precede it in the enumeration.
		/// </summary>
		/// <returns></returns>
		public abstract TermEnum Terms();

		/// <summary>
		/// Returns an enumeration of all terms after a given term.
		///	The enumeration is ordered by Term.CompareTo().  Each term
		///	is greater than all that precede it in the enumeration.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public abstract TermEnum Terms(Term t);

		/// <summary>
		/// Returns the number of documents containing the term <code>t</code>.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public abstract int DocFreq(Term t);

		/// <summary>
		/// Returns an enumeration of all the documents which contain
		///	<code>term</code>. For each document, the document number, the frequency of
		///	the term in that document is also provided, for use in search scoring.
		///	Thus, this method implements the mapping:
		///	<p><ul>
		///	Term  = docNum, freq<sup>*</sup>
		///	</ul></p>
		///	<p>The enumeration is ordered by document number.  Each document number
		///	is greater than all that precede it in the enumeration.</p>
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		public TermDocs TermDocs(Term term)  
		{
			TermDocs termDocs = TermDocs();
			termDocs.Seek(term);
			return termDocs;
		}

		/// <summary>
		/// Returns an unpositioned TermDocs enumerator.
		/// </summary>
		/// <returns></returns>
		public abstract TermDocs TermDocs();

		/// <summary>
		/// Returns an enumeration of all the documents which contain
		///	<code>term</code>.  For each document, in addition to the document number
		///	and frequency of the term in that document, a list of all of the ordinal
		///	positions of the term in the document is available.  Thus, this method
		///	implements the mapping:
		///	<p><ul>
		///	Term  =docNum, freq,
		///		pos<sub>1</sub>, pos<sub>2</sub>, ...
		///	pos<sub>freq-1</sub>&gt;
		///	&gt;<sup>*</sup>
		///	</ul></p>
		///	<p> This positional information faciliates phrase and proximity searching.</p>
		///	<p>The enumeration is ordered by document number.  Each document number is
		///	greater than all that precede it in the enumeration. </p>
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		public TermPositions TermPositions(Term term)  
		{
			TermPositions termPositions = TermPositions();
			termPositions.Seek(term);
			return termPositions;
		}

		/// <summary>
		/// Returns an unpositioned TermPositions enumerator.
		/// </summary>
		/// <returns></returns>
		public abstract TermPositions TermPositions();

		/// <summary>
		/// Deletes the document numbered <code>docNum</code>.  Once a document is
		///	deleted it will not appear in TermDocs or TermPostitions enumerations.
		///	Attempts to read its field with the Document
		///	method will result in an error.  The presence of this document may still be
		///	reflected in the DocFreq statistic, though
		///	this will be corrected eventually as the index is further modified.  
		/// </summary>
		/// <param name="docNum"></param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Delete(int docNum)  
		{
			if (writeLock == null) 
			{
				Lock _writeLock = directory.MakeLock(IndexWriter.WRITE_LOCK_NAME);
				if (!_writeLock.Obtain(IndexWriter.WRITE_LOCK_TIMEOUT))			  // obtain write lock
					throw new System.IO.IOException("Index locked for write: " + writeLock);
				this.writeLock = _writeLock;
				
				// we have to check whether index has changed since this reader was opened.
				// if so, this reader is no longer valid for deletion
				if(LastModified(directory) > segmentInfosAge)
				{
					this.writeLock.Release();
					this.writeLock = null;
					throw new System.IO.IOException(
						"IndexReader out of date and no longer valid for deletion");
				}
			}
			DoDelete(docNum);
		}
  
		protected internal abstract void DoDelete(int docNum);

		/// <summary>
		/// Deletes all documents containing <code>term</code>.
		///	This is useful if one uses a document field to hold a unique ID string for
		///	the document.  Then to delete such a document, one merely constructs a
		///	term with the appropriate field and the unique ID string as its text and
		///	passes it to this method.  Returns the number of documents deleted. 
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		public int Delete(Term term)
		{
			TermDocs docs = TermDocs(term);
			if ( docs == null ) return 0;
			int n = 0;
			try 
			{
				while (docs.Next()) 
				{
					Delete(docs.Doc());
					n++;
				}
			} 
			finally 
			{
				docs.Close();
			}
			return n;
		}

		/// <summary>
		/// Undeletes all documents currently marked as deleted in this index.
		/// </summary>
		public abstract void UndeleteAll();

		/// <summary>
		/// Closes files associated with this index.
		/// Also saves any new deletions to disk.
		/// No other methods should be called after this has been called.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Close()  
		{
			DoClose();
			if (writeLock != null) 
			{
				writeLock.Release();  // release write lock
				writeLock = null;
			}
		}

		/// <summary>
		/// Implements close.
		/// </summary>
		protected internal abstract void DoClose();

		/// <summary>
		/// Return a list of all unique field names which exist in the index pointed to by
		/// this IndexReader.
		/// </summary>
		/// <returns>
		///		Collection of Strings indicating the names of the 
		///		fields if there is a problem with accessing the index (hashtable)
		///	</returns>
		public abstract ICollection GetFieldNames();

		/// <summary>
		/// Returns a list of all unique field names that exist in the index pointed to by
		/// this IndexReader.  The boolean argument specifies whether the fields returned
		/// are indexed or not.
		/// </summary>
		/// <param name="indexed">
		/// indexed <code>true</code> if only indexed fields should be returned;
		/// <code>false</code> if only unindexed fields should be returned.
		/// </param>
		/// <returns>Collection of Strings indicating the names of the fields (hashtable)</returns>
		public abstract ICollection GetFieldNames(bool indexed);

		/// <summary>
		/// Returns <code>true</code> iff the index in the named directory is
		/// currently locked.
		/// </summary>
		/// <param name="directory">the directory to check for a lock
		/// if there is a problem with accessing the index</param>
		/// <returns></returns>
		public static bool IsLocked(Lucene.Net.Store.Directory directory)  
		{
			return directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).IsLocked() ||
				   directory.MakeLock(IndexWriter.COMMIT_LOCK_NAME).IsLocked();
		}

		/// <summary>
		/// Returns <code>true</code> iff the index in the named directory is
		/// currently locked.
		/// </summary>
		/// <param name="directory">the directory to check for a lock
		/// if there is a problem with accessing the index</param>
		/// <returns></returns>
		public static bool IsLocked(String directory)  
		{
			return IsLocked(FSDirectory.GetDirectory(directory, false));
		}

		/// <summary>
		/// Forcibly unlocks the index in the named directory.
		/// <P>
		/// Caution: this should only be used by failure recovery code,
		/// when it is known that no other process nor thread is in fact
		/// currently accessing this index.</P>
		/// </summary>
		/// <param name="directory"></param>
		public static void Unlock(Lucene.Net.Store.Directory directory)  
		{
			directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).Release();
			directory.MakeLock(IndexWriter.COMMIT_LOCK_NAME).Release();
		}
	}
}