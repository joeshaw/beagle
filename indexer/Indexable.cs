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

	public class Indexable {

		protected String uri;
		protected String domain = "unknown";
		protected String mimeType = "application/octet-stream";
		protected DateTime timestamp = new DateTime (0);
		protected long revision = -1;
	
		Hashtable metadata = null;

		public String Uri { 
			get { return uri; }
		}

		public String Domain {
			get { return domain; }
		}

		public String MimeType {
			get { return mimeType; }
		}

		public DateTime Timestamp {
			get { return timestamp; }
		}

		public bool ValidTimestamp {
			get { return timestamp.Ticks > 0; }
		}

		public long Revision {
			get { return revision; }
		}

		public bool ValidRevision {
			get { return revision >= 0; }
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

		virtual public void Open () { }
	
		virtual public void Close () { }

	}

}
