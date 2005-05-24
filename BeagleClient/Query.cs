//
// Query.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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
using System.IO;
using System.Collections;
using System.Text;
using System.Xml.Serialization;

using BU = Beagle.Util;

namespace Beagle {

	public enum QueryDomain {
		Local        = 1,
		Neighborhood = 2,
		Global       = 4
	}

	public class Query : RequestMessage {

		// FIXME: This is a good default when on an airplane.
		private Beagle.QueryDomain domainFlags = Beagle.QueryDomain.Local; 

		private ArrayList parts = new ArrayList ();
		private ArrayList mimeTypes = new ArrayList ();
		private ArrayList hitTypes = new ArrayList ();
		private ArrayList searchSources = new ArrayList ();

		// Events to make things nicer to clients
		public delegate void HitsAdded (HitsAddedResponse response);
		public event HitsAdded HitsAddedEvent;

		public delegate void HitsSubtracted (HitsSubtractedResponse response);
		public event HitsSubtracted HitsSubtractedEvent;

		public delegate void Finished (FinishedResponse response);
		public event Finished FinishedEvent;

		public delegate void Cancelled (CancelledResponse response);
		public event Cancelled CancelledEvent;


		public Query () : base (true)
		{
			this.RegisterAsyncResponseHandler (typeof (HitsAddedResponse), OnHitsAdded);
			this.RegisterAsyncResponseHandler (typeof (HitsSubtractedResponse), OnHitsSubtracted);
			this.RegisterAsyncResponseHandler (typeof (FinishedResponse), OnFinished);
			this.RegisterAsyncResponseHandler (typeof (CancelledResponse), OnCancelled);
			this.RegisterAsyncResponseHandler (typeof (ErrorResponse), OnError);
		}

		public Query (string str) : this ()
		{
			AddText (str);
		}

		///////////////////////////////////////////////////////////////

		private void OnHitsAdded (ResponseMessage r)
		{
			HitsAddedResponse response = (HitsAddedResponse) r;

			if (this.HitsAddedEvent != null)
				this.HitsAddedEvent (response);
		}

		private void OnHitsSubtracted (ResponseMessage r)
		{
			HitsSubtractedResponse response = (HitsSubtractedResponse) r;

			if (this.HitsSubtractedEvent != null)
				this.HitsSubtractedEvent (response);
		}

		private void OnFinished (ResponseMessage r)
		{
			FinishedResponse response = (FinishedResponse) r;

			if (this.FinishedEvent != null)
				this.FinishedEvent (response);
		}

		private void OnCancelled (ResponseMessage r)
		{
			CancelledResponse response = (CancelledResponse) r;

			if (this.CancelledEvent != null)
				this.CancelledEvent (response);
		}

		// FIXME: Useful?
		private void OnError (ResponseMessage r)
		{
			ErrorResponse response = (ErrorResponse) r;

			throw new Exception (response.Message);
		}

		///////////////////////////////////////////////////////////////

		public void AddPart (QueryPart part)
		{
			parts.Add (part);
		}

		public void AddTextRaw (string str)
		{
			QueryPart part = new QueryPart ();
			part.Target = QueryPart.TargetAll;
			part.Text = str;

			parts.Add (part);
		}

		public void AddText (string str)
		{
			foreach (string textPart in BU.StringFu.SplitQuoted (str))
				AddTextRaw (textPart);
		}

		// FIXME: Since it is possible to introduce quotes via the AddTextRaw
		// method, this function should replace " with \" when appropriate.
		// FIXME: This doesn't really do the right thing when we have property queries.
		[XmlIgnore]
		public string QuotedText {
			get { 
				StringBuilder builder = new StringBuilder ();
				
				foreach (QueryPart part in parts) {
					if (part.Text == null)
						continue;

					bool has_ws = BU.StringFu.ContainsWhiteSpace (part.Text);
					if (builder.Length > 0)
						builder.Append (' ');
					if (has_ws)
						builder.Append ('"');
					builder.Append (part.Text);
					if (has_ws)
						builder.Append ('"');
				}

				return builder.ToString ();
			}
		}

		[XmlArrayItem (ElementName="Parts",
			       Type=typeof (QueryPart))]
		[XmlArray (ElementName="Part")]
		public ArrayList Parts {
			get { return parts; }
		}

		[XmlIgnore]
		public ICollection Text {
			get {
				ArrayList text = new ArrayList ();
				foreach (QueryPart part in parts)
					if (part.Text != null)
						text.Add (part.Text);
				return text;
			}
		}

		///////////////////////////////////////////////////////////////

		public void AddMimeType (string str)
		{
			mimeTypes.Add (str);
		}

		public bool AllowsMimeType (string str)
		{
			if (mimeTypes.Count == 0)
				return true;
			foreach (string mt in mimeTypes)
				if (str == mt)
					return true;
			return false;
		}

		[XmlArrayItem (ElementName="MimeType",
			       Type=typeof (string))]
		[XmlArray (ElementName="MimeTypes")]
		public ArrayList MimeTypes {
			get { return mimeTypes; }
		}

		public bool HasMimeTypes {
			get { return mimeTypes.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public void AddHitType (string str)
		{
			hitTypes.Add (str);
		}

		public bool AllowsHitType (string str)
		{
			if (hitTypes.Count == 0)
				return true;
			foreach (string ht in hitTypes)
				if (str == ht)
					return true;
			return false;
		}

		[XmlArrayItem (ElementName="HitType",
			       Type=typeof(string))]
		[XmlArray (ElementName="HitTypes")]
		public ArrayList HitTypes {
			get { return hitTypes; }
		}

		[XmlIgnore]
		public bool HasHitTypes {
			get { return hitTypes.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public void AddSource (string str)
		{
			searchSources.Add (str);
		}
		

		public bool AllowsSource (string str)
		{
			if (searchSources.Count == 0)
				return true;
			foreach (string ss in searchSources)
				if (str.ToUpper () == ss.ToUpper ())
					return true;
			return false;
		}

		[XmlArrayItem (ElementName="Source",
			       Type=typeof (string))]
		[XmlArray (ElementName="Sources")]
		public ArrayList Sources {
			get { return searchSources; }
		}

		[XmlIgnore]
		public bool HasSources {
			get { return searchSources.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public void AddDomain (Beagle.QueryDomain d)
		{
			domainFlags |= d;
		}

		public void RemoveDomain (Beagle.QueryDomain d)
		{
			domainFlags &= ~d;
		}

		public bool AllowsDomain (Beagle.QueryDomain d)
		{
			return (domainFlags & d) != 0;
		}

		///////////////////////////////////////////////////////////////

		private int max_hits = 100;
		public int MaxHits {
			get { return max_hits; }
			set { max_hits = value; }
		}

		///////////////////////////////////////////////////////////////

		[XmlIgnore]
		public bool IsEmpty {
			get { return parts.Count == 0
				      && mimeTypes.Count == 0
				      && searchSources.Count == 0; }
		}

		///////////////////////////////////////////////////////////////

	}
}
