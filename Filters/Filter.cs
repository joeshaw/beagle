//
// Filter.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Dewey.Filters {

    abstract public class Filter {

	// Derived classes always must have a constructor that
	// takes no arguments.
	public Filter () { }

	//////////////////////////

	private   ArrayList supportedMimeTypes = new ArrayList ();
	protected String mimeType;

	protected void AddSupportedMimeType (String _mimeType) {
	    supportedMimeTypes.Add (_mimeType);
	}

	public IEnumerable SupportedMimeTypes {
	    get { return supportedMimeTypes; }
	}

	public String MimeType {
	    get { return mimeType; }
	}

	//////////////////////////

	StringBuilder content;
	StringBuilder hot;
	Hashtable     metadata;
	int           hotCount;
	int           freezeCount;

	protected void HotUp () {
	    ++hotCount;
	}

	protected void HotDown () {
	    if (hotCount > 0) {
		--hotCount;
		if (hotCount == 0)
		    BuilderAppendWhitespace (hot);
	    }
	}

	protected void FreezeUp () {
	    ++freezeCount;
	}

	protected void FreezeDown () {
	    if (freezeCount > 0)
		--freezeCount;
	}

	protected void AppendContent (String str) {
	    if (freezeCount == 0 && str != null) {
		if (content == null)
		    content = new StringBuilder ("");
		content.Append (str);
		if (hotCount > 0) {
		    if (hot == null)
			hot = new StringBuilder ("");
		    hot.Append (str);
		}
	    }
	}

	static void BuilderAppendWhitespace(StringBuilder builder) {
	    if (builder != null)
		builder.Append (" ");
	}

	protected void AppendWhiteSpace () {
	    BuilderAppendWhitespace (content);
	    BuilderAppendWhitespace (hot);
	}

	protected void SetMetadata (String key, String val) {
	    key = key.ToLower ();
	    if (key == null)
		throw new Exception ("Metadata keys may not be null");
	    if (metadata.Contains (key))
		throw new Exception ("Clobbering metadata " + key);
	    if (val != null)
		metadata[key] = val;
	}

	//////////////////////////

	static String CleanUp (StringBuilder builder) {
	    if (builder == null)
		return null;
	    String str = builder.ToString ();
	    str = Regex.Replace (str, "\\s{2,}", " ");
	    str = str.Trim ();
	    return str;
	}

	public String Content {
	    get { return CleanUp (content); }
	}

	public String HotContent {
	    get { return CleanUp (hot); }
	}

	public ICollection MetadataKeys {
	    get { return metadata.Keys; }
	}

	public String this [String key] {
	    get { return metadata[key.ToLower ()] as String; }
	}

	//////////////////////////

	abstract protected void Read (Stream stream);

	//////////////////////////

	public void Open (Stream stream) {
	    content = null;
	    hot = null;
	    metadata = new Hashtable ();
	    hotCount = 0;
	    freezeCount = 0;

	    if (stream != null)
		Read (stream);
	}

	public void Open (String path) {
	    Stream stream = new FileStream (path,
					    FileMode.Open,
					    FileAccess.Read);
	    Open (stream);
	    stream.Close ();
	}

	public void Close () {
	    content = null;
	    hot = null;
	}

	//////////////////////////

	// FIXME: This is idiotic.
	static private String GuessMimeTypeFromPath (String path) {

	    String ext = Path.GetExtension (path);
	    if (ext == null)
		return null;
	    
	    switch (ext.ToLower ()) {

		case ".txt":
		    return "text/plain";

		case ".html":
		case ".htm":
		    return "text/html";

		case ".pdf":
		    return "application/pdf";

		case ".sxw":
		    return "application/vnd.sun.xml.writer";

		case ".sxi":
		    return "application/vnd.sun.xml.impress";

		case ".sxc":
		    return "application/vnd.sun.xml.calc";

		case ".gif":
		    return "image/gif";

		case ".png":
		    return "image/png";

		case ".jpg":
		case ".jpeg":
		    return "image/jpeg";

		default:
		    return null;

	    }
	}

	//////////////////////////

	static Hashtable registry;

	static private void AutoRegisterFilters () {
	    Assembly a = Assembly.GetAssembly (typeof (Filter));
	    foreach (Type t in a.GetTypes ()) {
		if (t.IsSubclassOf (typeof (Filter))) {
		    Filter filter = (Filter) Activator.CreateInstance (t);
		    foreach (String mimeType in filter.SupportedMimeTypes) {
			if (registry.Contains(mimeType)) {
			    String estr = "Mime Type Collision: " + mimeType;
			    throw new Exception (estr);
			}
			registry[mimeType] = t;
		    }
		}
	    }
	}

	static public Filter FilterFromMimeType (String _mimeType) {
	    if (registry == null) {
		registry = new Hashtable ();
		AutoRegisterFilters ();
	    }

	    if (! registry.Contains (_mimeType))
		throw new Exception ("Unsupported mime type: " + _mimeType);

	    Type t = (Type) registry[_mimeType];

	    Filter filter = (Filter) Activator.CreateInstance (t);
	    filter.mimeType = _mimeType;

	    return filter;
	}

	static public Filter FilterFromPath (String path) {
	    String mime_type = GuessMimeTypeFromPath (path);
	    if (mime_type == null) 
		return null; // Can't guess Mime Type
	    return FilterFromMimeType (mime_type);
	}
    }
}
