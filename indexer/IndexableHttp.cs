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

		public IndexableHttp (String _uri)
		{
			if (! _uri.StartsWith ("http://"))
				throw new Exception ("Passed non-http: URI to IndexableHttp");
			uri = _uri;
			domain = "Web";
		}

		override public ICollection MetadataKeys {
			get { return filter.MetadataKeys; }
		}

		override public String this [String key] {
			get { return filter [key]; }
		}

		override public String Content {
			get { return filter.Content; }
		}

		override public String HotContent {
			get { return filter.HotContent; }
		}

		override public void DoPreload ()
		{
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (Uri);
			req.UserAgent = "Dewey.IndexableHttp";

			HttpWebResponse resp = (HttpWebResponse) req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.OK)
				throw new Exception (String.Format ("{0} returned {1}: {2}",
								    resp.StatusCode,
								    resp.StatusDescription));
			
			string type = resp.ContentType;

			// Strip out the charset.  (FIXME: Is that really a good idea?)
			int i = type.IndexOf (";");
			if (i != -1)
				type = type.Substring (0, i);

			mimeType = type;
			timestamp = resp.LastModified;

			filter = Filter.FilterFromMimeType (mimeType);
			stream = resp.GetResponseStream ();
		}

		override public void Open ()
		{
			filter.Open (stream);
		}

		override public void Close ()
		{
			filter.Close ();
		}
	}
}
