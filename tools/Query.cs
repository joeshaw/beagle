//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

using Dewey;

class QueryTool {

	static void WriteHit (Hit hit)
	{
		Console.WriteLine ("  Uri: {0}", hit.Uri);
		Console.WriteLine (" Type: {0}", hit.Type);
		Console.WriteLine ("MimeT: {0}", hit.MimeType == null ? "(null)" : hit.MimeType);
		Console.WriteLine ("  Src: {0}", hit.Source);
		Console.WriteLine ("Score: {0}", hit.Score);
		if (hit.ValidTimestamp)
			Console.WriteLine (" Time: {0}", hit.Timestamp);
		if (hit.ValidRevision)
			Console.WriteLine ("  Rev: {0}", hit.Revision);

		foreach (String key in hit.Keys)
			Console.WriteLine ("    {0} = {1}", key, hit [key]);

		Console.WriteLine ();
	}

	static void Main (String[] args) 
	{
		IndexDriver driver = new IndexDriver ();

		Query query = new Query (String.Join (" ", args));

		IEnumerable hits = driver.Query (query);

		int count = 0;
		foreach (Hit hit in hits) {
			WriteHit (hit);
			++count;
		}

		Console.WriteLine ("Total hits: {0}", count);
	}
}
