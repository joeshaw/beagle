using System;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
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
	/// Provides support for converting dates to strings and vice-versa.
	/// The strings are structured so that lexicographic sorting orders by date.
	/// This makes them suitable for use as field values and search terms.
	/// <P>
	/// Note: currenly dates before 1970 cannot be used, and therefore cannot be
	/// indexed.</P>
	/// </summary>
	public class DateField 
	{
		private DateField() {}

		/// <summary>
		/// make date strings long enough to last a millenium
		/// </summary>
		private static int DATE_LEN = Lucene.Net.Util.Number.ToString(
			1000L*365*24*60*60*1000,
			Lucene.Net.Util.Number.MAX_RADIX).Length;

		public static String MIN_DATE_STRING() 
		{
			return TimeToString(0);
		}

		public static String MAX_DATE_STRING() 
		{
			char[] buffer = new char[DATE_LEN];
			char c = 'F';
			for (int i = 0 ; i < DATE_LEN; i++)
				buffer[i] = c;
			return new string(buffer);
		}

		/// <summary>
		/// Converts a Date to a string suitable for indexing.
		/// This method will throw a Exception if the date specified in the
		/// method argument is before 1970.
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public static String DateToString(DateTime date) 
		{
			return TimeToString(Date.GetTime(date));
		}

		/// <summary>
		/// Converts a millisecond time to a string suitable for indexing.
		/// This method will throw a Exception if the time specified in the
		/// method argument is negative, that is, before 1970.
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static String TimeToString(long time) 
		{
			if (time < 0)
				throw new Exception("time too early");

			string s = Lucene.Net.Util.Number.ToString(
				time, Lucene.Net.Util.Number.MAX_RADIX
			);

			if (s.Length > DATE_LEN)
				throw new Exception("time too late");

			while (s.Length < DATE_LEN)
				s = "0" + s;				  // pad with leading zeros

			// Pad with leading zeros
			if (s.Length < DATE_LEN) 
			{
				StringBuilder sb = new StringBuilder(s);
				while (sb.Length < DATE_LEN)
					sb.Insert(0, 0);
				s = sb.ToString();
			}

			return s;
		}

		/// <summary>
		/// Converts a string-encoded date into a millisecond time.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static long StringToTime(String s) 
		{
			return Lucene.Net.Util.Number.Parse(s, Lucene.Net.Util.Number.MAX_RADIX);
		}

		/// <summary>
		/// Converts a string-encoded date into a Date object.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static DateTime StringToDate(String s) 
		{
			long ticks = StringToTime(s) * TimeSpan.TicksPerMillisecond;
			DateTime date = new DateTime(ticks);
			date = date.Add(TimeZone.CurrentTimeZone.GetUtcOffset(date));
			date = date.AddYears(1969);
			return date;
		}
	}
}