//
// ContentOpenOffice.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

namespace Dewey {

    public class ContentOpenOffice : Content {

	private class OpenOfficeReader : XmlContentReader {

	    public OpenOfficeReader (Stream stream, Content content) 
		: base (stream, content) { } 

	    protected override bool ThisElementIsHot () {
		return false;
	    }

	    protected override bool ThisElementIsCold () {
		return false;
	    }

	    protected override bool ThisEndElementWillFlush () {
		return false;
	    }
	}

	public ContentOpenOffice () : base () { }

	public override String[] HandledMimeTypes () {
	    return new String[] { "application/vnd.sun.xml.writer",
				  "application/vnd.sun.xml.impress" };
	}

	public override bool Read (Stream content_stream) {
	    ZipFile zip = new ZipFile (content_stream);
	    
	    ZipEntry entry;
	    Stream stream;

	    entry = zip.GetEntry ("content.xml");
	    stream = zip.GetInputStream (entry);
	    XmlContentReader xcr = new OpenOfficeReader (stream, this);
	    xcr.Debug = true;
	    xcr.DoWork ();
	    return true; // FIXME: should check for errors
	}

    }

}
