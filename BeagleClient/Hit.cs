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
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using BU = Beagle.Util;

namespace Beagle {
    
	public class Hit: Versioned, IComparable {

		// A unique ID.  id <= 0 means 'undefined'.
		private int id = 0;

		// A URI we can use to locate the source of this match.
		private Uri uri = null;

		// File, Web, MailMessage, IMLog, etc.
		private string type = null;

		// If applicable, otherwise set to null.
		private string mimeType = null;

		// IndexUser, IndexSystem, Google, Addressbook, iFolder, etc.
		private string source = null;

		// This is used to hold a copy of the Queryable in the
		// server-side copy of the Hit.  It is always null
		// on the client-side.
		private object sourceObject = null;
		private string source_object_name = null;

		// High scores imply greater relevance.
		private double scoreRaw = 0.0;
		private double scoreMultiplier = 1.0;

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

		[XmlIgnore]
		public Uri Uri {
			get { return uri; }
			set { uri = value; }
		}

		[XmlElement ("Uri")]
		public string UriAsString {
			get {
				return BU.UriFu.UriToSerializableString (uri);
			}

			set {
				uri = BU.UriFu.UriStringToUri (value);
			}
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

		[XmlIgnore]
		public object SourceObject {
			get { return sourceObject; }
			set { sourceObject = value; }
		}

		public string SourceObjectName {
			get { return source_object_name; }
			set { source_object_name = value; }
		}

		public double Score {
			get { return scoreRaw * scoreMultiplier; }
		}

		public double ScoreRaw {
			get { return scoreRaw; }
			set { scoreRaw = value; }
		}

		public double ScoreMultiplier {
			get { return scoreMultiplier; }
			set { 
				scoreMultiplier = value;
				if (scoreMultiplier < 0) {
					BU.Logger.Log.Warn ("Invalid ScoreMultiplier={0} for {1}", scoreMultiplier, Uri);
					scoreMultiplier = 0;
				} else if (scoreMultiplier > 1) {
					BU.Logger.Log.Warn ("Invalid ScoreMultiplier={0} for {1}", scoreMultiplier, Uri);
					scoreMultiplier = 1;

				}
			}
		}

		//////////////////////////
		
		private void SpecialHandling ()
		{
			if (special != SpecialType.Unknown)
				return;
			
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

		[XmlIgnore]
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

		[XmlIgnore]
		public FileInfo FileInfo {
			get { 
				if (fileInfo == null && IsFile)
					fileInfo = new FileInfo (Path);
				return fileInfo;
			}
		}

		[XmlIgnore]
		public DirectoryInfo DirectoryInfo {
			get {
				if (directoryInfo == null && IsDirectory)
					directoryInfo = new DirectoryInfo (Path);
				return directoryInfo;
			}
		}

		//////////////////////////

		[XmlIgnore]
		public IDictionary Properties {
			get { return properties; }
		}

		public struct KeyValuePair {
			public string Key, Value;

			public KeyValuePair (string key, string value)
			{
				this.Key = key;
				this.Value = value;
			}
		}
							       
		[XmlArray (ElementName="Properties")]
		[XmlArrayItem (ElementName="Property", Type=typeof (KeyValuePair))]
		public ArrayList PropertiesAsXmlElements {
			get {
				ArrayList props = new ArrayList (properties.Count);
				
				foreach (string key in properties.Keys) {
					KeyValuePair pair = new KeyValuePair (key, (string) properties[key]);
					props.Add (pair);
				}

				return props;
			}

			set {
				foreach (KeyValuePair pair in value)
					properties[pair.Key] = pair.Value;
			}
		}

		[XmlIgnore]
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

		[XmlIgnore]
		virtual public IDictionary Data {
			get { return data; }
		}

		[XmlArray (ElementName="Data")]
		[XmlArrayItem (ElementName="Data", Type=typeof (KeyValuePair))]
		public ArrayList DataAsXmlElements {
			get {
				ArrayList data_list = new ArrayList (data.Count);
				
				foreach (string key in data.Keys) {
					KeyValuePair pair = new KeyValuePair (key, (string) data[key]);
					data_list.Add (pair);
				}

				return data_list;
			}
		}

		[XmlIgnore]
		virtual public ICollection DataKeys {
			get { return data.Keys; }
		}

		virtual public byte [] GetData (string key)
		{
			return (byte []) data [key];
		}

		virtual public void SetData (string key, byte [] blob)
		{
			if (blob == null) {
				if (data.Contains (key))
					data.Remove (key);
				return;
			}
			data [key] = blob ;
		}
		

		//////////////////////////

		public override int GetHashCode ()
		{
			return (uri != null ? uri.ToString().GetHashCode () : 0)
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
