//
// XmlContentReader.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace Dewey {

    public class XmlContentReader : XmlTextReader {

	struct NodeProperties {
	    public bool isHot;
	    public bool isCold;
	}
	Stack nodePropertyStack = new Stack ();

	public bool Debug = false;

	int hotCount = 0;
	int coldCount = 0;
	int depth = 0;

	Content content;

	protected virtual bool ThisElementIsHot () {
	    return false;
	}

	protected virtual bool ThisElementIsCold () {
	    return false;
	}

	protected virtual bool ThisEndElementWillFlush () {
	    return false;
	}

	private void PushNode () {
	    NodeProperties prop = new NodeProperties ();

	    if (ThisElementIsHot ()) {
		++hotCount;
		prop.isHot = true;
	    }

	    if (ThisElementIsCold ()) {
		++coldCount;
		prop.isCold = true;
	    }
	    ++depth;
	    
	    nodePropertyStack.Push (prop);
	}

	private void PopNode () {
	    NodeProperties prop;
	    prop = (NodeProperties ) nodePropertyStack.Pop ();
	    
	    if (prop.isHot)
		--hotCount;
	    if (prop.isCold)
		--coldCount;
	    --depth;
	}

	public XmlContentReader (Stream stream, Content c) 
	    : base (stream) {
	    content = c;
	}

	private void Spew (String str) {
	    if (Debug) {
		for (int i = 0; i < depth; ++i)
		    Console.Write (" ");
		Console.WriteLine (str);
	    }
	}

	private void AddChunk (String str) {
	    if (coldCount == 0) {
		content.AppendBody (str);
		if (hotCount > 0)
		    content.AppendHotBody (str);
	    }
	}

	private void FlushChunk () {
	    content.AppendBody (" ");
	    content.AppendHotBody (" ");
	}

	public void DoWork () {

	    while (Read ()) {
		switch (NodeType) {
		    
		    case XmlNodeType.Element:
			Spew ("<"+Name+">");
			PushNode ();
			break;

		    case XmlNodeType.EndElement:
			Spew ("</"+Name+">");
			if (ThisEndElementWillFlush ())
			    FlushChunk ();
			break;

		    case XmlNodeType.Text:
			Spew ("\""+Value+"\"");
			AddChunk (Value);
			break;

		    case XmlNodeType.Whitespace:
			Spew ("*Whitespace*");
			FlushChunk ();
			break;

		    case XmlNodeType.SignificantWhitespace:
			Spew ("*SignificantWhitespace*");
			FlushChunk ();
			break;
		}
	    }

	}
    }

}
