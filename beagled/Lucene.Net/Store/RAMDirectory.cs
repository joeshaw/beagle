using System;
using System.IO;
using System.Threading;
using System.Collections;
using Lucene.Net.Util;

namespace Lucene.Net.Store
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
	/// A memory-resident Directory implementation.
	/// </summary>
	/// <version>$Id$</version>
	public sealed class RAMDirectory : Directory 
	{
		Hashtable files = new Hashtable();

		/// <summary>
		/// Constructs an empty Directory.
		/// </summary>
		public RAMDirectory() 
		{
		}

		/// <summary>
		/// Creates a new <code>RAMDirectory</code> instance from a different
		/// <code>Directory</code> implementation.  This can be used to load
		/// a disk-based index into memory.
		/// <P>
		/// This should be used only with indices that can fit into memory.
		/// </P>
		/// </summary>
		/// <param name="dir">a <code>Directory</code> value</param>
		/// <throws>IOException if an error occurs</throws>
		public RAMDirectory(Directory dir) 
		{
			String[] ar = dir.List();
			for (int i = 0; i < ar.Length; i++) 
			{
				// make place on ram disk
				OutputStream os = CreateFile(ar[i]);
				// read current file
				InputStream _is = dir.OpenFile(ar[i]);
				// and copy to ram disk
				int len = (int) _is.Length();
				byte[] buf = new byte[len];
				_is.ReadBytes(buf, 0, len);
				os.WriteBytes(buf, len);
				// graceful cleanup
				_is.Close();
				os.Close();
			}
		}

		/// <summary>
		/// Creates a new <code>RAMDirectory</code> instance from the FSDirectory.
		/// </summary>
		/// <param name="dir">a <code>File</code> specifying the index directory</param>
		public RAMDirectory(DirectoryInfo dir) : this(FSDirectory.GetDirectory(dir, false))
		{
		}

		/// <summary>
		/// Creates a new <code>RAMDirectory</code> instance from the FSDirectory.
		/// </summary>
		/// <param name="dir">a <code>String</code> specifying the full index directory path</param>
		public RAMDirectory(String dir) : this(FSDirectory.GetDirectory(dir, false))
		{
		}

		/// <summary>
		/// Returns an array of strings, one for each file in the directory.
		/// </summary>
		/// <returns></returns>
		public override String[] List() 
		{
			String[] result = new String[files.Count];
			int i = 0;
			foreach (String s in files.Keys)
			{
				result[i++] = s;
			}
			return result;
		}

		/// <summary>
		/// Returns true iff the named file exists in this directory.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override bool FileExists(String name) 
		{
			RAMFile file = (RAMFile)files[name];
			return file != null;
		}

		/// <summary>
		/// Returns the time the named file was last modified.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override long FileModified(String name)  
		{
			RAMFile file = (RAMFile)files[name];
			return file.lastModified;
		}

		/// <summary>
		/// Set the modified time of an existing file to now.
		/// </summary>
		/// <param name="name"></param>
		public override void TouchFile(String name)  
		{
			bool MONITOR = false;
			int count = 0;
			RAMFile file = (RAMFile)files[name];
			
			long ts2, ts1 = Date.GetTime(DateTime.Now);
			do 
			{
				try 
				{
					Thread.Sleep(1);
				} 
				catch (Exception) {}
				ts2 = Date.GetTime(DateTime.Now);
				if (MONITOR) count++;
			} while(ts1 == ts2);
    
			file.lastModified = ts1;

			if (MONITOR)
				Console.WriteLine("SLEEP COUNT: " + count);
		}

		/// <summary>
		/// Returns the length in bytes of a file in the directory.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override long FileLength(String name) 
		{
			RAMFile file = (RAMFile)files[name];
			return file.length;
		}

		/// <summary>
		/// Removes an existing file in the directory.
		/// </summary>
		/// <param name="name"></param>
		public override void DeleteFile(String name) 
		{
			files.Remove(name);
		}

		/// <summary>
		/// Removes an existing file in the directory.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public override void RenameFile(String from, String to) 
		{
			RAMFile file = (RAMFile)files[from];
			files.Remove(from);
			// Console.WriteLine(to);
			files[to] = file;
		}

		/// <summary>
		/// Creates a new, empty file in the directory with the given name.
		/// Returns a stream writing this file.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override OutputStream CreateFile(String name) 
		{
			RAMFile file = new RAMFile();
			files[name] = file;
			return new RAMOutputStream(file);
		}

		/// <summary>
		/// Returns a stream reading an existing file.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override InputStream OpenFile(String name) 
		{
			RAMFile file = (RAMFile)files[name];
			return new RAMInputStream(file);
		}

		private class RAMDirectoryLock : Lock
		{
			String name;
			RAMDirectory ramDirectory;
			internal RAMDirectoryLock(RAMDirectory ramDirectory, String name)
			{
				this.ramDirectory = ramDirectory;
				this.name = name;
			}

			public override bool Obtain()  
			{
				lock (ramDirectory.files) 
				{
					if (!ramDirectory.FileExists(name)) 
					{
						ramDirectory.CreateFile(name).Close();
						return true;
					}
					return false;
				}
			}

			public override void Release() 
			{
				ramDirectory.DeleteFile(name);
			}
			
			public override bool IsLocked() 
			{
				return ramDirectory.FileExists(name);
			}
		};

		/// <summary>
		/// </summary>
		/// <param name="name">the name of the lock file</param>
		/// <returns></returns>
		public override Lock MakeLock(String name) 
		{
			return new RAMDirectoryLock(this, name);
		}

		/// <summary>
		/// Closes the store to future operations.
		/// </summary>
		public override void Close() 
		{
		}
	}

	sealed class RAMInputStream : InputStream, ICloneable 
	{
		internal RAMFile file;
		internal int pointer = 0;

		public RAMInputStream(RAMFile f) 
		{
			file = f;
			length = file.length;
		}

		/// <summary>
		/// InputStream methods
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="destOffset"></param>
		/// <param name="len"></param>
		protected override void ReadInternal(byte[] dest, int destOffset, int len) 
		{
			int remainder = len;
			int start = pointer;
			while (remainder != 0) 
			{
				int bufferNumber = start/InputStream.BUFFER_SIZE;
				int bufferOffset = start%InputStream.BUFFER_SIZE;
				int bytesInBuffer = InputStream.BUFFER_SIZE - bufferOffset;
				int bytesToCopy = bytesInBuffer >= remainder ? remainder : bytesInBuffer;
				byte[] buffer = (byte[])file.buffers[bufferNumber];
				Array.Copy(buffer, bufferOffset, dest, destOffset, bytesToCopy);
				destOffset += bytesToCopy;
				start += bytesToCopy;
				remainder -= bytesToCopy;
			}
			pointer += len;
		}

		public override void Close() 
		{
		}

		/// <summary>
		/// Random-access method
		/// </summary>
		/// <param name="pos"></param>
		protected override void SeekInternal(long pos) 
		{
			pointer = (int)pos;
		}
	}

	sealed class RAMOutputStream : OutputStream 
	{
		RAMFile file;
		int pointer = 0;

		public RAMOutputStream(RAMFile f) 
		{
			file = f;
		}

		/// Output methods:

		public override void FlushBuffer(byte[] src, int len) 
		{
			int bufferNumber = pointer/OutputStream.BUFFER_SIZE;
			int bufferOffset = pointer%OutputStream.BUFFER_SIZE;
			int bytesInBuffer = OutputStream.BUFFER_SIZE - bufferOffset;
			int bytesToCopy = bytesInBuffer >= len ? len : bytesInBuffer;

			if (bufferNumber == file.buffers.Count)
				file.buffers.Add(new byte[OutputStream.BUFFER_SIZE]);

			byte[] buffer = (byte[])file.buffers[bufferNumber];
			Array.Copy(src, 0, buffer, bufferOffset, bytesToCopy);

			if (bytesToCopy < len) 
			{			  // not all in one buffer
				int srcOffset = bytesToCopy;
				bytesToCopy = len - bytesToCopy;		  // remaining bytes
				bufferNumber++;
				if (bufferNumber == file.buffers.Count)
					file.buffers.Add(new byte[OutputStream.BUFFER_SIZE]);
				buffer = (byte[])file.buffers[bufferNumber];
				Array.Copy(src, srcOffset, buffer, 0, bytesToCopy);
			}
			pointer += len;
			if (pointer > file.length)
				file.length = pointer;

			file.lastModified = Date.GetTime(DateTime.Now);
		}

		public override void Close()  
		{
			base.Close();
		}

		/// <summary>
		/// Random-access method
		/// </summary>
		/// <param name="pos"></param>
		public override void Seek(long pos)  
		{
			base.Seek(pos);
			pointer = (int)pos;
		}
		public override long Length()  
		{
			return file.length;
		}
	}

	sealed class RAMFile 
	{
		internal ArrayList buffers = new ArrayList();
		internal long length;
		internal long lastModified = Date.GetTime(DateTime.Now);
	}
}