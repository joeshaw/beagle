using System;
using System.IO;

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
	/// Abstract class for output to a file in a Directory.  A random-access output
	/// stream.  Used for all Lucene index output operations.
	/// <see cref="Directory"/>
	/// <see cref="InputStream"/>
	/// </summary>
	public abstract class OutputStream 
	{
		internal const int BUFFER_SIZE = 1024;

		private readonly byte[] buffer = new byte[BUFFER_SIZE];
		private long bufferStart = 0;			  // position in file of buffer
		private int bufferPosition = 0;		  // position in buffer

		/// <summary>
		/// Writes a single byte.
		/// <see cref="InputStream.ReadByte()"/>
		/// </summary>
		/// <param name="b"></param>
		public void WriteByte(byte b)  
		{
			if (bufferPosition >= BUFFER_SIZE)
				Flush();
			buffer[bufferPosition++] = b;
		}

		/// <summary>
		/// Writes an array of bytes.
		/// <see cref="InputStream.ReadBytes(byte[],int,int)"/>
		/// </summary>
		/// <param name="b">the bytes to write</param>
		/// <param name="length">the number of bytes to write</param>
		public void WriteBytes(byte[] b, int length)  
		{
			for (int i = 0; i < length; i++)
				WriteByte(b[i]);
		}

		/// <summary>
		/// Writes an int as four bytes.
		/// <see cref="InputStream.ReadInt()"/>
		/// </summary>
		/// <param name="i"></param>
		public void WriteInt(int i)  
		{
			WriteByte((byte)(i >> 24));
			WriteByte((byte)(i >> 16));
			WriteByte((byte)(i >>  8));
			WriteByte((byte) i);
		}

		/// <summary>
		/// Writes an int in a variable-length format.  Writes between one and
		/// five bytes.  Smaller values take fewer bytes.  Negative numbers are not
		/// supported.
		/// <see cref="InputStream.ReadVInt()"/> 
		/// </summary>
		/// <param name="i"></param>
		public void WriteVInt(int i)  
		{
			while ((i & ~0x7F) != 0) 
			{
				WriteByte((byte)((i & 0x7f) | 0x80));
				i = (int)(((uint)i) >> 7);
			}
			WriteByte((byte)i);
		}

		/// <summary>
		/// Writes a long as eight bytes.
		/// <see cref="InputStream.ReadLong()"/>
		/// </summary>
		/// <param name="i"></param>
		public void WriteLong(long i)  
		{
			WriteInt((int) (i >> 32));
			WriteInt((int) i);
		}

		/// <summary>
		/// Writes an long in a variable-length format.  Writes between one and five
		/// bytes.  Smaller values take fewer bytes.  Negative numbers are not
		/// supported.
		/// <see cref="InputStream.ReadVLong()"/>
		/// </summary>
		/// <param name="i"></param>
		public void WriteVLong(long i)  
		{
			while ((i & ~0x7F) != 0) 
			{
				WriteByte((byte)((i & 0x7f) | 0x80));
				i = (int)(((uint)i) >> 7);
			}
			WriteByte((byte)i);
		}

		/// <summary>
		/// Writes a string.
		/// <see cref="InputStream.ReadString()"/>
		/// </summary>
		/// <param name="s"></param>
		public void WriteString(String s)  
		{
			int length = s.Length;
			WriteVInt(length);
			WriteChars(s, 0, length);
		}

		/// <summary>
		/// Writes a sequence of UTF-8 encoded characters from a string.
		/// <see cref="InputStream.ReadChars(char[],int,int)"/>
		/// </summary>
		/// <param name="s">the source of the characters</param>
		/// <param name="start">the first character in the sequence</param>
		/// <param name="length">the number of characters in the sequence</param>
		public void WriteChars(String s, int start, int length)
		{
			int end = start + length;
			for (int i = start; i < end; i++) 
			{
				int code = (int)s[i];
				if (code >= 0x01 && code <= 0x7F)
					WriteByte((byte)code);
				else if (((code >= 0x80) && (code <= 0x7FF)) || code == 0) 
				{
					WriteByte((byte)(0xC0 | (code >> 6)));
					WriteByte((byte)(0x80 | (code & 0x3F)));
				} 
				else 
				{
					WriteByte((byte)(0xE0 | (((uint)code) >> 12)));
					WriteByte((byte)(0x80 | ((code >> 6) & 0x3F)));
					WriteByte((byte)(0x80 | (code & 0x3F)));
				}
			}
		}

		/// <summary>
		/// Forces any buffered output to be written. 
		/// </summary>
		public void Flush()  
		{
			FlushBuffer(buffer, bufferPosition);
			bufferStart += bufferPosition;
			bufferPosition = 0;
		}

		/// <summary>
		/// Expert: implements buffer write.  Writes bytes at the current position in
		/// the output.
		/// </summary>
		/// <param name="b">the bytes to write</param>
		/// <param name="len">the number of bytes to write</param>
		public abstract void FlushBuffer(byte[] b, int len) ;

		/// <summary>
		/// Closes this stream to further operations.
		/// </summary>
		virtual public void Close()  
		{
			Flush();
		}

		/// <summary>
		/// Returns the current position in this file, where the next write will occur.
		/// <see cref="Seek(long)"/>
		/// </summary>
		/// <returns></returns>
		public long GetFilePointer()  
		{
			return bufferStart + bufferPosition;
		}

		/// <summary>
		/// Sets current position in this file, where the next write will occur.
		/// <see cref="GetFilePointer()"/>
		/// </summary>
		/// <param name="pos"></param>
		virtual public void Seek(long pos)  
		{
			Flush();
			bufferStart = pos;
		}

		/// <summary>
		/// The number of bytes in the file.
		/// </summary>
		/// <returns></returns>
		public abstract long Length() ;
	}
}