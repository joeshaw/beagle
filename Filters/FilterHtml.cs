//
// FilterHtml.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Text;

using HtmlAgilityPack;

namespace Dewey.Filters {

    public class FilterHtml : Filter {

	public FilterHtml () {
	    AddSupportedMimeType ("text/html");
	}

	static bool NodeIsHot (String nodeName) {
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
		|| nodeName == "h6";
	}

	static bool NodeBreaksText (String nodeName) {
	    return nodeName == "p"
		|| nodeName == "br"
		|| nodeName == "td"
		|| nodeName == "a"
		|| nodeName == "div"
		|| nodeName == "option"
		|| nodeName == "h1"
		|| nodeName == "h2"
		|| nodeName == "h3"
		|| nodeName == "h4"
		|| nodeName == "h5"
		|| nodeName == "h6";
	}

	static bool NodeIsContentFree (String nodeName) {
	    return nodeName == "script"
		|| nodeName == "map"
		|| nodeName == "style";
	}

	String WalkChildNodesForText (HtmlNode node) {
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

	void WalkHeadNodes (HtmlNode node) {
	    foreach (HtmlNode subnode in node.ChildNodes) {
		if (subnode.NodeType == HtmlNodeType.Element
		    && subnode.Name == "title") {
		    String title = WalkChildNodesForText (subnode);
		    title = HtmlEntity.DeEntitize (title);
		    SetMetadata ("title", title);
		}
	    }
	}

	void WalkBodyNodes (HtmlNode node) {

	    switch (node.NodeType) {

		case HtmlNodeType.Document:
		case HtmlNodeType.Element:
		    if (! NodeIsContentFree (node.Name)) {
			bool isHot = NodeIsHot (node.Name);
			if (isHot)
			    HotUp ();
			foreach (HtmlNode subnode in node.ChildNodes)
			    WalkBodyNodes (subnode);
			if (NodeBreaksText (node.Name))
			    AppendWhiteSpace ();
			if (isHot)
			    HotDown ();
		    }
		    break;

		case HtmlNodeType.Text:
		    String text = ((HtmlTextNode)node).Text;
		    text = HtmlEntity.DeEntitize (text);
		    AppendContent (text);
		    break;

	    }
	}

	void WalkNodes (HtmlNode node) {
	    
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
			    WalkBodyNodes (subnode);
			    break;
		    }
		}
	    }
	}

	override protected void Read (Stream stream) {
	    HtmlDocument doc = new HtmlDocument ();
	    doc.Load (stream);
	    WalkNodes (doc.DocumentNode);
	}

    }

}
