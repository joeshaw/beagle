//
// Hit.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using System.IO;

namespace Beagle {
    
	public class Hit : Versioned, IComparable {

		// A unique ID.  id <= 0 means 'undefined'.
		private int id = 0;

		// A URI we can use to locate the source of this match.
		private string uri = null;

		// File, Web, MailMessage, IMLog, etc.
		private string type = null;

		// If applicable, otherwise set to null.
		private string mimeType = null;

		// IndexUser, IndexSystem, Google, Addressbook, iFolder, etc.
		private string source = null;

		// High scores imply greater relevance.
		private float scoreRaw = 0.0f;
		private float scoreMultiplier = 1.0f;

		private Hashtable properties = new Hashtable (new CaseInsensitiveHashCodeProvider (), 
							      new CaseInsensitiveComparer ());

		private Hashtable data = new Hashtable (new CaseInsensitiveHashCodeProvider (), 
							new CaseInsensitiveComparer ());

		
		private enum SpecialType {
			Unknown,
			None,
			Invalid,
			File,
			Directory
		}

		SpecialType special = SpecialType.Unknown;

		private string path;
		private FileInfo fileInfo = null;
		private DirectoryInfo directoryInfo = null;

		//////////////////////////

		public int Id {
			get { return id; }
			set { id = value; }
		}

		public string Uri {
			get { return uri; }
			set { uri = value; }
		}

		public string Type {
			get { return type; }
			set { type = value; }
		}

		public string MimeType {
			get { return mimeType; }
			set { mimeType = value; }
		}
	
		public string Source {
			get { return source; }
			set { source = value; }
		}

		public float Score {
			get { return scoreRaw * scoreMultiplier; }
		}

		public float ScoreRaw {
			get { return scoreRaw; }
			set { scoreRaw = value; }
		}

		public float ScoreMultiplier {
			get { return scoreMultiplier; }
			set { scoreMultiplier = value; }
		}

		//////////////////////////
		
		private void SpecialHandling ()
		{
			if (special != SpecialType.Unknown)
				return;

			System.Uri uri = new System.Uri (Uri);
			if (uri.IsFile) {
				path = uri.LocalPath;
				if (File.Exists (path))
					special = SpecialType.File;
				else if (Directory.Exists (path))
					special = SpecialType.Directory;
				else
					special = SpecialType.Invalid;
			}
			
			if (special == SpecialType.Unknown)
				special = SpecialType.None;
		}

		public bool IsValid {
			get { SpecialHandling (); return special != SpecialType.Invalid; }
		}

		public bool IsFile {
			get { SpecialHandling (); return special == SpecialType.File; }
		}

		public bool IsDirectory {
			get { SpecialHandling (); return special == SpecialType.Directory; }
		}

		public bool IsFileSystem {
			get { return IsFile || IsDirectory; }
		}

		public string Path {
			get { SpecialHandling (); return path; }
		}

		public string PathQuoted {
			get { return Path.Replace (" ", "\\ "); }
		}

		public string FileName {
			get { return Path != null ? System.IO.Path.GetFileName (Path) : null; }
		}

		public string DirectoryName {
			get { return Path != null ? System.IO.Path.GetDirectoryName (Path) : null; }
		}

		public FileSystemInfo FileSystemInfo {
			get {
				if (IsFile)
					return (FileSystemInfo) FileInfo;
				else if (IsDirectory)
					return (FileSystemInfo) DirectoryInfo;
				else
					return null;
			}
		}

		public FileInfo FileInfo {
			get { 
				if (fileInfo == null && IsFile)
					fileInfo = new FileInfo (Path);
				return fileInfo;
			}
		}

		public DirectoryInfo DirectoryInfo {
			get {
				if (directoryInfo == null && IsDirectory)
					directoryInfo = new DirectoryInfo (Path);
				return directoryInfo;
			}
		}

		public bool IsUpToDate {
			get { return ! IsFileSystem // non-files are always up-to-date
				      || ! ValidTimestamp // non-timestamped stuff too
				      || (FileSystemInfo.LastWriteTime  <= Timestamp); }
		}


		//////////////////////////

		public IDictionary Properties {
			get { return properties; }
		}

		public ICollection Keys {
			get { return properties.Keys; }
		}

		virtual public string this [string key] {
			get { return (string) properties [key]; }
			set { 
				if (value == null || value == "") {
					if (properties.Contains (key))
						properties.Remove (key);
					return;
				}
				properties [key] = value as string;
			}
		}

		//////////////////////////

		virtual public IDictionary Data {
			get { return data; }
		}

		virtual public ICollection DataKeys {
			get { return data.Keys; }
		}

		virtual public object GetData (string key)
		{
			return data [key];
		}

		virtual public void SetData (string key, object obj)
		{
			if (obj == null) {
				if (data.Contains (key))
					data.Remove (key);
				return;
			}
			data [key] = obj;
		}
		

		//////////////////////////

		public override int GetHashCode ()
		{
			return (uri != null ? uri.GetHashCode () : 0)
				^ (type != null ? type.GetHashCode () : 0)
				^ (source != null ? source.GetHashCode () : 0);
		}

		//////////////////////////

		public int CompareTo (object obj)
		{
			Hit otherHit = (Hit) obj;
			// Notice that we sort from high to low.
			return otherHit.Score.CompareTo (this.Score);
		}
	}
}
