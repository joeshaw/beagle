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

	    try {
		Indexable indexable = new IndexableFile (path);
		array.Add (indexable);
	    } catch {
		// If we get an exception, it means that we couldn't
		// filter the file.  In this case, just do nothing.
	    }
	}
    }

    static void Main (String[] args) {

	ArrayList array = new ArrayList ();

	if (args.Length > 0) {
	    foreach (String arg in args) {
		IndexOrCrawl (Path.GetFullPath (arg), array);
	    }
	} else {
	    IndexOrCrawl (Environment.GetEnvironmentVariable ("HOME"), array);
	}

	IndexDriver driver = new IndexDriver ();
	driver.Add (array);

    }

}
