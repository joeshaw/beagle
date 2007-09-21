//
// WebServer.cs
//
// This class knows the logic behind handling all the static web pages for webbeagle.
//
// Copyright (C) 2007 Debajyoti Bera <dbera.web@gmail.com>
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

using System.IO;
using System.Collections.Generic;
using System.Net;

using Beagle.Util;

namespace Beagle.Daemon {

	class WebServer {

		struct PageMapping {
			public string Filename;
			public string ContentType;

			public PageMapping (string filename, string content_type)
			{
				this.Filename = filename;
				this.ContentType = content_type;
			}
		}

		private static Dictionary<string, PageMapping> mappings;

		// FIXME: (If and) when released, this should be changed to ExternalStringsHack.SysConfDir+something
		// FIXME FIXME: Keep security always in mind. The doggy doesn't like to be blamed :-X
		const string WEBSERVER_DIR = "webinterface";

		static WebServer ()
		{
			mappings = new Dictionary<string, PageMapping> ();

			mappings.Add ("/", new PageMapping ("query.html", "text/html; charset=utf-8"));
			mappings.Add ("/queryresult.xsl", new PageMapping ("queryresult.xsl", "application/xml; charset=utf-8"));
			mappings.Add ("/default.css", new PageMapping ("default.css", "text/css"));
			// If E4X is needed, change the content-type here
			mappings.Add ("/default.js", new PageMapping ("default.js", "text/javascript"));
			mappings.Add ("/title_bg.png", new PageMapping ("title_bg.png", "image/png"));
			mappings.Add ("/beagle-logo.png", new PageMapping ("beagle-logo.png", "image/png"));
		}

		static byte[] buffer = new byte [1024];

		internal static void HandleStaticPages (HttpListenerContext context)
		{
			Log.Debug ("GET request:" + context.Request.RawUrl);
			context.Response.KeepAlive = false;
			context.Response.StatusCode = (int) HttpStatusCode.OK;

			if (! mappings.ContainsKey (context.Request.RawUrl)) {
				context.Response.StatusCode = 404;
				context.Response.Close ();
				return;
			}

			// Else serve the page
			PageMapping mapping = mappings [context.Request.RawUrl];
			context.Response.ContentType = mapping.ContentType;

			string path = Path.Combine (WEBSERVER_DIR, mapping.Filename);

			using (BinaryReader r = new BinaryReader (new FileStream (path, FileMode.Open, FileAccess.Read))) {
				using (BinaryWriter w = new BinaryWriter (context.Response.OutputStream)) {

					int count = 1024;
					while (count == 1024) {
						count = r.Read (buffer, 0, count);
						if (count == 0)
							break;
						w.Write (buffer, 0, count);
					}
				}
			}

			context.Response.Close ();
		}
	}
}
