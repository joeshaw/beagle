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
using Beagle.Util;

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

		// Parse the "style" nodes and mark appropriate styles as *HOT*
		// FIXME: Identify and ADD more *HOT* styles. ;)
		void StudyStyleNode (XmlReader reader)
		{
			string style_name = reader.GetAttribute ("style:name");
			string style_parent = reader.GetAttribute ("style:parent-style-name");

			string weight = null;
			string underline = null;
			string italic = null;
			int original_depth = reader.Depth;

			if (!reader.IsEmptyElement) {
				reader.Read ();
				while (reader.Depth > original_depth) {
					if (reader.NodeType == XmlNodeType.Element
					    && reader.Name == "style:properties") {
						weight = reader.GetAttribute ("fo:font-weight");
						italic = reader.GetAttribute ("fo:font-style");
						underline = reader.GetAttribute ("style:text-underline");
					}
					reader.Read ();
				}
			}

			if ((style_parent != null && style_parent.StartsWith("Heading"))
			    || (style_name != null && ((String.Compare (style_name, "Footnote") == 0)
						       || (String.Compare (style_name, "Endnote") == 0)
						       || (String.Compare (style_name, "Header") == 0)
						       || (String.Compare (style_name, "Footer") == 0)))
			    || (weight != null && weight == "bold")
			    || (italic != null && italic == "italic")
			    || (underline != null && underline != "none"))
				hotStyles[style_name] = true;
		}

		static bool NodeIsHot (String nodeName)
		{
			return nodeName == "text:h";
		}

		static bool NodeIsFreezing (String nodeName)
		{
			return nodeName == "text:footnote-citation"
				|| nodeName == "text:endnote-citation";

		}

		static bool NodeBreaksTextBefore (String nodeName)
		{
			return nodeName == "text:footnote"
				|| nodeName == "text:endnote"
				|| nodeName == "office:annotation";
		}

		static bool NodeBreaksTextAfter (String nodeName)
		{
			return  nodeName == "text:s"
				|| nodeName == "text:tab-stop"
				|| nodeName == "table:table-cell"
				|| nodeName == "office:annotation";
		}

		static bool NodeBreaksStructureAfter (String nodeName)
		{
			return nodeName == "text:p"
				|| nodeName == "text:h"
				|| nodeName == "text:footnote"
				|| nodeName == "text:endnote";
		}

		private Stack hot_nodes = new Stack ();
		private Stack span_nodes = new Stack ();
		private Stack part_hot_nodes = new Stack ();

		bool WalkContentNodes (XmlReader reader)
		{
			// total number of elements to read per-pull
			const int total_elements = 10;
			int num_elements = 0;

			// to handle partially formatted texts.
			bool isPartiallyHot = false;
			bool isTextSpanned = false;

			while (reader.Read ()) {
				switch (reader.NodeType) {
				case XmlNodeType.Element:
					if (reader.IsEmptyElement) {

						if (NodeBreaksStructureAfter (reader.Name))
							AppendStructuralBreak ();
						else {
							if (NodeBreaksTextBefore (reader.Name))
								AppendWhiteSpace ();
							if (NodeBreaksTextAfter (reader.Name))
								AppendWhiteSpace ();
						}

						// We check for a "whitespace" to identify
						// partially formatted texts.
						// In case if a *paragraph* ends with a *HOT* text
						// we should be able to reset it.
						// If "partiallyHot" flag is set, reset the *HOT*ness.
						isPartiallyHot = false;
						if (part_hot_nodes.Count > 0)
							isPartiallyHot = (bool) part_hot_nodes.Pop ();
						if (isPartiallyHot)
							HotDown ();
						continue;
					}

					if (reader.Name == "style:style") {
						StudyStyleNode (reader); 
						continue;
					}

					// Mark text as spanned
					if (reader.Name == "text:span") {
						isTextSpanned = true;
						span_nodes.Push (isTextSpanned);
					}
					
					// A node is hot if:
					// (1) It's name is hot
					// (2) It is flagged with a hot style
					// (3) annotations are always hot.

					bool isHot = false;

					if (NodeIsHot (reader.Name)) {
						isHot = true;
					} else if (reader.Name == "office:annotation") {
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
				
					isPartiallyHot = false;
					if (part_hot_nodes.Count > 0)
						isPartiallyHot = (bool) part_hot_nodes.Peek ();

					if (isHot&& !isPartiallyHot)
						HotUp ();
				
					if (NodeIsFreezing (reader.Name))
						FreezeUp ();
				
					if (NodeBreaksTextBefore (reader.Name))
						AppendWhiteSpace ();
					break;

				case XmlNodeType.Text:

					bool is_text_hot =  (bool) hot_nodes.Peek ();
					
					isPartiallyHot = false;
					isTextSpanned = false;

					if (part_hot_nodes.Count > 0)
						isPartiallyHot = (bool) part_hot_nodes.Pop ();

					if (span_nodes.Count > 0)
						isTextSpanned = (bool) span_nodes.Peek ();

					string text = reader.Value;

					// Partially formatted texts are called *partiallyHot* texts.
					// In case of partially Hot texts, 
					//     (i) find the first occurrance of 
					//         whitespace (ie.)
					//             <continuation-of-partially-hot-text><whitespace><normaltext>
					//     (ii) Add <continuation-of-partially-hot-text> to the textpool, 
					//          which will eventually add it to hotpool, since HotUp is not reset.
					//     (iii) call HotDown() to reset *HOT*ness.
					if (isPartiallyHot) {
						int index;
						string strPartialText = null;
						index = text.IndexOf (' ');
						if (index > -1) {
							strPartialText = text.Substring (0, index);
							text = text.Substring (index);
							if (!text.EndsWith (" "))
							    part_hot_nodes.Push (isPartiallyHot);
						} else 
							part_hot_nodes.Push (isPartiallyHot);

						if (strPartialText != null)
							AppendText (strPartialText);
						if (index > -1)
							HotDown ();
					} else 	if (is_text_hot && isTextSpanned && !text.EndsWith (" ")) {
						isPartiallyHot = true;
						part_hot_nodes.Push (isPartiallyHot);
						//Console.WriteLine ("Partially hot : [{0}]", text);
					}
					AppendText (text);
					break;

				case XmlNodeType.EndElement:
					if (reader.Name == "text:span")
						span_nodes.Pop ();

					if (NodeBreaksStructureAfter (reader.Name))
						AppendStructuralBreak ();
					else if (NodeBreaksTextAfter (reader.Name))
						AppendWhiteSpace ();

					if (reader.Name == "text:p") {
						if (part_hot_nodes.Count > 0) {
							part_hot_nodes.Clear ();
							HotDown ();
						}
					}
					
					bool is_hot = (bool) hot_nodes.Pop ();

					isPartiallyHot = false;
					if (part_hot_nodes.Count > 0)
						isPartiallyHot = (bool) part_hot_nodes.Peek ();

					// If text is *partiallyHot* do not reset
					// the *HOT*ness, which will eventually be get reset
					// when it finds an empty node or a text with whitespace.
					if (is_hot && !isPartiallyHot)
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

		// SlideCount is not stored in meta.xml rather we need to 
		// parse the whole of content.xml to find out the count of
		// slides present in an .sxi.
		private void ExtractSlideCount (XmlReader reader)
		{
			string slideCount = null;
			reader.Read ();
			do {
				reader.Read ();

				// Do not parse the whole file if it is not a
				// presentation (impress document)
				if (reader.Name == "office:document-content" 
				    && reader.NodeType == XmlNodeType.Element) {
					string docClass = reader.GetAttribute ("office:class");
					if (docClass != "presentation")
						return;
				}
			} while (reader.Depth < 2);
			
			while (reader.Depth >= 1) {
				if (reader.Depth != 2 || reader.NodeType != XmlNodeType.Element) {
					reader.Read ();
					continue;
				}
				switch (reader.Name) {
				case "draw:page":
					slideCount = reader.GetAttribute ("draw:id");
					break;
				}
				reader.Read ();
			}
			
			if (slideCount != null)
				AddProperty (Beagle.Property.NewKeyword ("fixme:slide-count", slideCount));

		}

		private void ExtractMetadata (XmlReader reader)
		{
			string slideCount = null;

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

					// Both writer and calc uses this attribute.  writer stores the
					// count of tables in a sxw whereas calc stores the count of
					// spreadsheets in a sxc.
					attr = reader.GetAttribute ("meta:table-count");
					if (attr != null && Convert.ToInt32 (attr) > 0 
					    && MimeType == "application/vnd.sun.xml.calc")
						AddProperty (Beagle.Property.NewKeyword ("fixme:spreadsheet-count", attr));
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
				Logger.Log.Error ("No meta.xml!");
			}
			
			entry = zip.GetEntry ("content.xml");
			if (entry != null) {
				Stream contents_stream = zip.GetInputStream (entry);
				XmlReader reader = new XmlTextReader (contents_stream);
				ExtractSlideCount (reader);
			} else {
				Logger.Log.Error ("No content.xml!");
			}
		}

		XmlReader content_reader = null;
		XmlReader style_reader = null;
		override protected void DoPull ()
		{
			// We need both styles.xml and content.xml as 
			// "Header", "Footer" are stored in styles.xml and
			// "[Foot/End]Notes are stored in content.xml
			if ((content_reader == null) && (style_reader == null)) {

				ZipEntry entry = zip.GetEntry ("content.xml");
				ZipEntry entry1 = zip.GetEntry ("styles.xml");

				if ((entry != null) && (entry1 != null)) {
					Stream content_stream = zip.GetInputStream (entry);
					Stream style_stream = zip.GetInputStream (entry1);
					content_reader = new XmlTextReader (content_stream);
					style_reader = new XmlTextReader (style_stream);
				}
			}				
			if ((content_reader == null) && (style_reader == null)) {
				Finished ();
				return;
			}

			// Note: Do not change the order.
			// we need to populate our hotStyles table with all posible hot styles.
			// Since, "footnotes" and "endnotes" gets stored in content.xml and these
			// styles needs to be marked as *HOT*, they need to be processed before contents.
			if ((WalkContentNodes (style_reader)) && (WalkContentNodes (content_reader)))
				Finished ();
		}
	}
}
