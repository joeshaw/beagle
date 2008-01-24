//
// KonqHistoryUtil.cs
//
// Copyright (C) 2005 Debajyoti Bera <dbera.web@gmail.com>
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

namespace Beagle.Util {
	public class KonqHistoryUtil {
		public const string KonqCacheMimeType = "beagle/x-konq-cache";

		public static bool ShouldIndex (StreamReader reader,
					  out string url,
					  out string creation_date,
					  out string mimetype,
					  out string charset)
		{
			// format from kdelibs/kioslave/http/http.cc
			// line-1: Cache revision - mine is 7
			// FIXME: What happens when cache revision changes ???
			reader.ReadLine ();

			// line-2: URL
			url = reader.ReadLine ();

			// line-3: creation date
			creation_date = reader.ReadLine ();

			// line-4: expiry date
			// dont need
			reader.ReadLine ();

			// line-5: ETag
			// dont need
			reader.ReadLine ();

			// line-6: last-modified
			// dont need
			reader.ReadLine ();

			// line-7: mimetype for the data
			// important stuff
			// we only index text/plain and text/html - anything else ?
			mimetype = reader.ReadLine ();

			// line-8: charset for the rest data
			// important stuff
			charset = reader.ReadLine ();
			
			/*
			Console.WriteLine ("FilterKonqHistory:" + 
					    " url=" + url +
					    " date=" + creation_date +
					    " mimetype=" + mimetype +
					    " charset=" + charset);
			*/
			// rest is data ...
			return (mimetype == "text/html");
		}
	}
}
