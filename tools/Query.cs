
using System;

using Dewey;

class QueryTool {

    static void Main(String[] args) {

	IndexDriver id = new IndexDriver();

	String query = String.Join (" ", args);
	
	IndexItem[] hits = id.QueryBody(query);

	foreach (IndexItem item in hits) {
	    Console.WriteLine(item.URI);
	}

	Console.WriteLine("Total hits: " + hits.Length);

    }

}
