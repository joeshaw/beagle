//
// FilterKonqHistory.cs
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
using System.Collections;
using System.IO;
using System.Text;

using Beagle.Daemon;
using Beagle.Util;

using HtmlAgilityPack;

namespace Beagle.Filters {

	public class FilterKonqHistory : Beagle.Filters.FilterHtml {

		// use a static buffer to prevent constant alloc and de-alloc
		private static byte[] buf = null;

		public FilterKonqHistory ()
		{
			RegisterSupportedTypes ();
		}

		override protected void DoOpen (FileInfo info)
		{
			if (buf == null)
				buf = new byte [1024];

			StreamReader reader = new StreamReader (Stream, Encoding.GetEncoding (28591));
			/*
			string url = null;
			string creation_date = null;
			string mimetype = null;
			string charset = null;
			bool is_ok = KonqHistoryUtil.ShouldIndex (reader, out url, out creation_date, out mimetype, out charset);
			if (!is_ok || url == String.Empty)
				Error ();

			//Console.WriteLine (url);
			// url, mimetype etc... have been all decided by the backend
			// we did all this just to get the charset in the filter
			*/

			// read the charset hint from indexable
			string charset = null;
			foreach (Property property in IndexableProperties) {
				if (property.Key != "charset")
					continue;
				charset = (string) property.Value;
				//Console.WriteLine ("charset hint accepted: " + charset);
				break;
			}
					

			// now create a memorystream where htmlfilter will begin his show
			Stream.Seek (0, SeekOrigin.Begin);
			// count past 8 lines ... Streams suck!
			int c = 0; // stores the number of newlines read
			int b = 0;
			while (c < 8 && (b = Stream.ReadByte ()) != -1) {
				if (b == '\n')
					c ++;
			}	
			// copy the rest of the file to a memory stream
			MemoryStream mem_stream = new MemoryStream ();
			while ((b = Stream.Read (buf, 0, 1024)) != 0)
				mem_stream.Write (buf, 0, b);
			mem_stream.Seek (0, SeekOrigin.Begin);
			reader.Close ();
			
			HtmlDocument doc = new HtmlDocument ();
			doc.ReportNode += HandleNodeEvent;
			doc.StreamMode = true;
			// we already determined encoding
			doc.OptionReadEncoding = false;
			Encoding enc = Encoding.UTF8;
			if (charset != null && charset != String.Empty)
			    enc = Encoding.GetEncoding (charset);
	
			try {
				if (enc == null)
					doc.Load (mem_stream);
				else
					doc.Load (mem_stream, enc);
			} catch (NotSupportedException e) {
				doc.Load (mem_stream, Encoding.ASCII);
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				Console.WriteLine (e.StackTrace);
			}

			Finished ();
		}

		override protected void RegisterSupportedTypes () 
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType (KonqHistoryUtil.KonqCacheMimeType));
		}
	}

}
