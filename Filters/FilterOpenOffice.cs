//
// FilterOpenOffice.cs
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

using ICSharpCode.SharpZipLib.Zip;

namespace Beagle.Filters {
    
	public class FilterOpenOffice : Beagle.Daemon.Filter {

		Hashtable hotStyles;
		
		public FilterOpenOffice () 
		{
			AddSupportedMimeType ("application/vnd.sun.xml.writer");
			AddSupportedMimeType ("application/vnd.sun.xml.impress");
			AddSupportedMimeType ("application/vnd.sun.xml.calc");
			SnippetMode = true;
		}
		
		static String FindChildAttribute (XmlNode node,
						  String nodeName,
						  String attributeName)
		{
			foreach (XmlNode subnode in node.ChildNodes) {
				if (subnode.Name == nodeName) {
					XmlAttribute attr = subnode.Attributes [attributeName];
					if (attr == null)
						return null;
					return attr.Value;
				}
			}
			return null;
		}

		void StudyStyleNode (XmlReader reader)
		{
			string style_name = reader.GetAttribute ("style:name");
			string style_parent = reader.GetAttribute ("style:parent-style-name");

			string weight = null;
			string underline = null;
			int original_depth = reader.Depth;

			if (!reader.IsEmptyElement) {
				reader.Read ();
				while (reader.Depth > original_depth) {
					if (reader.NodeType == XmlNodeType.Element
					    && reader.Name == "style:properties") {
						weight = reader.GetAttribute ("fo:font-weight");
						underline = reader.GetAttribute ("style:text-underline");
					}
					reader.Read ();
				}
			}

			if ((style_parent != null && style_parent.StartsWith("Heading"))
			    || weight == "bold" 
			    || (underline != null && underline != "none")) {
				
				hotStyles[style_name] = true;
			}
		}

		static bool NodeIsHot (String nodeName)
		{
			return nodeName == "text:h";
		}

		static bool NodeIsFreezing (String nodeName)
		{
			return nodeName == "text:footnote-citation";
		}

		static bool NodeBreaksTextBefore (String nodeName)
		{
			return nodeName == "text:footnote";
		}

		static bool NodeBreaksTextAfter (String nodeName)
		{
			return nodeName == "text:p"
				|| nodeName == "text:h"
				|| nodeName == "text:s"
				|| nodeName == "text:tab-stop"
				|| nodeName == "text:footnote"
				|| nodeName == "table:table-cell";
		}

		private Stack hot_nodes = new Stack ();
		bool WalkContentNodes (XmlReader reader)
		{
			// total number of elements to read per-pull
			const int total_elements = 10;
			int num_elements = 0;

			while (reader.Read ()) {
			switch (reader.NodeType) {
			case XmlNodeType.Element:
				if (reader.IsEmptyElement) {
					if (NodeBreaksTextBefore (reader.Name))
						AppendWhiteSpace ();
					if (NodeBreaksTextAfter (reader.Name))
						AppendWhiteSpace ();
					continue;
				}

				if (reader.Name == "style:style") {
					StudyStyleNode (reader); 
					continue;
				}

				// A node is hot if:
				// (1) It's name is hot
				// (2) It is flagged with a hot style
				bool isHot = false;
				if (NodeIsHot (reader.Name)) {
					isHot = true;
				} else {
					bool has_attr = reader.MoveToFirstAttribute ();
					while (has_attr) {
						if (reader.Name.EndsWith(":style-name")) {
							if (hotStyles.Contains (reader.Value))
								isHot = true;
							break;
						}
						has_attr = reader.MoveToNextAttribute ();

					}
					reader.MoveToElement();
				}				

				hot_nodes.Push (isHot);
				
				if (isHot)
					HotUp ();
				
				if (NodeIsFreezing (reader.Name))
					FreezeUp ();
				
				if (NodeBreaksTextBefore (reader.Name))
					AppendWhiteSpace ();
				break;
			case XmlNodeType.Text:
				string text = reader.Value;
				AppendText (text);
				break;
			case XmlNodeType.EndElement:
				if (NodeBreaksTextAfter (reader.Name))
					AppendWhiteSpace ();

				bool is_hot = (bool) hot_nodes.Pop ();
				if (is_hot)
					HotDown ();
				
				if (NodeIsFreezing (reader.Name))
					FreezeDown ();
				break;
			}
			num_elements++;
			if (num_elements >= total_elements) {
				return false;
			}
			} 
			return true;
		}

		private void ExtractMetadata (XmlReader reader)
		{
			do {
				reader.Read ();
			} while (reader.Depth < 2);

			while (reader.Depth >= 2) {
				if (reader.Depth != 2 || reader.NodeType != XmlNodeType.Element) {
					reader.Read ();
					continue;
				}

				switch (reader.Name) {
					
				case "dc:title":
					reader.Read ();
					AddProperty (Beagle.Property.New ("dc:title",
									  reader.Value));
					break;

				case "dc:description":
					reader.Read ();
					
					AddProperty (Beagle.Property.New ("dc:description",
									  reader.Value));
					break;

				case "dc:subject":
					reader.Read ();

					AddProperty (Beagle.Property.New ("dc:subject",
										 reader.Value));
					break;
					
				case "meta:document-statistic":
					string attr = reader.GetAttribute ("meta:page-count");
					if (attr != null)
						AddProperty (Beagle.Property.NewKeyword ("fixme:page-count", attr));
					attr = reader.GetAttribute ("meta:word-count");
					if (attr != null)
						AddProperty (Beagle.Property.NewKeyword ("fixme:word-count", attr));
					break;

				case "meta:user-defined":
					string name = reader.GetAttribute ("meta:name");
					reader.Read ();

					if (reader.Value != "") {
						AddProperty (Beagle.Property.New ("fixme:UserDefined-" + name,
											 reader.Value));
					}
					break;
					
				}
				
				reader.Read ();
			}
		}

		ZipFile zip = null;

		override protected void DoOpen (FileInfo info)
		{
			hotStyles = new Hashtable ();
			zip = new ZipFile (info.FullName);
		}

		override protected void DoPullProperties ()
		{
			ZipEntry entry = zip.GetEntry ("meta.xml");
			if (entry != null) {
				Stream meta_stream = zip.GetInputStream (entry);
				XmlReader reader = new XmlTextReader (meta_stream);
				ExtractMetadata (reader);
			} else {
				Console.WriteLine ("No meta.xml!");
			}
		}

		XmlReader content_reader = null;
		override protected void DoPull ()
		{
			if (content_reader == null) {
				ZipEntry entry = zip.GetEntry ("content.xml");
				if (entry != null) {
					Stream content_stream = zip.GetInputStream (entry);
					content_reader = new XmlTextReader (content_stream);
				}
			}				
			if (content_reader == null) {
				Finished ();
				return;
			}

			if (WalkContentNodes (content_reader))
				Finished ();
		}
	}
}
