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

using Beagle.Util;

namespace Beagle {
    
	public class Hit: Versioned, IComparable {

		// A unique ID.  id <= 0 means 'undefined'.
		private int id = 0;

		// A URI we can use to locate the source of this match.
		private Uri uri = null;

		// A URI of this Hit's container element
		private Uri parent_uri = null;

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

		private ArrayList properties = new ArrayList ();
		private bool sorted = true;

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

		[XmlAttribute]
		public int Id {
			get { return id; }
			set { id = value; }
		}

		[XmlIgnore]
		public Uri Uri {
			get { return uri; }
			set { uri = value; }
		}

		[XmlAttribute ("Uri")]
		public string UriAsString {
			get {
				return UriFu.UriToSerializableString (uri);
			}

			set {
				uri = UriFu.UriStringToUri (value);
			}
		}

		[XmlIgnore]
		public Uri ParentUri {
			get { return parent_uri; }
			set { parent_uri = value; }
		}

		[XmlAttribute ("ParentUri")]
		public string ParentUriAsString {
			get {
				if (parent_uri == null)
					return null;

				return UriFu.UriToSerializableString (parent_uri);
			}

			set {
				if (value == null)
					parent_uri = null;
				else
					parent_uri = UriFu.UriStringToUri (value);
			}
		}

		[XmlAttribute]
		public string Type {
			get { return type; }
			set { type = value; }
		}

		[XmlAttribute]
		public string MimeType {
			get { return mimeType; }
			set { mimeType = value; }
		}
	
		[XmlAttribute]
		public string Source {
			get { return source; }
			set { source = value; }
		}

		[XmlIgnore]
		public object SourceObject {
			get { return sourceObject; }
			set { sourceObject = value; }
		}

		[XmlAttribute]
		public string SourceObjectName {
			get { return source_object_name; }
			set { source_object_name = value; }
		}

		public double Score {
			get { return scoreRaw * scoreMultiplier; }
		}

		[XmlAttribute]
		public double ScoreRaw {
			get { return scoreRaw; }
			set { scoreRaw = value; }
		}

		[XmlAttribute]
		public double ScoreMultiplier {
			get { return scoreMultiplier; }
			set { 
				scoreMultiplier = value;
				if (scoreMultiplier < 0) {
					Logger.Log.Warn ("Invalid ScoreMultiplier={0} for {1}", scoreMultiplier, Uri);
					scoreMultiplier = 0;
				} else if (scoreMultiplier > 1) {
					Logger.Log.Warn ("Invalid ScoreMultiplier={0} for {1}", scoreMultiplier, Uri);
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

		[XmlArray]
		[XmlArrayItem (ElementName="Property", Type=typeof (Property))]
		public ArrayList Properties {
			get {  return properties; }
		}

		public void AddProperty (Property prop)
		{
			if (sorted && properties.Count > 0) {
				Property last_prop;
				last_prop = properties [properties.Count - 1] as Property;
				if (last_prop.CompareTo (prop) > 0) // i.e. last_prop > prop
					sorted = false;
			}

			properties.Add (prop);
		}

		private bool FindProperty (string key, out int first, out int top)
		{
			// FIXME: Should use binary search on sorted property list
			if (! sorted) {
				properties.Sort ();
				sorted = true;
			}
			
			first = 0;
			top = 0;

			while  (first < properties.Count) {
				Property prop;
				prop = properties [first] as Property;
				if (prop.Key == key)
					break;
				++first;
			}

			if (first >= properties.Count)
				return false;

			top = first + 1;
			while (top < properties.Count) {
				Property prop;
				prop = properties [top] as Property;
				if (prop.Key != key)
					break;
				++top;
			}

			return true;
		}

		public string this [string key] {
			get {
				int first, top;
				if (! FindProperty (key, out first, out top))
					return null;

				if (top - first != 1) {
					Logger.Log.Debug ("Accessed multi-property key with Hit's indexer.");
					return null;
				}

				Property prop;
				prop = properties [first] as Property;
				return prop.Value;
			}

			set {
				int first = 0, top = 0;

				// If we've never heard of this property, add it.
				if (! FindProperty (key, out first, out top)) {
					AddProperty (Property.New (key, value));
					return;
				}

				// If it has appeared once before, clobber the existing
				// value.  This emulates the previous (broken) semantics.

				if (top - first == 1) {
					properties [first] = Property.New (key, value);
					return;
				}
				
				// Otherwise throw an exception (which sort of sucks,
				// but we don't really know what to do there)
				throw new Exception (String.Format ("Attempt to re-set multi-property '{0}' via the indexer", key));
			}
		}

		public string GetFirstProperty (string key)
		{
			int first, top;
			if (! FindProperty (key, out first, out top))
				return null;
			Property prop;
			prop = properties [first] as Property;
			return prop.Value;
		}

		public string[] GetProperties (string key)
		{
			int first, top;
			if (! FindProperty (key, out first, out top))
				return null;

			string[] values = new string [top - first];

			for (int i = 0; first + i < top; i++) {
				Property prop = properties [first + i] as Property;
				values [i] = prop.Value;
			}

			return values;
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
