//
// ExtractContent.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;

using Dewey;

class ExtractContentTool {

    static void Main (String[] args) {
	
	Content.RegisterEverythingByHand (); // FIXME: this still sucks

	foreach (String arg in args) {

	    IndexItemWithPayload item;
	    item = new IndexItemWithPayload ("file://" + arg);

	    Console.WriteLine ("");
	    Console.WriteLine ("     URI: " + item.URI);

	    if (item.AttachFile (arg)) {
		Stream stream = item.OpenPayloadStream ();
		Content c = Content.Extract (item.MimeType, stream);




		Console.WriteLine ("MimeType: " + item.MimeType);

		Console.WriteLine ("-----");
		
		if (c.Body == null) {
		    Console.WriteLine ("Body is empty");
		} else {
		    Console.WriteLine ("Body:");
		    Console.WriteLine (c.Body);
		}

		Console.WriteLine ("-----");

		if (c.HotBody == null || c.HotBody.Length == 0) {
		    Console.WriteLine ("HotBody is empty");
		} else {
		    Console.WriteLine ("HotBody:");
		    Console.WriteLine (c.HotBody);
		}

	    } else {
		Console.WriteLine ("MimeType: (unknown)");
	    }

	    Console.WriteLine ("");
		
		    
	    
	}
    }

}
