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

	public enum IndexableFiltering {
		Never,     // Never try to filter this indexable
		Automatic, // Try to determine automatically if this needs to be filtered
		Always     // Always try to filter this indexable
	}

	public class Indexable : Versioned {


		// The URI of the item being indexed.
		private Uri uri = null;

		// The URI of the parent indexable, if any.
		private Uri parent_uri = null;

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

		// Is this being indexed because of crawling or other
		// background activity?
		private bool crawled = true;

		// Is this object inherently contentless?
		private bool no_content = false;

		// If necessary, should we cache this object's content?
		// The cached version is used to generate snippets.
		private bool cache_content = true;

		// A stream of the content to index
		private TextReader textReader;

		// A stream of the hot content to index
		private TextReader hotTextReader;

		// A stream of binary data to filter
		private Stream binary_stream;

		// When should we try to filter this indexable?
		private IndexableFiltering filtering = IndexableFiltering.Automatic;

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

		[XmlIgnore]
		public Uri Uri { 
			get { return uri; }
			set { uri = value; }
		}

		[XmlAttribute ("Uri")]
		public string UriString {
			get { return UriFu.UriToSerializableString (uri); }
			set { uri = UriFu.UriStringToUri (value); }
		}

		[XmlIgnore]
		public Uri ParentUri { 
			get { return parent_uri; }
			set { parent_uri = value; }
		}

		[XmlAttribute ("ParentUri")]
		public string ParentUriString {
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

		[XmlIgnore]
		public Uri ContentUri {
			get { return contentUri != null ? contentUri : Uri; }
			set { contentUri = value; }
		}

		[XmlAttribute ("ContentUri")]
		public string ContentUriString {
			get { return UriFu.UriToSerializableString (ContentUri); }
			set { contentUri = UriFu.UriStringToUri (value); } 
		}

		[XmlIgnore]
		private Uri HotContentUri {
			get { return hotContentUri; }
			set { hotContentUri = value; }
		}
		
		[XmlAttribute ("HotContentUri")]
		public string HotContentUriString {
			get { return HotContentUri != null ? UriFu.UriToSerializableString (HotContentUri) : ""; }
			set { hotContentUri = (value != "") ? new Uri (value) : null; }
		}

		[XmlIgnore]
		public Uri DisplayUri {
			get { return uri.Scheme == "uid" ? ContentUri : Uri; }
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
			get { return ! DeleteContent && ContentUri.IsFile && ParentUri == null; }
		}

		[XmlAttribute]
		public bool Crawled {
			get { return crawled; }
			set { crawled = value; }
		}

		[XmlAttribute]
		public bool NoContent {
			get { return no_content; }
			set { no_content = value; }
		}

		[XmlAttribute]
		public bool CacheContent {
			get { return cache_content; }
			set { cache_content = value; }
		}

		[XmlAttribute]
		public IndexableFiltering Filtering {
			get { return filtering; }
			set { filtering = value; }
		}

		//////////////////////////

		private Stream StreamFromUri (Uri uri)
		{
			Stream stream = null;

			if (uri != null && uri.IsFile && ! no_content) {
				stream = new FileStream (uri.LocalPath,
							 FileMode.Open,
							 FileAccess.Read,
							 FileShare.Read);

				// Paranoia: never delete the thing we are actually indexing.
				if (DeleteContent && uri != Uri)
					File.Delete (uri.LocalPath);
			}

			return stream;
		}

		private TextReader ReaderFromUri (Uri uri)
		{
			Stream stream = StreamFromUri (uri);

			if (stream == null)
				return null;

			return new StreamReader (stream);
		}
				

		public TextReader GetTextReader ()
		{
			if (NoContent)
				return null;

			if (textReader == null)
				textReader = ReaderFromUri (ContentUri);

			return textReader;
		}
		
		public void SetTextReader (TextReader reader)
		{ 
			textReader = reader;
		}

		public TextReader GetHotTextReader ()
		{
			if (NoContent)
				return null;

			if (hotTextReader == null)
				hotTextReader = ReaderFromUri (HotContentUri);
			return hotTextReader;
		}

		public void SetHotTextReader (TextReader reader)
		{
			hotTextReader = reader;
		}

		public Stream GetBinaryStream ()
		{
			if (NoContent)
				return null;

			if (binary_stream == null)
				binary_stream = StreamFromUri (ContentUri);

			return binary_stream;
		}

		public void SetBinaryStream (Stream stream)
		{
			binary_stream = stream;
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
				if (prop.IsSearched 
				    && keywords ? prop.IsKeyword : ! prop.IsKeyword) {
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

		public void SetChildOf (Indexable parent)
		{
			this.ParentUri = parent.Uri;

			// FIXME: Set all of the parent's properties on the
			// child so that we get matches against the child
			// that otherwise would match only the parent, at
			// least until we have proper RDF support.
			foreach (Property prop in parent.Properties) {
				Property new_prop = (Property) prop.Clone ();
				new_prop.Key = "parent:" + new_prop.Key;
				this.AddProperty (new_prop);
			}
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

			// When we dump the contents of an indexable into a file, we
			// expect to use it again soon.
			FileAdvise.PreLoad (fileStream);

			// Make sure the temporary file is only readable by the owner.
			// FIXME: There is probably a race here.  Could some malicious program
			// do something to the file between creation and the chmod?
			Mono.Posix.Syscall.chmod (filename, (Mono.Posix.FileMode) 256);

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

		private static Uri BinaryStreamToTempFileUri (Stream stream)
		{
			if (stream == null)
				return null;

			string filename = Path.GetTempFileName ();
			FileStream fileStream = File.OpenWrite (filename);

			// When we dump the contents of an indexable into a file, we
			// expect to use it again soon.
			FileAdvise.PreLoad (fileStream);

			// Make sure the temporary file is only readable by the owner.
			// FIXME: There is probably a race here.  Could some malicious program
			// do something to the file between creation and the chmod?
			Mono.Posix.Syscall.chmod (filename, (Mono.Posix.FileMode) 256);

			BufferedStream bufferedStream = new BufferedStream (fileStream);

			const int BUFFER_SIZE = 8192;
			byte [] buffer = new byte [BUFFER_SIZE];

			int read;
			do {
				read = stream.Read (buffer, 0, BUFFER_SIZE);
				if (read > 0)
					bufferedStream.Write (buffer, 0, read);
			} while (read > 0);

			bufferedStream.Close ();

			return UriFu.PathToFileUri (filename);
		}

		public void StoreStream () {
			if (textReader != null) {
				ContentUri = TextReaderToTempFileUri (textReader);
				DeleteContent = true;
			} else if (binary_stream != null) {
				ContentUri = BinaryStreamToTempFileUri (binary_stream);
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
