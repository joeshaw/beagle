
using System;
using System.Collections;

using Lucene.Net.Documents;

namespace Dewey {

    public class IndexItem {

	private   String    uri;
	protected String    mime_type;
	protected String    md5;
	protected DateTime  timestamp;
	protected Hashtable metadata;

	public String URI {
	    get { return uri; }
	}

	public String MimeType {
	    get { return mime_type; }
	}

	public String MD5 {
	    get { return md5; }
	}

	public DateTime Timestamp {
	    get { return timestamp; }
	}

	public ICollection MetadataKeys {
	    get { return metadata.Keys; }
	}

	public String this[String key] {
	    get { return metadata[key.ToLower ()] as String; }
	}

	public bool IsSupercededBy (IndexItem item) {
	    if (uri != item.uri)
		return false;
	    if (md5 != null 
		&& item.md5 != null 
		&& md5 == item.md5)
		return false;
	    if (timestamp >= item.timestamp)
		return false;
	    return true;
	}

	protected IndexItem (String _uri) {
	    uri = _uri;
	}

	// Map Lucene documents to IndexItems
	public IndexItem (Document doc) {
	    Field f;

	    f = doc.GetField ("URI");
	    if (f == null) {
		// FIXME: complain
	    }
	    uri = f.StringValue ();

	    f = doc.GetField ("MimeType");
	    if (f == null) {
		// FIXME: complain
	    }
	    mime_type = f.StringValue ();

	    f = doc.GetField ("MD5");
	    if (f != null)
		md5 = f.StringValue ();

	    f = doc.GetField ("Timestamp");
	    if (f != null) {
		long ticks = Convert.ToInt64 (f.StringValue ());
		timestamp = new DateTime (ticks);
	    }

	    foreach (Field ff in doc.Fields ()) {
		String key = ff.Name ();
		if (key == key.ToLower ()) {
		    if (metadata.Contains (key)) {
			// FIXME: complain
		    }
		    metadata[key] = ff.StringValue ();
		}
	    }
	    
	}

    }

}
