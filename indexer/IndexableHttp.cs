//
// IndexableHttp.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;

using Beagle.Filters;

namespace Beagle {

	public class IndexableHttp : Indexable {

		Filter filter;
		Stream stream;

		public IndexableHttp (String uri)
		{
			if (! uri.StartsWith ("http://"))
				throw new Exception ("Passed non-http: URI to IndexableHttp");
			Uri = uri;
			Type = "WebLink";
		}

		override protected void DoBuild ()
		{
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (Uri);
			req.UserAgent = "Beagle.IndexableHttp";

			HttpWebResponse resp = (HttpWebResponse) req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.OK)
				throw new Exception (String.Format ("{0} returned {1}: {2}",
								    Uri,
								    resp.StatusCode,
								    resp.StatusDescription));
			
			string mimeType = resp.ContentType;

			// Strip out the charset, etc.  (FIXME: Is that really a good idea?)
			int i = mimeType.IndexOf (";");
			if (i != -1)
				mimeType = mimeType.Substring (0, i);

			MimeType = mimeType;
			Timestamp = resp.LastModified;

			filter = Filter.FilterFromMimeType (mimeType);
			stream = resp.GetResponseStream ();
			filter.Open (stream);
			foreach (String key in filter.Keys)
				this [key] = filter [key];
			Content = filter.Content;
			HotContent = filter.HotContent;
			filter.Close ();
		}
	}
}
