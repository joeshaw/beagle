//
// Hit.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.Collections;
using System.IO;

namespace Beagle {
    
	public class Hit : Versioned, IComparable {

		// A unique ID.  id <= 0 means 'undefined'.
		private int id = 0;

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

		private String path = ""; // == uninitialized
		private FileInfo fileInfo = null;

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

		public int Id {
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

		public String Path {
			get {
				if (path == null)
					return null;

				if (path == "") {
					if (Uri.StartsWith ("file://")) {
						path = Uri.Substring ("file://".Length);
						if (! File.Exists (path))
							path = null;
					}
				}

				return path;
			}
		}

		// This will return false for file:// URIs if the referenced
		// file doesn't exist.
		public bool IsFile {
			get { return Path != null; }
		}

		public String FileName {
			get { return IsFile ? System.IO.Path.GetFileName (Path) : null; }
		}

		public String DirectoryName {
			get { return IsFile ? System.IO.Path.GetDirectoryName (Path) : null; }
		}

		public FileInfo FileInfo
		{
			get { 
				if (fileInfo == null && Path != null)
					fileInfo = new FileInfo (Path);
				return fileInfo;
			}
		}

		public bool IsUpToDate {
			get { return ! IsFile // non-files are always up-to-date
				      || ! ValidTimestamp // non-timestamped stuff too
				      || (FileInfo.LastWriteTime  <= Timestamp); }
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
