//
// Content.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

namespace Dewey {
    
    abstract public class Content {

	static Hashtable registry = new Hashtable ();
	
	static public void RegisterEverythingByHand () {
	    // FIXME: This is idiotic
	    Register (typeof (ContentText));
	    Register (typeof (ContentOpenOffice));
	}

	static public void Register (Type handler) {
	    if (! handler.IsSubclassOf (typeof (Content))) {
		// FIXME: throw exception: type not subclass of Content
	    }
	    
	    Content dummy = (Content) Activator.CreateInstance (handler);
	    foreach (String mime_type in dummy.HandledMimeTypes ()) {
		if (registry.Contains (mime_type)) {
		    // FIXME: throw exception: duplicated mime type
		}
		registry [mime_type] = handler;
	    }
	}

	static public Content Extract (String mime_type, Stream stream) {
	    Content content = null;
	    if (registry.Contains (mime_type)) {
		Type handler = registry [mime_type] as Type;
		content = Activator.CreateInstance (handler) as Content;
		
		if (! content.Read (stream))
		    return null;
	    }
	    return content;
	}

	//////////////////////////////

	Hashtable metadata = new Hashtable ();
	StringBuilder body;
	StringBuilder hot_body;

	public Content () { }

	abstract public String[] HandledMimeTypes ();
	abstract public bool Read (Stream content_stream);

	public void SetMetadata (String key, String value) {
	    metadata [key.ToLower ()] = value;
	}

	public void AppendBody (String _body) {
	    if (body == null)
		body = new StringBuilder ("");
	    else
		body.Append (" ");
	    body.Append (_body);
	}

	// Deprecated
	public void SetBody (String _body) {
	    body = null;
	    AppendBody (_body);
	}

	public void AppendHotBody (String _body) {
	    if (hot_body == null)
		hot_body = new StringBuilder ("");
	    else
		hot_body.Append (" ");
	    hot_body.Append (_body);
	}
	
	// Deprecated
	public void SetHotBody (String _body) {
	    hot_body = null;
	    AppendHotBody (_body);
	}

	public String Body {
	    get { return (body == null) ? null : body.ToString (); }
	}

	public String HotBody {
	    get { return (hot_body == null) ? null : hot_body.ToString (); }
	}

	public ICollection MetadataKeys {
	    get { return metadata.Keys; }
	}

	public String this [String key] {
	    get { return metadata [key.ToLower ()] as String; }
	}


    }

}
