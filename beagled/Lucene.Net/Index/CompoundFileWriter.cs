using System;
using System.Collections;

using Lucene.Net.Store;

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
	/// Combines multiple files into a single compound file.
	/// The file format:<br/>
	/// <ul>
	///     <li>VInt fileCount</li>
	///     <li>{Directory}
	///         fileCount entries with the following structure:</li>
	///         <ul>
	///             <li>long dataOffset</li>
	///             <li>UTFString extension</li>
	///         </ul>
	///     <li>{File Data}
	///         fileCount entries with the raw data of the corresponding file</li>
	/// </ul>
	///
	/// The fileCount integer indicates how many files are contained in this compound
	/// file. The {directory} that follows has that many entries. Each directory entry
	/// contains an encoding identifier, an long pointer to the start of this file's
	/// data section, and a UTF String with that file's extension.
	///
	/// @author Dmitry Serebrennikov
	/// @version $Id$
	/// </summary>
	public sealed class CompoundFileWriter 
	{

		private sealed class FileEntry 
		{
			/// <summary>
			/// source file
			/// </summary>
			internal String file;

			/// <summary>
			/// temporary holder for the start of directory entry for this file
			/// </summary>
			internal long directoryOffset;

			/// <summary>
			/// temporary holder for the start of this file's data section
			/// </summary>
			internal long dataOffset;
		}


		private Directory directory;
		private String fileName;
		private Hashtable ids;
		private ArrayList entries;
		private bool merged = false;


		/// <summary>
		/// Create the compound stream in the specified file. The file name is the
		/// entire name (no extensions are added).
		/// </summary>
		public CompoundFileWriter(Directory dir, String name) 
		{
			if (dir == null)
				throw new ArgumentException("Missing directory");
			if (name == null)
				throw new ArgumentException("Missing name");

			directory = dir;
			fileName = name;
			ids = new Hashtable();
			entries = new ArrayList();
		}

		/// <summary>
		/// Returns the directory of the compound file.
		/// </summary>
		public Directory GetDirectory() 
		{
			return directory;
		}

		/// <summary>
		/// Returns the name of the compound file.
		/// </summary>
		public String GetName() 
		{
			return fileName;
		}

		/// <summary>
		/// Add a source stream. If sourceDir is null, it is set to the
		/// same value as the directory where this compound stream exists.
		/// The id is the string by which the sub-stream will be know in the
		/// compound stream. The caller must ensure that the ID is unique. If the
		/// id is null, it is set to the name of the source file.
		/// </summary>
		public void AddFile(String file) 
		{
			if (merged)
				throw new InvalidOperationException(
					"Can't add extensions after merge has been called");

			if (file == null)
				throw new ArgumentException(
					"Missing source file");

			try
			{
				ids.Add(file, file);
			}
			catch(Exception e)
			{
				throw new ArgumentException(
					"File " + file + " already added", e);
			}

			FileEntry entry = new FileEntry();
			entry.file = file;
			entries.Add(entry);
		}

		/// <summary>
		/// Merge files with the extensions added up to now.
		/// All files with these extensions are combined sequentially into the
		/// compound stream. After successful merge, the source files
		/// are deleted.
		/// </summary>
		public void Close()
		{
			if (merged)
				throw new InvalidOperationException(
					"Merge already performed");

			if (entries.Count == 0)
				throw new InvalidOperationException(
					"No entries to merge have been defined");

			merged = true;

			// open the compound stream
			OutputStream os = null;
			try 
			{
				os = directory.CreateFile(fileName);

				// Write the number of entries
				os.WriteVInt(entries.Count);

				// Write the directory with all offsets at 0.
				// Remember the positions of directory entries so that we can
				// adjust the offsets later
				foreach(FileEntry fe in entries)
				{
					fe.directoryOffset = os.GetFilePointer();
					os.WriteLong(0);    // for now
					os.WriteString(fe.file);
				}

				// Open the files and copy their data into the stream.
				// Remeber the locations of each file's data section.
				byte[] buffer = new byte[1024];
				
				foreach(FileEntry fe in entries)
				{
					fe.dataOffset = os.GetFilePointer();
					CopyFile(fe, os, buffer);
				}

				// Write the data offsets into the directory of the compound stream
				foreach(FileEntry fe in entries)
				{
					os.Seek(fe.directoryOffset);
					os.WriteLong(fe.dataOffset);
				}

				// Close the output stream. Set the os to null before trying to
				// close so that if an exception occurs during the close, the
				// finally clause below will not attempt to close the stream
				// the second time.
				OutputStream tmp = os;
				os = null;
				tmp.Close();
			} 
			finally 
			{
				if (os != null) try { os.Close(); } 
								catch (System.IO.IOException) { }
			}
		}

		/// <summary>
		/// Copy the contents of the file with specified extension into the
		/// provided output stream. Use the provided buffer for moving data
		/// to reduce memory allocation.
		/// </summary>
		private void CopyFile(FileEntry source, OutputStream os, byte[] buffer)
		{
			InputStream stream = null;
			try 
			{
				long startPtr = os.GetFilePointer();

				stream = directory.OpenFile(source.file);
				long length = stream.Length();
				long remainder = length;
				int chunk = buffer.Length;

				while(remainder > 0) 
				{
					int len = (int) Math.Min(chunk, remainder);
					stream.ReadBytes(buffer, 0, len);
					os.WriteBytes(buffer, len);
					remainder -= len;
				}

				// Verify that remainder is 0
				if (remainder != 0)
					throw new System.IO.IOException(
						"Non-zero remainder length after copying: " + remainder
						+ " (id: " + source.file + ", length: " + length
						+ ", buffer size: " + chunk + ")");

				// Verify that the output length diff is equal to original file
				long endPtr = os.GetFilePointer();
				long diff = endPtr - startPtr;
				if (diff != length)
					throw new System.IO.IOException(
						"Difference in the output file offsets " + diff
						+ " does not match the original file length " + length);
			} 
			finally 
			{
				if (stream != null) stream.Close();
			}
		}
	}
}
