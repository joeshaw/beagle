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
using BU = Beagle.Util;

class QueryTool {

	static int count = 0;

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

	static void OnGotHits (string results)
	{
		foreach (Hit hit in Hit.ReadHitXml (results)) {
			WriteHit (hit);
			++count;
		}
	}

	static void QuitWithCount (string type)
	{
		Console.WriteHit ("{0} after {1} hit{2}.",
				  type,
				  count,
				  count != 1 ? "s" : "");
	}

	static void OnFinished ()
	{
		Console.WriteLine ("Finished after {0} hits.", count);
		Gtk.Application.Quit ();
	}

	static void OnCancelled ()
	{
		Console.WriteLine ("Cancelled after {0} hits.", count);
		Gtk.Application.Quit ();
	}

	static void Main (string[] args) 
	{
		Gtk.Application.Init ();

		Query query = Query.New ();
		query.GotHitsEvent += OnGotHits;
		query.FinishedEvent += OnFinished;
		query.CancelledEvent += OnCancelled;

		int i = 0;
		while (i < args.Length) {
			if (args [i].StartsWith ("mimetype:")) {
				string mt = args [i].Substring ("mimetype:".Length);
				query.AddMimeType (mt);
			} else {
				query.AddTextRaw (args [i]);
			}
			++i;
		}

		query.Start ();

		Gtk.Application.Run ();
	}


}
