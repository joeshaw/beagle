//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.Threading;

using Gtk;

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
		Gtk.Application.Init ();

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
