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
using BU = Beagle.Util;

namespace Beagle {

	public class Indexable : Versioned {

		// The URI of the item being indexed.
		private String uri = null;

		// The URI of the contents to index
		private String contentUri = null;

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

		//////////////////////////

		public Indexable (string _uri) {
			uri = _uri;

			type = "File";
		}

		protected Indexable () {
			// Only used when reading from xml
		}

		public static Indexable NewFromXml (string xml)
		{
			Indexable indexable = new Indexable ();
			indexable.ReadFromXml (xml);

			return indexable;
		}

		//////////////////////////
		public String Uri { 
			get { return uri; }
			set { uri = value; }
		}

		public String ContentUri {
			get { return contentUri != null ? contentUri : Uri; }
			set { contentUri = value; }
		}

		public bool DeleteContent {
			get { return deleteContent; }
			set { deleteContent = value; }
		}

		public String Type {
			get { return type; }
			set { type = value; }
		}

		public String MimeType {
			get { return mimeType; }
			set { mimeType = value; }
		}

		//////////////////////////

		public virtual TextReader GetTextReader ()
		{
			return null;
		}
		
		public void SetTextReader (TextReader reader)
		{ 
			textReader = reader;
		}

		public virtual TextReader GetHotTextReader ()
		{
			return null;
		}

		public IEnumerable Properties {
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

		public void WriteToXml (XmlTextWriter writer)
		{
			writer.WriteStartElement ("indexable");
			writer.WriteAttributeString ("uri", uri);
			if (contentUri != null) 
				writer.WriteAttributeString ("contenturi", contentUri);
			if (deleteContent)
				writer.WriteAttributeString ("deletecontent", "1");
			if (mimeType != null)
				writer.WriteAttributeString ("mimetype", mimeType);
			if (type != null) 
				writer.WriteAttributeString ("type", type);

			writer.WriteAttributeString ("timestamp", BU.StringFu.DateTimeToString (Timestamp));
			writer.WriteAttributeString ("revision", Revision.ToString ());

			writer.WriteStartElement ("properties");
			foreach (Property prop in properties) {
				writer.WriteStartElement ("property");
				writer.WriteAttributeString ("key", prop.Key);
				writer.WriteAttributeString ("value", prop.Value);
				if (prop.IsKeyword) 
					writer.WriteAttributeString ("iskeyword", "1");

				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			
			writer.WriteEndElement ();
		}

		public void StoreStream () {
			if (textReader == null)
				return;

			string filename = Path.GetTempFileName ();
			FileStream fileStream = File.OpenWrite (filename);
			BufferedStream bufferedStream = new BufferedStream (fileStream);
			StreamWriter writer = new StreamWriter (bufferedStream);
			char []buffer = new char[1024];
			
			int read = textReader.Read (buffer, 0, 1024);
			
			while (read > 0) {
				writer.Write (buffer, 0, read);
				read = textReader.Read (buffer, 0, 1024);
			}
			
			writer.Close ();

			ContentUri = "file://" + filename;
			DeleteContent = true;

			Console.WriteLine ("saved to {0}", ContentUri);
		}

		public void RestoreStream () 
		{
		}

		public void ReadFromXml (string text)
		{
			XmlTextReader reader = new XmlTextReader (new StringReader (text));
			Console.WriteLine ("reading {0}", text);
			
			ReadFromXml (reader);
		}

		public void ReadFromXml (XmlTextReader reader) 
		{
			reader.Read ();
			// This is a pretty lame reader 
			uri = reader.GetAttribute ("uri");
			type = reader.GetAttribute ("type");
			mimeType = reader.GetAttribute ("mimetype");
			contentUri = reader.GetAttribute ("contenturi");
			
			deleteContent = (reader.GetAttribute ("deletecontent") == "1");
			Revision = long.Parse (reader.GetAttribute ("revision"));
			Timestamp = BU.StringFu.StringToDateTime (reader.GetAttribute ("timestamp"));
			while (reader.Read ()) {		
				if (reader.NodeType == XmlNodeType.Element
				    && reader.Name == "property") {
					string key = reader.GetAttribute ("key");
					
					string value = reader.GetAttribute ("value");
					Property prop;
					if (reader.GetAttribute ("iskeyword") == "1") {
						prop = Property.NewKeyword (key, value);
					} else {
						prop = Property.New (key, value);
					}

					properties.Add (prop);
				} else if (reader.NodeType == XmlNodeType.EndElement
					   && reader.Name == "indexable") {
					break;
				}
			}
		}

		public string ToXml () {
			StringWriter stringWriter = new StringWriter ();
			XmlTextWriter writer = new XmlTextWriter (stringWriter);
			WriteToXml (writer);
			
			writer.Close ();
			stringWriter.Close ();

			Console.WriteLine (stringWriter.ToString ());

			return stringWriter.ToString ();
		}
		
		//////////////////////////

		public override int GetHashCode ()
		{
			return uri.GetHashCode () ^ type.GetHashCode ();
		}
	}
}
