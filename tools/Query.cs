
using System;

using Dewey;

class QueryTool {

    static void Main (String[] args) {

	IndexDriver id = new IndexDriver ();

	String query_str = String.Join (" ", args);
	
	Query query = new Query (query_str);

	IndexItem[] hits = id.Query (query);

	foreach (IndexItem item in hits) {
	    Console.WriteLine (item.URI);
	}

	Console.WriteLine ("Total hits: " + hits.Length);

    }

}
