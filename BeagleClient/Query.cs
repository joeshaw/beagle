//
// Query.cs
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
using DBus;
using Beagle.Util;

namespace Beagle
{

	public abstract class Query : QueryProxy, IDisposable {

		public delegate void HitsAddedHandler (Query source, ICollection hits);
		public virtual event HitsAddedHandler HitsAddedEvent;

		public delegate void HitsSubtractedHandler (Query source, ICollection uris);
		public virtual event HitsSubtractedHandler HitsSubtractedEvent;

		private bool cancelled = false;

		private class HitInfo {
			public Hit Hit = null;
			public int RefCount = 0;
		}

		public Query ()
		{
			HitsAddedAsBinaryEvent += OnHitsAddedAsBinary;
			HitsSubtractedAsStringEvent += OnHitsSubtractedAsString;
			CancelledEvent += OnCancelled;
		}

		public string [] Text {
			get {
				string str = GetTextBlob ();
				if (str == null || str == "")
					return null;
				return str.Split ('|'); // FIXME: hacky and stupid
			}
		}

		public bool IsCancelled {
			get { return cancelled; }
		}

		public void Dispose ()
		{
			try {
				CloseQuery ();
			}
			catch (Exception e) { }
			
			GC.SuppressFinalize (this);
		}

		~Query ()
		{
			try {
				CloseQuery ();
			}
			catch (Exception e) { }
		}

		public string GetSnippet (Uri uri)
		{
			string snippet;
			snippet = GetSnippetFromUriString (uri.ToString ());
			if (snippet == "")
				snippet = null;
			return snippet;
		}

		private void OnHitsAddedAsBinary (QueryProxy sender, string hitsData)
		{
			byte[] binaryData = Convert.FromBase64String (hitsData);
			MemoryStream memStream = new MemoryStream (binaryData, false);
			BinaryReader reader = new BinaryReader (memStream);

			int numHits = reader.ReadInt32 ();

			//Console.WriteLine ("Got {0} hits", numHits);

			ArrayList hits = new ArrayList ();
			for (int i = 0; i < numHits; i++) {
				Hit hit = Hit.ReadAsBinary (reader);
				hits.Add (hit);
			}

			reader.Close ();

			if (HitsAddedEvent != null && hits.Count > 0)
				HitsAddedEvent (this, hits);
		}

		private void OnHitsSubtractedAsString (QueryProxy sender, string uriString)
		{
			if (HitsSubtractedEvent != null && uriString.Length > 0) {
				string[] uris = uriString.Split ('|');

				ArrayList uriList = new ArrayList ();

				foreach (string uriStr in uris) {
					Uri uri = new Uri (uriStr, true);
					uriList.Add (uri);
				}

				HitsSubtractedEvent (this, uriList);
			}
		}

		private void OnCancelled (QueryProxy sender)
		{
			cancelled = true;
		}
	}
}
