using System;
using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;

using Lucene.Net.Store;
using Lucene.Net.Search; 
using Lucene.Net.Documents;
using Lucene.Net.Analysis;

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
	/// An IndexWriter creates and maintains an index.
	///  
	/// The third argument to the IndexWriter <b>constructor</b>
	/// determines whether a new index is created, or whether an existing index is
	/// opened for the addition of new documents.
	/// 
	/// In either case, documents are added with the <b>AddDocument</b> method.  
	/// When finished adding documents, <b>Close</b> should be called.
	///
	/// If an index will not have more documents added for a while and optimal search
	/// performance is desired, then the <b>Optimize</b>
	/// method should be called before the index is closed.
	/// </summary>
	public class IndexWriter 
	{
		public static long WRITE_LOCK_TIMEOUT = 1000;
		public static long COMMIT_LOCK_TIMEOUT = 10000;

		public static string WRITE_LOCK_NAME = "write.lock";
		public static string COMMIT_LOCK_NAME = "commit.lock";

		private Lucene.Net.Store.Directory directory;			  // where this index resides
		private Analyzer analyzer;			  // how to analyze text

		private Similarity similarity = Similarity.GetDefault(); // how to normalize

		private SegmentInfos segmentInfos = new SegmentInfos(); // the segments
		private readonly Lucene.Net.Store.Directory ramDirectory = new RAMDirectory(); // for temp segs

		private Lock writeLock;

		/// <summary>
		/// Use compound file setting. Defaults to false to maintain multiple files 
		/// per segment behavior.
		/// </summary>
		private bool useCompoundFile = false;
  
  		/// <summary>
		/// Setting to turn on usage of a compound file. When on, multiple files
		/// for each segment are merged into a single file once the segment creation
		/// is finished. This is done regardless of what directory is in use.
		/// </summary>
		public bool GetUseCompoundFile() 
		{
			return useCompoundFile;
		}
  
		/// <summary>
		/// Setting to turn on usage of a compound file. When on, multiple files
		/// for each segment are merged into a single file once the segment creation
		/// is finished. This is done regardless of what directory is in use.
		/// </summary>
		public void SetUseCompoundFile(bool val) 
		{
			useCompoundFile = val;
		}
  
		/// <summary>
		/// Expert: Set the Similarity implementation used by this IndexWriter.
		/// </summary>
		/// <param name="similarity"></param>
		/// <see cref="Similarity.SetDefault(Similarity)"/>
		public void SetSimilarity(Similarity similarity) 
		{
			this.similarity = similarity;
		}

		/// <summary>
		/// Expert: Return the Similarity implementation used by this IndexWriter.
		/// <p>This defaults to the current value of Similarity.GetDefault().</p>
		/// </summary>
		/// <returns></returns>
		public Similarity GetSimilarity() 
		{
			return this.similarity;
		}

		/// <summary>
		/// Constructs an IndexWriter for the index in <code>path</code>.  Text will
		///	be analyzed with <code>a</code>.  If <code>create</code> is true, then a
		///	new, empty index will be created in <code>path</code>, replacing the index
		///	already there, if any. 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="a"></param>
		/// <param name="create"></param>
		public IndexWriter(String path, Analyzer a, bool create) : 
			this(FSDirectory.GetDirectory(path, create), a, create)
		{
		}

		/// <summary>
		/// Constructs an IndexWriter for the index in <code>path</code>.  Text will
		///	be analyzed with <code>a</code>.  If <code>create</code> is true, then a
		///	new, empty index will be created in <code>path</code>, replacing the index
		///	already there, if any. 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="a"></param>
		/// <param name="create"></param>
		public IndexWriter(FileInfo path, Analyzer a, bool create)
			: this(FSDirectory.GetDirectory(path.FullName, create), a, create)
		{
		}

		class IndexWriterLockWith : Lock.With
		{
			bool create;
			IndexWriter indexWriter;
			internal IndexWriterLockWith(Lock _lock, long lockTimeout, IndexWriter writer, bool create) 
				: base(_lock, lockTimeout)
			{
				this.indexWriter = writer;
				this.create = create;
			}

			internal IndexWriterLockWith(Lock _lock, IndexWriter writer, bool create) 
				: base(_lock)
			{
				this.indexWriter = writer;
				this.create = create;
			}

			override public Object DoBody()
			{
				if (create)
					indexWriter.segmentInfos.Write(indexWriter.directory);
				else
					indexWriter.segmentInfos.Read(indexWriter.directory);
				return null;
			}
		}

		/// <summary>
		/// Release the write lock, if needed.
		/// </summary>
		~IndexWriter()  
		{
			if (writeLock != null) 
			{
				writeLock.Release();                        // release write lock
				writeLock = null;
			}
		}

		/// <summary>
		/// Constructs an IndexWriter for the index in <code>d</code>.  Text will be
		///	analyzed with <code>a</code>.  If <code>create</code> is true, then a new,
		///	empty index will be created in <code>d</code>, replacing the index already
		///	there, if any. 
		/// </summary>
		/// <param name="d"></param>
		/// <param name="a"></param>
		/// <param name="create"></param>
		public IndexWriter(Lucene.Net.Store.Directory d, Analyzer a, bool create)
		{
			directory = d;
			analyzer = a;

			Lock writeLock = directory.MakeLock(IndexWriter.WRITE_LOCK_NAME);
			if (!writeLock.Obtain(WRITE_LOCK_TIMEOUT))                      // obtain write lock
				throw new IOException("Index locked for write: " + writeLock);
			this.writeLock = writeLock;                   // save it

			lock (directory) 
			{			  // in- & inter-process sync
				new IndexWriterLockWith(
					directory.MakeLock(IndexWriter.COMMIT_LOCK_NAME), COMMIT_LOCK_TIMEOUT, this, create
				).Run();
			}
		}

		/// <summary>
		/// Flushes all changes to an index, closes all associated files, and closes
		///	the directory that the index is stored in. 
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Close()  
		{
			FlushRamSegments();
			ramDirectory.Close();
			writeLock.Release();                          // release write lock
			writeLock = null;
			directory.Close();
		}

		/// <summary>
		/// Returns the analyzer used by this index.
		/// </summary>
		/// <returns></returns>
		public Analyzer GetAnalyzer() 
		{
			return analyzer;
		}

		/// <summary>
		/// Returns the number of documents currently in this index.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public int DocCount() 
		{
			int count = 0;
			for (int i = 0; i < segmentInfos.Count; i++) 
			{
				SegmentInfo si = segmentInfos.Info(i);
				count += si.docCount;
			}
			return count;
		}

		/// <summary>
		/// The maximum number of terms that will be indexed for a single field in a
		/// document.  This limits the amount of memory required for indexing, so that
		/// collections with very large files will not crash the indexing process by
		/// running out of memory.<p/>
		/// Note that this effectively truncates large documents, excluding from the
		/// index terms that occur further in the document.  If you know your source
		/// documents are large, be sure to set this value high enough to accomodate
		/// the expected size.  If you set it to Integer.MAX_VALUE, then the only limit
		/// is your memory, but you should anticipate an OutOfMemoryError.<p/>
		/// By default, no more than 10,000 terms will be indexed for a field.
		/// </summary>
		public int maxFieldLength = 10000;

		/// <summary>
		/// Adds a document to this index. If the document contains more than
		///	maxFieldLength terms for a given field, the remainder are
		///	discarded.
		/// </summary>
		/// <param name="doc"></param>
		public void AddDocument(Document doc)  
		{
			AddDocument(doc, analyzer);
		}

		/// <summary>
		/// Adds a document to this index, using the provided analyzer instead of the
		/// value of {@link #getAnalyzer()}.  If the document contains more than
		/// {@link #maxFieldLength} terms for a given field, the remainder are
		/// discarded.
		/// </summary>
		public void AddDocument(Document doc, Analyzer analyzer)
		{
			DocumentWriter dw =
				new DocumentWriter(ramDirectory, analyzer, similarity, maxFieldLength);
			String segmentName = NewSegmentName();
			dw.AddDocument(segmentName, doc);
			lock (this) 
			{
				segmentInfos.Add(new SegmentInfo(segmentName, 1, ramDirectory));
				MaybeMergeSegments();
			}

		}
		
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		private String NewSegmentName() 
		{
			return "_" + Lucene.Net.Util.Number.ToString(segmentInfos.counter++, Lucene.Net.Util.Number.MAX_RADIX);
		}

		/// <summary>
		/// Determines how often segment indexes are merged by AddDocument().  With
		/// smaller values, less RAM is used while indexing, and searches on
		/// unoptimized indexes are faster, but indexing speed is slower.  With larger
		/// values more RAM is used while indexing and searches on unoptimized indexes
		/// are slower, but indexing is faster.  Thus larger values (&gt; 10) are best
		/// for batched index creation, and smaller values (&lt; 10) for indexes that are
		/// interactively maintained.
		/// <p>This must never be less than 2.  The default value is 10.</p>
		/// </summary>
		public int mergeFactor = 10;

		/// <summary>
		/// Determines the largest number of documents ever merged by AddDocument().
		/// Small values (e.g., less than 10,000) are best for interactive indexing,
		/// as this limits the length of pauses while indexing to a few seconds.
		/// Larger values are best for batched indexing and speedier searches.
		///
		/// <p>The default value is Int32.MaxValue</p>
		/// </summary>
		public int maxMergeDocs = Int32.MaxValue;

		/// <summary>
		/// If non-null, information about merges will be printed to this.
		/// </summary>
		public TextWriter infoStream = null;

		/// <summary>
		/// Merges all segments together into a single segment, optimizing 
		/// an index for search.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Optimize()  
		{
			FlushRamSegments();
			while (segmentInfos.Count > 1 ||
				(segmentInfos.Count == 1 &&
				(SegmentReader.HasDeletions(segmentInfos.Info(0)) ||
				(useCompoundFile && 
                 !SegmentReader.UsesCompoundFile(segmentInfos.Info(0))) ||
				segmentInfos.Info(0).dir != directory))) 
			{
				int minSegment = segmentInfos.Count - mergeFactor;
				MergeSegments(minSegment < 0 ? 0 : minSegment);
			}
		}

		/// <summary>
		/// Merges all segments from an array of indexes into this index.
		///
		/// <p>This may be used to parallelize batch indexing.  A large document
		/// collection can be broken into sub-collections.  Each sub-collection can be
		/// indexed in parallel, on a different thread, process or machine.  The
		/// complete index can then be created by merging sub-collection indexes
		/// with this method.
		/// </p>
		/// 
		/// <p>After this completes, the index is optimized. </p>
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void AddIndexes(Lucene.Net.Store.Directory[] dirs)
		{
			Optimize();					  // start with zero or 1 seg
			for (int i = 0; i < dirs.Length; i++) 
			{
				SegmentInfos sis = new SegmentInfos();	  // read infos from dir
				sis.Read(dirs[i]);
				for (int j = 0; j < sis.Count; j++) 
				{
					segmentInfos.Add(sis.Info(j));	  // add each info
				}
			}
			Optimize();					  // final cleanup
		}
		
		/// <summary>
		/// Merges the provided indexes into this index.
		/// <p/>After this completes, the index is optimized.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void AddIndexes(IndexReader[] readers)
		{
			Optimize();					  // start with zero or 1 seg

			String mergedName = NewSegmentName();
			SegmentMerger merger = new SegmentMerger(directory, mergedName, false);

			if (segmentInfos.Count == 1)                 // add existing index, if any
				merger.Add(new SegmentReader(segmentInfos.Info(0)));

			for (int i = 0; i < readers.Length; i++)      // add new indexes
				merger.Add(readers[i]);

			int docCount = merger.Merge();                // merge 'em

			segmentInfos.Clear();                      // pop old infos & add new
			segmentInfos.Add(new SegmentInfo(mergedName, docCount, directory));

			lock (directory) 
			{			  // in- & inter-process sync
				new IndexWriterLockWith(directory.MakeLock(IndexWriter.COMMIT_LOCK_NAME), this, true).Run();
			}
		}


		/// <summary>
		/// Merges all RAM-resident segments.
		/// </summary>
		private void FlushRamSegments()  
		{
			int minSegment = segmentInfos.Count-1;
			int docCount = 0;
			while (minSegment >= 0 &&
				(segmentInfos.Info(minSegment)).dir == ramDirectory) 
			{
				docCount += segmentInfos.Info(minSegment).docCount;
				minSegment--;
			}
			if (minSegment < 0 ||			  // add one FS segment?
				(docCount + segmentInfos.Info(minSegment).docCount) > mergeFactor ||
				!(segmentInfos.Info(segmentInfos.Count-1).dir == ramDirectory))
				minSegment++;
			if (minSegment >= segmentInfos.Count)
				return;					  // none to merge
			MergeSegments(minSegment);
		}

		/// <summary>
		/// Incremental segment merger.
		/// </summary>
		private void MaybeMergeSegments()  
		{
			long targetMergeDocs = mergeFactor;
			while (targetMergeDocs <= maxMergeDocs) 
			{
				// find segments smaller than current target size
				int minSegment = segmentInfos.Count;
				int mergeDocs = 0;
				while (--minSegment >= 0) 
				{
					SegmentInfo si = segmentInfos.Info(minSegment);
					if (si.docCount >= targetMergeDocs)
						break;
					mergeDocs += si.docCount;
				}

				if (mergeDocs >= targetMergeDocs)		  // found a merge to do
					MergeSegments(minSegment+1);
				else
					break;
      
				targetMergeDocs *= mergeFactor;		  // increase target size
			}
		}

		class IndexWriterLockWith2 : Lock.With
		{
			IndexWriter indexWriter;
			ArrayList segmentsToDelete;

			internal IndexWriterLockWith2(Lock _lock, long lockTimeout, IndexWriter indexWriter, 
				ArrayList segmentsToDelete) : base(_lock, lockTimeout) 
			{
				this.indexWriter = indexWriter;
				this.segmentsToDelete = segmentsToDelete;
			}
			override public Object DoBody()  
			{
				indexWriter.segmentInfos.Write(indexWriter.directory);	  // commit before deleting
				indexWriter.DeleteSegments(segmentsToDelete);	  // delete now-unused segments
				return null;
			}
		}

		/// <summary>
		/// Pops segments off of segmentInfos stack down to minSegment, merges them,
		///	and pushes the merged index onto the top of the segmentInfos stack. 
		/// </summary>
		/// <param name="minSegment"></param>
		private void MergeSegments(int minSegment)
		{
			String mergedName = NewSegmentName();
			int mergedDocCount = 0;
			if (infoStream != null) infoStream.Write("merging segments");
			SegmentMerger merger = new SegmentMerger(directory, mergedName, useCompoundFile);
			ArrayList segmentsToDelete = new ArrayList();
			for (int i = minSegment; i < segmentInfos.Count; i++) 
			{
				SegmentInfo si = segmentInfos.Info(i);
				if (infoStream != null)
					infoStream.Write(" " + si.name + " (" + si.docCount + " docs)");
				SegmentReader reader = new SegmentReader(si);
				merger.Add(reader);
				if ((reader.Directory() == this.directory) || // if we own the directory
					(reader.Directory() == this.ramDirectory))
					segmentsToDelete.Add(reader);	  // queue segment for deletion
				mergedDocCount += reader.NumDocs();
			}
			if (infoStream != null) 
			{
				infoStream.WriteLine();
				infoStream.WriteLine(" into "+mergedName+" ("+mergedDocCount+" docs)");
			}
			merger.Merge();

			segmentInfos.RemoveRange(minSegment,segmentInfos.Count - minSegment);  // pop old infos & add new
			segmentInfos.Add(
				new SegmentInfo(mergedName, mergedDocCount,
				directory)
			);
    
			lock (directory) 
			{			  // in- & inter-process sync
				IndexWriterLockWith2 lockWith = new IndexWriterLockWith2(
					directory.MakeLock(IndexWriter.COMMIT_LOCK_NAME), COMMIT_LOCK_TIMEOUT, this, segmentsToDelete
				);
				lockWith.Run();
			}
		}

		/// <summary>
		/// Some operating systems (e.g. Windows) don't permit a file to be deleted
		/// while it is opened for read (e.g. by another process or thread).  So we
		/// assume that when a delete fails it is because the file is open in another
		/// process, and queue the file for subsequent deletion. 
		/// </summary>
		/// <param name="segments"></param>
		internal void DeleteSegments(ArrayList segments)  
		{
			ArrayList deletable = new ArrayList();

			DeleteFiles(ReadDeleteableFiles(), deletable); // try to delete deleteable
    
			for (int i = 0; i < segments.Count; i++) 
			{
				SegmentReader reader = (SegmentReader)segments[i];
				if (reader.Directory() == this.directory)
					DeleteFiles(reader.Files(), deletable);	  // try to delete our files
				else
					DeleteFiles(reader.Files(), reader.Directory()); // delete, eg, RAM files
			}

			WriteDeleteableFiles(deletable);		  // note files we can't delete
		}

		private void DeleteFiles(ArrayList files, Lucene.Net.Store.Directory directory)
		{
			for (int i = 0; i < files.Count; i++)
				directory.DeleteFile((String)files[i]);
		}

		private void DeleteFiles(ArrayList files, ArrayList deletable)
		{
			for (int i = 0; i < files.Count; i++) 
			{
				String file = (String)files[i];
				try 
				{
					directory.DeleteFile(file);		  // try to delete each file
				} 
				catch (IOException e) 
				{			  // if delete fails
					if (directory.FileExists(file)) 
					{
						if (infoStream != null)
							infoStream.WriteLine(e.Message + "; Will re-try later.");
						deletable.Add(file);		  // add to deletable
					}
				}
			}
		}

		private ArrayList ReadDeleteableFiles()  
		{
			ArrayList result = new ArrayList();
			if (!directory.FileExists("deletable"))
				return result;

			InputStream input = directory.OpenFile("deletable");
			try 
			{
				for (int i = input.ReadInt(); i > 0; i--)	  // read file names
					result.Add(input.ReadString());
			} 
			finally 
			{
				input.Close();
			}
			return result;
		}

		private void WriteDeleteableFiles(ArrayList files)  
		{
			OutputStream output = directory.CreateFile("deleteable.new");
			try 
			{
				output.WriteInt(files.Count);
				for (int i = 0; i < files.Count; i++)
					output.WriteString((String)files[i]);
			} 
			finally 
			{
				output.Close();
			}
			directory.RenameFile("deleteable.new", "deletable");
		}
	}
}