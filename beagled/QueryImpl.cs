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
using System.Text;
using System.Xml;

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public class QueryImpl : Beagle.QueryProxy {
		private QueryDriver driver;
		private QueryBody body;
		private QueryResult result = null;
		
		public override event StartedHandler StartedEvent;
		public override event HitsAddedAsXmlHandler HitsAddedAsXmlEvent;
		public override event HitsSubtractedAsStringHandler HitsSubtractedAsStringEvent;
		public override event CancelledHandler CancelledEvent;
		
		public delegate void ClosedHandler (QueryImpl sender);
		public event ClosedHandler ClosedEvent;

		public QueryImpl (QueryDriver _driver)
		{
			driver = _driver;

			body = new QueryBody ();
		}

		private void DisconnectResult ()
		{
			if (result != null) {
				result.HitsAddedEvent -= OnHitsAddedToResult;
				result.HitsSubtractedEvent -= OnHitsSubtractedFromResult;
				result.CancelledEvent -= OnCancelledResult;

				result.Cancel ();

				result = null;
			}
		}

		private void AttachResult ()
		{
			DisconnectResult ();

			result = new QueryResult ();

			result.HitsAddedEvent += OnHitsAddedToResult;
			result.HitsSubtractedEvent += OnHitsSubtractedFromResult;
			result.CancelledEvent += OnCancelledResult;
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
			
			if (StartedEvent != null)
				StartedEvent (this);

			AttachResult ();

			driver.DoQuery (body, result);
			driver.ChangedEvent += OnQueryDriverChanged;
		}

		public override void Cancel ()
		{
			driver.ChangedEvent -= OnQueryDriverChanged;
			DisconnectResult ();
		}

		public override void CloseQuery () 
		{
			driver.ChangedEvent -= OnQueryDriverChanged;
			DisconnectResult ();
			if (ClosedEvent != null)
				ClosedEvent (this);
		}

		//////////////////////////////////////////////////////

		//
		// QueryResult event handlers
		//

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
		
		private void OnHitsAddedToResult (QueryResult source, ICollection someHits)
		{
			if (source != result)
				return;

			if (HitsAddedAsXmlEvent != null)
				HitsAddedAsXmlEvent (this, HitsToXml (someHits));
		}

		private string UrisToString (ICollection uris)
		{
			StringBuilder builder = null;
			foreach (Uri uri in uris) {
				if (builder == null)
					builder = new StringBuilder ("");
				else
					builder.Append (" ");
				builder.Append (uri.ToString ());
			}
			return builder  != null ? builder.ToString () : "";
		}

		private void OnHitsSubtractedFromResult (QueryResult source, ICollection someUris)
		{
			if (source != result)
				return;

			if (HitsSubtractedAsStringEvent != null)
				HitsSubtractedAsStringEvent (this, UrisToString (someUris));
		}

		private void OnCancelledResult (QueryResult source) 
		{
			if (source != result) 
				return;

			System.Console.WriteLine ("Cancelled");
			if (CancelledEvent != null)
				CancelledEvent (this);
		}

		//////////////////////////////////////////////////////

		//
		// QueryDriver.ChangedEvent handling
		//

		private void OnQueryDriverChanged (QueryDriver source, IQueryable queryable, IQueryableChangeData changeData)
		{
			if (result != null)
				source.DoQueryChange (queryable, changeData, body, result);
		}
	}
}
