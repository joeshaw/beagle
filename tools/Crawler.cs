
using System;
using System.Collections;
using System.IO;

using Dewey;

class IndexFilesTool {

    static void IndexOrCrawl(String path, ArrayList array) {

	if (Path.GetFileName(path).StartsWith("."))
	    return;
	
	FileAttributes attr = System.IO.File.GetAttributes(path);
	if (attr == FileAttributes.Directory) {
	    Console.WriteLine("Crawling " + path);
	    DirectoryInfo dir = new DirectoryInfo(path);
	    foreach (FileInfo info in dir.GetFiles()) {
		IndexOrCrawl(Path.Combine(path, info.Name), array);
	    }
	    foreach (DirectoryInfo info in dir.GetDirectories()) {
		IndexOrCrawl(Path.Combine(path, info.Name), array);
	    }
	} else {
	    if (Path.GetExtension(path) == ".txt") {
		IndexItemWithPayload item;
		item = new IndexItemWithPayload("file://" + path);
		item.AttachFile(path);
		array.Add(item);
	    }
	}
    }

    static void Main(String[] args) {

	Content.Register(typeof(ContentText));

	ArrayList array = new ArrayList();

	if (args.Length > 0) {
	    foreach (String arg in args) {
		IndexOrCrawl(Path.GetFullPath(arg), array);
	    }
	} else {
	    IndexOrCrawl(Environment.GetEnvironmentVariable("HOME"), array);
	}

	IndexDriver id = new IndexDriver();
	id.Add(array);

    }

}
