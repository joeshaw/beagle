//
// XmpFile.cs
//
// We don't handle xmp files outside file system backend !!!
// That is why this is here and not in Util/ or Filters/
//
// Copyright (C) 2007 Alexander McDonald
// Copyright (C) 2007 Debajyoti Bera <dbera.web@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Daemon.FileSystemQueryable {

	public class XmpFile
	{
		private const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
		private const string XmpNamespace = "adobe:ns:meta/";
		
		public const string FilterName = "FilterFileSystemXmp";
		public const int FilterVersion = 0;

		private string xmpfile = null;
		private List<Beagle.Property> properties = null;
		private XmlReader reader;
		
#if FALSE
		public static void Main(string[] args)
		{
			Console.WriteLine("Properties:");
			
			foreach( Beagle.Property bp in new xmp(args[0]).GetProperties())
			{
				Console.WriteLine(bp.Key + " --- " + bp.Value);
			}
		}
#endif	

		public XmpFile (string path)
		{
			xmpfile = path;
			XmlReaderSettings ReaderSettings = new XmlReaderSettings ();
			reader = XmlReader.Create (xmpfile, ReaderSettings);
			// FIXME: Check here if the preamble matches correct xmp preamble
		}

		public void Close ()
		{
			properties = null;
			reader.Close ();
		}
		
		private void AddProperty(string Name, string Value)
		{
			if (Name.Contains("Date")) {
				try {
					properties.Add (Beagle.Property.NewDate (Name, System.Convert.ToDateTime (Value)));
				} catch (FormatException) {
					properties.Add (Beagle.Property.New (Name, Value));
				}
			} else {
				properties.Add (Beagle.Property.New (Name, Value));
			}
		}

		public IList<Property> Properties {
			get {
				if (properties == null) {
					properties = new List<Beagle.Property>();
					Parse ();
				}

				return properties;
			}
		}
					
		// FIXME: Change it to a pulling (returning IEnumerable) method
		private void Parse()
		{
			StringBuilder sb = new StringBuilder ();
			
			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.Element) {
					switch (reader.NamespaceURI) {
					case XmpNamespace:
					case RdfNamespace:
						break;
					default:
						if (reader.GetAttribute("rdf:parseType") == "Resource") {
							HandleResource (reader, sb);
						} else {
							HandleElement (reader, sb, null);
						}
						break;
					}
				}
			}
		}

		protected void HandleElement (XmlReader r, StringBuilder sb, string prefix)
		{
			int StartDepth = r.Depth;
			int ListNum = 0;
			sb.Length = 0;
	
			string Name;
			if (prefix == null)
				Name = r.Name;
			else
				Name = String.Format ("{0}:{1}-{2}", r.Prefix, prefix, r.LocalName);
	
			string ns = Name.ToLower ();
			// Only add some properties and not all, they are a huge list!!!
			// FIXME: Which properties to choose to add ?
			if (! ns.StartsWith ("exif") && 
			    ! ns.StartsWith ("dc") &&
			    ! ns.StartsWith ("tiff") &&
			    ! ns.StartsWith ("aux") &&
			    ! ns.StartsWith ("Iptc4xmpCore"))
				return;
			
			while (r.Read() && r.Depth > StartDepth) {
				switch (r.NodeType) {
				case XmlNodeType.Text:
					sb.Append (r.Value);
					break;
				case XmlNodeType.Element:
					if (r.NamespaceURI == RdfNamespace && r.LocalName == "li") {
						if (ListNum > 0) {
							sb.Append (", ");
						}
						ListNum++;
					}
					break;
				}
			}
			
			AddProperty (Name, sb.ToString());
		}
		
		protected void HandleResource (XmlReader r, StringBuilder sb)
		{
			int StartDepth = r.Depth;
			string prefix = r.LocalName;
	
			while (r.Read() && r.Depth > StartDepth) {
				if (r.NodeType == XmlNodeType.Element) {
					HandleElement (r, sb, prefix);
				}
			}
		}
	}
}

