
using System;
using System.Collections;
using System.IO;

namespace Dewey {

    public class ContentText : Content {

	public ContentText () : base () { }

	public override String[] HandledMimeTypes () {
	    return new String[] { "text/plain" };
	}

	public override bool Read (Stream content_stream) {
	    StreamReader sr = new StreamReader (content_stream);
	    String body = sr.ReadToEnd ();
	    AppendBody (body);
	    return true;
	}
    }

}
