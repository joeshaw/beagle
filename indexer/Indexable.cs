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

	public class Indexable : Versioned, IDisposable {

		// The URI of the item being indexed.
		private String uri = null;
		
		// File, WebLink, MailMessage, IMLog, etc.
		private String type = null;

		// If applicable, otherwise set to null.
		private String mimeType = null;

		private Hashtable properties = new Hashtable (new CaseInsensitiveHashCodeProvider (),
							      new CaseInsensitiveComparer ());

		private String content = null;
		private String hotContent = null;

		private bool built = false;
		private bool locked = false;

		//////////////////////////

		public void Lockdown ()
		{
			if (uri == null)
				throw new Exception ("Locking Hit with undefined Uri");
			if (type == null)
				throw new Exception ("Locking Indexable with undefined Type");
			locked = true;
		}

		void CheckLock ()
		{
			if (locked)
				throw new Exception ("Attempt to modify locked indexable '" + uri + "'");
		}
		
		//////////////////////////
		
		virtual protected void DoBuild () { }

		public void Build ()
		{
			if (! built) {
				bool lockedSave = locked;
				locked = false;
				DoBuild ();
				locked = lockedSave;
				built = true;
			}
		}

		//////////////////////////

		public String Uri { 
			get { return uri; }
			set { CheckLock (); uri = value; }
		}

		public String Type {
			get { return type; }
			set { CheckLock ();  type = value; }
		}

		public String MimeType {
			get { return mimeType; }
			set { CheckLock (); mimeType = value; }
		}

		//////////////////////////

		public IDictionary Properties {
			get { return properties; }
		}

		public ICollection Keys {
			get { return properties.Keys; }
		}

		public String this [String key] {
			get { return (String) properties [key]; }
			set { CheckLock (); properties [key] = value as String; }
		}

		public String PropertiesAsString {
			get {
				StringBuilder propStr = null;
				foreach (String key in Keys) {
					if (propStr == null)
						propStr = new StringBuilder ("");
					else
						propStr.Append (" ");
					propStr.Append (this [key]);
				}
				return propStr == null ? "" : propStr.ToString ();
			}
		}

		//////////////////////////

		virtual public String Content {
			get { return content; }
			set { CheckLock (); content = value; }
		}

		virtual public String HotContent {
			get { return hotContent; }
			set { CheckLock (); hotContent = value; }
		}

		//////////////////////////

		public void Dispose ()
		{
			properties = null;
			content = null;
			hotContent = null;
			built = false;
		}
	}

}
