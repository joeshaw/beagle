using System;
using System.Collections;
using System.Runtime.CompilerServices;

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
	/// Class for accessing a compound stream.
	/// This class implements a directory, but is limited to only read operations.
	/// Directory methods that would normally modify data throw an exception.
	/// @author Dmitry Serebrennikov
	/// @version $Id$
	/// </summary>
	public class CompoundFileReader : Directory 
	{

		private sealed class FileEntry 
		{
			internal long offset;
			internal long length;
		}

		// Base info
		private Directory directory;
		private String fileName;

		// Reference count
		//private bool open;

		private InputStream stream;
		private Hashtable entries = new Hashtable();


		public CompoundFileReader(Directory dir, String name)
		{
			directory = dir;
			fileName = name;

			bool success = false;

			try 
			{
				stream = dir.OpenFile(name);

				// read the directory and init files
				int count = stream.ReadVInt();
				FileEntry entry = null;
				
				for (int i=0; i<count; i++) 
				{
					long offset = stream.ReadLong();
					String id = stream.ReadString();

					if (entry != null) 
					{
						// set length of the previous entry
						entry.length = offset - entry.offset;
					}

					entry = new FileEntry();
					entry.offset = offset;
					entries.Add(id, entry);
				}

				// set the length of the final entry
				if (entry != null) 
				{
					entry.length = stream.Length() - entry.offset;
				}

				success = true;

			} 
			finally 
			{
				if (! success) 
				{
					try 
					{
						stream.Close();
					} 
					catch (System.IO.IOException) { }
				}
			}
		}

		public Directory GetDirectory() 
		{
			return directory;
		}

		public String GetName() 
		{
			return fileName;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Close()
		{
			if (stream == null)
				throw new System.IO.IOException("Already closed");

			entries.Clear();
			stream.Close();
			stream = null;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override InputStream OpenFile(String id)
		{
			if (stream == null)
				throw new System.IO.IOException("Stream closed");

			FileEntry entry = (FileEntry) entries[id];
			if (entry == null)
				throw new System.IO.IOException("No sub-file with id " + id + " found");

			return new CSInputStream(stream, entry.offset, entry.length);
		}

		/// <summary>
		/// Returns an array of strings, one for each file in the directory.
		/// </summary>
		public override String[] List() 
		{
			String[] res = new String[entries.Count];
			entries.Keys.CopyTo(res, 0);
			return res;
		}

		/// <summary>
		/// Returns true iff a file with the given name exists.
		/// </summary>
		public override bool FileExists(String name) 
		{
			return entries.ContainsKey(name);
		}

		/// <summary>
		/// Returns the time the named file was last modified.
		/// </summary>
		public override long FileModified(String name)
		{
			return directory.FileModified(fileName);
		}

		/// <summary>
		/// Set the modified time of an existing file to now.
		/// </summary>
		/// <param name="name"></param>
		public override void TouchFile(String name)
		{
			directory.TouchFile(fileName);
		}

		/// <summary>
		/// Removes an existing file in the directory.
		/// </summary>
		public override void DeleteFile(String name)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Renames an existing file in the directory.
		/// If a file already exists with the new name, then it is replaced.
		/// This replacement should be atomic. */
		/// </summary>
		public override void RenameFile(String from, String to)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Returns the length of a file in the directory.
		/// </summary>
		public override long FileLength(String name)
		{
			FileEntry e = (FileEntry) entries[name];
			if (e == null)
				throw new System.IO.IOException("File " + name + " does not exist");
			return e.length;
		}

		/// <summary>
		/// Creates a new, empty file in the directory with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>a stream writing the file.</returns>
		public override OutputStream CreateFile(String name)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Construct a {@link Lock}.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>the name of the lock file</returns>
		public override Lock MakeLock(String name)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Implementation of an InputStream that reads from a portion of the
		/// compound file. The visibility is left as "package" *only* because
		/// this helps with testing since JUnit test cases in a different class
		/// can then access package fields of this class.
		/// </summary>
		public sealed class CSInputStream : InputStream 
		{

			internal InputStream _base;
			internal long fileOffset;

			internal CSInputStream( InputStream _base,
						long fileOffset,
						long length)
			{
				this._base = (InputStream) _base.Clone();
				this.fileOffset = fileOffset;
				this.length = length;   // variable in the superclass
				SeekInternal(0);        // position to the adjusted 0th byte
			}

			/// <summary>
			/// Expert: implements buffer refill.  Reads bytes from the current position in the input.
			/// </summary>
			/// <param name="b">the array to read bytes into</param>
			/// <param name="offset">the offset in the array to start storing bytes</param>
			/// <param name="len">the number of bytes to read</param>
			protected override void ReadInternal(byte[] b, int offset, int len)
			{
				_base.ReadBytes(b, offset, len);
			}

			/// <summary>
			/// Expert: implements seek.  Sets current position in this file, where
			/// the next ReadInternal(byte[],int,int) will occur.
			/// <see>ReadInternal(byte[],int,int)</see>
			/// </summary>
			/// <param name="pos"></param>
			protected override void SeekInternal(long pos)
			{
				if (pos > 0 && pos >= length)
					throw new System.IO.IOException("Seek past the end of file");

				if (pos < 0)
					throw new System.IO.IOException("Seek to a negative offset");

				_base.Seek(fileOffset + pos);
			}

			/// <summary>
			/// Closes the stream to futher operations.
			/// </summary>
			public override void Close()
			{
				_base.Close();
			}

			/// <summary>
			/// <p>Clones of a stream access the same data, and are positioned at the same
			/// point as the stream they were cloned from.</p>p>
			///
			/// <p>Expert: Subclasses must ensure that clones may be positioned at
			/// different points in the input from each other and from the stream they
			/// were cloned from.</p>p>
			/// </summary>
			/// <returns>clone of this stream</returns>
			public override Object Clone()
			{
				CSInputStream other = (CSInputStream) base.Clone();
				other._base = (InputStream) _base.Clone();
				return other;
			}
		}
	}
}
