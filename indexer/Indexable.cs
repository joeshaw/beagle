//
// Indexable.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Dewey {

	public class Indexable : Versioned {

		protected String uri;
		protected String domain = "unknown";
		protected String mimeType = "application/octet-stream";

		protected bool needPreload = true;

		Hashtable metadata = null;
		bool preloaded = false;

		public String Uri { 
			get { return uri; }
		}

		public String Domain {
			get { return domain; }
		}

		public String MimeType {
			get { return mimeType; }
		}

		virtual public ICollection MetadataKeys {
			get { return metadata == null ? new String [0] : metadata.Keys; }
		}

		virtual public String this [String key] {
			get { return metadata == null ? null : metadata [key.ToLower ()] as String; }
		}

		virtual protected void SetMetadata (String key, String val) {
			if (metadata == null)
				metadata = new Hashtable ();
			metadata [key.ToLower ()] = val;
		}

		virtual public String Content {
			get { return null; }
		}

		virtual public String HotContent {
			get { return null; }
		}

		public String Metadata {
			get {
				StringBuilder meta = null;
				foreach (String key in MetadataKeys) {
					if (meta == null)
						meta = new StringBuilder ("");
					else
						meta.Append (" ");
					meta.Append (this [key]);
				}
				return meta == null ? null : meta.ToString ();
			}
		}

		public bool NeedPreload {
			get { return needPreload; }
		}

		public void Preload ()
		{
			if (NeedPreload && ! preloaded) {
				// Do some locking and stuff
				DoPreload ();
				preloaded = true;
			}
		}
		
		// Do any slow, blocking operations in DoPreload.  Before
		// Preload (and hence DoPreload) is called, a consumer can't
		// assume that Indexables contain any information other than
		// the domain and the Uri.
		virtual public void DoPreload () { }

		virtual public void Open () { }
	
		virtual public void Close () { }

	}

}
