//
// Crawler.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;

using Dewey;

class IndexFilesTool {

    static void IndexOrCrawl (String path, ArrayList array) {

	if (Path.GetFileName (path).StartsWith ("."))
	    return;
	
	FileAttributes attr = System.IO.File.GetAttributes (path);
	if (attr == FileAttributes.Directory) {
	    Console.WriteLine ("Crawling " + path);
	    DirectoryInfo dir = new DirectoryInfo (path);
	    foreach (FileSystemInfo info in dir.GetFileSystemInfos ()) {
		IndexOrCrawl (info.FullName, array);
	    }
	} else {
	    IndexItemWithPayload item;
	    item = new IndexItemWithPayload ("file://" + path);

	    // If AttachFile returns false, we can't handle the file type
	    if (item.AttachFile (path))
		array.Add (item);
	}
    }

    static void Main (String[] args) {

	Content.RegisterEverythingByHand ();

	ArrayList array = new ArrayList ();

	if (args.Length > 0) {
	    foreach (String arg in args) {
		IndexOrCrawl (Path.GetFullPath (arg), array);
	    }
	} else {
	    IndexOrCrawl (Environment.GetEnvironmentVariable ("HOME"), array);
	}

	IndexDriver id = new IndexDriver ();
	id.Add (array);

    }

}
