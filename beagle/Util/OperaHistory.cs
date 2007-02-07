//
// OperaHistory.cs: An implementation of the format used by Opera to store web history
//
// Copyright (C) 2006 Pierre Östlund
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
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;

namespace Beagle.Daemon.OperaQueryable {
	
	public class OperaHistory {
		private string filename;
		private ArrayList rows;
		
		public enum Directives : byte {
			RowStart		=	0x01,	// Row start (new entry)
			Address			=	0x03,	// Web address
			Length			=	0x08,	// Object length (e.g. image size)
			MimeType		=	0x09,	// Mime type
			Attributes		=	0x10,	// Attributes
			Encoding		=	0x0A,	// Encoding used
			Filename 		=	0x0D,	// Local filename used for this object
			LocalSaveTime	=	0x15,	// Time when an object was saved to the harddrive
			LastChanged		=	0x17,	// Time when the object was last modified on the server
			Compression		=	0x20	// Compression algorithm used (usually gzip)
		}
		
		public class Property {
			private byte directive;
			private byte[] content;
		
			public Property (byte directive, byte[] content)
			{
				this.directive = directive;
				this.content = content;
			}
			
			public byte Directive {
				get { return directive; }
			}
			
			public byte[] Content {
				get { return content; }
			}
		}
		
		public class Row {
			private ArrayList properties;
			private ArrayList attributes;
			private System.Text.Encoding encoding = System.Text.Encoding.Default;
			
			public Row ()
			{
				this.properties = new ArrayList ();
			}
			
			public void AddProperty (Property p)
			{
				if (p != null)
					properties.Add (p);
				
				if (p.Directive == (byte) Directives.Attributes)
					attributes = OperaHistory.ParseRow (p.Content).Properties;
			}
			
			public byte[] GetContent (Directives directive)
			{
				if (properties == null)
					return null;
				
				foreach (Property p in properties) {
					if (p.Directive == (byte) directive)
						return p.Content;
				}
				
				foreach (Property p in attributes) {
					if (p.Directive == (byte) directive)
						return p.Content;
				}
				
				return null;
			}
			
			public ArrayList Properties {
				get { return properties; }
			}
			
			public Uri Address {
				get {
					try {
						return new Uri (encoding.GetString (GetContent (Directives.Address)));
					} catch {
						return null;
					}
				}
			}
			
			public long Length {
				get {
					return OperaHistory.GetLength (GetContent (Directives.Length));
				}
			}
			
			public string LocalFileName {
				get {
					try {
						return encoding.GetString (GetContent (Directives.Filename));
					} catch {
						return String.Empty;
					}
				}
			}
			
			public DateTime LocalSaveTime {
				get {
					try {
						byte[] content = GetContent (Directives.LocalSaveTime);
						return DateTime.Parse (encoding.GetString (content));
					} catch {
						return DateTime.MinValue;
					}
				}
			}
			
			public DateTime LastChanged {
				get {
					try {
						byte[] content = GetContent (Directives.LastChanged);
						return DateTime.Parse (encoding.GetString (content));
					} catch {
						return DateTime.MinValue;
					}
				}
			}
			
			public string MimeType {
				get {
					try {
						return encoding.GetString (GetContent (Directives.MimeType));
					} catch {
						return string.Empty;
					}
				}
			}
			
			public Encoding Encoding {
				get {
					try {
						byte[] content =GetContent (Directives.Encoding);
						return System.Text.Encoding.GetEncoding (encoding.GetString (content));
					} catch {
						return encoding;
					}
				}
			}
			
			public string Compression {
				get {
					try {
						return encoding.GetString (GetContent (Directives.Compression));
					} catch {
						return string.Empty;
					}
				}
			}
		}
		
		public OperaHistory (string filename)
		{
			this.filename = filename;
			this.rows = new ArrayList ();
		}
		
		public void Read ()
		{
			StreamReader stream = new StreamReader (filename);
			BinaryReader binary = new BinaryReader (stream.BaseStream);
			
			// Skip first 12 bytes since their purpose is yet unknown
			binary.BaseStream.Seek (12, SeekOrigin.Begin);
			while (binary.ReadByte () == 1) {
				int length = Convert.ToInt32 (GetLength (binary.ReadByte (), binary.ReadByte ()));
				ReadLine (binary.ReadBytes (length));
			}
		}
		
		private void ReadLine (byte[] line)
		{
			Row r = ParseRow (line);
			if (r.Properties.Count > 0)
				rows.Add (r);
		}
		
		public static Row ParseRow (byte[] line)
		{
			int position = 0;
			Row row = new Row ();
			
			while (position <= line.Length) {
				try {
					Property prop = NewProperty (line, ref position);
				
					if (prop != null)
						row.AddProperty (prop);
				} catch { }
			}
			
			return row;
		}
		
		public static Property NewProperty (byte[] line, ref int position)
		{
			if (position+3 > line.Length) {
				position++;
				return null;
			} else if (line [position] == (byte) 0x8F) {
				// It seems to be something magic with 0x8F because it appears when you least 
				// expect it and doesn't seem to belong anywhere. Just ignore it.
				position++;
				return NewProperty (line, ref position);
			}
			
			int start = position+1, length = 0, directive = position;
			
			// Read the two bytes that follows the directive byte and parse them as an integer.
			// This will be how far we will be reading in the stream
			byte[] length_bytes = new byte [2];
			Array.Copy (line, start, length_bytes, 0, 2);
			length = Convert.ToInt32 (GetLength (length_bytes));
			
			// The content is what we really is after. This can be an address, object size or 
			// something else valuable.
			byte[] content = new byte [length];
			Array.Copy (line, start+2, content, 0, length);

			position += 3 + length;
			
			return new Property (line [directive], content);
		}
		
		public static long GetLength (params byte[] bytes)
		{
			byte[] t = new byte [8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			
			if (bytes == null || bytes.Length > 8 || bytes.Length == 0)
				return 0;
			
			for (int i = 0; i < bytes.Length; i++)
				t [i] = bytes [bytes.Length-i-1];
			
			return BitConverter.ToInt64 (t, 0);
		}
		
		public IEnumerator GetEnumerator ()
		{
			return rows.GetEnumerator ();
		}
	}
}
