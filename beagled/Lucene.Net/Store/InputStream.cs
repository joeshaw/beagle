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
	/// Abstract base class for input from a file in a Directory. A
	/// random-access input stream.  Used for all Lucene index input operations.
	/// <see cref="Directory"/> 
	/// <see cref="OutputStream"/>
	/// </summary>
	public abstract class InputStream : ICloneable 
	{
		internal static readonly int BUFFER_SIZE = OutputStream.BUFFER_SIZE;

		private byte[] buffer;
		private char[] chars;

		private long bufferStart = 0;			  // position in file of buffer
		private int bufferLength = 0;			  // end of valid bytes
		private int bufferPosition = 0;		  // next byte to read

		protected long length;			  // set by subclasses

		/// <summary>
		/// Reads and returns a single byte.
		/// <see cref="OutputStream.WriteByte(byte)"/>
		/// </summary>
		/// <returns></returns>
		public byte ReadByte()  
		{
			if (bufferPosition >= bufferLength)
				Refill();
			return buffer[bufferPosition++];
		}

		/// <summary>
		/// Reads a specified number of bytes into an array at the specified offset.
		/// <see cref="OutputStream.WriteBytes(byte[],int)"/>
		/// </summary>
		/// <param name="b">the array to read bytes into</param>
		/// <param name="offset">the offset in the array to start storing bytes</param>
		/// <param name="len">the number of bytes to read</param>
		public void ReadBytes(byte[] b, int offset, int len)
		{
			if (len < BUFFER_SIZE) 
			{
				for (int i = 0; i < len; i++)		  // read byte-by-byte
					b[i + offset] = (byte)ReadByte();
			} 
			else 
			{					  // read all-at-once
				long start = GetFilePointer();
				SeekInternal(start);
				ReadInternal(b, offset, len);

				bufferStart = start + len;		  // adjust stream variables
				bufferPosition = 0;
				bufferLength = 0;				  // trigger refill() on read
			}
		}

		/// <summary>
		/// Reads four bytes and returns an int.
		/// <see cref="OutputStream.WriteInt(int)"/>
		/// </summary>
		/// <returns></returns>
		public int ReadInt()  
		{
			return ((ReadByte() & 0xFF) << 24) | ((ReadByte() & 0xFF) << 16)
				| ((ReadByte() & 0xFF) <<  8) |  (ReadByte() & 0xFF);
		}

		/// <summary>
		/// Reads an int stored in variable-length format.  Reads between one and
		/// five bytes.  Smaller values take fewer bytes.  Negative numbers are not
		/// supported.
		/// <see cref="OutputStream.WriteVInt(int)"/>
		/// </summary>
		/// <returns></returns>
		public int ReadVInt()  
		{
			byte b = ReadByte();
			int i = b & 0x7F;
			for (int shift = 7; (b & 0x80) != 0; shift += 7) 
			{
				b = ReadByte();
				i |= (b & 0x7F) << shift;
			}
			return i;
		}

		/// <summary>
		/// Reads eight bytes and returns a long.
		/// <see cref="OutputStream.WriteLong(long)"/>
		/// </summary>
		/// <returns></returns>
		public long ReadLong()  
		{
			return (((long)ReadInt()) << 32) | (ReadInt() & 0xFFFFFFFFL);
		}

		/// <summary>
		/// Reads a long stored in variable-length format.  Reads between one and
		/// nine bytes.  Smaller values take fewer bytes.  Negative numbers are not
		/// supported. 
		/// </summary>
		/// <returns></returns>
		public long ReadVLong()  
		{
			byte b = ReadByte();
			long i = b & 0x7F;
			for (int shift = 7; (b & 0x80) != 0; shift += 7) 
			{
				b = ReadByte();
				i |= (b & 0x7FL) << shift;
			}
			return i;
		}

		/// <summary>
		/// Reads a string.
		/// <see cref="OutputStream.WriteString(String)"/>
		/// </summary>
		/// <returns></returns>
		public String ReadString()  
		{
			int _length = ReadVInt();
			if (chars == null || _length > chars.Length)
				chars = new char[_length];
			ReadChars(chars, 0, _length);
			return new String(chars, 0, _length);
		}

		/// <summary>
		/// Reads UTF-8 encoded characters into an array.
		/// <see cref="OutputStream.WriteChars(String,int,int)"/>
		/// </summary>
		/// <param name="buffer">the array to read characters into</param>
		/// <param name="start">the offset in the array to start storing characters</param>
		/// <param name="_length">the number of characters to read</param>
		public void ReadChars(char[] buffer, int start, int _length)
		{
			int end = start + _length;
			for (int i = start; i < end; i++) 
			{
				byte b = ReadByte();
				if ((b & 0x80) == 0)
					buffer[i] = (char)(b & 0x7F);
				else if ((b & 0xE0) != 0xE0) 
				{
					buffer[i] = (char)(((b & 0x1F) << 6)
						| (ReadByte() & 0x3F));
				} 
				else
					buffer[i] = (char)(((b & 0x0F) << 12)
						| ((ReadByte() & 0x3F) << 6)
						|  (ReadByte() & 0x3F));
			}
		}

		private void Refill()  
		{
			long start = bufferStart + bufferPosition;
			long end = start + BUFFER_SIZE;
			if (end > length)				  // don't read past EOF
				end = length;
			bufferLength = (int)(end - start);
			if (bufferLength == 0)
				throw new IOException("read past EOF");

			if (buffer == null)
				buffer = new byte[BUFFER_SIZE];		  // allocate buffer lazily
			ReadInternal(buffer, 0, bufferLength);

			bufferStart = start;
			bufferPosition = 0;
		}

		/// <summary>
		/// Expert: implements buffer refill.  Reads bytes from the current position
		/// in the input.
		/// </summary>
		/// <param name="b">the array to read bytes into</param>
		/// <param name="offset">the offset in the array to start storing bytes</param>
		/// <param name="_length">the number of bytes to read</param>
		protected abstract void ReadInternal(byte[] b, int offset, int _length);

		/// <summary>
		/// Closes the stream to futher operations.
		/// </summary>
		public abstract void Close() ;

		/// <summary>
		/// Returns the current position in this file, where the next read will
		/// occur.
		/// <see cref="Seek(long)"/>
		/// </summary>
		/// <returns></returns>
		public long GetFilePointer() 
		{
			return bufferStart + bufferPosition;
		}

		/// <summary>
		/// Sets current position in this file, where the next read will occur.
		/// <see cref="GetFilePointer()"/>
		/// </summary>
		/// <param name="pos"></param>
		public void Seek(long pos)  
		{
			if (pos >= bufferStart && pos < (bufferStart + bufferLength))
				bufferPosition = (int)(pos - bufferStart);  // seek within buffer
			else 
			{
				bufferStart = pos;
				bufferPosition = 0;
				bufferLength = 0;				  // trigger refill() on read()
				SeekInternal(pos);
			}
		}

		/// <summary>
		/// Expert: implements seek.  Sets current position in this file, where the
		/// next ReadInternal(byte[],int,int) will occur.
		/// <see cref="ReadInternal(byte[],int,int)"/>
		/// </summary>
		/// <param name="pos"></param>
		protected abstract void SeekInternal(long pos) ;

		/// <summary>
		/// The number of bytes in the file.
		/// </summary>
		/// <returns></returns>
		public long Length() 
		{
			return length;
		}

		/// <summary>
		/// Returns a clone of this stream.
		///
		/// <p>Clones of a stream access the same data, and are positioned at the same
		/// point as the stream they were cloned from.</p>
		///
		/// <p>Expert: Subclasses must ensure that clones may be positioned at
		/// different points in the input from each other and from the stream they
		/// were cloned from.</p>
		/// </summary>
		/// <returns></returns>
		public virtual Object Clone() 
		{
			InputStream clone = null;
			try 
			{
				clone = (InputStream) this.MemberwiseClone();
			} 
			catch (Exception e)
			{
				throw new Exception("Can't clone InputStream.", e);
			}

			if (buffer != null) 
			{
				clone.buffer = new byte[BUFFER_SIZE];
				Array.Copy(buffer, 0, clone.buffer, 0, bufferLength);
			}

			clone.chars = null;

			return clone;
		}
	}
}