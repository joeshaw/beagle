//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Threading;

using Beagle;

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
		QueryDriver driver = new QueryDriver ();
		driver.AutoPopulateHack ();

		Query query = new Query (String.Join (" ", args));

		QueryResult result = driver.Query (query);
		result.Start ();

		Thread.Sleep (1000);
		result.Wait ();

		foreach (Hit hit in result.Hits) {
			WriteHit (hit);
		}

		Console.WriteLine ("Total hits: {0}", result.Count);

		// FIXME: Works around mono dangling thread bug.
		Environment.Exit (0);
	}


}
