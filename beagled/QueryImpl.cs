//
// QueryImpl.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;
using System.Xml;

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public class QueryImpl : Beagle.QueryProxy {
		private QueryBody body;
		private QueryDriver driver;
		private QueryResult result;

		public override event GotHitsXmlHandler GotHitsXmlEvent;
		public override event FinishedHandler FinishedEvent;
		public override event CancelledHandler CancelledEvent;

		public QueryImpl (QueryDriver _driver)
		{
			body = new QueryBody ();
			driver = _driver;
		}

		public override void AddText (string text)
		{
			body.AddText (text);
		}

		public override void AddTextRaw (string text)
		{
			body.AddTextRaw (text);
		}

		public override void AddDomain (Beagle.QueryDomain d)
		{
			body.AddDomain (d);
		}

		public override void RemoveDomain (Beagle.QueryDomain d)
		{
			body.RemoveDomain (d);
		}

		public override void AddMimeType (string type)
		{
			body.AddMimeType (type);
		}

		public override void Start ()
		{
			System.Console.WriteLine ("starting query");
			
			if (result != null) {
				return;
			}
			
			result = driver.DoQuery (body);

			result.GotHitsEvent += OnGotHitsXml;
			result.FinishedEvent += OnFinished;
			result.CancelledEvent += OnCancelled;

			result.Start ();
		}

		private void Cancel (bool disconnectHandlers)
		{
			if (disconnectHandlers) {
			    result.FinishedEvent -= OnFinished;
			    result.CancelledEvent -= OnCancelled;
			    result.GotHitsEvent -= OnGotHitsXml;
			}

			if (!result.Finished) {
				result.Cancel ();
			}
		} 

		public override void Cancel ()
		{
			Cancel (false);
		}

		public override void CloseQuery () 
		{
			Cancel (true);

			System.Console.WriteLine ("Closing Query");
			DBusisms.Factory.UnregisterObject (this);
		}

		private string HitsToXml (ICollection hits)
		{
			StringWriter stringWriter = new StringWriter ();
			XmlTextWriter writer = new XmlTextWriter (stringWriter);

			writer.WriteStartElement ("hits");
			
			foreach (Hit hit in hits) {
				hit.WriteToXml (writer);
			}

			writer.WriteEndElement ();

			writer.Close ();
			stringWriter.Close ();

			return stringWriter.ToString ();
		}
		

		private void OnGotHitsXml (QueryResult src,
					   QueryResult.GotHitsArgs args)
		{
			System.Console.WriteLine ("Got {0} Hits", args.Count);
			if (src != result)
				return;

			string hits = HitsToXml (args.Hits);
			GotHitsXmlEvent (hits);
		}

		private void OnFinished (QueryResult src) 
		{
			if (src != result)
				return;
			System.Console.WriteLine ("Finished");
			FinishedEvent ();
		}

		private void OnCancelled (QueryResult src) 
		{
			if (src != result) 
				return;

			System.Console.WriteLine ("Cancelled");
			CancelledEvent ();
		}
	}
}
