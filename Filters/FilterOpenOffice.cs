//
// FilterOpenOffice.cs
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
using System.Text;
using System.Xml;

using ICSharpCode.SharpZipLib.Zip;

namespace Beagle.Filters {
    
	public class FilterOpenOffice : Filter {

		Hashtable hotStyles;
		
		public FilterOpenOffice () 
		{
			AddSupportedMimeType ("application/vnd.sun.xml.writer");
			AddSupportedMimeType ("application/vnd.sun.xml.impress");
			AddSupportedMimeType ("application/vnd.sun.xml.calc");
		}
		
		static String FindAttribute (XmlNode node,
					     String attributeName)
		{
			XmlAttribute attr = node.Attributes [attributeName];
			return attr == null ? null : attr.Value;
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

		void StudyStyleNode (XmlNode node)
		{
			String styleName = FindAttribute (node, "style:name");
			String styleParent = FindAttribute (node, "style:parent-style-name");

			String weight = FindChildAttribute (node,
							    "style:properties",
							    "fo:font-weight");
			String underline = FindChildAttribute (node,
							       "style:properties",
							       "style:text-underline");

			if ((styleParent != null && styleParent.StartsWith("Heading"))
			    || weight == "bold" 
			    || (underline != null && underline != "none")) {
				
				hotStyles[styleName] = true;
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

		void WalkContentNodes (XmlNode node)
		{
	    
			switch (node.NodeType) {
		
			case XmlNodeType.Element:

				// A node is hot if:
				// (1) It's name is hot
				// (2) It is flagged with a hot style
				bool isHot = false;
				if (NodeIsHot (node.Name)) {
					isHot = true;
				} else {
					foreach (XmlAttribute attr in node.Attributes) {
						if (attr.Name.EndsWith(":style-name")) {
							if (hotStyles.Contains (attr.Value))
								isHot = true;
							break;
			    }
					}
				}
				if (isHot)
					HotUp ();
				
				bool isFreezing = false;
				if (NodeIsFreezing (node.Name))
					isFreezing = true;
				if (isFreezing)
					FreezeUp ();
				
				if (NodeBreaksTextBefore (node.Name))
					AppendWhiteSpace ();
				
				switch (node.Name) {
					
				case "style:style":
					StudyStyleNode (node);
					break;

				default:
					foreach (XmlNode subnode in node.ChildNodes)
						WalkContentNodes (subnode);
					break;
				}
				
				if (NodeBreaksTextAfter (node.Name))
					AppendWhiteSpace ();
				
				if (isHot)
					HotDown ();
				
				if (isFreezing)
					FreezeDown ();
				
				break;
				
			case XmlNodeType.Text:
				String text = node.Value;
				AppendContent (text);
				break;
			}
			
		}
		
		override protected void Read (Stream stream)
		{
			hotStyles = new Hashtable ();
			
			ZipFile zip = new ZipFile (stream);
			
			ZipEntry entry = zip.GetEntry ("content.xml");
			if (entry != null) {
				Stream content_stream = zip.GetInputStream (entry);
				XmlDocument doc = new XmlDocument ();
				doc.Load (content_stream);
				WalkContentNodes (doc.DocumentElement);
			}
			
			// FIXME: pull metadata out of meta.xml
			// This task is blocking on Mono bug 57907.
		}
		
	}
	
}
