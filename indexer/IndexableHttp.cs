//
// IndexableHttp.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;

using Dewey.Filters;

namespace Dewey {

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
			req.UserAgent = "Dewey.IndexableHttp";

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
