
using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Dewey {
    
    abstract public class Content {

	static Hashtable registry = new Hashtable();

	static public void Register(Type handler) {
	    if (! handler.IsSubclassOf(typeof(Content))) {
		// throw exception: type not subclass of Content
	    }
	    
	    Content dummy = (Content)Activator.CreateInstance(handler);
	    foreach (String mime_type in dummy.HandledMimeTypes()) {
		if (registry.Contains(mime_type)) {
		    // throw exception: duplicated mime type
		}
		registry[mime_type] = handler;
	    }
	}

	static public Content Extract(String mime_type, Stream stream) {
	    Content content = null;
	    if (registry.Contains(mime_type)) {
		Type handler = registry[mime_type] as Type;
		content = Activator.CreateInstance(handler) as Content;
		
		if (! content.Read(stream))
		    return null;
	    }
	    return content;
	}

	//////////////////////////////

	Hashtable metadata = new Hashtable();
	String body;

	public Content() { }

	abstract public String[] HandledMimeTypes();
	abstract public bool Read(Stream content_stream);

	protected void SetMetadata(String key, String value) {
	    metadata[key.ToLower()] = value;
	}

	protected void SetBody(String _body) {
	    if (body != null) {
		// FIXME: complain that you can't set the body twice
	    }
	    body = _body;
	}

	public ICollection MetadataKeys {
	    get { return metadata.Keys; }
	}

	public String this[String key] {
	    get { return metadata[key.ToLower()] as String; }
	}

	public String Body {
	    get { return body; }
	}

    }

}
