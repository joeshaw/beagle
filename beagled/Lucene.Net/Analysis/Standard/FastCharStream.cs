using System;
using System.IO;

namespace Lucene.Net.Analysis.Standard
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

	/// An efficient implementation of JavaCC's CharStream interface.  
	/// <p>Note that
	/// this does not do line-number counting, but instead keeps track of the
	/// character position of the token in the input, as required by Lucene's 
	/// Lucene.Net.Analysis.Token API. 
	/// </p>
	public sealed class FastCharStream : CharStream
	{
		char[] buffer = null;

		int bufferLength = 0;					// end of valid chars
		int bufferPosition = 0;					// next char to read
  
		int tokenStart = 0;						// offset in buffer
		int bufferStart = 0;					// position in file of buffer

		TextReader input;						// source of chars

		/// <summary>
		/// Constructs from a TextReader. 
		/// </summary>
		/// <param name="r"></param>
		public FastCharStream(TextReader r) 
		{
			input = r;
		}

		public char ReadChar()  
		{
			if (bufferPosition >= bufferLength)
				Refill();
			return buffer[bufferPosition++];
		}

		private void Refill()  
		{
			int newPosition = bufferLength - tokenStart;

			if (tokenStart == 0) 
			{			  // token won't fit in buffer
				if (buffer == null) 
				{			  // first time: alloc buffer
					buffer = new char[2048];		  
				} 
				else if (bufferLength == buffer.Length) 
				{ // grow buffer
					char[] newBuffer = new char[buffer.Length*2];
					Array.Copy(buffer, 0, newBuffer, 0, bufferLength);
					buffer = newBuffer;
				}
			} 
			else 
			{					  // shift token to front
				Array.Copy(buffer, tokenStart, buffer, 0, newPosition);
			}

			bufferLength = newPosition;			  // update state
			bufferPosition = newPosition;
			bufferStart += tokenStart;
			tokenStart = 0;

			int charsRead = 0;
			
			try
			{
				charsRead =				  // fill space in buffer
					input.Read(buffer, newPosition, buffer.Length-newPosition);
			}
			catch
			{
			}
			
			if (charsRead == 0)
				throw new IOException("read past eof");
			else
				bufferLength += charsRead;
		}

		public char BeginToken()  
		{
			tokenStart = bufferPosition;
			return ReadChar();
		}

		public void Backup(int amount) 
		{
			bufferPosition -= amount;
		}

		public String GetImage() 
		{
			return new String(buffer, tokenStart, bufferPosition - tokenStart);
		}

		public char[] GetSuffix(int len) 
		{
			char[] value = new char[len];
			Array.Copy(buffer, bufferPosition - len, value, 0, len);
			return value;
		}

		public void Done() 
		{
			try 
			{
				input.Close();
			} 
			catch (IOException e) 
			{
				Console.Error.WriteLine("Caught: " + e + "; ignoring.");
			}
		}

		public int GetColumn() 
		{
			return bufferStart + bufferPosition;
		}
		public int GetLine() 
		{
			return 1;
		}
		public int GetEndColumn() 
		{
			return bufferStart + bufferPosition;
		}
		public int GetEndLine() 
		{
			return 1;
		}
		public int GetBeginColumn() 
		{
			return bufferStart + tokenStart;
		}
		public int GetBeginLine() 
		{
			return 1;
		}
	}
}