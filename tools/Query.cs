//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

using Dewey;

class QueryTool {

	static void Main (String[] args) 
	{
		IndexDriver driver = new IndexDriver ();

		Query query = new Query (String.Join (" ", args));

		IEnumerable hits = driver.Query (query);

		int count = 0;
		foreach (Hit hit in hits) {
			Console.WriteLine (hit.Uri);
			++count;
		}

		Console.WriteLine ("Total hits: {0}", count);
	}
}
