
using System;
using System.IO;
using System.Text;

using Lucene.Net.Documents;

namespace Dewey {

    public class IndexItemWithPayload : IndexItem {

	bool payload_attached = false;
	String file_path;
	Stream payload_stream;

	public IndexItemWithPayload(String uri): base(uri) {	    
	}

	public void AttachFile(String path, String _mime_type) {
	    if (payload_attached) {
		// FIXME: complain
		return;
	    }

	    // FIXME: check the file exists, etc.

	    mime_type = _mime_type;
	    timestamp = File.GetLastWriteTime(path);
	    file_path = path;

	    payload_attached = true;
	}

	public void AttachFile(String path) {
	    // FIXME: sniff mime type
	    AttachFile(path, "text/plain");
	}

	public Stream OpenPayloadStream() {
	    if (! payload_attached) {
		// FIXME: complain
		return null;
	    }
	    if (payload_stream == null) {

		if (file_path != null) {
		    // FIXME: check for errors
		    payload_stream = new FileStream(file_path,
						    FileMode.Open,
						    FileAccess.Read);
		} else {
		    // FIXME: complain
		    Console.WriteLine("Oops!");
		}
	    }
	    
	    return payload_stream;
	}

	public void ClosePayloadStream() {
	    if (payload_stream != null) {
		payload_stream.Close();
		payload_stream = null;
	    }
	}

	public Document ToDocument() {
	    Document doc = new Document();

	    Field f;
	    
	    f = Field.Keyword("URI", URI);
	    doc.Add(f);

	    f = Field.UnIndexed("MimeType", MimeType);
	    doc.Add(f);

	    if (MD5 != null) {
		f = Field.UnIndexed("MD5", MD5);
		doc.Add(f);
	    }

	    f = Field.UnIndexed("Timestamp",
				Convert.ToString(Timestamp.Ticks));
	    doc.Add(f);

	    Stream stream = OpenPayloadStream();
	    Content c = Content.Extract(MimeType, stream);
	    ClosePayloadStream();

	    StringBuilder body = new StringBuilder(c.Body);

	    foreach (String key in c.MetadataKeys) {
		String val = c[key];
		
		f = Field.Text(key, val);
		doc.Add(f);

		if (body.Length > 0)
		    body.Append(" ");
		body.Append(val);
	    }

	    f = Field.UnStored("Body", body.ToString());
	    doc.Add(f);

	    return doc;
	}

	
    }

}
