//
// Hit.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

namespace Dewey {
    
	public class Hit : Versioned, IComparable {

		// A unique ID.  id <= 0 means 'undefined'.
		private long id = 0;

		// A URI we can use to locate the source of this match.
		private String uri = null;

		// File, Web, MailMessage, IMLog, etc.
		private String type = null;

		// If applicable, otherwise set to null.
		private String mimeType = null;

		// IndexUser, IndexSystem, Google, Addressbook, iFolder, etc.
		private String source = null;

		// High scores imply greater relevance.
		private float score = 0.0f;

		private Hashtable properties = new Hashtable (new CaseInsensitiveHashCodeProvider (), 
							      new CaseInsensitiveComparer ());

		private bool locked = false;

		//////////////////////////

		public void Lockdown ()
		{
			if (uri == null)
				throw new Exception ("Locking Hit with undefined Uri");
			if (type == null)
				throw new Exception ("Locking Hit with undefined Type");
			if (source == null)
				throw new Exception ("Locking Hit with undefined Source");
			locked = true;
		}

		void CheckLock ()
		{
			if (locked)
				throw new Exception ("Attempt to modify locked hit '" + uri + "'");
		}

		//////////////////////////

		public long Id {
			get { return id; }
			set { CheckLock (); id = value; }
		}

		public String Uri {
			get { return uri; }
			set { CheckLock (); uri = value; }
		}

		public String Type {
			get { return type; }
			set { CheckLock (); type = value; }
		}

		public String MimeType {
			get { return mimeType; }
			set { CheckLock (); mimeType = value; }
		}
	
		public String Source {
			get { return source; }
			set { CheckLock (); source = value; }
		}

		public float Score {
			get { return score; }
			set { CheckLock (); score = value; }
		}

		//////////////////////////

		public IDictionary Properties {
			get { return properties; }
		}

		public ICollection Keys {
			get { return properties.Keys; }
		}

		virtual public String this [String key] {
			get { return (String) properties [key]; }
			set { 
				CheckLock (); 
				if ((value == null || value == "")
				    && ! properties.Contains (key))
					return;
				if (value == "")
					value = null;
				properties [key] = value as String;
			}
		}

		//////////////////////////

		public int CompareTo (object obj)
		{
			Hit otherHit = (Hit) obj;
			int cmp = Source.CompareTo (otherHit.Source);
			if (cmp == 0) {
				// Notice that we take the negative of the CompareTo,
				// so that we sort from high to low.
				cmp = - score.CompareTo (otherHit.score);
			}
			return cmp;
		}

	}
}
