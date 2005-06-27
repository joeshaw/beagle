//
// FilterHtml.cs
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

using Beagle.Daemon;

using HtmlAgilityPack;

namespace Beagle.Filters {

	public class FilterHtml : Beagle.Daemon.Filter {

		public FilterHtml ()
		{
			RegisterSupportedTypes ();
			SnippetMode = true;
		}

		protected bool NodeIsHot (String nodeName) 
		{
			return nodeName == "b"
				|| nodeName == "u"
				|| nodeName == "em"
				|| nodeName == "strong"
				|| nodeName == "big"
				|| nodeName == "h1"
				|| nodeName == "h2"
				|| nodeName == "h3"
				|| nodeName == "h4"
				|| nodeName == "h5"
				|| nodeName == "h6"
				|| nodeName == "i"
				|| nodeName == "th";
		}

		protected static bool NodeBreaksText (String nodeName) 
		{
			return nodeName == "td"
				|| nodeName == "a"
				|| nodeName == "div"
				|| nodeName == "option";
		}

		protected static bool NodeBreaksStructure (string nodeName)
		{
			return nodeName == "p"
				|| nodeName == "br"
				|| nodeName == "h1"
				|| nodeName == "h2"
				|| nodeName == "h3"
				|| nodeName == "h4"
				|| nodeName == "h5"
				|| nodeName == "h6";
		}
		
		protected static bool NodeIsContentFree (String nodeName) 
		{
			return nodeName == "script"
				|| nodeName == "map"
				|| nodeName == "style";
		}
		
		protected String WalkChildNodesForText (HtmlNode node)
		{
			StringBuilder builder = new StringBuilder ("");
			foreach (HtmlNode subnode in node.ChildNodes) {
				switch (subnode.NodeType) {
				case HtmlNodeType.Element:
					if (! NodeIsContentFree (subnode.Name)) {
						String subtext = WalkChildNodesForText (subnode);
						builder.Append (subtext);
					}
					break;
					
				case HtmlNodeType.Text:
					String text = ((HtmlTextNode)subnode).Text;
					text = HtmlEntity.DeEntitize (text);
					builder.Append (text);
					break;
				}
			}
			return builder.ToString ().Trim ();
		}
		
		protected void WalkHeadNodes (HtmlNode node)
		{
			foreach (HtmlNode subnode in node.ChildNodes) {
				if (subnode.NodeType == HtmlNodeType.Element
				    && subnode.Name == "title") {
					String title = WalkChildNodesForText (subnode);
					title = HtmlEntity.DeEntitize (title);
					AddProperty (Beagle.Property.New ("dc:title", title));
				}
				if (subnode.NodeType == HtmlNodeType.Element
				    && subnode.Name == "meta") {
	   				string name = subnode.GetAttributeValue ("name", "");
           				string content = subnode.GetAttributeValue ("content", "");
					if (name != "" && content != "")
						AddProperty (Beagle.Property.New (name, content));
				}
			}
		}
	
		protected void WalkBodyNodes (HtmlNode node)
		{
			switch (node.NodeType) {
				
			case HtmlNodeType.Document:
			case HtmlNodeType.Element:
				if (! NodeIsContentFree (node.Name)) {
					bool isHot = NodeIsHot (node.Name);
					bool breaksText = NodeBreaksText (node.Name);
					bool breaksStructure = NodeBreaksStructure (node.Name);
					if (isHot)
						HotUp ();
					if (breaksText)
						AppendWhiteSpace ();
					if (node.Name == "img") {
						string attr = node.GetAttributeValue ("alt", "");
						if (attr != "") {
							AppendText (attr);
						}
					}
					if (node.Name == "a") {
						string attr = node.GetAttributeValue ("href", "");
						if (attr != "") {
							AppendText (attr);
						}
					}
					foreach (HtmlNode subnode in node.ChildNodes)
						WalkBodyNodes (subnode);
					if (breaksText)
						AppendWhiteSpace ();
					if (breaksStructure)
						AppendStructuralBreak ();
					if (isHot)
						HotDown ();

				}				
				break;
				
			case HtmlNodeType.Text:
				String text = ((HtmlTextNode)node).Text;
				text = HtmlEntity.DeEntitize (text);
				AppendText (text);
				break;
				
			}
		}
	
		protected void WalkNodes (HtmlNode node)
		{
			foreach (HtmlNode subnode in node.ChildNodes) {
				if (subnode.NodeType == HtmlNodeType.Element) {
					switch (subnode.Name) {
					case "html":
						WalkNodes (subnode);
						break;
					case "head":
						WalkHeadNodes (subnode);
						break;
					case "body":
					default:
						WalkBodyNodes (subnode);
						break;
					}
				}
			}
		}

		override protected void DoOpen (FileInfo info)
		{
			HtmlDocument doc = new HtmlDocument ();
	
			try {
				doc.Load (Stream);
			} catch (NotSupportedException e) {
				doc.Load (Stream, Encoding.ASCII);
			}

			if (doc != null)
				WalkNodes (doc.DocumentNode);
			Finished ();
		}


		virtual protected void RegisterSupportedTypes () 
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/html"));
		}
	}
	
}
