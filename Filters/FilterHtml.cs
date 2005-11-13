//
// FilterHtml.cs
//
// Copyright (C) 2005 Debajyoti Bera <dbera.web@gmail.com>
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
		// When see <b> push "b" in the stack
		// When see </b> pop from the stack
		// For good error checking, we should compare
		// current element with what was popped
		// Currently, we just pop, this might allow
		// unmatched elements to pass through
		private Stack hot_stack;
		private Stack ignore_stack;
		private bool building_text;
		private StringBuilder builder;

		public FilterHtml ()
		{
			RegisterSupportedTypes ();
			SnippetMode = true;
			hot_stack = new Stack ();
			ignore_stack = new Stack ();
			building_text = false;
			builder = new StringBuilder ();
		}

		// Safeguard against spurious stack pop ups...
		// caused by mismatched tags in bad html files
		// FIXME: If matching elements is not required
		// and if HtmlAgilityPack matches elements itself,
		// then we can just use a counter hot_stack_depth
		// instead of the hot_stack
		private void SafePop (Stack st)
		{
			if (st != null && st.Count != 0)
				st.Pop ();
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

		protected void HandleNodeEvent (HtmlNode node)
		{
			switch (node.NodeType) {
				
			case HtmlNodeType.Document:
			case HtmlNodeType.Element:
				if (node.Name == "title") {
					if (node.StartTag) {
						builder.Length = 0;
						building_text = true;
					} else {
						String title = HtmlEntity.DeEntitize (builder.ToString ().Trim ());
						AddProperty (Beagle.Property.New ("dc:title", title));
						builder.Length = 0;
						building_text = false;
					}
				} else if (node.Name == "meta") {
	   				string name = node.GetAttributeValue ("name", "");
           				string content = node.GetAttributeValue ("content", "");
					if (name != "" && content != "")
						AddProperty (Beagle.Property.New (name, content));
				} else if (! NodeIsContentFree (node.Name)) {
					bool isHot = NodeIsHot (node.Name);
					bool breaksText = NodeBreaksText (node.Name);
					bool breaksStructure = NodeBreaksStructure (node.Name);
					if (isHot && node.StartTag) {
						if (hot_stack.Count == 0)
							HotUp ();
						hot_stack.Push (node.Name);
					}
					if (breaksText && node.StartTag)
						AppendWhiteSpace ();
					if (node.Name == "img" && node.StartTag) {
						string attr = node.GetAttributeValue ("alt", "");
						if (attr != "") {
							AppendText (attr);
						}
					}
					if (node.Name == "a" && node.StartTag) {
						string attr = node.GetAttributeValue ("href", "");
						if (attr != "") {
							AppendText (attr);
						}
						//Console.WriteLine ("Start AAAAAAAAAAAA = " + node.GetAttributeValue ("href", ""));
					}
					//if (node.Name == "a" && !node.StartTag)
					//	Console.WriteLine ("Done AAAAAAAAAAAAA");
					if (breaksText && !node.StartTag)
						AppendWhiteSpace ();
					if (breaksStructure && !node.StartTag)
						AppendStructuralBreak ();
					if (isHot && !node.StartTag) {
						if (hot_stack.Count != 0)
							SafePop (hot_stack);
						if (hot_stack.Count == 0)
							HotDown ();
					}	
				} else {
					// so node is a content-free node
					// ignore contents of such node
					if (node.StartTag)
						ignore_stack.Push (node.Name);
					else
						SafePop (ignore_stack);
				}
				break;
				
			case HtmlNodeType.Text:
				// FIXME Do we need to trim the text ?
				String text = ((HtmlTextNode)node).Text;
				if (ignore_stack.Count != 0)
					break; // still ignoring ...
				if (building_text)
					builder.Append (text);
				else
					AppendText (HtmlEntity.DeEntitize (text));
				//if (hot_stack.Count != 0)
				//Console.WriteLine (" TEXT:" + text + " ignore=" + ignore_stack.Count);
				break;
			}
		}

		override protected void DoOpen (FileInfo info)
		{
			// we need to tell the parser to detect encoding,
			HtmlDocument temp_doc = new HtmlDocument ();
			Encoding enc = temp_doc.DetectEncoding (Stream);
			//Console.WriteLine ("Detected encoding:" + (enc == null ? "null" : enc.EncodingName));
			temp_doc = null;
			Stream.Seek (0, SeekOrigin.Begin);

			HtmlDocument doc = new HtmlDocument ();
			doc.ReportNode += HandleNodeEvent;
			doc.StreamMode = true;
			// we already determined encoding
			doc.OptionReadEncoding = false;
	
			try {
				if (enc == null)
					doc.Load (Stream);
				else
					doc.Load (Stream, enc);
			} catch (NotSupportedException e) {
				doc.Load (Stream, Encoding.ASCII);
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				Console.WriteLine (e.StackTrace);
			}

			Finished ();
		}


		virtual protected void RegisterSupportedTypes () 
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/html"));
		}
	}

}
