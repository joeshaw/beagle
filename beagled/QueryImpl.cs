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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public class QueryImpl : Beagle.QueryProxy, IDisposable {
		private QueryDriver driver;
		private QueryBody body;
		private QueryResult result = null;
		private string id;

		private Hashtable allHits = new Hashtable ();
		
		public override event StartedHandler StartedEvent;
		public override event HitsAddedAsXmlHandler HitsAddedAsXmlEvent;
		public override event HitsSubtractedAsStringHandler HitsSubtractedAsStringEvent;
		public override event CancelledHandler CancelledEvent;
		public override event FinishedHandler FinishedEvent;
		
		public delegate void ClosedHandler (QueryImpl sender);
		public event ClosedHandler ClosedEvent;

		public QueryImpl (QueryDriver _driver,
				  string id)
		{
			this.id = id;

			driver = _driver;
			driver.ChangedEvent += OnQueryDriverChanged;

			body = new QueryBody ();
		}

		private void DisconnectResult ()
		{
			if (result != null) {
				result.HitsAddedEvent -= OnHitsAddedToResult;
				result.HitsSubtractedEvent -= OnHitsSubtractedFromResult;
				result.FinishedEvent -= OnFinishedResult;
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
			result.FinishedEvent += OnFinishedResult;
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

		public override string GetTextBlob ()
		{
			StringBuilder builder = new StringBuilder ();
			foreach (string str in body.Text) {
				if (builder.Length > 0)
					builder.Append ("|"); // FIXME: hacky and stupid
				builder.Append (str);
			}
			return builder.ToString ();
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

		public override void AddSource (string source)
		{
			body.AddSource (source);
		}

		public override void Start ()
		{
			if (StartedEvent != null)
				StartedEvent (this);

			AttachResult ();

			driver.DoQuery (body, result);
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

		public void Dispose ()
		{
			DisconnectResult ();
			driver.ChangedEvent -= OnQueryDriverChanged;
			GC.SuppressFinalize (this);
		}

		~QueryImpl ()
		{
			DisconnectResult ();
			driver.ChangedEvent -= OnQueryDriverChanged;
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

			ArrayList toSubtract = new ArrayList ();
			
			foreach (Hit hit in someHits) {
				// If necessary, synthesize a subtracted event
				if (allHits.Contains (hit.Uri))
					toSubtract.Add (hit.Uri);
				
				allHits[hit.Uri] = hit;
			}
			
			if (HitsSubtractedAsStringEvent != null && toSubtract.Count > 0)
				HitsSubtractedAsStringEvent (this, UrisToString (toSubtract));
			if (HitsAddedAsXmlEvent != null && someHits.Count > 0)
				HitsAddedAsXmlEvent (this, HitsToXml (someHits));
		}

		private void OnFinishedResult (QueryResult source) 
		{
			if (source != result)
				return;

			if (FinishedEvent != null) 
				FinishedEvent (this);
		}

		private string UrisToString (ICollection uris)
		{
			StringBuilder builder = null;
			foreach (Uri uri in uris) {
				if (builder == null)
					builder = new StringBuilder ("");
				else
					builder.Append ("|");
				builder.Append (uri.ToString ());
			}
			return builder  != null ? builder.ToString () : "";
		}

		private void OnHitsSubtractedFromResult (QueryResult source, ICollection someUris)
		{
			if (source != result)
				return;

			ArrayList toSubtract = new ArrayList ();
			foreach (Uri uri in someUris) {
				// Only subtract previously-added Uris
				if (allHits.Contains (uri)) {
					toSubtract.Add (uri);
					allHits.Remove (uri);
				}
			}
			if (HitsSubtractedAsStringEvent != null && toSubtract.Count > 0)
				HitsSubtractedAsStringEvent (this, UrisToString (toSubtract));			
		}

		private void OnCancelledResult (QueryResult source) 
		{
			if (source != result) 
				return;

			if (CancelledEvent != null)
				CancelledEvent (this);
		}

		//////////////////////////////////////////////////////

		//
		// QueryDriver.ChangedEvent handling
		//

		private void OnQueryDriverChanged (QueryDriver source, Queryable queryable, IQueryableChangeData changeData)
		{
			if (result != null && queryable.AcceptQuery (body))
				queryable.DoQuery (body, result, changeData);
		}
	}
}
