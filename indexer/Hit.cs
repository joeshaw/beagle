//
// Hit.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

namespace Dewey {
    
	public class Hit : Versioned, IComparable {
	
		int id = 0; /* some sort of unique ID, or 0 if undefined */

		String uri;
		String domain = "unknown";
		String mimeType = "application/octet-stream";

		String source = "unknown";

		// Scores are comparable iff they have the same source.
		float score = 0;

		Hashtable metadata = new Hashtable ();

		bool locked = false;

		//////////////////////////

		public void lockdown ()
		{
			if (uri == null)
				throw new Exception ("Locking Hit with undefined URI");
			locked = true;
		}

		void checkLock ()
		{
			if (locked)
				throw new Exception ("Attempt to modify locked hit '" + uri + "'");
		}

		//////////////////////////

		public int Id {
			get { return id; }
			set { checkLock (); id = value; }
		}

		public String Uri {
			get { return uri; }
			set { checkLock (); uri = value; }
		}

		public String Domain {
			get { return domain; }
			set { checkLock (); domain = value; }
		}

		public String MimeType {
			get { return mimeType; }
			set { checkLock (); mimeType = value; }
		}
	
		public String Source {
			get { return source; }
			set { checkLock (); source = value; }
		}

		public float Score {
			get { return score; }
			set { checkLock (); score = value; }
		}

		//////////////////////////

		public ICollection MetadataKeys {
			get { return metadata.Keys; }
		}

		virtual public String this [String key] {
			get { return (String) metadata [key]; }
			set { checkLock (); metadata [key] = (String) value; }
		}

		//////////////////////////

		public int CompareTo (object obj)
		{
			Hit otherHit = (Hit) obj;
			int cmp = Source.CompareTo (otherHit.Source);
			if (cmp == 0)
				cmp = score.CompareTo (otherHit.score);
			return cmp;
		}

	}
}
