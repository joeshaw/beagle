using System;
using System.Globalization;

namespace Lucene.Net.Util
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
	/// A simple class for number conversions.
	/// </summary>
	public class Number
	{
		/// <summary>
		/// Min radix value.
		/// </summary>
		public const int MIN_RADIX = 2;
		/// <summary>
		/// Max radix value.
		/// </summary>
		public const int MAX_RADIX = 36;

		private const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";

		public static string ToString(float f)
		{
			if (((float)(int)f) == f)
			{
				return ((int)f).ToString() + ".0";
			}
			else
			{
				return f.ToString(NumberFormatInfo.InvariantInfo);
			}
		}

		/// <summary>
		/// Converts a number to string in the specified radix.
		/// </summary>
		/// <param name="i">A number to be converted.</param>
		/// <param name="radix">A radix.</param>
		/// <returns>A string representation of the number in the specified redix.</returns>
		public static string ToString(long i, int radix)
		{
			if (radix < MIN_RADIX || radix > MAX_RADIX)
				radix = 10;

			char[] buf = new char[65];
			int charPos = 64;
			bool negative = (i < 0);

			if (!negative) 
			{
				i = -i;
			}

			while (i <= -radix) 
			{
				buf[charPos--] = digits[(int)(-(i % radix))];
				i = i / radix;
			}
			buf[charPos] = digits[(int)(-i)];

			if (negative) 
			{
				buf[--charPos] = '-';
			}

			return new String(buf, charPos, (65 - charPos)); 
		}

		/// <summary>
		/// Parses a number in the specified radix.
		/// </summary>
		/// <param name="s">An input string.</param>
		/// <param name="radix">A radix.</param>
		/// <returns>The parsed number in the specified radix.</returns>
		public static long Parse(string s, int radix)
		{
			if (s == null) 
			{
				throw new ArgumentException("null");
			}

			if (radix < MIN_RADIX) 
			{
				throw new NotSupportedException("radix " + radix +
					" less than Number.MIN_RADIX");
			}
			if (radix > MAX_RADIX) 
			{
				throw new NotSupportedException("radix " + radix +
					" greater than Number.MAX_RADIX");
			}

			long result = 0;
			long mult = 1;

			s = s.ToLower();
			
			for(int i=s.Length-1; i>=0; i--)
			{
				int weight = digits.IndexOf(s[i]);
				if(weight == -1)
					throw new FormatException("Invalid number for the specified radix");

				result += (weight * mult);
				mult *= radix;
			}

			return result;
		}
	}

	public class Date
	{
		static public long GetTime(DateTime dateTime)
		{
			TimeSpan ts = dateTime.Subtract(new DateTime(1970, 1, 1));
			ts = ts.Subtract(TimeZone.CurrentTimeZone.GetUtcOffset(dateTime));
			return ts.Ticks / TimeSpan.TicksPerMillisecond;
		}
	}
}