//
// Indexable.cs
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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Beagle.Util;

namespace Beagle {

	public class Indexable : Versioned {

		// The URI of the item being indexed.
		private Uri uri = null;

		// The URI of the contents to index
		private Uri contentUri = null;

		// The URI of the hot contents to index
		private Uri hotContentUri = null;

		// Whether the content should be deleted after indexing
		private bool deleteContent = false;
		
		// File, WebLink, MailMessage, IMLog, etc.
		private String type = null;

		// If applicable, otherwise set to null.
		private String mimeType = null;

		// List of Property objects
		private ArrayList properties = new ArrayList ();

		// A stream of the content to index
		private TextReader textReader;

		// A stream of the hot content to index
		private TextReader hotTextReader;

		//////////////////////////

		static private XmlSerializer our_serializer;

		static Indexable ()
		{
			our_serializer = new XmlSerializer (typeof (Indexable));
		}

		//////////////////////////

		public Indexable (Uri _uri) {
			uri = _uri;

			type = "File";
		}

		public Indexable () {
			// Only used when reading from xml
		}

		public static Indexable NewFromXml (string xml)
		{
			StringReader reader = new StringReader (xml);
			return (Indexable) our_serializer.Deserialize (reader);
		}

		//////////////////////////

		// Use Build to do any set-up that you want to defer until
		// immediately before indexing.
		public virtual void Build ()
		{

		}

		//////////////////////////

		[XmlIgnore]
		public Uri Uri { 
			get { return uri; }
			set { uri = value; }
		}

		[XmlAttribute ("Uri")]
		public string UriString {
			get { return uri.ToString (); }
			set { uri = UriFu.UriStringToUri (value); }
		}

		[XmlIgnore]
		public Uri ContentUri {
			get { return contentUri != null ? contentUri : Uri; }
			set { contentUri = value; }
		}

		[XmlAttribute ("ContentUri")]
		public string ContentUriString {
			get { return ContentUri.ToString (); }
			set { contentUri = UriFu.UriStringToUri (value); } 
		}

		[XmlIgnore]
		private Uri HotContentUri {
			get { return hotContentUri; }
			set { hotContentUri = value; }
		}
		
		[XmlAttribute ("HotContentUri")]
		public string HotContentUriString {
			get { return HotContentUri != null ? HotContentUri.ToString () : ""; }
			set { hotContentUri = (value != "") ? new Uri (value) : null; }
		}

		[XmlAttribute]
		public bool DeleteContent {
			get { return deleteContent; }
			set { deleteContent = value; }
		}

		[XmlAttribute]
		public String Type {
			get { return type; }
			set { type = value; }
		}

		[XmlAttribute]
		public String MimeType {
			get { return mimeType; }
			set { mimeType = value; }
		}

		[XmlIgnore]
		public bool IsNonTransient {
			get { return ! DeleteContent && ContentUri.IsFile; }
		}

		//////////////////////////
		
		private TextReader ReaderFromUri (Uri uri)
		{
			TextReader reader = null;

			if (uri != null && uri.IsFile) {
				Stream stream = new FileStream (uri.LocalPath,
								FileMode.Open,
								FileAccess.Read,
								FileShare.Read);

				reader = new StreamReader (stream);

				// Paranoia: never delete the thing we are actually indexing.
				if (DeleteContent && uri != Uri)
					File.Delete (uri.LocalPath);
			}

			return reader;
		}

		public virtual TextReader GetTextReader ()
		{
			if (textReader == null)
				textReader = ReaderFromUri (ContentUri);

			return textReader;
		}
		
		public void SetTextReader (TextReader reader)
		{ 
			textReader = reader;
		}

		public virtual TextReader GetHotTextReader ()
		{
			if (hotTextReader == null)
				hotTextReader = ReaderFromUri (HotContentUri);
			return hotTextReader;
		}

		public virtual void SetHotTextReader (TextReader reader)
		{
			hotTextReader = reader;
		}

		[XmlArrayItem (ElementName="Property", Type=typeof (Property))]
		public ArrayList Properties {
			get { return properties; }
		}

		public void AddProperty (Property prop) {
			properties.Add (prop);
		}

		//////////////////////////

		private string PropertiesAsString (bool keywords)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (Property prop in Properties) {
				if (keywords ? prop.IsKeyword : ! prop.IsKeyword) {
					if (sb.Length > 0)
						sb.Append (" ");
					sb.Append (prop.Value);
				}
			}
			return sb.ToString ();
		}

		public string TextPropertiesAsString {
			get { return PropertiesAsString (false); }
		}

		public string KeywordPropertiesAsString {
			get { return PropertiesAsString (true); }
		}
		
		//////////////////////////

		public override string ToString () 
		{
			StringWriter writer = new StringWriter ();
			our_serializer.Serialize (writer, this);
			writer.Close ();
			return writer.ToString ();
		}

		private static Uri TextReaderToTempFileUri (TextReader reader)
		{
			if (reader == null)
				return null;

			string filename = Path.GetTempFileName ();
			FileStream fileStream = File.OpenWrite (filename);
			BufferedStream bufferedStream = new BufferedStream (fileStream);
			StreamWriter writer = new StreamWriter (bufferedStream);

			const int BUFFER_SIZE = 8192;
			char [] buffer = new char [BUFFER_SIZE];

			int read;
			do {
				read = reader.Read (buffer, 0, BUFFER_SIZE);
				if (read > 0)
					writer.Write (buffer, 0, read);
			} while (read > 0);
			
			writer.Close ();

			return UriFu.PathToFileUri (filename);
		}

		public void StoreStream () {
			if (textReader != null) {
				ContentUri = TextReaderToTempFileUri (textReader);
				DeleteContent = true;
			}

			if (hotTextReader != null) {
				HotContentUri = TextReaderToTempFileUri (hotTextReader);
				DeleteContent = true;
			}
		}

		//////////////////////////

		public override int GetHashCode ()
		{
			return (uri != null ? uri.GetHashCode () : 0) ^ type.GetHashCode ();
		}
	}
}
