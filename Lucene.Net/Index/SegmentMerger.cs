using System;
using System.Collections;

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

	public sealed class SegmentMerger 
	{
		private bool useCompoundFile;
		private Directory directory;
		private String segment;

		private ArrayList readers = new ArrayList();
		private FieldInfos fieldInfos;
  
		// File extensions of old-style index files
		private static string[] COMPOUND_EXTENSIONS = new string[] 
		{"fnm", "frq", "prx", "fdx", "fdt", "tii", "tis"};

		public SegmentMerger(Directory dir, String name, bool compoundFile) 
		{
			directory = dir;
			segment = name;
			useCompoundFile = compoundFile;
		}

		public void Add(IndexReader reader) 
		{
			readers.Add(reader);
		}

		public IndexReader SegmentReader(int i) 
		{
			return (IndexReader)readers[i];
		}

		public int Merge()  
		{
			int _value;
			
			try 
			{
				MergeFields();
				MergeTerms();
				_value = MergeNorms();
      		} 
			finally 
			{
				for (int i = 0; i < readers.Count; i++) 
				{  // close readers
					IndexReader reader = (IndexReader)readers[i];
					reader.Close();
				}
			}
			
			if (useCompoundFile)
				CreateCompoundFile();

			return _value;
		}

		private void CreateCompoundFile()
		{
			CompoundFileWriter cfsWriter = 
				new CompoundFileWriter(directory, segment + ".cfs");
    
			ArrayList files = 
				new ArrayList(COMPOUND_EXTENSIONS.Length + fieldInfos.Size());    
    
			// Basic files
			for (int i=0; i<COMPOUND_EXTENSIONS.Length; i++) 
			{
				files.Add(segment + "." + COMPOUND_EXTENSIONS[i]);
			}

			// Field norm files
			for (int i = 0; i < fieldInfos.Size(); i++) 
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed) 
				{
					files.Add(segment + ".f" + i);
				}
			}

			// Now merge all added files
			foreach(string file in files)
			{
				cfsWriter.AddFile(file);
			}
    
			// Perform the merge
			cfsWriter.Close();
        
			// Now delete the source files
			foreach(string file in files)
			{
				directory.DeleteFile(file);
			}
		}
 
		private void MergeFields()  
		{
			fieldInfos = new FieldInfos();		  // merge field names
			for (int i = 0; i < readers.Count; i++) 
			{
				IndexReader reader = (IndexReader)readers[i];
				
				fieldInfos.Add(reader.GetFieldNames(true), true);
				fieldInfos.Add(reader.GetFieldNames(false), false);
			}
			fieldInfos.Write(directory, segment + ".fnm");
    
			FieldsWriter fieldsWriter =			  // merge field values
				new FieldsWriter(directory, segment, fieldInfos);
			try 
			{
				for (int i = 0; i < readers.Count; i++) 
				{
					IndexReader reader = (IndexReader)readers[i];

					int maxDoc = reader.MaxDoc();
					for (int j = 0; j < maxDoc; j++)
						if (!reader.IsDeleted(j)) // skip deleted docs
							fieldsWriter.AddDocument(reader.Document(j));
				}
			} 
			finally 
			{
				fieldsWriter.Close();
			}
		}

		private OutputStream freqOutput = null;
		private OutputStream proxOutput = null;
		private TermInfosWriter termInfosWriter = null;
		private SegmentMergeQueue queue = null;

		private void MergeTerms()  
		{
			try 
			{
				freqOutput = directory.CreateFile(segment + ".frq");
				proxOutput = directory.CreateFile(segment + ".prx");
				termInfosWriter =
					new TermInfosWriter(directory, segment, fieldInfos);
      
				MergeTermInfos();
      		} 
			finally 
			{
				if (freqOutput != null) 		freqOutput.Close();
				if (proxOutput != null) 		proxOutput.Close();
				if (termInfosWriter != null) 	termInfosWriter.Close();
				if (queue != null)		queue.Close();
			}
		}

		private void MergeTermInfos()  
		{
			queue = new SegmentMergeQueue(readers.Count);
			int _base = 0;
			for (int i = 0; i < readers.Count; i++) 
			{
				IndexReader reader = (IndexReader)readers[i];
				TermEnum termEnum = reader.Terms();
				SegmentMergeInfo smi = new SegmentMergeInfo(_base, termEnum, reader);
				_base += reader.NumDocs();
				if (smi.Next())
					queue.Put(smi);				  // initialize queue
				else
					smi.Close();
			}

			SegmentMergeInfo[] match = new SegmentMergeInfo[readers.Count];
    
			while (queue.Size() > 0) 
			{
				int matchSize = 0;			  // pop matching terms
				match[matchSize++] = (SegmentMergeInfo)queue.Pop();
				Term term = match[0].term;
				SegmentMergeInfo top = (SegmentMergeInfo)queue.Top();
      
				while (top != null && term.CompareTo(top.term) == 0) 
				{
					match[matchSize++] = (SegmentMergeInfo)queue.Pop();
					top = (SegmentMergeInfo)queue.Top();
				}

				MergeTermInfo(match, matchSize);		  // add new TermInfo
      
				while (matchSize > 0) 
				{
					SegmentMergeInfo smi = match[--matchSize];
					if (smi.Next())
						queue.Put(smi);			  // restore queue
					else
						smi.Close();				  // done with a segment
				}
			}
		}

		private readonly TermInfo termInfo = new TermInfo(); // minimize consing

		private void MergeTermInfo(SegmentMergeInfo[] smis, int n)
		{
			long freqPointer = freqOutput.GetFilePointer();
			long proxPointer = proxOutput.GetFilePointer();

			int df = AppendPostings(smis, n);		  // append posting data

			if (df > 0) 
			{
				// add an entry to the dictionary with pointers to prox and freq files
				termInfo.Set(df, freqPointer, proxPointer);
				termInfosWriter.Add(smis[0].term, termInfo);
			}
		}
       
		private int AppendPostings(SegmentMergeInfo[] smis, int n)
		{
			int lastDoc = 0;
			int df = 0;					  // number of docs w/ term
			for (int i = 0; i < n; i++) 
			{
				SegmentMergeInfo smi = smis[i];
				TermPositions postings = smi.postings;
				int _base = smi._base;
				int[] docMap = smi.docMap;
				postings.Seek(smi.termEnum);
				while (postings.Next()) 
				{
					int doc = postings.Doc();
					if (docMap != null)
						doc = docMap[doc];		  // map around deletions
					doc += _base;				  // convert to merged space

					if (doc < lastDoc)
						throw new InvalidOperationException("docs out of order");

					int docCode = (doc - lastDoc) << 1;	  // use low bit to flag freq=1
					lastDoc = doc;

					int freq = postings.Freq();
					if (freq == 1) 
					{
						freqOutput.WriteVInt(docCode | 1);	  // write doc & freq=1
					} 
					else 
					{
						freqOutput.WriteVInt(docCode);	  // write doc
						freqOutput.WriteVInt(freq);		  // write frequency in doc
					}
	  
					int lastPosition = 0;			  // write position deltas
					for (int j = 0; j < freq; j++) 
					{
						int position = postings.NextPosition();
						proxOutput.WriteVInt(position - lastPosition);
						lastPosition = position;
					}

					df++;
				}
			}
			return df;
		}

		private int MergeNorms()  
		{
			int docCount = 0;
			for (int i = 0; i < fieldInfos.Size(); i++) 
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.isIndexed) 
				{
					OutputStream output = directory.CreateFile(segment + ".f" + i);
					try 
					{
						for (int j = 0; j < readers.Count; j++) 
						{
							IndexReader reader = (IndexReader)readers[j];
							byte[] input = reader.Norms(fi.name);

							int maxDoc = reader.MaxDoc();
							for (int k = 0; k < maxDoc; k++) 
							{
								byte norm = input != null ? input[k] : (byte)0;
								if (!reader.IsDeleted(k))
								{
									output.WriteByte(norm);
									docCount++;
								}
							}
						}
					} 
					finally 
					{
						output.Close();
					}
				}
			}
			
			return docCount;
		}
	}
}